using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using static SimpleExec.Command;
using System.Management;
using System.Threading.Tasks;
using System.Threading;

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
            System.Console.WriteLine("Is running: " + isRunning);
            if (project.AlwaysRunCommand || (isNew && project.UpdateOnChanges) || (project.RunIfNotRunning && !isRunning))
            {
                System.Console.WriteLine("Running stuff, before stop");
                if (isRunning) StopProcess(_process);
                if (project.SoftMatch) StopAllProcessesInWorkingDirectory(project);
                var _task = new Task(()=> {
                    System.Console.WriteLine("Running stuff");
                    RunPreRunCommands(project);
                    RunCommand(project.CommandAfter, project.Directory,project.EnvironmentVariables);        
                });
                _task.Start();
                _task.Wait();
            }
        }

        private void StopProcess(Process _process)
        {
            _process.Kill();
            _process.WaitForExit();
        }

        private void StopAllProcessesInWorkingDirectory(Project project)
        {
            System.Console.WriteLine("Checking if {0} is running...", project.ProcessName);
            foreach (var process in Process.GetProcessesByName(project.ProcessName.ToLower()))
            {
                System.Console.WriteLine("Checking process: " + process.ProcessName);
                if (process.ProcessName.ToLower().Contains(project.ProcessName.ToLower()))
                {
                    try
                    {
                        System.Console.WriteLine("Matches wanted process");;
                        var workingDirectory = GetProcessWorkingDirectory(process.Id);
                        System.Console.WriteLine("Working directory: " + workingDirectory);
                        if (workingDirectory != null && workingDirectory.ToLower().Contains(project.Directory.ToLower()))
                        {

                            System.Console.WriteLine("Matches wanted directory");
                            process.Kill();
                            process.WaitForExit();    


                        }
                    }
                    catch (System.Exception)
                    {
                    }
                }
            }
        }


        private bool IsRunning(Project project, out Process _process)
        {
            System.Console.WriteLine("Checking if {0} is running...", project.ProcessName);
            foreach (var process in Process.GetProcessesByName(project.ProcessName.ToLower()))
            {
                System.Console.WriteLine("Checking process: " + process.ProcessName);
                if (process.ProcessName.ToLower().Contains(project.ProcessName.ToLower()))
                {
                    System.Console.WriteLine("Matches wanted process");;
                    var workingDirectory = GetProcessWorkingDirectory(process.Id);
                    System.Console.WriteLine("Working directory: " + workingDirectory);
                    if (workingDirectory != null && workingDirectory.ToLower().Contains(project.Directory.ToLower()))
                    {
                        System.Console.WriteLine("Matches wanted directory");
                        var args = GetProcessArguments(process).Select(x=> x.ToLower());
                        var projectArgs = GetArguments(project.CommandAfter);
                        System.Console.WriteLine("Project args: " + string.Join(",", projectArgs));
                        System.Console.WriteLine("args args: " + string.Join(",", args));
                        if (project.SoftMatch) {
                            System.Console.WriteLine("Matches wanted arguments " + $" SoftMatch: {project.SoftMatch}");
                            _process = process;
                            return true;
                        }
                        foreach (var item in projectArgs)
                        {
                            if (args.Contains(item.ToLower()))
                            {
                                System.Console.WriteLine("Matches wanted arguments " + $" SoftMatch: {project.SoftMatch}");
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

        private string RemoveStringFromStart(string input, string start)
        {
            if (input.StartsWith(start))
            {
                return input.Substring(start.Length);
            }
            return input;
        }
        
        private List<string> GetLinxArguments(int processId)
        {
            var commandLine = RemoveAllNewlines(Read("ps", $"-p {processId} -o args", ""));
            commandLine = RemoveStringFromStart(commandLine, "COMMAND");
            System.Console.WriteLine("Command line: " + commandLine);
            var args = GetArguments(commandLine).ToList();
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
            if (string.IsNullOrEmpty(project.CommandBeforeGit)) return;
            RunCommand(project.CommandBeforeGit, project.Directory, project.EnvironmentVariables);
        }

        private void RunPreRunCommands(Project project)
        {
            if (string.IsNullOrEmpty(project.CommandBefore)) return;
            System.Console.WriteLine("Running pre run commands: " + project.CommandBefore);
            RunCommand(project.CommandBefore, project.Directory, project.EnvironmentVariables);
        }

        private void RunCommand(string command, string directory, List<EnvironmentVariable> environmentVariables)
        {
            var envs = environmentVariables.ToDictionary(x => x.Name, x => x.Value) as IDictionary<string,string>;
            System.Console.WriteLine($"Running command: {directory} | {command}");
            if (command.Contains(" "))
            {
                var split = command.Split(' ').ToList();
                RunProgram(split.FirstOrDefault(), string.Join(' ',split.Skip(1)), directory, environmentVariables);
            }
            
            else RunProgram(command, "", directory, environmentVariables);
        }

        private void RunProgram(string command,string arguments, string directory, List<EnvironmentVariable> environmentVariables)
        {
            var _task = new Task(() => {
                var startInfo = new ProcessStartInfo()
                    {
                        FileName = command,
                        Arguments = arguments,
                        WorkingDirectory = directory,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true
                    };
                if (environmentVariables != null) {
                    foreach (var env in environmentVariables)
                    {
                        startInfo.EnvironmentVariables[env.Name] =  env.Value;
                    }
                }
                System.Console.WriteLine($"Running program: {directory} | {command} {arguments}");
                var p = Process.Start(startInfo);
                Thread.Sleep(5000);
            });
            _task.Start();
            _task.Wait();
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