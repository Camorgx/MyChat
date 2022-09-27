namespace Utils {
    public class User {
        public string Name { get; set; } = "Undefined";
        public string Email { get; set; } = "Undefined";
        public int Id { get; set; } = 0;
        public User() { }
        public User(string name, string email, int id) {
            Name = name;
            Email = email;
            Id = id;
        }
    }
}