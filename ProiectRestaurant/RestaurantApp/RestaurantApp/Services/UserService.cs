using Npgsql;
using RestaurantApp.Helpers;
using RestaurantApp.Models;
using System;

namespace RestaurantApp.Services
{
    public class UserService
    {
        private readonly DatabaseService _databaseService;
        private User _currentUser;

        public User CurrentUser => _currentUser;
        public bool IsAuthenticated => _currentUser != null;
        public bool IsEmployee => IsAuthenticated && _currentUser.UserType == "Employee";
        public bool IsCustomer => IsAuthenticated && _currentUser.UserType == "Customer";

        public UserService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public bool Register(User user, string password)
        {
            using var connection = _databaseService.GetConnection();
            connection.Open();

            using (var checkCmd = new NpgsqlCommand("SELECT check_user_exists(@p_email)", connection))
            {
                checkCmd.Parameters.AddWithValue("p_email", user.Email);
                int exists = Convert.ToInt32(checkCmd.ExecuteScalar());
                if (exists > 0)
                    return false;
            }

            using (var cmd = new NpgsqlCommand("SELECT register_user(@p_firstname, @p_lastname, @p_email, @p_passwordhash, @p_phonenumber, @p_deliveryaddress, @p_usertype)", connection))
            {
                cmd.Parameters.AddWithValue("p_firstname", user.FirstName);
                cmd.Parameters.AddWithValue("p_lastname", user.LastName);
                cmd.Parameters.AddWithValue("p_email", user.Email);
                cmd.Parameters.AddWithValue("p_passwordhash", PasswordHasher.HashPassword(password));
                cmd.Parameters.AddWithValue("p_phonenumber", string.IsNullOrEmpty(user.PhoneNumber) ? DBNull.Value : user.PhoneNumber);
                cmd.Parameters.AddWithValue("p_deliveryaddress", string.IsNullOrEmpty(user.DeliveryAddress) ? DBNull.Value : user.DeliveryAddress);
                cmd.Parameters.AddWithValue("p_usertype", user.UserType);

                object result = cmd.ExecuteScalar();
                user.UserId = Convert.ToInt32(result);
                return user.UserId > 0;
            }
        }

        public bool Login(string email, string password)
        {
            using var connection = _databaseService.GetConnection();
            connection.Open();

            using var cmd = new NpgsqlCommand("SELECT userid, firstname, lastname, email, passwordhash, phonenumber, deliveryaddress, usertype FROM get_user_by_email(@p_email)", connection);
            cmd.Parameters.AddWithValue("p_email", email);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return false;

            string storedHash = reader["passwordhash"].ToString();
            if (!PasswordHasher.VerifyPassword(password, storedHash))
                return false;

            _currentUser = new User
            {
                UserId = Convert.ToInt32(reader["userid"]),
                FirstName = reader["firstname"].ToString(),
                LastName = reader["lastname"].ToString(),
                Email = reader["email"].ToString(),
                PasswordHash = storedHash,
                PhoneNumber = reader["phonenumber"] is DBNull ? null : reader["phonenumber"].ToString(),
                DeliveryAddress = reader["deliveryaddress"] is DBNull ? null : reader["deliveryaddress"].ToString(),
                UserType = reader["usertype"].ToString()
            };

            SessionManager.SaveSession(_currentUser);
            return true;
        }

        public bool TryAutoLogin()
        {
            var session = SessionManager.LoadSession();
            if (session == null)
                return false;

            using var connection = _databaseService.GetConnection();
            connection.Open();

            using var cmd = new NpgsqlCommand("SELECT * FROM get_user_by_email(@p_email)", connection);
            cmd.Parameters.AddWithValue("p_email", session.Email);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return false;

            if (reader["passwordhash"].ToString() != session.PasswordHash)
                return false;

            _currentUser = new User
            {
                UserId = Convert.ToInt32(reader["userid"]),
                FirstName = reader["firstname"].ToString(),
                LastName = reader["lastname"].ToString(),
                Email = reader["email"].ToString(),
                PasswordHash = session.PasswordHash,
                PhoneNumber = reader["phonenumber"] is DBNull ? null : reader["phonenumber"].ToString(),
                DeliveryAddress = reader["deliveryaddress"] is DBNull ? null : reader["deliveryaddress"].ToString(),
                UserType = reader["usertype"].ToString()
            };

            return true;
        }


        public void Logout()
        {
            _currentUser = null;
            SessionManager.ClearSession();
        }

    }
}
