using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace OpenVPN
{
    internal class Program
    {
        private static List<ServerObject> ServerList;

        private static string openVpnExeFileName = @"C:\Program Files\OpenVPN Connect\ovpnconnector.exe";

        private static async Task Main(string[] args)
        {
            await GetVPNServerList();
            ConnectVPN();
        }

        private static async Task GetVPNServerList()
        {
            try
            {
                List<ServerObject> _servers = new List<ServerObject>();

                HttpClient client = new HttpClient();
                var response = await client.GetAsync("https://www.vpngate.net/api/iphone/");
                var dataStream = await response.Content.ReadAsStreamAsync();

                using (StreamReader sr = new StreamReader(dataStream))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.StartsWith("*")) continue;
                        if (line.StartsWith("#")) continue;

                        var serverObject = new ServerObject(line.Split(','));

                        if (serverObject.CountryShort == "KR")
                            _servers.Add(serverObject);
                    }
                }

                if (_servers.Count != 0) ServerList = _servers;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static void ConnectVPN()
        {
            Console.WriteLine("try connect");

            var list = ServiceController.GetServices();
            var ovpnService = list.FirstOrDefault(x => x.ServiceName == "OVPNConnectorService");
            if (ovpnService == null)
            {
                //Install Service
                using (var proc = new Process())
                {
                    proc.StartInfo.FileName = openVpnExeFileName;
                    proc.StartInfo.Verb = "runas";
                    proc.StartInfo.Arguments = "install";
                    proc.Start();
                    proc.WaitForExit();
                }

                using (var proc = new Process())
                {
                    proc.StartInfo.FileName = openVpnExeFileName;
                    proc.StartInfo.Verb = "runas";
                    proc.StartInfo.Arguments = "set-config profile config.ovpn";
                    proc.Start();
                    proc.WaitForExit();
                }
            }
            else
            {
                //Stop Service
                if (ovpnService.Status == ServiceControllerStatus.Running)
                {
                    using (var proc = new Process())
                    {
                        proc.StartInfo.FileName = openVpnExeFileName;
                        proc.StartInfo.Verb = "runas";
                        proc.StartInfo.Arguments = "stop";
                        proc.Start();
                        proc.WaitForExit();
                    }
                }

                using (var proc = new Process())
                {
                    proc.StartInfo.FileName = openVpnExeFileName;
                    proc.StartInfo.Verb = "runas";
                    proc.StartInfo.Arguments = "set-config profile config.ovpn";
                    proc.Start();
                    proc.WaitForExit();
                }

                using (var proc = new Process())
                {
                    proc.StartInfo.FileName = openVpnExeFileName;
                    proc.StartInfo.Verb = "runas";
                    proc.StartInfo.Arguments = "start";
                    proc.Start();
                    proc.WaitForExit();
                }
            }
        }
    }

    internal class ServerObject
    {
        public string HostName;
        public string IP;
        public string Score;
        public string Ping;
        public string Speed;
        public string CountryLong;
        public string CountryShort;
        public string NumVpnSessions;
        public string Uptime;
        public string TotalUsers;
        public string TotalTraffic;
        public string LogType;
        public string Operator;
        public string Message;
        public string ConfigData;

        internal ServerObject(string[] data)
        {
            HostName = data[0];
            IP = data[1];
            Score = data[2];
            Ping = data[3];
            Speed = data[4];
            CountryLong = data[5];
            CountryShort = data[6];
            NumVpnSessions = data[7];
            Uptime = data[8];
            TotalUsers = data[9];
            TotalTraffic = data[10];
            LogType = data[11];
            Operator = data[12];
            Message = data[13];
            ConfigData = data[14];
        }
    }
}
