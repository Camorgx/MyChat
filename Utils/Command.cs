namespace Utils {
    public class Command {
        public enum CommandType {
            Register,
            Login,
            Logout,
            SendMessage
        }
        public CommandType Type { get; }
        public User From { get; }
        public User Target { get; } = new();
        public string Message { get; } = "";
        public string Password { get; } = "";
        public Command(CommandType type, User from, string password) {
            if (type != CommandType.Register && type != CommandType.Login)
                throw new ArgumentException("Invalid CommandType.");
            Type = type;
            From = from;
            Password = password;
        }
        public Command(CommandType type, User from) {
            if (type != CommandType.Logout)
                throw new ArgumentException("Invalid CommandType.");
            Type = type;
            From = from;
        }
        public Command(CommandType type, User from, User target, string message) {
            if (type != CommandType.SendMessage)
                throw new ArgumentException("Invalid CommandType.");
            Type = type;
            From = from;
            Target = target;
            Message = message;
        }
    }
}
