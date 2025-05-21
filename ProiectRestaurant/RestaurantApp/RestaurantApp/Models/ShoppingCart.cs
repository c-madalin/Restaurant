using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace RestaurantApp.Models
{
    public class ShoppingCart : INotifyPropertyChanged
    {
        private ObservableCollection<CartItem> _items = new ObservableCollection<CartItem>();
        private decimal _deliveryFee;
        private decimal _discount;

        public ObservableCollection<CartItem> Items
        {
            get { return _items; }
            set
            {
                _items = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Subtotal));
                OnPropertyChanged(nameof(Total));
            }
        }

        public decimal Subtotal => Items.Sum(item => item.TotalPrice);

        public decimal DeliveryFee
        {
            get { return _deliveryFee; }
            set
            {
                _deliveryFee = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Total));
            }
        }

        public decimal Discount
        {
            get { return _discount; }
            set
            {
                _discount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Total));
            }
        }

        public decimal Total => Subtotal + DeliveryFee - Discount;

        public void AddItem(Product product, int quantity = 1)
        {
            var existingItem = Items.FirstOrDefault(i => i.ProductId == product.ProductId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                Items.Add(new CartItem
                {
                    ProductId = product.ProductId,
                    ProductName = product.Name,
                    UnitPrice = product.Price,
                    Quantity = quantity
                });
            }

            OnPropertyChanged(nameof(Subtotal));
            OnPropertyChanged(nameof(Total));
        }

        public void UpdateItemQuantity(int productId, int quantity)
        {
            var item = Items.FirstOrDefault(i => i.ProductId == productId);

            if (item != null)
            {
                if (quantity <= 0)
                {
                    Items.Remove(item);
                }
                else
                {
                    item.Quantity = quantity;
                }

                OnPropertyChanged(nameof(Subtotal));
                OnPropertyChanged(nameof(Total));
            }
        }

        public void RemoveItem(int productId)
        {
            var item = Items.FirstOrDefault(i => i.ProductId == productId);

            if (item != null)
            {
                Items.Remove(item);
                OnPropertyChanged(nameof(Subtotal));
                OnPropertyChanged(nameof(Total));
            }
        }

        public void Clear()
        {
            Items.Clear();
            DeliveryFee = 0;
            Discount = 0;

            OnPropertyChanged(nameof(Subtotal));
            OnPropertyChanged(nameof(Total));
        }

        public void ReplaceWith(ShoppingCart other)
        {
            if (other == null) return;

            Clear();

            foreach (var item in other.Items)
            {
                Items.Add(item);
            }

            DeliveryFee = other.DeliveryFee;
            Discount = other.Discount;

            OnPropertyChanged(nameof(Subtotal));
            OnPropertyChanged(nameof(Total));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}