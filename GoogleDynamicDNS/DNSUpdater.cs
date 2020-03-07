using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Configuration;
using System.Net.Http;
using System.IO;

namespace GoogleDynamicDNS
{
    public partial class DNSUpdater : ServiceBase
    {
        private static System.Timers.Timer timer;
        public DNSUpdater()
        {
            InitializeComponent();
        }

        public void OnDebug()
        {
            OnStart(null);
        }

        protected override void OnStart(string[] args)
        {
            WriteToLog("Service Started");
            if (int.TryParse(ConfigurationManager.AppSettings["LogFileDaysToKeep"], out int logFileDaysToKeep))
            {
                RemoveOldLogFiles();
            }
            else
            {
                WriteToLog("ERROR: LogFileDaysToKeep could not be parsed to integer. Please correct the value in the GoogleDynamicDNS.exe.config");
            }
                
            Thread loopThread = new Thread(new ThreadStart(SetTimer));
            loopThread.IsBackground = true;
            loopThread.Start();
        }

        protected override void OnStop()
        {
            WriteToLog("Service Stopped");
        }

        private static void SetTimer()
        {
            if (int.TryParse(ConfigurationManager.AppSettings["ExecutionInterval"], out int executionInterval))
            {
                timer = new System.Timers.Timer(executionInterval * 1000);
                timer.Elapsed += OnTimedEvent;
                timer.AutoReset = true;
                timer.Enabled = true;
            }
            else
            {
                WriteToLog("ERROR: ExecutionInterval could not be parsed to integer. Please correct the value in the GoogleDynamicDNS.exe.config");
            }
        }

        private static async void OnTimedEvent(Object source, ElapsedEventArgs args)
        {
            try
            {
                timer.Enabled = false;
            }
            catch (Exception e) 
            {
                WriteToLog("ERROR: Timer.Enabled could not be set to false");
                WriteToLog(e.Message);
            }

            string OldIP = GetLastIP();

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "C# Application");

                try
                {
                    string responseBody = await client.GetStringAsync(@"http://checkip.dyndns.org/");
                    string newIP = responseBody.Split(new string[] { "<body>", "</body>" }, StringSplitOptions.RemoveEmptyEntries)[1].Replace("Current IP Address: ", "");
                    if (OldIP == newIP)
                    {
                        WriteToLog("External IP has not changed no action is required");
                    }
                    else
                    {
                        UpdateDNSRecord(client, newIP);
                    }
                }
                catch (Exception e)
                {
                    WriteToLog(@"ERROR: Could not get DNS from http://checkip.dyndns.org/");
                    WriteToLog(e.Message);
                }
            }

            try
            {
                timer.Enabled = true;
            }
            catch (Exception e)
            {
                WriteToLog("ERROR: Timer.Enabled could not be set to true");
                WriteToLog(e.Message);
            }
        }

        private static void WriteToLog(string message)
        {
            using (StreamWriter writer = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "Log_" + DateTime.Now.ToString("yyyyMMdd") + ".txt", true)) 
            {
                writer.WriteLine(DateTime.Now.ToString() + " - " + message);
            }
        }

        private static void RemoveOldLogFiles()
        {
            DirectoryInfo d = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            FileInfo[] files = d.GetFiles("Log_*.txt");
            foreach (var file in files)
            {
                if (file.LastWriteTime > DateTime.Now.AddDays(-30))
                {
                    //No action required
                }
                else
                {
                    string fileName = file.Name;
                    try
                    {
                        file.Delete();
                        WriteToLog($"Log [{fileName}] has been deleted.");
                    }
                    catch (Exception e)
                    {
                        WriteToLog($"ERROR: Unable to delete [{fileName}]");
                        WriteToLog($"{e.Message}");
                    }
                }
            }
        }

        private static string GetLastIP()
        {
            string output = string.Empty;
            try
            {
                using (StreamReader reader = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + "data.txt"))
                {
                    output = reader.ReadToEnd();
                }
            }
            catch (Exception e) 
            {

                WriteToLog($"ERROR: Problem reading the data.txt file [{e.Message}]");
            }
            return output;
        }

        private static void SetLastIP(string ip)
        {
            using (StreamWriter writer = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "data.txt"))
            {
                writer.Write(ip);
            }
        }

        public static void UpdateDNSRecord(HttpClient client, string ip)
        {
            SetLastIP(ip);
            
            string username = ConfigurationManager.AppSettings["Username"];
            string password = ConfigurationManager.AppSettings["Password"];
            string hostname = ConfigurationManager.AppSettings["DomainURL"];

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://domains.google.com/nic/update?hostname=" + hostname + "&myip=" + ip);
            var byteArray = Encoding.ASCII.GetBytes(username + ":" + password);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            request.Headers.UserAgent.ParseAdd("C# Application");

            var task = client.SendAsync(request);
            task.Wait();

            string content = task.Result.Content.ReadAsStringAsync().Result;
            WriteToLog(content); //The response will look like this [good 192.168.0.1]
            string result = content.Split(' ')[0];

            if (result == "good")
            {
                WriteToLog($"The update was successful [{ip}]");
            }
            else if (result == "nochg")
            {
                WriteToLog($"The supplied IP address [{ip}] is already set for this host. You should not attempt another update until your IP address changes");
            }
            else if (result == "nohost")
            {
                WriteToLog($"The hostname {hostname} does not exist, or does not have Dynamic DNS enabled");
            }
            else if (result == "badauth")
            {
                WriteToLog($"The username [{username}] / password [{password}] combination is not valid for the specified host [{hostname}]");
            }
            else if (result == "notfqdn")
            {
                WriteToLog($"The supplied hostname [{hostname}] is not a valid fully-qualified domain name");
            }
            else if (result == "badagent")
            {
                WriteToLog("Your Dynamic DNS client is making bad requests. Ensure the user agent is set in the request");
            }
            else if (result == "abuse")
            {
                WriteToLog($"Dynamic DNS access for the hostname [{hostname}] has been blocked due to failure to interpret previous responses correctly");
            }
            else if (result == "911")
            {
                WriteToLog($"An error happened at Googles end. Wait 5 minutes and retry");
            }
            else 
            {
                WriteToLog("A custom A or AAAA resource record conflicts with the update. Delete the indicated resource record within DNS settings page and try the update again");
            }
        }
    }
}
