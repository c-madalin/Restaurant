using RestaurantApp.Helpers;
using RestaurantApp.Models;
using RestaurantApp.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace RestaurantApp.ViewModels
{
    public class MenuViewModel : BaseViewModel
    {
        private readonly CategoryService _categoryService;
        private readonly ProductService _productService;
        private readonly ShoppingCart _cart;
        private ObservableCollection<CategoryWithProducts> _categories;
        private string _message;

        public ObservableCollection<CategoryWithProducts> Categories
        {
            get { return _categories; }
            set { SetProperty(ref _categories, value); }
        }

        public string Message
        {
            get { return _message; }
            set { SetProperty(ref _message, value); }
        }

        public ICommand AddToCartCommand { get; }

        public MenuViewModel(CategoryService categoryService, ProductService productService, ShoppingCart cart)
        {
            _categoryService = categoryService;
            _productService = productService;
            _cart = cart;

            AddToCartCommand = new RelayCommand(ExecuteAddToCart, CanExecuteAddToCart);

            LoadMenu();
        }

        private void LoadMenu()
        {
            var categories = _categoryService.GetAllCategories();
            var products = _productService.GetAllProducts();

            var categoriesWithProducts = categories.Select(c => new CategoryWithProducts
            {
                CategoryId = c.CategoryId,
                Name = c.Name,
                Description = c.Description,
                Products = new ObservableCollection<Product>(
                    products.Where(p => p.CategoryId == c.CategoryId).ToList())
            }).ToList();

            Categories = new ObservableCollection<CategoryWithProducts>(categoriesWithProducts);
        }

        private bool CanExecuteAddToCart(object parameter)
        {
            if (parameter is Product product)
            {
                return product.IsAvailable && product.TotalQuantity > 0;
            }
            return false;
        }

        private void ExecuteAddToCart(object parameter)
        {
            if (parameter is Product product)
            {
                _cart.AddItem(product);
                Message = $"Added {product.Name} to cart";

                System.Threading.Tasks.Task.Delay(3000).ContinueWith(_ =>
                {
                    if (Message == $"Added {product.Name} to cart")
                    {
                        Message = string.Empty;
                        OnPropertyChanged(nameof(Message));
                    }
                }, System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
            }
        }
    }

    public class CategoryWithProducts : Category
    {
        public ObservableCollection<Product> Products { get; set; } = new ObservableCollection<Product>();
    }
}