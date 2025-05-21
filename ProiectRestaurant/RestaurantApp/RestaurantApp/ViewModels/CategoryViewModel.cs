using RestaurantApp.Helpers;
using RestaurantApp.Models;
using RestaurantApp.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace RestaurantApp.ViewModels
{
    public class CategoryViewModel : BaseViewModel
    {
        private readonly CategoryService _categoryService;
        private ObservableCollection<Category> _categories;
        private Category _selectedCategory;
        private Category _newCategory = new Category();
        private bool _isEditing;
        private string _message;

        public ObservableCollection<Category> Categories
        {
            get { return _categories; }
            set { SetProperty(ref _categories, value); }
        }

        public Category SelectedCategory
        {
            get { return _selectedCategory; }
            set
            {
                SetProperty(ref _selectedCategory, value);
                if (value != null)
                {
                    NewCategory.CategoryId = value.CategoryId;
                    NewCategory.Name = value.Name;
                    NewCategory.Description = value.Description;
                }
            }
        }

        public Category NewCategory
        {
            get { return _newCategory; }
            set { SetProperty(ref _newCategory, value); }
        }

        public bool IsEditing
        {
            get { return _isEditing; }
            set { SetProperty(ref _isEditing, value); }
        }

        public string Message
        {
            get { return _message; }
            set { SetProperty(ref _message, value); }
        }

        public ICommand AddCommand { get; }
        public ICommand UpdateCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand RefreshCommand { get; }

        public CategoryViewModel(CategoryService categoryService)
        {
            _categoryService = categoryService;

            AddCommand = new RelayCommand(ExecuteAdd, CanExecuteAdd);
            UpdateCommand = new RelayCommand(ExecuteUpdate, CanExecuteUpdate);
            DeleteCommand = new RelayCommand(ExecuteDelete, CanExecuteDelete);
            CancelCommand = new RelayCommand(_ =>
            {
                IsEditing = false;
                NewCategory = new Category();
            });
            RefreshCommand = new RelayCommand(_ => LoadCategories());

            LoadCategories();
        }

        private void LoadCategories()
        {
            var categoryList = _categoryService.GetAllCategories();
            Categories = new ObservableCollection<Category>(categoryList);
        }

        private bool CanExecuteAdd(object parameter)
        {
            return !string.IsNullOrWhiteSpace(NewCategory.Name) && !IsEditing;
        }

        private void ExecuteAdd(object parameter)
        {
            if (_categoryService.AddCategory(NewCategory))
            {
                Message = "Category added successfully.";
                NewCategory = new Category();
                LoadCategories();
            }
            else
            {
                Message = "Failed to add category.";
            }
        }

        private bool CanExecuteUpdate(object parameter)
        {
            return !string.IsNullOrWhiteSpace(NewCategory.Name) &&
                   IsEditing &&
                   SelectedCategory != null;
        }

        private void ExecuteUpdate(object parameter)
        {
            if (_categoryService.UpdateCategory(NewCategory))
            {
                Message = "Category updated successfully.";
                IsEditing = false;
                NewCategory = new Category();
                LoadCategories();
            }
            else
            {
                Message = "Failed to update category.";
            }
        }

        private bool CanExecuteDelete(object parameter)
        {
            return SelectedCategory != null;
        }

        private void ExecuteDelete(object parameter)
        {
            if (_categoryService.DeleteCategory(SelectedCategory.CategoryId))
            {
                Message = "Category deleted successfully.";
                LoadCategories();
            }
            else
            {
                Message = "Failed to delete category. It may be in use by products.";
            }
        }
    }
}