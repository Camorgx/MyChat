namespace Utils {
    public enum VerifyRes {
        Passed,
        UserNotExisted,
        WrongPassword
    }
    public class Command {
        public enum CommandType {
            Register,
            Login,
            Logout,
            CreateRoom,
            JoinRoom,
            LeaveRoom,
            SearchForUser,
            SearchForRoom,
            GetRoomInfo
        }
        public CommandType Type { get; set; }
        public User From { get; set; } = new();
        public User Target { get; set; } = new();
        public int RoomId { get; set; } = 0;
        public string Message { get; set; } = "";
        public string Password { get; set; } = "";
        public string Name { get; set; } = "";
        public Command() { }
        public Command(CommandType type, User from, string passwordOrName) {
            if (type != CommandType.Register && type != CommandType.Login
                && type != CommandType.CreateRoom && type != CommandType.SearchForUser
                && type != CommandType.SearchForRoom)
                throw new ArgumentException("Invalid CommandType.");
            Type = type;
            From = from;
            if (type == CommandType.Login || type == CommandType.Register)
                Password = passwordOrName;
            else Name = passwordOrName;
        }
        public Command(CommandType type, User from) {
            if (type != CommandType.Logout)
                throw new ArgumentException("Invalid CommandType.");
            Type = type;
            From = from;
        }
        public Command(CommandType type, User from, int roomId) {
            if (type != CommandType.JoinRoom && type != CommandType.LeaveRoom
                && type != CommandType.GetRoomInfo)
                throw new ArgumentException("Invalid CommandType.");
            Type = type;
            From = from;
            RoomId = roomId;
        }
    }
}
