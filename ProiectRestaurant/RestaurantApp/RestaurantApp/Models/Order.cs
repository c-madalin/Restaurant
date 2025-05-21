using System;
using System.Collections.Generic;

namespace RestaurantApp.Models
{
    public class Order
    {
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal Discount { get; set; }
        public string Status { get; set; }
        public DateTime? EstimatedDeliveryTime { get; set; }
        public string DeliveryAddress { get; set; }

        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
    }
}