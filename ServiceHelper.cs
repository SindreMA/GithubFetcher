using System;
using System.Diagnostics;
using System.Linq;
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
                //process
            }
            return false;
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
            if (output.Contains("Already up-to-date.")) return false;
            return true;
        }

    }
}