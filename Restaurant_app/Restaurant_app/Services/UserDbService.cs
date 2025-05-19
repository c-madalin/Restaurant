using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace Restaurant_app.Services
{
    public static class UserDbService
    {
        private static readonly string connectionString =
            File.ReadAllText("appsettings.json") // sau folosind config provider
            .Split("\"DefaultConnection\": \"")[1].Split("\"")[0];

        public static void InsertUser(string email, string hash, string firstName, string lastName)
        {
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand("INSERT INTO users (email, password_hash, first_name, last_name) VALUES (@e, @p, @f, @l)", conn);
            cmd.Parameters.AddWithValue("e", email);
            cmd.Parameters.AddWithValue("p", hash);
            cmd.Parameters.AddWithValue("f", firstName);
            cmd.Parameters.AddWithValue("l", lastName);
            cmd.ExecuteNonQuery();
        }

        public static string GetPasswordHashByEmail(string email)
        {
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand("SELECT password_hash FROM users WHERE email = @e", conn);
            cmd.Parameters.AddWithValue("e", email);
            using var reader = cmd.ExecuteReader();

            return reader.Read() ? reader.GetString(0) : null;
        }
    }

}
