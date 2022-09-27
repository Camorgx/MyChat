﻿using System.Text.Json;
using Utils;

namespace Server {
    public static class Database {
        private class Item {
            public User User { get; }
            public string Password { get; }
            public Item(User user, string password) {
                User = user;
                Password = password;
            }
        }
        private static Dictionary<int, Item> data = new();
        private static readonly string jsonFileName = "UserDataBase.json";
        private static readonly string maxIdFileName = "MaxId.txt";
        private static int currentMaxId = 0;
        public static void Store() {
            File.WriteAllText(maxIdFileName, currentMaxId.ToString());
            string jsonString = JsonSerializer.Serialize(data);
            File.WriteAllText(jsonFileName, jsonString);
        }
        public static int Load() {
            if (!File.Exists(jsonFileName) || !File.Exists(maxIdFileName)) 
                return 0;
            currentMaxId = int.Parse(File.ReadAllText(maxIdFileName));
            string jsonString = File.ReadAllText(jsonFileName);
            var newData = JsonSerializer.Deserialize<Dictionary<int, Item>>(jsonString);
            if (newData != null) data = newData;
            return data.Count;
        }
        public enum VerifyRes {
            Passed,
            UserNotExisted,
            WrongPassword
        }
        public static VerifyRes Verify(int userId, string password) {
            if (!data.ContainsKey(userId)) 
                return VerifyRes.UserNotExisted;
            Item item = data[userId];
            return (password == item.Password) 
                ? VerifyRes.Passed : VerifyRes.WrongPassword;
        }
        public static User Register(string name, string email, string password) {
            int id = ++currentMaxId;
            User user = new(name, email, id);
            data.Add(id, new Item(user, password));
            return user;
        }
    }
}