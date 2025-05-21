using RestaurantApp.Helpers;
using RestaurantApp.Services;
using System;
using System.Windows.Input;

namespace RestaurantApp.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly UserService _userService;
        private readonly Action _navigateToRegister;
        private readonly Action _onLoginSuccess;
        private string _email;
        private string _password;
        private string _errorMessage;

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

        public string ErrorMessage
        {
            get { return _errorMessage; }
            set { SetProperty(ref _errorMessage, value); }
        }

        public ICommand LoginCommand { get; }
        public ICommand RegisterNavigationCommand { get; }

        public LoginViewModel(UserService userService, Action navigateToRegister, Action onLoginSuccess)
        {
            _userService = userService;
            _navigateToRegister = navigateToRegister;
            _onLoginSuccess = onLoginSuccess;

            LoginCommand = new RelayCommand(ExecuteLogin, CanExecuteLogin);
            RegisterNavigationCommand = new RelayCommand(_ => _navigateToRegister());
        }

        private bool CanExecuteLogin(object parameter)
        {
            return !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Password);
        }

        private void ExecuteLogin(object parameter)
        {
            if (_userService.Login(Email, Password))
            {
                _onLoginSuccess();
            }
            else
            {
                ErrorMessage = "Invalid email or password.";
            }
        }
    }
}