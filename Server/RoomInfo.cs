namespace Server {
    public class RoomInfo {
        public string Name { get; set; } = "";
        public int Id { get; set; } = 0;
        public ISet<int> Members { get; set; } = new HashSet<int>(); 
        public override string ToString() {
            string head = $"ID: {Id}, Count: {Members.Count}\n";
            string users = "";
            foreach (int userId in Members)
                users += Database.GetUser(userId).ToString() + "\n";
            return head + users;
        }
        public RoomInfo(string name, int id) {
            Name = name;
            Id = id;
        }
    }
}
