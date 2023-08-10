using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlackExport.Common;

namespace SlackExport.Service
{
    public class FileExportService
    {
        public FileExportService() { }


        public void Execute() {

            string token = ConfigurationManager.AppSettings["token"];
            var slackApiAccess = new SlackApiAccess();
            slackApiAccess.Export(token);
        }
    }
}
