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
        private static readonly IList<Socket> clients = new List<Socket>();
        private static readonly IDictionary<Socket, User> userBySocket = new Dictionary<Socket, User>();
        private static readonly IDictionary<User, Socket> socketByUser = new Dictionary<User, Socket>();
        private static readonly IDictionary<int, RoomInfo> rooms = new Dictionary<int, RoomInfo>();
        private static readonly IDictionary<int, IList<Socket>> roomSockets = new Dictionary<int, IList<Socket>>(); 
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
            Thread waitForRooms = new(WaitForRooms);
            waitForClients.Start(server);
            waitForRooms.Start();
            return true;
        }
        private static void WaitForRooms() {
            while (true) {
                Socket client = messageHandler.Accept();
                byte[] buffer = new byte[10240];
                int byteCnt = client.Receive(buffer);
                string jsonString = Encoding.UTF8.GetString(buffer, 0, byteCnt);
                (int roomId, int userId) = JsonSerializer.Deserialize<(int, int)>(jsonString);
                if (!roomSockets.ContainsKey(roomId))
                    roomSockets[roomId] = new List<Socket>(roomId);
                roomSockets[roomId].Add(client);
                Thread handleMessage = new(HandleMessage);
                handleMessage.Start((client, roomId, userId));
            }
        }
        private static void HandleMessage(object? argv) {
            if (argv == null) return;
            (Socket client, int roomId, int userId) = ((Socket, int, int))argv;
            byte[] buffer = new byte[10240];
            while (true) {
                int byteCnt = client.Receive(buffer);
                string message = Encoding.UTF8.GetString(buffer, 0, byteCnt);
                foreach (Socket socket in roomSockets[roomId]) {
                    if (socket != client) {
                        socket.Send(
                            JsonSerializer.SerializeToUtf8Bytes(
                                new Message(userId, message)));
                    }
                }
            }
        }
        private static void GetClient(object? serv) {
            if (serv == null) return;
            Socket server = (Socket)serv;
            while (true) {
                Socket client = server.Accept();
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
                clients.Add(client);
                Thread waitForCommand = new(GetCommand);
                waitForCommand.Start(client);
            }
        }
        private static void GetCommand(object? cli) {
            if (cli == null) return;
            Socket client = (Socket)cli;
            byte[] buffer = new byte[10240];
            Command? command = null;
            while (true) {
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
                            lock (userBySocket)
                                userBySocket[client] = target;
                            lock (socketByUser)
                                socketByUser[target] = client;
                            client.Send(JsonSerializer.SerializeToUtf8Bytes(target));
                        }
                        break;
                    case Command.CommandType.Logout:
                        lock (socketByUser)
                            socketByUser.Remove(command.From);
                        lock (userBySocket)
                            userBySocket[client] = new User();
                        break;
                    case Command.CommandType.CreateRoom:
                        ++maxRoomId;
                        rooms.Add(maxRoomId, new RoomInfo(command.Name, maxRoomId, From));
                        client.Send(JsonSerializer.SerializeToUtf8Bytes(maxRoomId));
                        break;
                    case Command.CommandType.JoinRoom:
                        if (!rooms.ContainsKey(command.RoomId))
                            client.Send(JsonSerializer.SerializeToUtf8Bytes(false));
                        else {
                            rooms[command.RoomId].Members.Add(From);
                            client.Send(JsonSerializer.SerializeToUtf8Bytes(true));
                        }
                        break;
                    case Command.CommandType.LeaveRoom:
                        var members = rooms[command.RoomId].Members;
                        members.Remove(From);
                        if (members.Count == 0)
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
            var users = socketByUser.Keys;
            Console.WriteLine($"当前在线用户数目: {users.Count}");
            foreach(var user in users)
                Console.WriteLine(user.ToString());
        }
    }
}
