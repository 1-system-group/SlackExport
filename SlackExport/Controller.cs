using SlackExport.Service;

namespace SlackExport
{
    public class Controller
    {
        private const string FileImport = "1";

        private const string FileExport = "2";

        public void Execute(string param)
        {
            // コマンドライン引数で呼び出す機能を分岐する予定
            switch (param)
            {
                // ファイルをDBにインポートする
                case FileImport:
                    var fileImportService = new FileImportService();
                    fileImportService.Execute();
                    break;
                // ファイルをS3にエクスポートする
                case FileExport:
                    var fileExportService = new FileExportService();
                    fileExportService.Execute();
                    break;
                default:
                    Console.WriteLine("引数が対象外です：" + param);
                    break;
            }
        }
    }
}
