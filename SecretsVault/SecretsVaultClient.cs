using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eleven41.Logging;

namespace SecretsVault
{
	public class SecretsVaultClient
	{
		Config _config;

		public SecretsVaultClient(Config config)
		{
			if (config == null)
				throw new ArgumentNullException("config");

			_config = config;
		}

		private Amazon.KeyManagementService.IAmazonKeyManagementService CreateKmsClient()
		{
			var config = new Amazon.KeyManagementService.AmazonKeyManagementServiceConfig();
			if (!String.IsNullOrEmpty(_config.RegionName))
				config.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(_config.RegionName);
			return new Amazon.KeyManagementService.AmazonKeyManagementServiceClient(config);
		}

		private Amazon.S3.IAmazonS3 CreateS3Client()
		{
			var config = new Amazon.S3.AmazonS3Config();
			if (!String.IsNullOrEmpty(_config.RegionName))
				config.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(_config.RegionName);
			return new Amazon.S3.AmazonS3Client();
		}

		public async Task PutAsync(string key, string value, ILog log)
		{
			if (String.IsNullOrWhiteSpace(key))
				throw new ArgumentOutOfRangeException("key");
			key = key.Trim();

			if (String.IsNullOrEmpty(value))
				throw new ArgumentOutOfRangeException("value");

			// Encrypt the value
			var kmsClient = CreateKmsClient();
			log.Log(LogLevels.Info, "Encrypting {0}...", key);
			var encryptedStream = await Encrypt(value, kmsClient);
			encryptedStream.Seek(0, SeekOrigin.Begin);

			// Upload the blob
			string objectKey = String.Format("{0}{1}", _config.Prefix, key);

			var s3Client = CreateS3Client();
			log.Log(LogLevels.Info, "Uploading {0} to s3://{1}/{2}", key, _config.BucketName, objectKey);
			await Upload(objectKey, encryptedStream, s3Client);
		}

		public async Task<string> GetAsync(string key, ILog log)
		{
			if (String.IsNullOrWhiteSpace(key))
				throw new ArgumentOutOfRangeException("key");
			key = key.Trim();

			// Get the blob
			string objectKey = String.Format("{0}{1}", _config.Prefix, key);

			var s3Client = CreateS3Client();
			log.Log(LogLevels.Info, "Retrieving {0} from s3://{1}/{2}", key, _config.BucketName, objectKey);
			var encryptedStream = await Download(objectKey, s3Client);

			var kmsClient = CreateKmsClient();
			return await Decrypt(encryptedStream, kmsClient);
		}

		private async Task<Stream> Download(string objectKey, Amazon.S3.IAmazonS3 s3Client)
		{
			var request = new Amazon.S3.Model.GetObjectRequest()
			{
				BucketName = _config.BucketName,
				Key = objectKey
			};

			var response = await s3Client.GetObjectAsync(request);

			return response.ResponseStream;
		}

		private async Task Upload(string objectKey, MemoryStream stream, Amazon.S3.IAmazonS3 client)
		{
			var request = new Amazon.S3.Model.PutObjectRequest()
			{
				BucketName = _config.BucketName,
				Key = objectKey,
				InputStream = stream,
				ServerSideEncryptionMethod = Amazon.S3.ServerSideEncryptionMethod.AES256
			};

			var response = await client.PutObjectAsync(request);
		}

		private async Task<MemoryStream> Encrypt(string value, Amazon.KeyManagementService.IAmazonKeyManagementService client)
		{
			using (MemoryStream memoryStream = new MemoryStream())
			{
				using (var writer = new StreamWriter(memoryStream, Encoding.UTF8))
				{
					writer.Write(value);
					writer.Flush();
					memoryStream.Seek(0, SeekOrigin.Begin);

					var request = new Amazon.KeyManagementService.Model.EncryptRequest()
					{
						KeyId = _config.KmsKeyId,
						Plaintext = memoryStream
					};

					var response = await client.EncryptAsync(request);

					return response.CiphertextBlob;
				}
			}
		}

		private async Task<string> Decrypt(Stream stream, Amazon.KeyManagementService.IAmazonKeyManagementService client)
		{
			using (MemoryStream memoryStream = new MemoryStream())
			{
				await stream.CopyToAsync(memoryStream);
				memoryStream.Seek(0, SeekOrigin.Begin);

				var request = new Amazon.KeyManagementService.Model.DecryptRequest()
				{
					CiphertextBlob = memoryStream
				};

				var response = await client.DecryptAsync(request);
				using (var reader = new StreamReader(response.Plaintext, Encoding.UTF8))
				{
					return await reader.ReadToEndAsync();
				}
			}
		}
	}
}
