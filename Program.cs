using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization.Formatters.Binary;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace OpenVPN
{
    internal class Program
    {
        private static List<ServerObject> ServerList;
        private static List<ServerObject> BanServerList;

        private static string configFilePath = @"config.ovpn";
        private static string vpnFilePath = @"C:\Program Files\OpenVPN Connect\ovpnconnector.exe";

        private static async Task Main(string[] args)
        {
            CloseVPN();
            StopVPNService();
            await GetVPNServerList();
            Read4080ServerList();
            MakeVPNConfig(SelectServer());
            ConnectVPN();
        }

        private static void StopVPNService()
        {
            using (var proc = SudoProcess())
            {
                proc.StartInfo.FileName = vpnFilePath;
                proc.StartInfo.Arguments = "stop";
                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                proc.WaitForExit();
            }
        }

        private static ServerObject SelectServer()
        {
            var serverList = ServerList.Where(x => !BanServerList.Exists(y => y.HostName == x.HostName))
                .OrderByDescending(x => x.Speed).ToList();

            Append4080ServerList(serverList.First());
            return serverList.First();
        }

        private static void Append4080ServerList(ServerObject serverObject)
        {
            BanServerList.Add(serverObject);
            BinaryFormatter binfmt = new BinaryFormatter();
            using (FileStream fs = new FileStream("banserver.dat", FileMode.Create))
            {
                binfmt.Serialize(fs, BanServerList);
            }
        }

        private static void Read4080ServerList()
        {
            if (File.Exists("banserver.dat"))
            {
                BinaryFormatter binfmt = new BinaryFormatter();
                using (FileStream rdr = new FileStream("banserver.dat", FileMode.Open))
                {
                    BanServerList = (List<ServerObject>)binfmt.Deserialize(rdr);
                }
            }
            else
            {
                BanServerList = new List<ServerObject>();
            }
        }

        private static void MakeVPNConfig(ServerObject serverObject)
        {
            var data = Convert.FromBase64String(serverObject.ConfigData);
            var dataString = Encoding.UTF8.GetString(data);

            File.WriteAllText("config.ovpn", dataString);
        }

        private static void CloseVPN()
        {
            var process = Process.GetProcessesByName("OpenVPNConnect").ToList();

            if (process.Count > 0)
            {
                process.ForEach(x => x.Kill());
            }
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
                    while ((line = await sr.ReadLineAsync()) != null)
                    {
                        if (line.StartsWith("*")) continue;
                        if (line.StartsWith("#")) continue;

                        var serverObject = new ServerObject(line.Split(','));

                        if (serverObject.CountryShort == "KR")
                            _servers.Add(serverObject);
                    }
                }

                if (_servers.Count != 0)
                {
                    ServerList = _servers;
                }
                else
                {
                    Console.WriteLine("vpn서버 받아오는 중 오류가 발생했습니다.");
                    Console.WriteLine("기존 서버 파일 탐색");
                    if (File.Exists("server.dat"))
                    {
                        Console.WriteLine("기존 서버데이터 파일을 사용합니다.");

                        ServerList = GetServerListFromFile();
                    }
                    else
                    {
                        Console.WriteLine("서버 파일이 없습니다.");
                    }
                }

                BinaryFormatter binfmt = new BinaryFormatter();
                using (FileStream fs = new FileStream("server.dat", FileMode.Create))
                {
                    binfmt.Serialize(fs, ServerList);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("vpn서버 받아오는 중 오류가 발생했습니다.");
                Console.WriteLine("기존 서버 파일 탐색");
                if (File.Exists("server.dat"))
                {
                    Console.WriteLine("기존 서버데이터 파일을 사용합니다.");

                    ServerList = GetServerListFromFile();
                }
                else
                {
                    Console.WriteLine("서버 파일이 없습니다.");
                }
            }
        }

        private static List<ServerObject> GetServerListFromFile()
        {
            var list = new List<ServerObject>();
            BinaryFormatter binfmt = new BinaryFormatter();
            using (FileStream rdr = new FileStream("server.dat", FileMode.Open))
            {
                list = (List<ServerObject>)binfmt.Deserialize(rdr);
            }

            return list;
        }



        private static void ConnectVPN()
        {
            var list = ServiceController.GetServices();
            var ovpnService = list.FirstOrDefault(x => x.ServiceName == "OVPNConnectorService");
            if (ovpnService == null)
            {
                //Install Service
                InstallVPNService();

                DeleteConfigVPNService();

                SetConfigVPNService();
            }

            //Stop Service
            if (ovpnService.Status == ServiceControllerStatus.Running)
            {
                StopVPNService();
            }

            DeleteConfigVPNService();

            SetConfigVPNService();

            StartVPNService();

        }

        private static void StartVPNService()
        {
            using (var proc = SudoProcess())
            {
                proc.StartInfo.FileName = vpnFilePath;
                proc.StartInfo.Arguments = "start";
                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                proc.WaitForExit();
            }
        }

        private static void DeleteConfigVPNService()
        {
            using (var proc = SudoProcess())
            {
                proc.StartInfo.FileName = vpnFilePath;
                proc.StartInfo.Arguments = $"unset-config profile";
                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                proc.WaitForExit();
            }
        }

        private static void SetConfigVPNService()
        {
            using (var proc = SudoProcess())
            {
                proc.StartInfo.FileName = vpnFilePath;
                proc.StartInfo.Arguments = $"set-config profile config.ovpn";
                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                proc.WaitForExit();
            }
        }

        private static void InstallVPNService()
        {
            using (var proc = SudoProcess())
            {
                proc.StartInfo.FileName = vpnFilePath;
                proc.StartInfo.Arguments = "install";
                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                proc.WaitForExit();
            }
        }

        private static Process SudoProcess()
        {
            Process proc = new Process();
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.Verb = "runas";
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.RedirectStandardInput = true;
            proc.StartInfo.UseShellExecute = false;
            
            proc.OutputDataReceived += (sender, args) => Console.WriteLine(args.Data);
            proc.ErrorDataReceived += (sender, args) => Console.WriteLine(args.Data);

            return proc;
        }
    }

    [Serializable]
    public class ServerObject
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

        public ServerObject(string[] data)
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
