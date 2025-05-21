using Npgsql;
using RestaurantApp.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace RestaurantApp.Services
{
    public class ProductService
    {
        private readonly DatabaseService _databaseService;

        public ProductService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public List<Product> GetAllProducts()
        {
            List<Product> products = new List<Product>();

            using (NpgsqlConnection connection = _databaseService.GetConnection())
            {
                connection.Open();

                using (NpgsqlCommand command = new NpgsqlCommand("SELECT * FROM sp_get_all_products()", connection))
                {
                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            products.Add(new Product
                            {
                                ProductId = reader.GetInt32("productid"),
                                Name = reader.GetString("name"),
                                Price = reader.GetDecimal("price"),
                                PortionSize = reader.GetInt32("portionsize"),
                                TotalQuantity = reader.GetInt32("totalquantity"),
                                CategoryId = reader.GetInt32("categoryid"),
                                CategoryName = reader.GetString("categoryname"),
                                IsAvailable = reader.GetBoolean("isavailable")
                            });
                        }
                    }
                }

                foreach (var product in products)
                {
                    product.Allergens = GetProductAllergens(product.ProductId);
                }
            }

            return products;
        }

        private List<Allergen> GetProductAllergens(int productId)
        {
            List<Allergen> allergens = new List<Allergen>();

            using (NpgsqlConnection connection = _databaseService.GetConnection())
            {
                connection.Open();

                using (NpgsqlCommand command = new NpgsqlCommand("SELECT * FROM sp_get_product_allergens(@ProductId)", connection))
                {
                    command.Parameters.AddWithValue("@ProductId", productId);

                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            allergens.Add(new Allergen
                            {
                                AllergenId = reader.GetInt32("allergenid"),
                                Name = reader.GetString("name"),
                                Description = reader.IsDBNull("description") ? string.Empty : reader.GetString("description")
                            });
                        }
                    }
                }
            }

            return allergens;
        }

        public Product GetProductById(int productId)
        {
            using (NpgsqlConnection connection = _databaseService.GetConnection())
            {
                connection.Open();

                using (NpgsqlCommand command = new NpgsqlCommand("SELECT * FROM sp_get_product_by_id(@ProductId)", connection))
                {
                    command.Parameters.AddWithValue("@ProductId", productId);

                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            Product product = new Product
                            {
                                ProductId = reader.GetInt32("productid"),
                                Name = reader.GetString("name"),
                                Price = reader.GetDecimal("price"),
                                PortionSize = reader.GetInt32("portionsize"),
                                TotalQuantity = reader.GetInt32("totalquantity"),
                                CategoryId = reader.GetInt32("categoryid"),
                                CategoryName = reader.GetString("categoryname"),
                                IsAvailable = reader.GetBoolean("isavailable")
                            };

                            product.Allergens = GetProductAllergens(product.ProductId);
                            return product;
                        }
                    }
                }

                return null;
            }
        }

        public List<Product> GetLowStockProducts(int threshold)
        {
            List<Product> products = new List<Product>();

            using (NpgsqlConnection connection = _databaseService.GetConnection())
            {
                connection.Open();

                using (NpgsqlCommand command = new NpgsqlCommand("SELECT * FROM sp_get_low_stock_products(@Threshold)", connection))
                {
                    command.Parameters.AddWithValue("@Threshold", threshold);

                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            products.Add(new Product
                            {
                                ProductId = reader.GetInt32("productid"),
                                Name = reader.GetString("name"),
                                TotalQuantity = reader.GetInt32("totalquantity"),
                                PortionSize = reader.GetInt32("portionsize"),
                                CategoryName = reader.GetString("categoryname")
                            });
                        }
                    }
                }
            }

            return products;
        }

        public bool AddProduct(Product product)
        {
            using (NpgsqlConnection connection = _databaseService.GetConnection())
            {
                connection.Open();

                int[] allergenIds = product.Allergens?.Select(a => a.AllergenId).ToArray();

                using (NpgsqlCommand command = new NpgsqlCommand("SELECT sp_add_product(@Name, @Price, @PortionSize, @TotalQuantity, @CategoryId, @IsAvailable, @AllergenIds)", connection))
                {
                    command.Parameters.AddWithValue("@Name", product.Name);
                    command.Parameters.AddWithValue("@Price", product.Price);
                    command.Parameters.AddWithValue("@PortionSize", product.PortionSize);
                    command.Parameters.AddWithValue("@TotalQuantity", product.TotalQuantity);
                    command.Parameters.AddWithValue("@CategoryId", product.CategoryId);
                    command.Parameters.AddWithValue("@IsAvailable", product.IsAvailable);
                    command.Parameters.AddWithValue("@AllergenIds", allergenIds ?? new int[0]);

                    int productId = Convert.ToInt32(command.ExecuteScalar());
                    return productId > 0;
                }
            }
        }
    }
}