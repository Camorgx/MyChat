using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Utils;

namespace Client {
    internal static class Client {
        private static User clientUser = new();
        private static Socket client 
            = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static Socket messageHandler
            = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static string serverIP = "";
        public static bool Init(string ip) {
            try {
                EndPoint endPoint = new IPEndPoint(IPAddress.Parse(ip), 8000);
                client.Connect(endPoint);
            }
            catch (Exception) {
                return false;
            }
            serverIP = ip;
            return true;
        }
        public static void End() {
            client.Shutdown(SocketShutdown.Both);
            client.Close();
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
            End();
        }
        public static int CreateRoom(string name) {
            Command command = new(Command.CommandType.CreateRoom,
                clientUser, name);
            client.Send(JsonSerializer.SerializeToUtf8Bytes(command));
            byte[] buffer = new byte[10240];
            int byteCnt = client.Receive(buffer);
            string jsonString = Encoding.UTF8.GetString(buffer, 0, byteCnt);
            int roomId = JsonSerializer.Deserialize<int>(jsonString);
            EndPoint endPoint = new IPEndPoint(IPAddress.Parse(serverIP), 8100);
            messageHandler.Connect(endPoint);
            messageHandler.Send(JsonSerializer.SerializeToUtf8Bytes((roomId, clientUser.Id)));
            return roomId;
        }
    }
}
