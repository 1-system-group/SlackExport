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

            if (args.Length == 0)
            {
                logger.Info("引数エラー");
            }
            string param = args[0];

            var controller = new Controller();
            controller.Execute(param);

            logger.Info("処理終了");

        }
    }
}
