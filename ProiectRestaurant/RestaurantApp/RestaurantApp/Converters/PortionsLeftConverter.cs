using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RestaurantApp.Converters
{
    public class PortionsLeftConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int portionSize && parameter is int totalQuantity)
            {
                int portions = totalQuantity / portionSize;
                return $"{portions} ({totalQuantity}g total)";
            }
            return "0";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}