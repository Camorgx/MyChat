using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Utils;

namespace Server {
    internal static class Server {
        private static readonly Socket server
            = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static readonly Socket messageHandler
            = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static readonly IDictionary<int, (Socket, Socket)> socketsByUser = new Dictionary<int, (Socket, Socket)>();
        private static readonly IDictionary<int, RoomInfo> rooms = new Dictionary<int, RoomInfo>();
        private static readonly IDictionary<int, int> roomByUser = new Dictionary<int, int>();
        private static int maxRoomId = 0;
        public static bool Init() {
            EndPoint serverEnd = new IPEndPoint(IPAddress.Any, 8000);
            EndPoint messageEnd = new IPEndPoint(IPAddress.Any, 8100);
            try {
                server.Bind(serverEnd);
                messageHandler.Bind(messageEnd);
            }
            catch (SocketException) {
                Console.WriteLine("服务端启动失败，同一时刻您只能启动一个服务端。");
                return false;
            }
            server.Listen(1024);
            messageHandler.Listen(1024);
            Thread waitForClients = new(GetClient);
            waitForClients.Start(server);
            return true;
        }
        private static void HandleMessage(object? argv) {
            if (argv == null) return;
            int userId = (int)argv;
            string userName = Database.GetUser(userId).Name;
            byte[] buffer = new byte[10240];
            ISet<int> targetUsers = rooms[roomByUser[userId]].Members;
            Socket clientMessage = socketsByUser[userId].Item2;
            string message;
            while (true) {
                if (!roomByUser.ContainsKey(userId)) return;
                try {
                    int byteCnt = clientMessage.Receive(buffer);
                    message = Encoding.UTF8.GetString(buffer, 0, byteCnt);
                    if (message == "") continue;
                }
                catch (Exception) {
                    return;
                }
                foreach (int targetId in targetUsers) {
                    if (targetId == userId) continue;
                    Socket targetSocket = socketsByUser[targetId].Item2;
                    targetSocket.Send(JsonSerializer.SerializeToUtf8Bytes(
                                new Message(userName, message)));
                }
            }
        }
        private static void GetClient(object? serv) {
            if (serv == null) return;
            Socket server = (Socket)serv;
            while (true) {
                Socket client = server.Accept();
                Socket clientMessage = messageHandler.Accept();
                IPEndPoint? clientIpE = null; 
                if (client.RemoteEndPoint != null)
                clientIpE = (IPEndPoint)client.RemoteEndPoint;
                if (clientIpE != null) {
                    var color = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("接收到新的客户端连接请求，IP: {0}", 
                        clientIpE.Address.ToString());
                    Console.ForegroundColor = color;
                }
                Thread waitForCommand = new(GetCommand);
                waitForCommand.Start((client, clientMessage));
            }
        }
        private static void GetCommand(object? argv) {
            if (argv == null) return;
            (Socket client, Socket clientMessage) = ((Socket, Socket))argv;
            byte[] buffer = new byte[10240];
            while (true) {
                Command? command;
                try {
                    int byteCnt = client.Receive(buffer);
                    string jsonString = Encoding.UTF8.GetString(buffer, 0, byteCnt);
                    command = JsonSerializer.Deserialize<Command>(jsonString);
                }
                catch (Exception) {
                    IPEndPoint? clientIpE = null;
                    if (client.RemoteEndPoint != null)
                        clientIpE = (IPEndPoint)client.RemoteEndPoint;
                    if (clientIpE != null) {
                        var color = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("客户端断开连接，IP: {0}",
                            clientIpE.Address.ToString());
                        Console.ForegroundColor = color;
                    }
                    return;
                }
                if (command == null)
                    throw new ArgumentException("Null object received.");
                User From = command.From;
                switch (command.Type) {
                    case Command.CommandType.Register:
                        User resUser
                            = Database.Register(From.Name, From.Email, command.Password);
                        client.Send(JsonSerializer.SerializeToUtf8Bytes(resUser));
                        break;
                    case Command.CommandType.Login:
                        VerifyRes verifyRes = Database.Verify(From.Id, command.Password);
                        client.Send(JsonSerializer.SerializeToUtf8Bytes(verifyRes));
                        if (verifyRes == VerifyRes.Passed) {
                            User target = Database.GetUser(From.Id);
                            lock (socketsByUser)
                                socketsByUser[From.Id] = (client, clientMessage);
                            client.Send(JsonSerializer.SerializeToUtf8Bytes(target));
                        }
                        break;
                    case Command.CommandType.Logout:
                        lock (socketsByUser)
                            socketsByUser.Remove(command.From.Id);
                        break;
                    case Command.CommandType.CreateRoom:
                        // 只创建房间，而不向房间内添加任何成员
                        // 随后 Client 应当再次发出加入房间的请求
                        ++maxRoomId;
                        rooms.Add(maxRoomId, new RoomInfo(command.Name, maxRoomId));
                        client.Send(JsonSerializer.SerializeToUtf8Bytes(maxRoomId));
                        break;
                    case Command.CommandType.JoinRoom:
                        if (!rooms.ContainsKey(command.RoomId))
                            client.Send(JsonSerializer.SerializeToUtf8Bytes(false));
                        else {
                            rooms[command.RoomId].Members.Add(From.Id);
                            roomByUser[From.Id] = command.RoomId;
                            Thread thread = new(HandleMessage);
                            thread.Start(From.Id);
                            client.Send(JsonSerializer.SerializeToUtf8Bytes(true));
                        }
                        break;
                    case Command.CommandType.LeaveRoom:
                        var roomMembers = rooms[command.RoomId].Members;
                        roomMembers.Remove(From.Id);
                        roomByUser.Remove(From.Id);
                        if (roomMembers.Count == 0)
                            rooms.Remove(command.RoomId);
                        break;
                    case Command.CommandType.SearchForUser:
                        var users = Database.SearchForUser(command.Name);
                        client.Send(JsonSerializer.SerializeToUtf8Bytes(users));
                        break;
                    case Command.CommandType.SearchForRoom:
                        IList<RoomInfo> res = new List<RoomInfo>();
                        foreach (var pair in rooms)
                            if (pair.Value.Name == command.Name)
                                res.Add(pair.Value);
                        client.Send(JsonSerializer.SerializeToUtf8Bytes(res));
                        break;
                    default: break;
                }
            }
        } 
        public static void DisplayRooms() {
            Console.WriteLine($"现有聊天室数目：{rooms.Count}");
            foreach (var pair in rooms)
                Console.WriteLine(pair.Value.ToString());
        }
        public static void DisplayUsers() {
            var users = socketsByUser.Keys;
            Console.WriteLine($"当前在线用户数目: {users.Count}");
            foreach(int userId in users)
                Console.WriteLine(Database.GetUser(userId).ToString());
        }
    }
}
