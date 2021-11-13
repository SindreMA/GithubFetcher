using System.Collections.Generic;

namespace GithubFetcher
{
    public class Project
    {
        public int Order { get; set; }
        public string Directory { get; set; }
        public string ProcessName { get; set; }        
        public string CommandAfter { get; set; }
        public string CommandBefore { get; set; }
        public bool UpdateOnChanges { get; set; }
        public bool RunIfNotRunning { get; set; }        
        public bool AlwaysRunCommand { get; set; }
        public List<EnvironmentVariable> EnvironmentVariables { get; set; }
    }
    public class EnvironmentVariable
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
}