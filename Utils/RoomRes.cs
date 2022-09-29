namespace Utils {
    public class RoomRes {
        public int Count { get; set; } = 0;
        public IList<User> Users { get; set; } = new List<User>();
        public string Name { get; set; } = "";
        public int Id { get; set; } = 0;
        public override string ToString() {
            string ans = "";
            ans += $"房间名: {Name}" + "\n";
            ans += $"ID: {Id}" + "\n";
            ans += $"房间内的用户数目：{Count}" + "\n";
            ans += "用户列表: " + "\n";
            foreach (var member in Users)
                ans += member.ToString() + "\n";
            return ans;
        }
    }
}
