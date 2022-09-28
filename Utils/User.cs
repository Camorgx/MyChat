namespace Utils {
    public class User {
        public string Name { get; set; } = "Undefined";
        public string Email { get; set; } = "Undefined";
        public int Id { get; set; } = 0;
        public User() { }
        public User(string name, string email, int id = 0) {
            Name = name;
            Email = email;
            Id = id;
        }
        public override string ToString() {
            return $"ID: {Id}, Name: {Name}, Email: {Email}";
        }
        public static bool operator==(User a, User b) {
            return a.Id == b.Id;
        }
        public static bool operator!=(User a, User b) {
            return a.Id != b.Id;
        }
        public override bool Equals(object? obj) {
            if (ReferenceEquals(this, obj))
                return true;
            if (obj is null)
                return false;
            return this == (User)obj;
        }

        public override int GetHashCode() {
            return Id.GetHashCode();
        }
    }
}