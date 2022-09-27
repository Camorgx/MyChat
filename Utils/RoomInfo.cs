namespace Utils {
    public class RoomInfo {
        public string Name { get; set; } = "";
        public int Id { get; set; } = 0;
        public IList<User> Members { get; set; } = new List<User>(); 
        public override string ToString() {
            string head = $"ID: {Id}, Count: {Members.Count}";
            string users = "";
            foreach (User user in Members)
                users += user.ToString() + "\n";
            return head + users;
        }
        public RoomInfo(string name, int id, User initUser) {
            Name = name;
            Id = id;
            Members = new List<User>() { initUser };
        }
    }
}
