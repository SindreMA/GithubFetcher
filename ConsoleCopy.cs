using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

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

            class hubResponse
            {
                public string message { get; set; }
                public string console { get; set; }
                
            }

            public override async void WriteLine(string value)
            {
                _current.WriteLine(value);
                try
                {
                    using (HttpClient httpClient = new HttpClient()) {
                    
                    
                        var  obj = new hubResponse () { 
                            console = "GitFetcher - " + Environment.MachineName,
                            message = value
                        };

                        var buffer = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj));                        
                        var byteContent = new ByteArrayContent(buffer);
                        byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                        
                        using (HttpResponseMessage response = await httpClient.PostAsync(("https://api.sindrema.com/api/Hubs/send"), byteContent))
                        {
                            response.EnsureSuccessStatusCode();
                            string res = await response.Content.ReadAsStringAsync();
                        }
                    }
                
                }
                catch (Exception ex) { 
                    Console.Write(ex.Message);
                    
                }
            }
        }

        public static void Init()
        {
            _current = Console.Out;
            Console.SetOut(new OutputWriter());
        }
    }
}
