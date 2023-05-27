using SlackExport.Service;

namespace SlackExport
{
    public class Controller
    {
        private const string FileImport = "1";

        public void Execute(string param)
        {
            // コマンドライン引数で呼び出す機能を分岐する予定
            switch (param)
            {
                // ファイルをDBにインポートする
                case FileImport:
                    var service = new FileImportService();
                    service.Execute();
                    break;
                default:
                    break;
            }
        }
    }
}
