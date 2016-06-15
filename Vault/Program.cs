using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using Eleven41.Logging;
using SecretsVault;

namespace Vault
{
	class Program
	{
		static void Main(string[] args)
		{
			var log = new ConsoleLog(true, true, true, true);
			MainAsync(args, log).Wait();
		}

		static async Task MainAsync(string[] args, ILog log)
		{
			string invokedVerb = null;
			object invokedVerbInstance = null;

			try
			{
				var options = new CommandLineOptions();
				if (!CommandLine.Parser.Default.ParseArguments(args, options,
				  (verb, subOptions) =>
				  {
					  // if parsing succeeds the verb name and correct instance
					  // will be passed to onVerbCommand delegate (string,object)
					  invokedVerb = verb;
					  invokedVerbInstance = subOptions;
				  }))
				{
					Environment.Exit(CommandLine.Parser.DefaultExitCodeFail);
				}

				if (invokedVerb == "create-batch-file")
				{
					var createBatchFileOptions = (CreateBatchFileOptions)invokedVerbInstance;
					await CreateBatchFile(createBatchFileOptions, log);
				}
				else if (invokedVerb == "put-batch")
				{
					var putBatchOptions = (PutBatchOptions)invokedVerbInstance;
					await PutBatch(putBatchOptions, log);
				}
				else if (invokedVerb == "put")
				{
					var putOptions = (PutOptions)invokedVerbInstance;
					await PutSetting(putOptions, log);
				}
				else if (invokedVerb == "get")
				{
					var getOptions = (GetOptions)invokedVerbInstance;
					await GetSetting(getOptions, log);
				}
				else if (invokedVerb == "delete")
				{
					var deleteOptions = (DeleteOptions)invokedVerbInstance;
					await DeleteSetting(deleteOptions, log);
				}

			}
			catch (Exception e)
			{
				Console.WriteLine("{0}", e.Message);
			}

			CommonOptions commonVerbOptions = invokedVerbInstance as CommonOptions;
			if (commonVerbOptions != null)
			{
				if (commonVerbOptions.IsPrompt)
				{
					Console.WriteLine();
					Console.WriteLine("Press Enter to continue...");
					Console.ReadLine();
				}
			}
			
		}

		private static async Task GetSetting(GetOptions options, ILog log)
		{
			Config config = Config.LoadFromAppSettings(log);
			if (!String.IsNullOrEmpty(options.Region))
			{
				log.Log(LogLevels.Diagnostic, "Region: {0}", options.Region);
				config.RegionName = options.Region;
			}

			var client = new SecretsVaultClient(config);
			string value = await client.GetAsync(options.Key, log);
			log.Log(LogLevels.Info, "Value: {0}", value);
		}

		private static async Task DeleteSetting(DeleteOptions options, ILog log)
		{
			Config config = Config.LoadFromAppSettings(log);
			if (!String.IsNullOrEmpty(options.Region))
			{
				log.Log(LogLevels.Diagnostic, "Region: {0}", options.Region);
				config.RegionName = options.Region;
			}

			var client = new SecretsVaultClient(config);
			await client.DeleteAsync(options.Key, log);
		}

		private static async Task PutSetting(PutOptions options, ILog log)
		{
			Config config = Config.LoadFromAppSettings(log);
			if (!String.IsNullOrEmpty(options.Region))
			{
				log.Log(LogLevels.Diagnostic, "Region: {0}", options.Region);
				config.RegionName = options.Region;
			}

			var client = new SecretsVaultClient(config);
			await client.PutAsync(options.Key, options.Value, log);
		}

		private static async Task PutBatch(PutBatchOptions options, ILog log)
		{
			Config config = Config.LoadFromAppSettings(log);
			if (!String.IsNullOrEmpty(options.Region))
			{
				log.Log(LogLevels.Diagnostic, "Region: {0}", options.Region);
				config.RegionName = options.Region;
			}

			var client = new SecretsVaultClient(config);

			log.Log(LogLevels.Info, "Reading {0}...", options.InputFile);
			var settings = ReadSettingsFile(options.InputFile);

			for (int i = 0; i < settings.Count(); ++i)
			{
				var setting = settings[i];
				if (String.IsNullOrEmpty(setting.Key))
					throw new Exception("settings[" + i + "].key is null or empty");
				if (String.IsNullOrEmpty(setting.Value))
					throw new Exception("settings[" + i + "].value is null or empty");

				await client.PutAsync(setting.Key, setting.Value, log);
			}
		}

