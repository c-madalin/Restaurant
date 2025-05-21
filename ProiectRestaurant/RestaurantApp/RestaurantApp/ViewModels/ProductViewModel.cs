using RestaurantApp.Helpers;
using RestaurantApp.Models;
using RestaurantApp.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace RestaurantApp.ViewModels
{
    public class ProductViewModel : BaseViewModel
    {
        private readonly ProductService _productService;
        private readonly ShoppingCart _cart;
        private ObservableCollection<Product> _products;
        private Product _selectedProduct;
        private string _message;

        public ObservableCollection<Product> Products
        {
            get { return _products; }
            set { SetProperty(ref _products, value); }
        }

        public Product SelectedProduct
        {
            get { return _selectedProduct; }
            set { SetProperty(ref _selectedProduct, value); }
        }

        public string Message
        {
            get { return _message; }
            set { SetProperty(ref _message, value); }
        }

        public ICommand RefreshCommand { get; }
        public ICommand AddToCartCommand { get; }

        public ProductViewModel(ProductService productService, ShoppingCart cart = null)
        {
            _productService = productService;
            _cart = cart ?? new ShoppingCart();

            RefreshCommand = new RelayCommand(_ => LoadProducts());
            AddToCartCommand = new RelayCommand(ExecuteAddToCart, CanAddToCart);

            LoadProducts();
        }

        private void LoadProducts()
        {
            var productList = _productService.GetAllProducts();
            Products = new ObservableCollection<Product>(productList);
        }

        private bool CanAddToCart(object parameter)
        {
            if (parameter is Product product)
            {
                return product.IsAvailable && product.TotalQuantity > 0;
            }
            return false;
        }

        private void ExecuteAddToCart(object parameter)
        {
            if (_cart != null && parameter is Product product)
            {
                _cart.AddItem(product);
                Message = $"Added {product.Name} to cart";
            }
        }
    }
}