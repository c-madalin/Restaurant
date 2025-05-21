using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RestaurantApp.Models
{
    public class CartItem : INotifyPropertyChanged
    {
        private int _quantity;
        private decimal _unitPrice;

        public int ProductId { get; set; }
        public string ProductName { get; set; }

        public decimal UnitPrice
        {
            get { return _unitPrice; }
            set
            {
                if (_unitPrice != value)
                {
                    _unitPrice = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TotalPrice));
                }
            }
        }

        public int Quantity
        {
            get { return _quantity; }
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TotalPrice));
                }
            }
        }

        public decimal TotalPrice => UnitPrice * Quantity;
        public CartItem()
        {
            ProductId = 0;
            ProductName = string.Empty;
            _unitPrice = 0;
            _quantity = 0;
        }
        public CartItem(CartItem other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));

            ProductId = other.ProductId;
            ProductName = other.ProductName;
            _unitPrice = other.UnitPrice;
            _quantity = other.Quantity;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override bool Equals(object obj)
        {
            if (!(obj is CartItem other))
                return false;

            return this.ProductId == other.ProductId;
        }

        public override int GetHashCode()
        {
            return ProductId.GetHashCode();
        }
    }
}