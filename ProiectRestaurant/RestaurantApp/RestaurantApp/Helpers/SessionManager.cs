using RestaurantApp.Models;
using System;
using System.IO;
using System.Text.Json;

namespace RestaurantApp.Helpers
{
    public static class SessionManager
    {
        private const string SessionFile = "session.json";

        public static void SaveSession(User user)
        {
            if (user == null)
                return;

            var session = new SessionData
            {
                Email = user.Email,
                PasswordHash = user.PasswordHash
            };

            var json = JsonSerializer.Serialize(session);
            File.WriteAllText(SessionFile, json);
        }

        public static SessionData LoadSession()
        {
            if (!File.Exists(SessionFile))
                return null;

            try
            {
                var json = File.ReadAllText(SessionFile);
                return JsonSerializer.Deserialize<SessionData>(json);
            }
            catch
            {
                return null;
            }
        }

        public static void ClearSession()
        {
            if (File.Exists(SessionFile))
                File.Delete(SessionFile);
        }

        public class SessionData
        {
            public string Email { get; set; }
            public string PasswordHash { get; set; }
        }
    }
}
