using System;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using ConsoleCopy;

namespace GithubFetcher
{
    class Program
    {
        static void Main(string[] args)
        {
            DualOut.Init();
            if (args.Length == 1 && args[0] == "--generate")
            {
                File.WriteAllText(
                    Environment.CurrentDirectory + "/settings-sample.json",
                    JsonConvert.SerializeObject(
                        new SettingsObject() {
                            Projects = new System.Collections.Generic.List<Project>() {
                                new Project() {
                                    AlwaysRunCommand = true,
                                    CommandAfter = "hostname",
                                    CommandBefore = "hostname",
                                    Directory = Environment.CurrentDirectory,
                                    Order = 1,
                                    UpdateOnChanges = true,
                                    RunIfNotRunning = true,
                                    EnvironmentVariables = new List<EnvironmentVariable>() {
                                        new EnvironmentVariable() {
                                            Name = "TestKey",
                                            Value = "TestVal"
                                        }
                                    },
                                }
                            }
                        }, 
                        Formatting.Indented
                    )
                );
                return;
            }
            var settingsFile = Environment.GetEnvironmentVariable("SETTINGS_FILE");
            if (settingsFile == null)
            {
                Console.WriteLine("Please set the environment variable SETTINGS_FILE to the path of your settings file.");
                Thread.Sleep(5000);
                return;
            }
            if (!File.Exists(settingsFile))
            {
                Console.WriteLine($"The settings file {settingsFile} does not exist.");
                Thread.Sleep(5000);
                return;
            }
            var settings = JsonConvert.DeserializeObject<SettingsObject>(File.ReadAllText(settingsFile));
            
            Console.WriteLine("Starting fetching");
            var checkTask = new Task (() => {
                var helper = new ServiceHelper(settings);
                helper.CheckForUpdates();
            });

            checkTask.Start();
            checkTask.Wait();
            Thread.Sleep(5000);
            System.Console.WriteLine("Done");
        }
    }
}
