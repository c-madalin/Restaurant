using RestaurantApp.Helpers;
using RestaurantApp.Models;
using RestaurantApp.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace RestaurantApp.ViewModels
{
    public class StockAlertViewModel : BaseViewModel
    {
        private readonly ProductService _productService;
        private ObservableCollection<Product> _lowStockProducts;

        public ObservableCollection<Product> LowStockProducts
        {
            get { return _lowStockProducts; }
            set { SetProperty(ref _lowStockProducts, value); }
        }

        public ICommand RefreshCommand { get; }

        public StockAlertViewModel(ProductService productService)
        {
            _productService = productService;

            RefreshCommand = new RelayCommand(_ => LoadLowStockProducts());

            LoadLowStockProducts();
        }

        private void LoadLowStockProducts()
        {
            var products = _productService.GetLowStockProducts(AppSettings.LowStockThreshold);
            LowStockProducts = new ObservableCollection<Product>(products);
        }
    }
}