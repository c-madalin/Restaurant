using RestaurantApp.Helpers;
using RestaurantApp.Models;
using RestaurantApp.Services;
using System;
using System.Windows.Input;

namespace RestaurantApp.ViewModels
{
    public class RegisterViewModel : BaseViewModel
    {
        private readonly UserService _userService;
        private readonly Action _navigateToLogin;
        private readonly Action _navigateToMain;
        private string _firstName;
        private string _lastName;
        private string _email;
        private string _password;
        private string _confirmPassword;
        private string _phoneNumber;
        private string _deliveryAddress;
        private string _errorMessage;

        public string FirstName
        {
            get { return _firstName; }
            set { SetProperty(ref _firstName, value); }
        }

        public string LastName
        {
            get { return _lastName; }
            set { SetProperty(ref _lastName, value); }
        }

        public string Email
        {
            get { return _email; }
            set { SetProperty(ref _email, value); }
        }

        public string Password
        {
            get { return _password; }
            set { SetProperty(ref _password, value); }
        }

        public string ConfirmPassword
        {
            get { return _confirmPassword; }
            set { SetProperty(ref _confirmPassword, value); }
        }

        public string PhoneNumber
        {
            get { return _phoneNumber; }
            set { SetProperty(ref _phoneNumber, value); }
        }

        public string DeliveryAddress
        {
            get { return _deliveryAddress; }
            set { SetProperty(ref _deliveryAddress, value); }
        }

        public string ErrorMessage
        {
            get { return _errorMessage; }
            set { SetProperty(ref _errorMessage, value); }
        }

        public ICommand RegisterCommand { get; }
        public ICommand LoginNavigationCommand { get; }

        public RegisterViewModel(UserService userService, Action navigateToLogin, Action navigateToMain)
        {
            _userService = userService;
            _navigateToLogin = navigateToLogin;
            _navigateToMain = navigateToMain;

            RegisterCommand = new RelayCommand(ExecuteRegister, CanExecuteRegister);
            LoginNavigationCommand = new RelayCommand(_ => _navigateToLogin());
        }

        private bool CanExecuteRegister(object parameter)
        {
            return !string.IsNullOrWhiteSpace(FirstName) &&
                   !string.IsNullOrWhiteSpace(LastName) &&
                   !string.IsNullOrWhiteSpace(Email) &&
                   !string.IsNullOrWhiteSpace(Password) &&
                   !string.IsNullOrWhiteSpace(ConfirmPassword);
        }

        private void ExecuteRegister(object parameter)
        {
            if (Password != ConfirmPassword)
            {
                ErrorMessage = "Passwords do not match.";
                return;
            }

            var user = new User
            {
                FirstName = FirstName,
                LastName = LastName,
                Email = Email,
                PhoneNumber = PhoneNumber,
                DeliveryAddress = DeliveryAddress,
                UserType = "Customer"
            };

            if (_userService.Register(user, Password))
            {
                if (_userService.Login(Email, Password))
                {
                    _navigateToMain();
                }
            }
            else
            {
                ErrorMessage = "Registration failed. Email may already be in use.";
            }
        }
    }
}