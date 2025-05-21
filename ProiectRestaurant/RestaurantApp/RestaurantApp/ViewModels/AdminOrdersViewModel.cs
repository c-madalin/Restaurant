using RestaurantApp.Helpers;
using RestaurantApp.Models;
using RestaurantApp.Services;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace RestaurantApp.ViewModels
{
    public class AdminOrdersViewModel : BaseViewModel
    {
        private readonly OrderService _orderService;
        private ObservableCollection<Order> _orders;
        private Order _selectedOrder;
        private bool _activeOrdersOnly;
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

        public bool ActiveOrdersOnly
        {
            get { return _activeOrdersOnly; }
            set
            {
                SetProperty(ref _activeOrdersOnly, value);
                LoadOrders();
            }
        }

        public string Message
        {
            get { return _message; }
            set { SetProperty(ref _message, value); }
        }

        public ICommand ChangeStatusCommand { get; }
        public ICommand RefreshCommand { get; }

        public AdminOrdersViewModel(OrderService orderService)
        {
            _orderService = orderService;
            _activeOrdersOnly = true;
            ChangeStatusCommand = new RelayCommand<string>(ExecuteChangeStatus);
            RefreshCommand = new RelayCommand(_ => LoadOrders());

            LoadOrders();
        }

        private void LoadOrders()
        {
            var orderList = _orderService.GetAllOrders(ActiveOrdersOnly);
            Orders = new ObservableCollection<Order>(orderList);
        }

        private void ExecuteChangeStatus(string newStatus)
        {
            if (SelectedOrder != null && !string.IsNullOrEmpty(newStatus))
            {
                bool success = false;

                if (newStatus == "Preparing" && SelectedOrder.Status == "Registered")
                {
                    success = _orderService.UpdateProductQuantities(SelectedOrder.OrderId);

                    if (!success)
                    {
                        Message = "Failed to update status: Not enough product quantity available.";
                        return;
                    }
                }

                success = _orderService.UpdateOrderStatus(SelectedOrder.OrderId, newStatus);

                if (success)
                {
                    Message = $"Order status changed to {newStatus} successfully.";
                    LoadOrders();
                }
                else
                {
                    Message = "Failed to update order status.";
                }
            }
        }
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Predicate<T> _canExecute;

        public RelayCommand(Action<T> execute, Predicate<T> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute((T)parameter);
        }

        public void Execute(object parameter)
        {
            _execute((T)parameter);
        }
    }
}