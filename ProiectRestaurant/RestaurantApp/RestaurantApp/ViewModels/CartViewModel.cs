using RestaurantApp.Helpers;
using RestaurantApp.Models;
using RestaurantApp.Services;
using System;
using System.Linq;
using System.Windows.Input;

namespace RestaurantApp.ViewModels
{
    public class CartViewModel : BaseViewModel
    {
        private readonly UserService _userService;
        private readonly OrderService _orderService;
        private ShoppingCart _cart;
        private string _message;

        public ShoppingCart Cart
        {
            get { return _cart; }
            set { SetProperty(ref _cart, value); }
        }

        public string Message
        {
            get { return _message; }
            set { SetProperty(ref _message, value); }
        }

        public bool CanCheckout => _userService.IsAuthenticated && Cart.Items.Count > 0;

        public ICommand CheckoutCommand { get; }
        public ICommand UpdateQuantityCommand { get; }
        public ICommand RemoveItemCommand { get; }
        public ICommand ClearCartCommand { get; }

        public CartViewModel(UserService userService, OrderService orderService, ShoppingCart cart)
        {
            _userService = userService;
            _orderService = orderService;
            Cart = cart;

            if (_userService.IsAuthenticated && Cart.Items.Count > 0)
            {
                _orderService.CalculateOrderCosts(Cart, _userService.CurrentUser);
            }

            CheckoutCommand = new RelayCommand(ExecuteCheckout, CanExecuteCheckout);
            UpdateQuantityCommand = new RelayCommand(ExecuteUpdateQuantity);
            RemoveItemCommand = new RelayCommand(ExecuteRemoveItem);
            ClearCartCommand = new RelayCommand(_ =>
            {
                Cart.Clear();
                OnPropertyChanged(nameof(CanCheckout));
            });
        }

        private bool CanExecuteCheckout(object parameter)
        {
            return CanCheckout;
        }

        private void ExecuteCheckout(object parameter)
        {
            try
            {
                if (!_userService.IsAuthenticated)
                {
                    Message = "Please log in to place an order.";
                    return;
                }

                if (string.IsNullOrEmpty(_userService.CurrentUser.DeliveryAddress))
                {
                    Message = "Please update your profile with a delivery address before checking out.";
                    return;
                }

                int orderId = _orderService.PlaceOrder(Cart, _userService.CurrentUser);

                if (orderId > 0)
                {
                    Message = $"Order placed successfully! Your order ID is: {orderId}";
                    Cart.Clear();
                    OnPropertyChanged(nameof(CanCheckout));
                }
            }
            catch (Exception ex)
            {
                Message = $"Error placing order: {ex.Message}";
            }
        }

        private void ExecuteUpdateQuantity(object parameter)
        {
            if (parameter is Tuple<int, int> update)
            {
                int productId = update.Item1;
                int change = update.Item2;

                var item = Cart.Items.FirstOrDefault(i => i.ProductId == productId);
                if (item != null)
                {
                    int newQuantity = item.Quantity + change;
                    if (newQuantity <= 0)
                    {
                        Cart.RemoveItem(productId);
                    }
                    else
                    {
                        Cart.UpdateItemQuantity(productId, newQuantity);
                    }

                    if (_userService.IsAuthenticated && Cart.Items.Count > 0)
                    {
                        _orderService.CalculateOrderCosts(Cart, _userService.CurrentUser);
                    }

                    OnPropertyChanged(nameof(CanCheckout));
                }
            }
        }

        private void ExecuteRemoveItem(object parameter)
        {
            if (parameter is int productId)
            {
                Cart.RemoveItem(productId);

                if (_userService.IsAuthenticated && Cart.Items.Count > 0)
                {
                    _orderService.CalculateOrderCosts(Cart, _userService.CurrentUser);
                }

                OnPropertyChanged(nameof(CanCheckout));
            }
        }
    }
}