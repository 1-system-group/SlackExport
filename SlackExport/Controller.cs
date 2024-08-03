using NLog;
using SlackExport.Service;

namespace SlackExport
{
    public class Controller
    {
        private const string ArgumentTypFileImport = "1";

        private const string ArgumentTypFileExport = "2";

        private const string ArgumentTypFileImportTargetDay = "11";

        private const string ArgumentTypFileExportTargetDay = "21";

        public static Logger logger = LogManager.GetCurrentClassLogger();

        public void Execute(string param1, string? param2)
        {
            // コマンドライン引数で呼び出す機能を分岐する予定
            switch (param1)
            {
                // ファイルをDBにインポートする
                case ArgumentTypFileImport:
                    FileImport();
                    break;
                // ファイルをS3にエクスポートする
                case ArgumentTypFileExport:
                    FileExport();
                    break;
                // ファイルをDBにインポートする（日指定）
                case ArgumentTypFileImportTargetDay:
                    FileImportTargetDay(param2);
                    break;
                // ファイルをS3にエクスポートする（日指定）
                case ArgumentTypFileExportTargetDay:
                    FileExportTargetDay(param2);
                    break;
                default:
                    Console.WriteLine("引数が対象外です：" + param1);
                    break;
            }
        }

        private void FileImport()
        {
            var fileImportService = new FileImportService();
            fileImportService.Execute();

        }

        private void FileExport()
        {
            var fileExportService = new FileExportService();
            fileExportService.Execute();
        }


        private void FileImportTargetDay(string? targetDay)
        {
            // 現在日時を取得して、
            // コマンドライン引数で受け取った対象日付と一致していたら実行する
            var day = DateTime.Now.Day;
            if ((targetDay != null) && (int.Parse(targetDay) == day))
            {
                var fileImportService = new FileImportService();
                fileImportService.Execute();
            } else
            {
                Console.WriteLine("指定日と異なるため実行しません：" + targetDay);
                logger.Info("指定日と異なるため実行しません：" + targetDay);
            }
        }

        private void FileExportTargetDay(string? targetDay)
        {
            // 現在日時を取得して、
            // コマンドライン引数で受け取った対象日付と一致していたら実行する
            var day = DateTime.Now.Day;
            if ((targetDay != null) && (int.Parse(targetDay) == day))
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
