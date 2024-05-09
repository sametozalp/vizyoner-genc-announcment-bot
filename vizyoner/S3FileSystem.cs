using Amazon.S3.Model;
using Amazon.S3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using vizyoner;

namespace deneme {
    public class S3FileSystem {
        private String awsAccessKey;
        private String awsSecretKey;
        RegionEndpoint region;
        private String bucketName;
        private String keyName;
        private AmazonS3Client client;

        public S3FileSystem(string awsAccessKey, string awsSecretKey) {
            this.awsAccessKey = awsAccessKey;
            this.awsSecretKey = awsSecretKey;
        }

        public AmazonS3Client getS3Client(string bucketName, string keyName) {
            this.bucketName = bucketName;
            this.keyName = keyName;
            region = RegionEndpoint.USEast1; // S3 bölgesini değiştirin
            client = new AmazonS3Client(awsAccessKey, awsSecretKey, region);
            return client;
        }

        public async Task sendData(string duyuruIcerik) {
            var request = new PutObjectRequest {
                BucketName = bucketName,
                Key = keyName,
                ContentBody = duyuruIcerik
            };

            try {
                await client.PutObjectAsync(request);
            } catch (AmazonS3Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }

        public async Task sendData(List<Model> list) {
            String strList = "";
            foreach(Model l in list) {
                strList += l.title + "\n";
            }

            var request = new PutObjectRequest {
                BucketName = bucketName,
                Key = keyName,
                ContentBody = strList
            };

            try {
                await client.PutObjectAsync(request);
            } catch (AmazonS3Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }

        public async Task<List<string>> getTitleList() {
            var resultList = new List<string>();

            // S3 nesnesinin içeriğini okumak için istek oluşturun
            var request = new GetObjectRequest {
                BucketName = bucketName,
                Key = keyName
            };

            try {
                // S3 nesnesinin içeriğini okuyun
                using var response = await client.GetObjectAsync(request);

                // İçeriği bir akıştan okuyun
                using var responseStream = response.ResponseStream;
                using var reader = new StreamReader(responseStream);

                // Satırları okuyun ve listeye ekleyin
                while (!reader.EndOfStream) {
                    var line = await reader.ReadLineAsync();
                    resultList.Add(line);
                }
            } catch (AmazonS3Exception ex) {
                Console.WriteLine($"Hata Oluştu. Hata Kodu: {ex.ErrorCode}, Hata Mesajı: {ex.Message}");
            }

            return resultList;
        }


        public async Task getData() {
            // S3 nesnesinin içeriğini okumak için istek oluşturun
            var request = new GetObjectRequest {
                BucketName = bucketName,
                Key = keyName
            };

            try {
                // S3 nesnesinin içeriğini okuyun
                using var response = await client.GetObjectAsync(request);

                // İçeriği bir akıştan okuyun
                using var responseStream = response.ResponseStream;
                using var reader = new StreamReader(responseStream);

                // Veriyi okuyun ve konsola yazın
                var content = reader.ReadToEnd();
                Console.WriteLine(content);
            } catch (AmazonS3Exception ex) {
                Console.WriteLine($"Hata Oluştu. Hata Kodu: {ex.ErrorCode}, Hata Mesajı: {ex.Message}");
            }
        }
    }
}
