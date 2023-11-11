using System.Configuration;
using Amazon.Runtime.CredentialManagement;
using Amazon.S3;
using Amazon.S3.Transfer;

namespace SlackExport.Common
{
    public class AmazonS3Access
    {
        private AmazonS3Client client;
        public AmazonS3Access()
        {

            // ～ユーザ/.aws/credentials
            // にアクセスキーとシークレットキーを持ったプロファイルが存在していることが前提になります
            string profileName = ConfigurationManager.AppSettings["awsCliProfile"];
            var credentilasFile = new SharedCredentialsFile();

            CredentialProfile profile = null;
            if (credentilasFile.TryGetProfile(profileName, out profile) == false)
            {
                System.Diagnostics.Debug.WriteLine("プロファイル名は存在しません。");
                return;
            }

            Amazon.Runtime.AWSCredentials awsCredentials = null;
            if (AWSCredentialsFactory.TryGetAWSCredentials(profile, credentilasFile, out awsCredentials) == false)
            {
                System.Diagnostics.Debug.WriteLine("認証情報の生成に失敗しました。");
                return;
            }

            client = new AmazonS3Client(awsCredentials, profile.Region);

        }

        public void UploadFile(string filePath, string bucketName, string objectKey)
        {
            TransferUtility fileTransferUtility = new TransferUtility(client);
            fileTransferUtility.Upload(filePath, bucketName, objectKey);
        }
    }
}
