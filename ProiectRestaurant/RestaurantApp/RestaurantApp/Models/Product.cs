using System.Collections.Generic;

namespace RestaurantApp.Models
{
    public class Product
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int PortionSize { get; set; }
        public int TotalQuantity { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public bool IsAvailable { get; set; }
        public List<Allergen> Allergens { get; set; } = new List<Allergen>();
    }
}