		static List<Setting> ReadSettingsFile(string fileName)
		{
			using (var fileStream = new System.IO.FileStream(fileName, System.IO.FileMode.Open))
			{
				return Eleven41.Helpers.JsonHelper.DeserializeFromStream<List<Setting>>(fileStream);
			}
		}

		private static async Task CreateBatchFile(CreateBatchFileOptions options, ILog log)
		{
			string text = "Creating";
			var fileMode = FileMode.CreateNew;

			if (options.IsOverwrite)
			{
				// User is allowing us to overwrite, so does the file exist?
				if (File.Exists(options.OutputFile))
				{
					// Yes, so overwrite
					text = "Overwriting";
					fileMode = FileMode.Create;
				}
			}

			string json = Eleven41.Helpers.JsonHelper.SerializeAndFormat(new List<Setting>()
				{
					new Setting()
					{
						Key = "", Value = ""
					},
					new Setting()
					{
						Key = "", Value = ""
					},
					new Setting()
					{
						Key = "", Value = ""
					}
				});
			log.Log(LogLevels.Info, "{0} {1}...", text, options.OutputFile);
			using (var fileStream = new FileStream(options.OutputFile, fileMode))
			{
				using (var writer = new StreamWriter(fileStream, new UTF8Encoding(false)))
				{
					await writer.WriteLineAsync(json);
				}
			}
		}
	}

	class CommandLineOptions
	{
		[VerbOption("create-batch-file", HelpText = "Creates a skeleton batch file.")]
		public CreateBatchFileOptions CreateBatchFileVerb { get; set; }

		[VerbOption("delete", HelpText = "Delete a key and value from the vault.")]
		public DeleteOptions DeleteVerb { get; set; }

		[VerbOption("get", HelpText = "Get a key's value from the vault.")]
		public GetOptions GetVerb { get; set; }

		[VerbOption("put", HelpText = "Puts a single key/value into the vault.")]
		public PutOptions PutVerb { get; set; }

		[VerbOption("put-batch", HelpText = "Puts one or more key/values into the vault using a JSON batch file.")]
		public PutBatchOptions PutBatchVerb { get; set; }

		[HelpVerbOption]
		public string GetUsage(string verb)
		{
			return HelpText.AutoBuild(this, verb);
		}
		
	}

	class CommonOptions
	{
		[Option('p', "prompt",
			HelpText = "Prompt after performing the action.")]
		public bool IsPrompt { get; set; }

		[Option('r', "region",
			HelpText = "Region to operate in.")]
		public string Region { get; set; }
	}

	class PutOptions : CommonOptions
	{
		[Option('k', "key", Required = true,
			HelpText = "Setting key.")]
		public string Key { get; set; }

		[Option('v', "value", Required = true,
			HelpText = "Setting value.")]
		public string Value { get; set; }

		[HelpOption]
		public string GetUsage()
		{
			return HelpText.AutoBuild(this,
			  (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
		}
	}

	class GetOptions : CommonOptions
	{
		[Option('k', "key", Required = true,
			HelpText = "Setting key.")]
		public string Key { get; set; }

		[HelpOption]
		public string GetUsage()
		{
			return HelpText.AutoBuild(this,
			  (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
		}
	}

	class DeleteOptions : CommonOptions
	{
		[Option('k', "key", Required = true,
			HelpText = "Setting key.")]
		public string Key { get; set; }

		[HelpOption]
		public string GetUsage()
		{
			return HelpText.AutoBuild(this,
			  (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
		}
	}

	class PutBatchOptions : CommonOptions
	{
		[Option('f', "file", Required = true,
			HelpText = "Input JSON file to read.")]
		public string InputFile { get; set; }

		[HelpOption]
		public string GetUsage()
		{
			return HelpText.AutoBuild(this,
			  (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
		}
	}

	class CreateBatchFileOptions : CommonOptions
	{
		[Option('f', "file", Required = true,
			HelpText = "Output JSON file to write.")]
		public string OutputFile { get; set; }

		[Option('o', "overwrite", Required = false,
			HelpText = "Overwrite the existing file.")]
		public bool IsOverwrite { get; set; }

		[HelpOption]
		public string GetUsage()
		{
			return HelpText.AutoBuild(this,
			  (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
		}
	}

	class Setting
	{
		public string Key { get; set; }
		public string Value { get; set; }
	}
}
