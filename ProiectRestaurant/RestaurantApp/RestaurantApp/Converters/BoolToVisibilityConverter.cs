using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RestaurantApp.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = (bool)value;
            string param = parameter as string;

            if (param == "Inverse")
                boolValue = !boolValue;

            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility visibility = (Visibility)value;
            string param = parameter as string;
            bool result = visibility == Visibility.Visible;

            if (param == "Inverse")
                result = !result;

            return result;
        }
    }
}