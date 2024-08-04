using System.Configuration;
using System.Globalization;
using System.IO.Compression;
using NLog;
using SlackExport.Common;

namespace SlackExport.Service
{
    public class FileExportService
    {
        // TODO:App.configに移す想定
        private static readonly string ROOT_PATH = "./work";

        private static readonly string EXPORT_NAME = "第一システム部（仮） Slack export";

        // タイムゾーンは、SlackからはUTCで取れるので、これを指定しておく
        private static DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        public static Logger logger = LogManager.GetCurrentClassLogger();

        public FileExportService() { }


        public void Execute()
        {

            string token = Environment.GetEnvironmentVariable("SLACK_TOKEN");

            var slackApiAccess = new SlackApiAccess();
            var channelDtoList = slackApiAccess.GetThreadId(token);


            var userInfoDec = slackApiAccess.GetUserInfo(token);

            CultureInfo.CurrentCulture = CultureInfo.CreateSpecificCulture("en-US");
            var startDate = DateTime.MaxValue;
            var endDate = DateTime.MinValue;
            foreach (var channel in channelDtoList)
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

                    var message = slackApiAccess.GetMessage(channel, startDate2, endDate2, token);
                    if (message != string.Empty)
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
            string newDir = ROOT_PATH +
                            Path.DirectorySeparatorChar + EXPORT_NAME +
                            " " +
                            startDateStr +
                            " - " +
                            endDateStr;
            string zipDir = newDir + ".zip";

            Directory.Move(oldDir, newDir);
            // Zipファイルが既に存在していたら消してしまう
            if (File.Exists(zipDir))
            {
                File.Delete(zipDir);
            }
            // Zip圧縮する
            ZipFile.CreateFromDirectory(newDir, zipDir);
            // フォルダは消してZipファイルだけ残す
            Directory.Delete(newDir, true);

            AmazonS3Access amazonS3Service = new AmazonS3Access();

            // S3アクセスにZipファイルをエクスポートする。
            // （オブジェクトキーとオブジェクト名の区切りは、"/"である必要があるみたいで、
            //   Path.DirectorySeparatorCharでもUnix系で動かすなら問題ないと思いますが、
            //   一応明示的に"/"を指定するようにします）
            amazonS3Service.UploadFile(Path.GetFullPath(zipDir),
                                            ConfigurationManager.AppSettings["awsBucketName"],
                                            ConfigurationManager.AppSettings["awsObjectPath"] +
                                            "/" +
                                            Path.GetFileName(zipDir));
            Console.WriteLine("エクスポートしたファイルを格納しました：" + zipDir);
            logger.Info("エクスポートしたファイルを格納しました：" + zipDir);

            // エクスポートできたらローカルのZipファイルを消す
            File.Delete(zipDir);
        }
    }
}
