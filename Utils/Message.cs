namespace Utils {
    public class Message {
        public string UserName { get; set; } = "";
        public string Words { get; set; } = "";
        public Message() { }
        public Message(string userName, string words) {
            UserName = userName;
            Words = words;
        }
        private static readonly string mutex = "";
        public void Display() {
            lock (mutex) {
                var origin = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.Write(UserName);
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write(": ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(Words);
                Console.ForegroundColor = origin;
            }
        }
    }
}
