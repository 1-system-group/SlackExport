using NLog;
using SlackExport.Service;

namespace SlackExport
{
    public class Controller
    {
        private const string ArgumentTypFileImport = "1";

        private const string ArgumentTypFileExport = "2";

        public static Logger logger = LogManager.GetCurrentClassLogger();

        public void Execute(string param1, string? param2)
        {
            // コマンドライン引数で呼び出す機能を分岐する予定
            switch (param1)
            {
                // ファイルをDBにインポートする
                case ArgumentTypFileImport:
                    FileImport(param2);
                    break;
                // ファイルをS3にエクスポートする
                case ArgumentTypFileExport:
                    FileExport(param2);
                    break;
                default:
                    Console.WriteLine("引数が対象外です：" + param1);
                    break;
            }
        }

        private void FileImport(string? targetDay)
        {
            // 現在日時を取得して、
            // コマンドライン引数で受け取った対象日付と一致していたら実行する
            var day = DateTime.Now.Day;
            if (targetDay == null)
            {
                var fileImportService = new FileImportService();
                fileImportService.Execute();
            }
            else
            {
                if (int.Parse(targetDay) == day)
                {
                    var fileImportService = new FileImportService();
                    fileImportService.Execute();
                }
                else
                {
                    Console.WriteLine("指定日と異なるため実行しません：" + targetDay);
                    logger.Info("指定日と異なるため実行しません：" + targetDay);
                }
            }
        }

        private void FileExport(string? targetDay)
        {
            // 現在日時を取得して、
            // コマンドライン引数で受け取った対象日付と一致していたら実行する
            var day = DateTime.Now.Day;
            if (targetDay == null)
            {
                var fileExportService = new FileExportService();
                fileExportService.Execute();
            }
            else
            {
                if (int.Parse(targetDay) == day)
                {
                    var fileExportService = new FileExportService();
                    fileExportService.Execute();
                }
                else
                {
                    Console.WriteLine("指定日と異なるため実行しません：" + targetDay);
                    logger.Info("指定日と異なるため実行しません：" + targetDay);
                }
            }
        }
    }
}
