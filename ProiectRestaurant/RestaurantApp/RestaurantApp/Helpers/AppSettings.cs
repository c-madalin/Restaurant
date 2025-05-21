using System;
using System.Configuration;

namespace RestaurantApp.Helpers
{
    public static class AppSettings
    {
        public static decimal MenuDiscountPercentage => GetDecimalSetting("MenuDiscountPercentage", 10);
        public static decimal MinimumOrderForFreeDelivery => GetDecimalSetting("MinimumOrderForFreeDelivery", 50);
        public static decimal DeliveryFee => GetDecimalSetting("DeliveryFee", 5);
        public static decimal MinimumOrderForDiscount => GetDecimalSetting("MinimumOrderForDiscount", 100);
        public static decimal OrderDiscountPercentage => GetDecimalSetting("OrderDiscountPercentage", 5);
        public static int OrderCountForLoyaltyDiscount => GetIntSetting("OrderCountForLoyaltyDiscount", 5);
        public static int DaysForLoyaltyDiscountPeriod => GetIntSetting("DaysForLoyaltyDiscountPeriod", 30);
        public static int LowStockThreshold => GetIntSetting("LowStockThreshold", 1000);

        private static decimal GetDecimalSetting(string key, decimal defaultValue)
        {
            string value = ConfigurationManager.AppSettings[key];
            return decimal.TryParse(value, out decimal result) ? result : defaultValue;
        }

        private static int GetIntSetting(string key, int defaultValue)
        {
            string value = ConfigurationManager.AppSettings[key];
            return int.TryParse(value, out int result) ? result : defaultValue;
        }
    }
}