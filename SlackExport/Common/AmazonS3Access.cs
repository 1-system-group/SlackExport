using System.Configuration;
using Amazon;
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
            string awsAccessKeyId = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
            string awsSecretAccessKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
            var amazonS3Config = new AmazonS3Config();
            amazonS3Config.ServiceURL = "https://s3.amazonaws.com";

            // RegionEndpointを指定すると、
            // uploadする際にエンドポイントを利用するようにと言われてエラーになるので、
            // エンドポイント（ServiceURL）指定にする 
            client = new AmazonS3Client(awsAccessKeyId, awsSecretAccessKey, amazonS3Config);

        }

        public void UploadFile(string filePath, string bucketName, string objectKey)
        {
            TransferUtility fileTransferUtility = new TransferUtility(client);
            fileTransferUtility.Upload(filePath, bucketName, objectKey);
        }
    }
}
