using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using SlackExport.Dto;
using SlackExport.Http;

namespace SlackExport.Export
{
    public class ExportService
    {
        private static readonly string LIST_URL = "https://slack.com/api/conversations.list";
        private static readonly string HISTORY_URL = "https://slack.com/api/conversations.history";
        private static readonly string USER_URL = "https://slack.com/api/users.info";

        private static DateTime UNIX_EPOCH = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

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
            HttpService httpService = new HttpService();
            var response = httpService.get(LIST_URL, token);

            JObject jsonBody = JObject.Parse(response);
            if (jsonBody.Count <= 0)
            {
                Console.WriteLine("レスポンスなし");
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

            Dictionary<string, List<DataDto>> dataDic = new Dictionary<string, List<DataDto>>();
            HttpService httpService = new HttpService();

            foreach (var channel in list)
            {
                List<DataDto> dataList = new List<DataDto>();

                var historyUrl = HISTORY_URL + "?channel=" + channel.channnelId + "&limit=100";

                var historyResponse = httpService.get(historyUrl, token);
                JObject historyJson = JObject.Parse(historyResponse);

                if (historyJson.Count <= 0)
                {
                    Console.WriteLine("レスポンスなし");
                    break;
                }

                var message = historyJson["messages"];
                var child = message.Children();
                foreach (var value in child)
                {
                    DataDto data = new DataDto();

                    data.thread = channel.channnelName;
                    var ts = value["ts"].ToString();
                    long longTs = 0;
                    // tsは"1234567890.123456"のような形式で渡されてくるが、
                    // 年月日時分秒まであればいい（加えてミリ秒以下は扱いが面倒な）ので、小数点以下は除外する。
                    long.TryParse(ts.Split('.')[0], out longTs);
                    data.ts = UNIX_EPOCH.AddSeconds(longTs).ToLocalTime().ToString();

                    // ユーザIDからユーザ名を取得する
                    var userUrl = USER_URL + "?user=" + value["user"].ToString();
                    var userResponse = httpService.get(userUrl, token);
                    JObject userJson = JObject.Parse(userResponse);
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
