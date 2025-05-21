using Npgsql;
using RestaurantApp.Models;
using System;
using System.Collections.Generic;
using System.Text.Json;
using NpgsqlTypes;
using System.Data;

namespace RestaurantApp.Services
{
    public class CartService
    {
        private readonly DatabaseService _databaseService;
        private readonly ProductService _productService;

        public CartService(DatabaseService databaseService, ProductService productService)
        {
            _databaseService = databaseService;
            _productService = productService;
        }

        public bool SaveCartItems(int userId, ShoppingCart cart)
        {
            if (cart == null || cart.Items.Count == 0)
            {
                return ClearCartItems(userId);
            }

            using (NpgsqlConnection connection = _databaseService.GetConnection())
            {
                connection.Open();
                using (NpgsqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Convertim cart items în JSONB pentru procedura stocată
                        var cartItemsJson = JsonSerializer.Serialize(cart.Items.Select(item => new
                        {
                            productId = item.ProductId,
                            quantity = item.Quantity
                        }));

                        using (NpgsqlCommand command = new NpgsqlCommand("SELECT sp_save_cart_items(@UserId, @CartItems)", connection, transaction))
                        {
                            command.Parameters.AddWithValue("@UserId", userId);
                            command.Parameters.Add("@CartItems", NpgsqlDbType.Jsonb).Value = cartItemsJson;

                            bool result = (bool)command.ExecuteScalar();

                            if (result)
                            {
                                transaction.Commit();
                                return true;
                            }
                            else
                            {
                                transaction.Rollback();
                                return false;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Console.WriteLine($"Error saving cart items: {ex.Message}");
                        return false;
                    }
                }
            }
        }

        public ShoppingCart LoadCartItems(int userId)
        {
            ShoppingCart cart = new ShoppingCart();

            using (NpgsqlConnection connection = _databaseService.GetConnection())
            {
                connection.Open();

                using (NpgsqlCommand command = new NpgsqlCommand("SELECT * FROM sp_load_cart_items(@UserId)", connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);

                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int productId = reader.GetInt32("productid");
                            int quantity = reader.GetInt32("quantity");
                            string productName = reader.GetString("productname");
                            decimal price = reader.GetDecimal("price");
                            bool isAvailable = reader.GetBoolean("isavailable");

                            if (isAvailable)
                            {
                                cart.Items.Add(new CartItem
                                {
                                    ProductId = productId,
                                    ProductName = productName,
                                    UnitPrice = price,
                                    Quantity = quantity
                                });
                            }
                        }
                    }
                }
            }

            return cart;
        }

        public bool ClearCartItems(int userId)
        {
            using (NpgsqlConnection connection = _databaseService.GetConnection())
            {
                connection.Open();

                using (NpgsqlCommand command = new NpgsqlCommand("SELECT sp_clear_cart_items(@UserId)", connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);
                    bool result = (bool)command.ExecuteScalar();
                    return result;
                }
            }
        }
    }
}