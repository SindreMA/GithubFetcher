using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using static SimpleExec.Command;

namespace GithubFetcher
{
    public class ServiceHelper
    {
        private SettingsObject settings;

        public ServiceHelper(SettingsObject settings)
        {
            this.settings = settings;
        }

        internal void CheckForUpdates()
        {
            if (settings.Projects == null)
            {
                Console.WriteLine("No projects found.");
                return;
            }

                Console.WriteLine("Checking for updates...");
                foreach (var project in settings.Projects.OrderBy(x=> x.Order))
                {
                    Console.WriteLine("Checking for updates project in {0}...", project.Directory);
                    RunPreUpdateCommands(project);
                    var isNew = GitPull(project.Directory);
                    RunPostUpdateCommands(project, isNew);
                }
        }

        private void RunPostUpdateCommands(Project project, bool isNew)
        {
            if (string.IsNullOrEmpty(project.CommandBefore)) return;
            if (project.AlwaysRunCommand || (isNew && project.UpdateOnChanges) || (project.RunIfNotRunning && !IsRunning(project)))
            {
                RunCommand(project.CommandBefore, project.Directory);        
            }
        }

        private bool IsRunning(Project project)
        {
            foreach (var process in Process.GetProcesses())
            {
                var workingDirectory = GetProcessWorkingDirectory(process.Id);
            }
            return false;
        }

        public List<string> SplitStringOnString(string str, string splitOn)
        {
            var split = str.Split(new[] { splitOn }, StringSplitOptions.None);
            return split.ToList();
        }
        private object GetProcessWorkingDirectory(int id)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
              var output = Read("pwdx", id.ToString());
              var WorkingDir = string.Join(": ",SplitStringOnString(output, ": ").Skip(1));
              return WorkingDir;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var output = Read("wmic", "process where ProcessId=\"" + id + "\" get ExecutablePath", "");
                var WorkingDir = string.Join("", SplitStringOnString(output, " ").Skip(1));
                return WorkingDir;
            }
            else
            {
                return "";
            }
            return null;
        }

        private void RunPreUpdateCommands(Project project)
        {
            if (string.IsNullOrEmpty(project.CommandBefore)) return;
            RunCommand(project.CommandBefore, project.Directory);
        }

        private void RunCommand(string command, string directory)
        {
            if (command.Contains(" "))
            {
                var split = command.Split(' ').ToList();
                Run(split.FirstOrDefault(), string.Join(' ',split.Skip(1)), directory);
            }
            else Run(command, "", directory);
        }

        private bool GitPull(string directory)
        {
            var output = Read("git", "pull", directory);
            if (output.ToLower().Contains("already up to date")) return false;
            return true;
        }

    }
}