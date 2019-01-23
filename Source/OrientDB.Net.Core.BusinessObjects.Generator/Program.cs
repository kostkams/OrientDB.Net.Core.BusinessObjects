using System;
using System.IO;
using Microsoft.Extensions.CommandLineUtils;
using Newtonsoft.Json;

namespace OrientDB.Net.Core.BusinessObjects.Generator
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var commandLineApplication = new CommandLineApplication(false) {Name = "OrientDB.Net.Core.BusinessObjects.Generator"};
            commandLineApplication.HelpOption("-?|-h|--help");
            var outputDirOption = commandLineApplication.Option("-o|--output <outputDir>", "The output directory", CommandOptionType.SingleValue);
            var configurationFileOption = commandLineApplication.Option("-c|--configuration <configurationFile>", "The configuration file path", CommandOptionType.SingleValue);
            commandLineApplication.OnExecute(() =>
                                             {
                                                 try
                                                 {
                                                     if (!configurationFileOption.HasValue()) throw new Exception("Configuration is missing");
                                                     if (!outputDirOption.HasValue()) throw new Exception("Output directory is missing");

                                                     var configurationFile = new FileInfo(configurationFileOption.Value());
                                                     if (!configurationFile.Exists) throw new Exception("Configuration not found");
                                                     var configuration = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(configurationFile.FullName));
                                                     BOGenerator.Execute(new DirectoryInfo(outputDirOption.Value()), configuration);
                                                 }
                                                 catch (Exception e)
                                                 {
                                                     Console.WriteLine(e);
                                                 }

                                                 return 0;
                                             });

            commandLineApplication.Execute(args);
        }
    }
}