using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BCrypt.Net;
using Restaurant_app.Commands;
using Restaurant_app.Models;
using System.ComponentModel;
using System.Windows.Input;
using Restaurant_app.Services;

namespace Restaurant_app.ViewModels
{


    public class UserAuthViewModel : INotifyPropertyChanged
    {
        public User CurrentUser { get; set; } = new User();

        public ICommand RegisterCommand { get; }
        public ICommand LoginCommand { get; }

        public UserAuthViewModel()
        {
            RegisterCommand = new RelayCommand(_ => RegisterUser());
            LoginCommand = new RelayCommand(_ => LoginUser());
        }

        private void RegisterUser()
        {
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(CurrentUser.Password);

            // Aici folosești serviciul tău de DB pentru a salva utilizatorul
            UserDbService.InsertUser(CurrentUser.Email, hashedPassword, CurrentUser.FirstName, CurrentUser.LastName);
        }

        private void LoginUser()
        {
            string storedHash = UserDbService.GetPasswordHashByEmail(CurrentUser.Email);

            if (BCrypt.Net.BCrypt.Verify(CurrentUser.Password, storedHash))
            {
                // autentificare reușită
            }
            else
            {
                // parolă greșită
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

}
