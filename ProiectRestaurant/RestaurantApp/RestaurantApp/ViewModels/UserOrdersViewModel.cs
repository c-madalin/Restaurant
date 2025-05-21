using RestaurantApp.Helpers;
using RestaurantApp.Models;
using RestaurantApp.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace RestaurantApp.ViewModels
{
    public class UserOrdersViewModel : BaseViewModel
    {
        private readonly UserService _userService;
        private readonly OrderService _orderService;
        private ObservableCollection<Order> _orders;
        private Order _selectedOrder;
        private string _message;

        public ObservableCollection<Order> Orders
        {
            get { return _orders; }
            set { SetProperty(ref _orders, value); }
        }

        public Order SelectedOrder
        {
            get { return _selectedOrder; }
            set { SetProperty(ref _selectedOrder, value); }
        }

        public string Message
        {
            get { return _message; }
            set { SetProperty(ref _message, value); }
        }

        public ICommand CancelOrderCommand { get; }
        public ICommand RefreshCommand { get; }

        public UserOrdersViewModel(UserService userService, OrderService orderService)
        {
            _userService = userService;
            _orderService = orderService;

            CancelOrderCommand = new RelayCommand(ExecuteCancelOrder, CanExecuteCancelOrder);
            RefreshCommand = new RelayCommand(_ => LoadOrders());

            LoadOrders();
        }

        private void LoadOrders()
        {
            if (_userService.IsAuthenticated)
            {
                var orderList = _orderService.GetUserOrders(_userService.CurrentUser.UserId);
                Orders = new ObservableCollection<Order>(orderList);
            }
        }

        private bool CanExecuteCancelOrder(object parameter)
        {
            if (parameter is Order order)
            {
                return order.Status == "Registered" || order.Status == "Preparing";
            }
            return false;
        }

        private void ExecuteCancelOrder(object parameter)
        {
            if (parameter is Order order)
            {
                if (_orderService.UpdateOrderStatus(order.OrderId, "Cancelled"))
                {
                    Message = "Order cancelled successfully.";
                    LoadOrders();
                }
                else
                {
                    Message = "Failed to cancel order.";
                }
            }
        }
    }
}