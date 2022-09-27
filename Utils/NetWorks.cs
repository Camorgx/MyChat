using System.Diagnostics;
using System.Net.Sockets;
using System.Net;
using System.Text.RegularExpressions;
using System.Text;

namespace Utils {
    public static class NetWorks {
        public static string GetLocalIP() {
            string result = RunApp("route", "print", true);
            Match m = Regex.Match(result, @"0.0.0.0\s+0.0.0.0\s+(\d+.\d+.\d+.\d+)\s+(\d+.\d+.\d+.\d+)");
            if (m.Success)
                return m.Groups[2].Value;
            else {
                try {
                    TcpClient c = new();
                    c.Connect("www.baidu.com", 80);
                    string ip = c.Client.LocalEndPoint is not IPEndPoint 
                        endPoint ? "" : endPoint.Address.ToString();
                    c.Close();
                    return ip;
                }
                catch (Exception) {
                    return "";
                }
            }
        }

        private static string RunApp(string filename, string arguments, bool recordLog) {
            try {
                if (recordLog)
                    Trace.WriteLine(filename + " " + arguments);
                Process proc = new();
                proc.StartInfo.FileName = filename;
                proc.StartInfo.CreateNoWindow = true;
                proc.StartInfo.Arguments = arguments;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.UseShellExecute = false;
                proc.Start();

                using StreamReader sr = new (proc.StandardOutput.BaseStream, Encoding.Default);
                Thread.Sleep(100);
                if (!proc.HasExited) proc.Kill();
                string txt = sr.ReadToEnd();
                sr.Close();
                if (recordLog)
                    Trace.WriteLine(txt);
                return txt;
            }
            catch (Exception ex) {
                Trace.WriteLine(ex);
                return ex.Message;
            }
        }
    }
}
