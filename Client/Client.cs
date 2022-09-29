using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Utils;

namespace Client {
    internal static class Client {
        private static User clientUser = new();
        private static readonly Socket client
            = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static readonly Socket messageHandler
            = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static int currentRoomId = 0;
        public static bool Init(string ip) {
            try {
                EndPoint endPoint = new IPEndPoint(IPAddress.Parse(ip), 8000);
                EndPoint message = new IPEndPoint(IPAddress.Parse(ip), 8100);
                client.Connect(endPoint);
                messageHandler.Connect(message);
            }
            catch (Exception) {
                return false;
            }
            return true;
        }
        public static void End() {
            client.Shutdown(SocketShutdown.Both);
            client.Close();
            messageHandler.Shutdown(SocketShutdown.Both);
            messageHandler.Close();
        }
        public static int Register(string name, string email, string password) {
            Command command = new(Command.CommandType.Register,
                new(name, email), password);
            client.Send(JsonSerializer.SerializeToUtf8Bytes(command));
            byte[] buffer = new byte[10240];
            int byteCnt = client.Receive(buffer);
            string jsonString = Encoding.UTF8.GetString(buffer, 0, byteCnt);
            User? res = JsonSerializer.Deserialize<User>(jsonString);
            if (res is null)
                throw new ApplicationException("Invalid User object from the server");
            clientUser = res;
            return res.Id;
        }
        public static VerifyRes Login(int id, string password) {
            Command command = new(Command.CommandType.Login,
                new("", "", id), password);
            client.Send(JsonSerializer.SerializeToUtf8Bytes(command));
            byte[] buffer = new byte[10240];
            int byteCnt = client.Receive(buffer);
            string jsonString = Encoding.UTF8.GetString(buffer, 0, byteCnt);
            VerifyRes res = JsonSerializer.Deserialize<VerifyRes>(jsonString);
            if (res == VerifyRes.Passed) {
                byteCnt = client.Receive(buffer);
                jsonString = Encoding.UTF8.GetString(buffer, 0, byteCnt);
                User? user = JsonSerializer.Deserialize<User>(jsonString);
                if (user is null)
                    throw new ApplicationException("Invalid User object from the server");
                clientUser = user;
            }
            return res;
        }
        public static void Logout() {
            Command command = new(Command.CommandType.Logout, clientUser);
            client.Send(JsonSerializer.SerializeToUtf8Bytes(command));
        }
        private static bool inRoom = false;
        /// <summary>
        /// 只创建房间而不将创建者加入房间
        /// 随后需要立即调用 JoinRoom
        /// </summary>
        public static int CreateRoom(string name) {
            Command command = new(Command.CommandType.CreateRoom,
                clientUser, name);
            client.Send(JsonSerializer.SerializeToUtf8Bytes(command));
            byte[] buffer = new byte[10240];
            int byteCnt = client.Receive(buffer);
            string jsonString = Encoding.UTF8.GetString(buffer, 0, byteCnt);
            return JsonSerializer.Deserialize<int>(jsonString);
        }
        public static bool JoinRoom(int id) {
            Command command = new(Command.CommandType.JoinRoom, clientUser, id);
            client.Send(JsonSerializer.SerializeToUtf8Bytes(command));
            byte[] buffer = new byte[1024];
            int byteCnt = client.Receive(buffer);
            string jsonString = Encoding.UTF8.GetString(buffer, 0, byteCnt);
            bool res = JsonSerializer.Deserialize<bool>(jsonString);
            if (res) {
                inRoom = true;
                currentRoomId = id;
                Thread messageThread = new(ReceiveMessage);
                messageThread.Start();
            }
            return res;
        }
        public static void ReceiveMessage() {
            byte[] buffer = new byte[10240];
            while (true) {
                if (!inRoom) return;
                try {
                    int byteCnt = messageHandler.Receive(buffer);
                    string jsonString = Encoding.UTF8.GetString(buffer, 0, byteCnt);
                    Message? message = JsonSerializer.Deserialize<Message>(jsonString);
                    if (message is null) continue;
                    if (message.Words == "$Join") {
                        var color = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"{message.UserName} 加入了房间。");
                        Console.ForegroundColor = color;
                    }
                    else message.Display();
                }
                catch (Exception) {
                    return;
                }
            }
        }
        public static void LeaveRoom() {
            Command command = new(Command.CommandType.LeaveRoom,
                clientUser, currentRoomId);
            client.Send(JsonSerializer.SerializeToUtf8Bytes(command));
            inRoom = false;
        }
        public static void SendMessage(string message) {
            messageHandler.Send(Encoding.UTF8.GetBytes(message));
        }
        public static RoomRes GetRoomInfo() {
            Command command = new(Command.CommandType.GetRoomInfo,
                clientUser, currentRoomId);
            client.Send(JsonSerializer.SerializeToUtf8Bytes(command));
            byte[] buffer = new byte[10240];
            int byteCnt = client.Receive(buffer);
            string jsonString = Encoding.UTF8.GetString(buffer, 0, byteCnt);
            RoomRes? res = JsonSerializer.Deserialize<RoomRes>(jsonString);
            if (res is null)
                throw new ApplicationException("Invalid RoomRes object from the server");
            return res;
        }
        public static (bool, IList<User>) SearchUser(string name) {
            Command command = new(Command.CommandType.SearchForUser,
                clientUser, name);
            client.Send(JsonSerializer.SerializeToUtf8Bytes(command));
            byte[] buffer = new byte[10240];
            int byteCnt = client.Receive(buffer);
            string jsonString = Encoding.UTF8.GetString(buffer, 0, byteCnt);
            bool res = JsonSerializer.Deserialize<bool>(jsonString);
            IList<User>? users;
            if (res) {
                byteCnt = client.Receive(buffer);
                jsonString = Encoding.UTF8.GetString(buffer, 0, byteCnt);
                users = JsonSerializer.Deserialize<IList<User>>(jsonString);
                if (users is null)
                    throw new ApplicationException("Invalid object from the server");
                return (res, users);
            }
            else return (res, new List<User>());
        }
        public static (bool, IList<RoomRes>) SearchRoom(string name) {
            Command command = new(Command.CommandType.SearchForRoom,
                clientUser, name);
            client.Send(JsonSerializer.SerializeToUtf8Bytes(command));
            byte[] buffer = new byte[10240];
            int byteCnt = client.Receive(buffer);
            string jsonString = Encoding.UTF8.GetString(buffer, 0, byteCnt);
            bool res = JsonSerializer.Deserialize<bool>(jsonString);
            IList<RoomRes>? rooms;
            if (res) {
                byteCnt = client.Receive(buffer);
                jsonString = Encoding.UTF8.GetString(buffer, 0, byteCnt);
                rooms = JsonSerializer.Deserialize<IList<RoomRes>>(jsonString);
                if (rooms is null)
                    throw new ApplicationException("Invalid object from the server");
                return (res, rooms);
            }
            else return (res, new List<RoomRes>());
        }
    }
}
