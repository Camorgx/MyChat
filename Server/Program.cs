using Utils;

namespace Server {
    internal class Program {
        static void Main(string[] args) {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("MyChat Server v1.0");
            Console.WriteLine("欢迎使用 MyChat!");
            Console.WriteLine($"读取了 {Database.Load()} 个用户。");
            bool state = Server.Init();
            if (!state) return;
            Console.WriteLine("服务端已启动，IP：" 
                + NetWorks.GetLocalIP());
            HandleInput();
            Database.Store();
            Console.WriteLine("服务端已关闭。");
            Environment.Exit(0);
        }
        static void HandleInput() {
            while (true) {
                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Cyan;
                string? input = Console.ReadLine();
                Console.ForegroundColor = color;
                if (input == null || input == "") continue;
                var inputs = input.Split();
                if (input == "shutdown") break;
                else if (inputs[0] == "list") {
                    if (inputs.Length == 1) Console.WriteLine("请检查输入。");
                    else if (inputs[1] == "users") {
                        if (inputs.Length == 3 && inputs[2] == "-a") {
                            var users = Database.GetUsers();
                            Console.WriteLine($"已注册用户数目：{users.Count}");
                            foreach (User user in users)
                                Console.WriteLine(user.ToString());
                        }
                        else if (inputs.Length == 2)
                            Server.DisplayUsers();
                        else Console.WriteLine("请检查输入。");
                    }
                    else if (inputs[1] == "rooms") Server.DisplayRooms();
                    else Console.WriteLine("请检查输入。");
                }
                else Console.WriteLine("请检查输入。");
            }
        }
    }
}