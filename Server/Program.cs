using System.Net.Sockets;

namespace Server {
    internal class Program {
        static void Main(string[] args) {
            Console.WriteLine("MyChat Server v1.0");
            Console.WriteLine("欢迎使用 MyChat!");
            Database.Register("TestUser", "TestEmail", "TestPassword");
            Console.WriteLine($"读取了 {Database.Load()} 个用户。");
            Socket socket = new Socket(AddressFamily.InterNetwork, 
                SocketType.Stream, ProtocolType.Tcp);
            Console.WriteLine("服务端已启动，IP：" 
                + Utils.NetWorks.GetLocalIP());
        }
    }
}