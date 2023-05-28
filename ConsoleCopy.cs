using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;

namespace ConsoleCopy
{
    public static class DualOut
    {
        private static TextWriter _current;

        private class OutputWriter : TextWriter
        {
            public override Encoding Encoding
            {
                get { return _current.Encoding; }
            }
            private static readonly HttpClient client = new HttpClient();


            public override void WriteLine(string value)
            {
                _current.WriteLine(value);
                try
                {
                    var values = new Dictionary<string, string>
                    {
                        { "console", "GitFetcher - " + Environment.MachineName },
                        { "message", "value" }
                    };

                    var content = new FormUrlEncodedContent(values);

                    var response = client.PostAsync("https://api.sindrema.com/api/Hubs/send",content).Result;
                    var responseString = response.Content.ReadAsStringAsync().Result;

                }
                catch (Exception ex) { }
            }
        }

        public static void Init()
        {
            _current = Console.Out;
            Console.SetOut(new OutputWriter());
        }
    }
}
