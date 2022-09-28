using Utils;

namespace Client {
    internal class Program {
        static void Main(string[] args) {
            Console.WriteLine("MyChat Client v1.0");
            Console.WriteLine("欢迎使用 MyChat!");
            Console.WriteLine($"当前 IP: {NetWorks.GetLocalIP()}");
            string? ip;
            while(true) {
                Console.Write("连接至服务器：");
                ip = Console.ReadLine();
                while (ip == null) ip = Console.ReadLine();
                bool test = Client.Init(ip);
                if (test) break;
                else Console.WriteLine("连接服务器失败");
            }
            Console.WriteLine("连接服务器成功");
            HandleInput();
        }
        static string? Input(ConsoleColor color) {
            var origin = Console.ForegroundColor;
            Console.ForegroundColor = color;
            string? input = Console.ReadLine();
            Console.ForegroundColor = origin;
            return input;
        }
        static void HandleInput() {
            while (true) {
                string? input = Input(ConsoleColor.Cyan);
                if (input == "") continue;
                if (input == "shutdown") {
                    Client.End();
                    break;
                }
                else if (input == "register")
                    Register();
                else Console.WriteLine("请检查输入。");
            }
        }
        static void Register() {
            string name, email, password;
            var GetName = () => {
                Console.Write("昵称: ");
                string? name = Input(ConsoleColor.Yellow);
                while (name == "") {
                    Console.WriteLine("昵称不能为空，请重新输入。");
                    Console.Write("昵称: ");
                    name = Input(ConsoleColor.Yellow);
                }
                return name;
            };
            var GetEmail = () => {
                Console.Write("邮箱: ");
                string? email = Input(ConsoleColor.Yellow);
                if (email == "") return "null";
                return email;
            };
            var GetPassWord = () => {
                Console.Write("密码: ");
                string? password = Input(ConsoleColor.Yellow);
                while (password == "") {
                    Console.WriteLine("密码不能为空，请重新输入。");
                    Console.Write("密码: ");
                    password = Input(ConsoleColor.Yellow);
                }
                return password;
            };
            name = GetName();
            email = GetEmail();
            password = GetPassWord();
            int res;
            do {
                Console.WriteLine("请确认您的注册信息：");
                Console.WriteLine($"昵称: {name}");
                Console.WriteLine($"邮箱: {email}");
                Console.WriteLine($"密码: {password}");
                Console.WriteLine("是否需要修改？");
                Console.WriteLine("0. 确认");
                Console.WriteLine("1. 昵称");
                Console.WriteLine("2. 邮箱");
                Console.WriteLine("3. 密码");
                Console.WriteLine("4. 取消注册");
                bool state = int.TryParse(Input(ConsoleColor.Cyan), out res);
                if (!state || res == 4) {
                    Console.WriteLine("取消注册");
                    return;
                }
                switch (res) {
                    case 1:
                        name = GetName();
                        break;
                    case 2:
                        email = GetEmail();
                        break;
                    case 3:
                        password = GetPassWord();
                        break;
                    default:
                        break;
                }
            } while (res != 0);
            int id = Client.Register(name, email, password);
            Console.WriteLine($"您的 ID 为 {id}。");
        }
    }
}
