using RestaurantApp.Helpers;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RestaurantApp.Converters
{
    public class StockStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int totalQuantity)
            {
                return totalQuantity <= AppSettings.LowStockThreshold ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}