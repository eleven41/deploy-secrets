using System;
using System.Threading.Tasks;
using Eleven41.Logging;

namespace SecretsVault
{
	public interface ISecretsVaultClient
	{
		Task<string> GetAsync(string key, ILog log);
		Task PutAsync(string key, string value, ILog log);
		Task DeleteAsync(string key, ILog log);
	}
}
