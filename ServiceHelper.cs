using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using static SimpleExec.Command;
using System.Management;
using System.Threading.Tasks;

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
            if (string.IsNullOrEmpty(project.CommandAfter)) return;
            var isRunning = IsRunning(project,out Process _process);
            if (project.AlwaysRunCommand || (isNew && project.UpdateOnChanges) || (project.RunIfNotRunning && !isRunning))
            {
                if (isRunning) StopProcess(_process);
                new Task(()=> {
                    RunCommand(project.CommandAfter, project.Directory);        
                }).Start();
            }
        }

        private void StopProcess(Process _process)
        {
            _process.Kill();
            _process.WaitForExit();
        }

        private bool IsRunning(Project project, out Process _process)
        {
            foreach (var process in Process.GetProcessesByName(project.ProcessName.ToLower()))
            {
                if (process.ProcessName.ToLower().Contains(project.ProcessName.ToLower()))
                {
                    var workingDirectory = GetProcessWorkingDirectory(process.Id);

                    if (workingDirectory != null && workingDirectory.ToLower().Contains(project.Directory.ToLower()))
                    {
                        var args = GetProcessArguments(process).Select(x=> x.ToLower());
                        var projectArgs = GetArguments(project.CommandAfter);

                        foreach (var item in projectArgs)
                        {
                            if (args.Contains(item.ToLower()))
                            {
                                _process = process;
                                return true;
                            }
                        }
                    }
                }
            }
            _process = null;
            return false;
        }

        private List<string> GetProcessArguments(Process process)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var args = GetWindowsArguments(process.Id);
                return args;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var args = GetLinxArguments(process.Id);
                return args;
            }
            return new List<string>();
        }

        private string RemoveAllNewlines(string input)
        {
            return input.Replace("\r\n", "").Replace("\n", "");
        }
    
        private List<string> GetArguments(string commandLine)
        {
            var arguments = new List<string>();
            var split = commandLine.Split(' ');
            split.Skip(1).ToList().ForEach(x => arguments.Add(x));
            return arguments;
        }

        private List<string> GetWindowsArguments(int processId)
        {
            var commandLine = RemoveAllNewlines(Read("powershell", $@"-command ""gwmi win32_process | Where-Object {{$_.Handle -eq ""{processId}""}} | select -ExpandProperty CommandLine | Out-String""", ""));
            var args = GetArguments(commandLine).ToList();
            return args;
        }
        
        private List<string> GetLinxArguments(int processId)
        {
            var commandLine = RemoveAllNewlines(Read("ps", $"-p {processId} -o args", ""));
            var args = GetArguments(commandLine).Skip(1).ToList();
            return args;
        }

        public List<string> SplitStringOnString(string str, string splitOn)
        {
            var split = str.Split(new[] { splitOn }, StringSplitOptions.None);
            return split.ToList();
        }
        private string GetProcessWorkingDirectory(int id)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
              var output = Read("pwdx", id.ToString());
              var WorkingDir = string.Join(": ",SplitStringOnString(output, ": ").Skip(1));
              return WorkingDir;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var proc = Process.GetProcessById(id);
                var output = Read("handle64.exe", $"-p {proc.Id}","").Split('\n');
                
                var WorkingDir = SplitStringOnString(output.FirstOrDefault(x=> x.Contains(": File  (RW-)")),": File  (RW-)").LastOrDefault().Replace("\r\r","").Trim();
                return WorkingDir;
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
            string output = "";
            try
            {
                output = Read("git", "pull", directory);    
            }
            catch (System.Exception ex) {
                output = ex.Message;
            }
            
            if (output.ToLower().Contains("already up to date")) return false;
            return true;
        }

    }
}