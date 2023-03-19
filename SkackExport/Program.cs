using SlackExport.Db;
using SlackExport.Export;

namespace SlackExport
{
    internal class Program
    {
        // トークン
        // 引数か設定ファイルから受け取るようにしたいが、とりあえずは定数にしておく
        // 下記のスコープを持った「User Token Scopes」のトークンをセットする。
        // channels:history
        // channels:read
        // users:read
        private static readonly string SLACK_API_TOKENT = "xxxxx";

        static void Main(string[] args)
        {

            // slack apiのトークン
            //string token = args[0];
            string token = SLACK_API_TOKENT;

            // slack apiから投稿内容を取得してくる
            var export = new ExportService();
            var dataDic = export.Export(token);

            // デバッグ用
            /*
            foreach (var key in dataDic.Keys)
            {
                Console.WriteLine("=== データ：" + key + "===");
                foreach (var data in dataDic[key])
                {
                    Console.WriteLine("-------------");
                    Console.WriteLine(data.thread);
                    Console.WriteLine(data.ts);
                    Console.WriteLine(data.message);
                    Console.WriteLine(data.user);
                    Console.WriteLine("-------------");
                }
                Console.WriteLine("=====================");
            }
            */

            // 投稿内容をDBに登録する
            Regist regist = new Regist();
            foreach (var key in dataDic.Keys)
            {
                foreach (var data in dataDic[key])
                {
                    regist.insert(data);
                }
            }
        }
    }
}
