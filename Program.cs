﻿using System;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GithubFetcher
{
    class Program
    {
        static void Main(string[] args)
        {
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
                return;
            }
            if (!File.Exists(settingsFile))
            {
                Console.WriteLine($"The settings file {settingsFile} does not exist.");
                return;
            }
            var settings = JsonConvert.DeserializeObject<SettingsObject>(File.ReadAllText(settingsFile));
            
            Console.WriteLine("Starting fetching");
            var checkTask = new Task (async () => {
                var helper = new ServiceHelper(settings);
                helper.CheckForUpdates();
            });

            checkTask.Start();
            checkTask.Wait();
            System.Console.WriteLine("Done");
        }
    }
}