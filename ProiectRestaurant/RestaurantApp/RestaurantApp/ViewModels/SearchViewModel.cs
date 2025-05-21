using RestaurantApp.Helpers;
using RestaurantApp.Models;
using RestaurantApp.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace RestaurantApp.ViewModels
{
    public class SearchViewModel : BaseViewModel
    {
        private readonly ProductService _productService;
        private readonly ShoppingCart _cart;
        private string _searchTerm;
        private bool _searchForAllergen;
        private bool _invertSearch;
        private ObservableCollection<Product> _searchResults;
        private string _message;

        public string SearchTerm
        {
            get { return _searchTerm; }
            set { SetProperty(ref _searchTerm, value); }
        }

        public bool SearchForAllergen
        {
            get { return _searchForAllergen; }
            set { SetProperty(ref _searchForAllergen, value); }
        }

        public bool InvertSearch
        {
            get { return _invertSearch; }
            set { SetProperty(ref _invertSearch, value); }
        }

        public ObservableCollection<Product> SearchResults
        {
            get { return _searchResults; }
            set { SetProperty(ref _searchResults, value); }
        }

        public string Message
        {
            get { return _message; }
            set { SetProperty(ref _message, value); }
        }

        public ICommand SearchCommand { get; }
        public ICommand AddToCartCommand { get; }

        public SearchViewModel(ProductService productService, ShoppingCart cart = null)
        {
            _productService = productService;
            _cart = cart ?? new ShoppingCart();

            SearchResults = new ObservableCollection<Product>();
            SearchCommand = new RelayCommand(ExecuteSearch, CanExecuteSearch);
            AddToCartCommand = new RelayCommand(ExecuteAddToCart, CanAddToCart);
        }

        private bool CanExecuteSearch(object parameter)
        {
            return !string.IsNullOrWhiteSpace(SearchTerm);
        }

        private void ExecuteSearch(object parameter)
        {
            var allProducts = _productService.GetAllProducts();
            IEnumerable<Product> results;

            if (SearchForAllergen)
            {
                if (InvertSearch)
                {
                    results = allProducts.Where(p => !p.Allergens.Any(a =>
                        a.Name.IndexOf(SearchTerm, StringComparison.OrdinalIgnoreCase) >= 0));
                }
                else
                {
                    results = allProducts.Where(p => p.Allergens.Any(a =>
                        a.Name.IndexOf(SearchTerm, StringComparison.OrdinalIgnoreCase) >= 0));
                }
            }
            else
            {
                if (InvertSearch)
                {
                    results = allProducts.Where(p =>
                        p.Name.IndexOf(SearchTerm, StringComparison.OrdinalIgnoreCase) < 0);
                }
                else
                {
                    results = allProducts.Where(p =>
                        p.Name.IndexOf(SearchTerm, StringComparison.OrdinalIgnoreCase) >= 0);
                }
            }

            SearchResults.Clear();
            foreach (var product in results)
            {
                SearchResults.Add(product);
            }
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