using System;
using System.Globalization;
using System.Windows.Data;

namespace RestaurantApp.Converters
{
    public class OrderItemTotalConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int quantity && parameter is decimal unitPrice)
            {
                return quantity * unitPrice;
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}