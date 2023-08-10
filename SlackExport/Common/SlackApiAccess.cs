using System;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Globalization;
using Newtonsoft.Json.Linq;
using NLog;
using SlackExport.Dto;
using System.IO.Compression;


namespace SlackExport.Common
{
    public class SlackApiAccess
    {
        public static Logger logger = LogManager.GetCurrentClassLogger();

        private static readonly string LIST_URL = "https://slack.com/api/conversations.list";
        private static readonly string HISTORY_URL = "https://slack.com/api/conversations.history";
        private static readonly string USER_URL = "https://slack.com/api/users.info";

        // TODO:App.configに移す想定
        private static readonly string ROOT_PATH = "./work";

        private static readonly string EXPORT_NAME = "第一システム部（仮） Slack export";


        // タイムゾーンは、SlackからはUTCで取れるので、これを指定しておく
        private static DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        public void Export(string token)
        {
            // チャンネル一覧を取得する
            // https://slack.com/api/conversations.list
            var channelDtoList = GetThreadId(token);

            // チャンネルごとの投稿内容を取得する
            // https://slack.com/api/conversations.history
            GetMessage(channelDtoList, token);
        }

        private List<ChannelDto> GetThreadId(string token)
        {
            var channelDtoList = new List<ChannelDto>();
            var httpAccess = new HttpAccess();
            var response = httpAccess.get(LIST_URL, token);

            var jsonBody = JObject.Parse(response);
            if (jsonBody.Count <= 0)
            {
                logger.Info("レスポンスなし");
                return channelDtoList;
            }

            var child = jsonBody["channels"].Children();
            foreach (var value in child)
            {
                var channelDto = new ChannelDto();
                channelDto.channnelId = value["id"].ToString();
                channelDto.channnelName = value["name"].ToString();
                channelDtoList.Add(channelDto);
            }
            return channelDtoList;
        }


        private void GetMessage(List<ChannelDto> list, string token)
        {
            CultureInfo.CurrentCulture = CultureInfo.CreateSpecificCulture("en-US");
            var startDate = DateTime.MaxValue;
            var endDate = DateTime.MinValue;

            var httpAccess = new HttpAccess();

            foreach (var channel in list)
            {

                // 「tmp」フォルダを作成して、その中にチャンネルごとのフォルダを作る
                Directory.CreateDirectory(ROOT_PATH + Path.DirectorySeparatorChar + "tmp" + Path.DirectorySeparatorChar + channel.channnelName);

                // 〇日前まで取得対象にする
                int daysAgo = int.Parse(ConfigurationManager.AppSettings["daysAgo"]);
                var today = DateTime.Now;

                // 本日からdaysAgoの日数分、1日分ずつ遡って取得していく
                for (int i = 0; i <= daysAgo; i++)
                {
                    var targetDay = today.AddDays(i * -1);

                    // 指定日の0時から23時59分59秒までを取得対象にする
                    var startDate2 = DateTime.Parse(targetDay.ToString("yyyy/MM/dd") + " 00:00:00");
                    var endDate2 = DateTime.Parse(targetDay.ToString("yyyy/MM/dd") + " 23:59:59");
                    // UNIX時間に変換する
                    var startUnixTime = (startDate2.ToUniversalTime() - unixEpoch).TotalSeconds;
                    var endUnixTime = (endDate2.ToUniversalTime() - unixEpoch).TotalSeconds;

                    var historyUrl = HISTORY_URL + "?channel=" + channel.channnelId + "&limit=100" + "&latest=" + endUnixTime + "&oldest=" + startUnixTime;
                    var historyResponse = httpAccess.get(historyUrl, token);
                    var historyJson = JObject.Parse(historyResponse);

                    if (historyJson.Count <= 0)
                    {
                        logger.Info("レスポンスなし");
                        break;
                    }

                    // messagesがあったらファイルに書き出す
                    var message = historyJson["messages"];
                    if ((message != null) &&
                        (message.ToString() != null) &&
                        (message.ToString() != "[]"))
                    {
                        File.AppendAllText(ROOT_PATH + Path.DirectorySeparatorChar + "tmp" + Path.DirectorySeparatorChar + channel.channnelName + Path.DirectorySeparatorChar + targetDay.ToString("yyyy-MM-dd") + ".json", message.ToString());

                        // フォルダ名に取得した投稿の開始日と終了日を入れるため、
                        // 取得対象範囲日の中から、
                        // 実際に投稿を取得できた日時の開始日と終了日を覚えておく
                        if (startDate.CompareTo(targetDay) > 0)
                        {
                            startDate = targetDay;
                        }
                        if (endDate.CompareTo(targetDay) < 0)
                        {
                            endDate = targetDay;
                        }
                    }
                }
            }
            var startDateStr = startDate.ToString("MMM dd yyyy");
            var endDateStr = endDate.ToString("MMM dd yyyy");

            // 「tmp」フォルダを「第一システム部（仮） Slack export MMM dd yyyy - MMM dd yyyy」の形式のフォルダにリネームする
            string oldDir = ROOT_PATH + Path.DirectorySeparatorChar + "tmp";
            string newDir = ROOT_PATH + Path.DirectorySeparatorChar + EXPORT_NAME + " " + startDateStr + " - " + endDateStr;
            Directory.Move(oldDir, newDir);
            // Zip圧縮する
            ZipFile.CreateFromDirectory(newDir, newDir + ".zip");
            // フォルダは消してZipファイルだけ残す
            Directory.Delete(newDir, true);
        }
    }
}
