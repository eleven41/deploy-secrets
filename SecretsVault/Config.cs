using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eleven41.Logging;

namespace SecretsVault
{
	public class Config
	{
		public Config(string kmsKeyId, string bucketName, string prefix)
		{
			if (String.IsNullOrWhiteSpace(kmsKeyId))
				throw new ArgumentOutOfRangeException("kmsKeyId");
			if (String.IsNullOrWhiteSpace(bucketName))
				throw new ArgumentOutOfRangeException("bucketName");

			this.KmsKeyId = kmsKeyId.Trim();
			this.BucketName = bucketName.Trim();
			this.Prefix = prefix;
		}

		public static Config LoadFromAppSettings(ILog log)
		{
			if (log == null)
				throw new ArgumentNullException("log");

			string kmsKeyId = System.Configuration.ConfigurationManager.AppSettings["vault:KmsKeyId"];
			if (String.IsNullOrEmpty(kmsKeyId))
				throw new Exception("'vault:KmsKeyId' is null or empty.");
			log.Log(LogLevels.Diagnostic, "KMS Key ID: {0}", kmsKeyId);
			string bucketName = System.Configuration.ConfigurationManager.AppSettings["vault:BucketName"];
			if (String.IsNullOrEmpty(bucketName))
				throw new Exception("'vault:BucketName' is null or empty.");
			log.Log(LogLevels.Diagnostic, "S3 Bucket Name: {0}", bucketName);

			string prefix = System.Configuration.ConfigurationManager.AppSettings["vault:Prefix"];
			log.Log(LogLevels.Diagnostic, "S3 Prefix: {0}", prefix);

			return new Config(kmsKeyId, bucketName, prefix);
		}

		private string _kmsKeyId;

		public string KmsKeyId
		{
			get { return _kmsKeyId; }
			set 
			{
				if (String.IsNullOrWhiteSpace(value))
					throw new ArgumentOutOfRangeException("KmsKeyId");

				_kmsKeyId = value; 
			}
		}

		private string _bucketName;

		public string BucketName
		{
			get { return _bucketName; }
			set
			{
				if (String.IsNullOrWhiteSpace(value))
					throw new ArgumentOutOfRangeException("BucketName");

				_bucketName = value;
			}
		}

		private string _prefix;

		public string Prefix
		{
			get { return _prefix; }
			set
			{
				_prefix = value;
			}
		}

		private string _regionName;

		public string RegionName
		{
			get { return _regionName; }
			set { _regionName = value; }
		}
		
	}
}
