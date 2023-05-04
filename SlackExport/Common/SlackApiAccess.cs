using System;
using System.Collections.Generic;
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
        private static readonly string USER_URL = "https://slack.com/api/users.info";

        private static DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Local);

        public Dictionary<string, List<DataDto>> Export(string token)
        {
            // チャンネル一覧を取得する
            // https://slack.com/api/conversations.list
            var channelDtoList = GetThreadId(token);

            // チャンネルごとの投稿内容を取得する
            // https://slack.com/api/conversations.history
            var dataList = GetMessage(channelDtoList, token);

            return dataList;
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

        private Dictionary<string, List<DataDto>> GetMessage(List<ChannelDto> list, string token)
        {

            var dataDic = new Dictionary<string, List<DataDto>>();
            var httpAccess = new HttpAccess();

            foreach (var channel in list)
            {
                var dataList = new List<DataDto>();

                var historyUrl = HISTORY_URL + "?channel=" + channel.channnelId + "&limit=100";

                var historyResponse = httpAccess.get(historyUrl, token);
                var historyJson = JObject.Parse(historyResponse);

                if (historyJson.Count <= 0)
                {
                    logger.Info("レスポンスなし");
                    break;
                }

                var message = historyJson["messages"];
                var child = message.Children();
                foreach (var value in child)
                {
                    var data = new DataDto();

                    data.thread = channel.channnelName;
                    var ts = value["ts"].ToString();
                    // tsは"1234567890.123456"のような形式で渡されてくるが、
                    // 年月日時分秒まであればいい（加えてミリ秒以下は扱いが面倒な）ので、小数点以下は除外する。
                    double doubleTs = 0;
                    double.TryParse(ts, out doubleTs);
                    data.ts = unixEpoch.AddMilliseconds(doubleTs).ToLocalTime().ToString();

                    // ユーザIDからユーザ名を取得する
                    var userUrl = USER_URL + "?user=" + value["user"].ToString();
                    var userResponse = httpAccess.get(userUrl, token);
                    var userJson = JObject.Parse(userResponse);
                    var user = userJson["user"];
                    var name = user["real_name"].ToString();

                    data.message = value["text"].ToString();
                    data.user = name;
                    dataList.Add(data);
                }
                dataDic.Add(channel.channnelId, dataList);
            }
            return dataDic;
        }
    }
}
