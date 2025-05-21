using System;
using System.Globalization;
using System.Windows.Data;

namespace RestaurantApp.Converters
{
    public class OrderItemTotalMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is int quantity && values[1] is decimal unitPrice)
            {
                decimal total = quantity * unitPrice;
                return string.Format("{0:C}", total);
            }

            return "N/A";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}