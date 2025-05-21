using System;
using System.Globalization;
using System.Windows.Data;

namespace RestaurantApp.Converters
{
    public class ProductQuantityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int productId && parameter is string paramStr)
            {
                int change = int.Parse(paramStr);
                return new Tuple<int, int>(productId, change);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}