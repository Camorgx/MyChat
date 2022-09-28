namespace Utils {
    public class Message {
        public int UserId { get; set; } = 0;
        public string Words { get; set; } = "";
        public Message() { }
        public Message(int userId, string words) {
            UserId = userId;
            Words = words;
        }
    }
}
