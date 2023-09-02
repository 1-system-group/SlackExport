using System.Configuration;
using System.Text;
using System.Web;
using Newtonsoft.Json.Linq;
using NLog;
using SlackExport.Common;
using SlackExport.Dto;

namespace SlackExport.Service
{

    public class FileImportService
    {
        // ログを出力するロガー定義
        public static Logger logger = LogManager.GetCurrentClassLogger();

        private static DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Local);

        // key:ユーザID、value:ユーザ名
        // インポートファイル読み込みの過程でユーザ情報が取得できたら、この辞書に積んでいく
        private Dictionary<string, string> userDec = new Dictionary<string, string>();
        // key:ユーザID、value:ユーザ名
        // SlackAPIでユーザ情報を取得したモノ
        private Dictionary<string, string> userInfoDec = null;

        public void Execute()
        {
            string path = ConfigurationManager.AppSettings["importFilePath"];

            var fileDtoList = new List<FileDto>();
            // ファイル格納パスを読み込んで、ファイルのリストを作成する
            Read("", path, fileDtoList);

            // ファイルリストを1つずつ読み込んでインポートしていく
            foreach (var fileDto in fileDtoList)
            {
                // 開発チャンネル
                if (fileDto.channnelName == "開発")
                {
                    DevelopRegst(fileDto);
                }
                // github通知チャンネル
                // ※ 現状未実装
                else if (fileDto.channnelName == "github通知")
                {
                    GithubInfoRegist(fileDto);
                }
                // インポート対象外
                else
                {
                    logger.Info("スキップします。：" + fileDto.channnelName);
                }
            }
        }

        private void Read(string directoryName, string path, List<FileDto> fileDtoList)
        {
            if (Directory.Exists(path))
            {
                // 対象フォルダ内のファイルを読み込んでいく
                var files = Directory.GetFiles(path);
                foreach (var file in files)
                {
                    var fileDto = new FileDto();
                    fileDto.channnelName = directoryName;
                    fileDto.filePath = file;
                    fileDtoList.Add(fileDto);
                }

                // 対象フォルダ内にフォルダがあったら、
                // フォルダ内を読み込んでいく
                var directorys = Directory.GetDirectories(path);
                foreach (var directory in directorys)
                {
                    var subDirectoryName = Path.GetFileName(directory);
                    // フォルダを再帰的に読み込んでリストに組み込んでいく
                    Read(subDirectoryName, directory, fileDtoList);
                }
            }
            else
            {
                logger.Warn(path + "のフォルダが存在しません");
            }
        }

        // 開発チャンネル分のインポート
        private void DevelopRegst(FileDto fileDto)
        {
            try
            {
                //ファイルをオープンする
                using (StreamReader sr = new StreamReader(fileDto.filePath, Encoding.GetEncoding("UTF-8")))
                {
                    var endText = sr.ReadToEnd();
                    var jArray = JArray.Parse(endText);

                    foreach (var v in jArray)
                    {
                        var dataDto = new DataDto();

                        var jObject = JObject.Parse(v.ToString());

                        // ------------------------------
                        // 本文
                        // ------------------------------
                        var text = HttpUtility.HtmlDecode(jObject["text"].ToString());

                        // ------------------------------
                        // 日時
                        // ------------------------------
                        // ※ タイトルに使用する
                        //    tsはUNIX時刻（1970年1月1日からの絶対時間）が、
                        //    "1234567890.123456"（整数部が秒。小数部がミリ秒マイクロ秒）のような形式で渡されてくる
                        //    年月日時分秒だけを使用したかったが、URL添付か何かをした際に、
                        //    年月日時分秒が同じでミリ秒以下が異なる2つの投稿に分かれるケースがあったため、
                        //    小数点（ミリ秒）以下を待避して年月日時分秒を作成して、
                        //    そこに小数点以下を結合して一意性を保つ
                        var tsWork = jObject["ts"].ToString();
                        long longTs = 0;

                        // 整数部を取得して年月日時分秒に変換する
                        long.TryParse(tsWork.Split('.')[0], out longTs);
                        string dateTime = unixEpoch.AddSeconds(longTs).ToLocalTime().ToString();

                        // 小数点以下（ミリ秒とマイクロ秒）を取得する
                        string microSecoond = tsWork.Split('.')[1];
                        // 年月日時分秒とミリ秒マイクロ秒を結合する
                        var ts = dateTime + "." + microSecoond;

                        // ------------------------------
                        // 投稿者名
                        // ※ タイトルに使用する
                        // ------------------------------
                        // ユーザID
                        var user = jObject["user"].ToString();
                        // user_profileがあれば、その中のreal_nameから投稿者を取得する
                        string name = null;
                        if (jObject.ContainsKey("user_profile"))
                        {
                            var profile = jObject["user_profile"];
                            name = profile["display_name"].ToString();
                            // 同じ人が連投して投稿した場合、
                            // user_profileがなくてreal_nameを取得できないので、
                            // そのケースがあった時のために、
                            // real_nameが取得できた場合は、
                            // userとdisplay_nameを紐付けて保持しておく
                            if (!userDec.ContainsKey(user))
                            {
                                userDec.Add(user, name);
                            }
                        }
                        else
                        {
                            // 同じ人が連投の場合、user_profileがないので、
                            // 保持しておいたuserからreal_nameを取得する
                            if (userDec.ContainsKey(user))
                            {
                                name = userDec[user];
                            }
                            else
                            {
                                // 保持しておいたuserにも見つからない場合、
                                // SlackAPIからユーザ名を取得する
                                string slackApiName = getUserFromSlack(user);
                                if (slackApiName != string.Empty)
                                {
                                    name = slackApiName;
                                }
                                // SlackAPIに問い合わせても見つからなければuserを使う。
                                else 
                                {
                                    name = user;
                                }
                            }
                        }

                        dataDto.thread = fileDto.channnelName;
                        dataDto.user = name;
                        dataDto.ts = ts;
                        dataDto.message = text;

                        var dbAccess = new DbAccess();
                        // すでにインポート済でなければインポートする
                        if (dbAccess.CheckRegistered(dataDto) == false)
                        {
                            dbAccess.Insert(dataDto);
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                logger.Error(fileDto.filePath + "読み込みで例外エラー。 エラー内容：" + ex.Message + " スタックトレース：" + ex.StackTrace);
            }
        }

        // github通知チャンネル分のインポート
        // ※ 現時点では未実装
        private void GithubInfoRegist(FileDto fileDto)
        {

        }


        private string getUserFromSlack(string userId)
        {
            string userName = string.Empty;

            // SlackAPIに問い合わせしていなければ問い合わせる
            // SlackAPIを飛ばさずに済むなら飛ばしたくないので、
            // 問い合わせが必要になるまはせず、
            // あらかじめ問い合わせておくようなことはしない
            if (userInfoDec == null)
            {
                string token = ConfigurationManager.AppSettings["token"];
                var slackApiAccess = new SlackApiAccess();
                userInfoDec = slackApiAccess.GetUserInfo(token);
            }

            if (userInfoDec.ContainsKey(userId))
            {
                userName = userInfoDec[userId];
            }
            return userName;
        }
    }
}
