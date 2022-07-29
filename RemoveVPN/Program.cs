using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoveVPN
{
    internal class Program
    {
        private static string vpnFilePath = @"C:\Program Files\OpenVPN Connect\ovpnconnector.exe";

        static void Main(string[] args)
        {
            StopVPNService();
            RemoveVPNService();
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
        
        private static void RemoveVPNService()
        {
            using (var proc = SudoProcess())
            {
                proc.StartInfo.FileName = vpnFilePath;
                proc.StartInfo.Arguments = "remove";
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
}
