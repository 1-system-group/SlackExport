using System.Globalization;
using Newtonsoft.Json.Linq;
using NLog;
using SlackExport.Dto;


namespace SlackExport.Common
{
    public class SlackApiAccess
    {
        public static Logger logger = LogManager.GetCurrentClassLogger();

        private static readonly string LIST_URL = "https://slack.com/api/conversations.list";
        private static readonly string HISTORY_URL = "https://slack.com/api/conversations.history";
        private static readonly string USER_INFO_URL = "https://slack.com/api/users.info";
        private static readonly string USER_INFO_LIST = "https://slack.com/api/users.list";

        // TODO:App.configに移す想定
        private static readonly string ROOT_PATH = "./work";

        private static readonly string EXPORT_NAME = "第一システム部（仮） Slack export";


        // タイムゾーンは、SlackからはUTCで取れるので、これを指定しておく
        private static DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);


        public List<ChannelDto> GetThreadId(string token)
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

        public Dictionary<string, string> GetUserInfo(String token)
        {
            var httpAccess = new HttpAccess();
            var response = httpAccess.get(USER_INFO_LIST, token);
            var userJson = JObject.Parse(response);

            var ok = userJson["ok"];
            if (!Convert.ToBoolean(ok.ToString()))
            {
                return new Dictionary<string, string>();
            }

            var userInfoDec = new Dictionary<string, string>();
            var members = userJson["members"];
            foreach (var member in members)
            {
                var id = member["id"];
                var name = member["name"];

                var profile = member["profile"];
                var displayName = profile["display_name"];
                userInfoDec.Add(id.ToString(), displayName.ToString());
            }
            return userInfoDec;
        }


        public string GetMessage(ChannelDto channelDto, DateTime startDate, DateTime endDate, string token)
        {
            CultureInfo.CurrentCulture = CultureInfo.CreateSpecificCulture("en-US");

            // UNIX時間に変換する
            var startUnixTime = (startDate.ToUniversalTime() - unixEpoch).TotalSeconds;
            var endUnixTime = (endDate.ToUniversalTime() - unixEpoch).TotalSeconds;

            // github通知でたまにJSONにパースできないパターンなどがあるみたいなので、
            // 広めにSlackAPIアクセスからtryで囲っておいて、エラーだったら諦めるようにする。
            try
            {
                var httpAccess = new HttpAccess();
                var historyUrl = HISTORY_URL + "?channel=" + channelDto.channnelId + "&limit=100" + "&latest=" + endUnixTime + "&oldest=" + startUnixTime;
                var response = httpAccess.get(historyUrl, token);
                var historyJson = JObject.Parse(response);

                if (historyJson.Count <= 0)
                {
                    logger.Info("レスポンスなし。チャンネルID：" + channelDto.channnelId + " 取得範囲：" + startDate + " - " + endDate);
                    return string.Empty;
                }

                var ok = historyJson["ok"];
                if (!Convert.ToBoolean(ok.ToString()))
                {
                    return string.Empty;
                }

                // messagesがあったらファイルに書き出す
                var message = historyJson["messages"];
                if (message.ToString() == "[]")
                {
                    return string.Empty;
                }
                return message.ToString();
            } catch (Exception ex)
            {
                logger.Warn(ex.StackTrace);
                return string.Empty;
            }
        }
    }
}
