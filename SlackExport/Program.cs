using NLog;

namespace SlackExport
{
    internal class Program
    {
        public static Logger logger = LogManager.GetCurrentClassLogger();

        // コマンライン引数
        // 1：S3バックアップ済のファイルの内容を、Diary Sampleのdiaryテーブルにインポートする。
        //    →現状はS3からダウンロードはせずに、ローカルからインポートしている。S3からダウンロードするようにしたい。（予定）
        // 2：Slack APIを使用しSlackの投稿内容を取得して、S3に保存する。
        //    →現状はローカルに保存している。S3にアップロードするようにしたい。（予定）
        // 3：Slack APIを使用しSlackの投稿内容を取得して、Diary Sampleのdiaryテーブルにインポートする（予定）
        public static void Main(string[] args)
        {
            logger.Info("処理開始");
            Console.WriteLine("処理開始");

            string param1 = args[0];
            string? param2 = null;
            if (args.Length == 0)
            {
                logger.Info("引数エラー");
            }
            else if (args.Length == 2)
            {
                param2 = args[1];
            }

            var controller = new Controller();
            controller.Execute(param1, param2);

            logger.Info("処理終了");
            Console.WriteLine("処理終了");
        }
    }
}
