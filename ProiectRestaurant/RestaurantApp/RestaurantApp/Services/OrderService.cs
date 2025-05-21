using Npgsql;
using RestaurantApp.Helpers;
using RestaurantApp.Models;
using System;
using System.Collections.Generic;
using System.Text.Json;
using NpgsqlTypes;
using System.Linq;
using System.Data;

namespace RestaurantApp.Services
{
    public class OrderService
    {
        private readonly DatabaseService _databaseService;
        private readonly ProductService _productService;
        private readonly CartService _cartService;

        public OrderService(DatabaseService databaseService, ProductService productService, CartService cartService)
        {
            _databaseService = databaseService;
            _productService = productService;
            _cartService = cartService;
        }

        public ShoppingCart CalculateOrderCosts(ShoppingCart cart, User user)
        {
            cart.DeliveryFee = cart.Subtotal < AppSettings.MinimumOrderForFreeDelivery ? AppSettings.DeliveryFee : 0;

            if (cart.Subtotal >= AppSettings.MinimumOrderForDiscount)
            {
                cart.Discount = cart.Subtotal * (AppSettings.OrderDiscountPercentage / 100);
            }
            else
            {
                int recentOrderCount = GetRecentOrderCount(user.UserId, AppSettings.DaysForLoyaltyDiscountPeriod);

                if (recentOrderCount >= AppSettings.OrderCountForLoyaltyDiscount)
                {
                    cart.Discount = cart.Subtotal * (AppSettings.OrderDiscountPercentage / 100);
                }
                else
                {
                    cart.Discount = 0;
                }
            }

            return cart;
        }

        private int GetRecentOrderCount(int userId, int days)
        {
            using (NpgsqlConnection connection = _databaseService.GetConnection())
            {
                connection.Open();

                using (NpgsqlCommand command = new NpgsqlCommand("SELECT sp_get_recent_order_count(@UserId, @Days)", connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@Days", days);

                    return Convert.ToInt32(command.ExecuteScalar());
                }
            }
        }

        public int PlaceOrder(ShoppingCart cart, User user)
        {
            CalculateOrderCosts(cart, user);

            using (NpgsqlConnection connection = _databaseService.GetConnection())
            {
                connection.Open();

                using (NpgsqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var orderItemsJson = JsonSerializer.Serialize(cart.Items.Select(item => new
                        {
                            productId = item.ProductId,
                            quantity = item.Quantity,
                            unitPrice = item.UnitPrice
                        }));

                        using (NpgsqlCommand command = new NpgsqlCommand("SELECT sp_place_order(@UserId, @TotalAmount, @DeliveryFee, @Discount, @DeliveryAddress, @OrderItems)", connection, transaction))
                        {
                            command.Parameters.AddWithValue("@UserId", user.UserId);
                            command.Parameters.AddWithValue("@TotalAmount", cart.Total);
                            command.Parameters.AddWithValue("@DeliveryFee", cart.DeliveryFee);
                            command.Parameters.AddWithValue("@Discount", cart.Discount);
                            command.Parameters.AddWithValue("@DeliveryAddress", user.DeliveryAddress);
                            command.Parameters.Add("@OrderItems", NpgsqlDbType.Jsonb).Value = orderItemsJson;

                            int orderId = Convert.ToInt32(command.ExecuteScalar());

                            if (orderId > 0)
                            {
                                _cartService.ClearCartItems(user.UserId);
                                transaction.Commit();
                                return orderId;
                            }
                            else
                            {
                                transaction.Rollback();
                                throw new Exception("Failed to place order.");
                            }
                        }
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public List<Order> GetUserOrders(int userId)
        {
            List<Order> orders = new List<Order>();

            using (NpgsqlConnection connection = _databaseService.GetConnection())
            {
                connection.Open();

                using (NpgsqlCommand command = new NpgsqlCommand("SELECT orderid, userid, orderdate, totalamount, deliveryfee, discount, status, estimateddeliverytime, deliveryaddress FROM orders WHERE userid = @UserId ORDER BY orderdate DESC", connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);

                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            orders.Add(new Order
                            {
                                OrderId = reader.GetInt32("orderid"),
                                UserId = reader.GetInt32("userid"),
                                OrderDate = reader.GetDateTime("orderdate"),
                                TotalAmount = reader.GetDecimal("totalamount"),
                                DeliveryFee = reader.GetDecimal("deliveryfee"),
                                Discount = reader.GetDecimal("discount"),
                                Status = reader.GetString("status"),
                                EstimatedDeliveryTime = reader.IsDBNull("estimateddeliverytime") ? null : (DateTime?)reader.GetDateTime("estimateddeliverytime"),
                                DeliveryAddress = reader.GetString("deliveryaddress")
                            });
                        }
                    }
                }

                foreach (var order in orders)
                {
                    order.OrderItems = GetOrderItems(order.OrderId);
                }
            }

            return orders;
        }

        public List<Order> GetAllOrders(bool activeOnly = false)
        {
            List<Order> orders = new List<Order>();

            using (NpgsqlConnection connection = _databaseService.GetConnection())
            {
                connection.Open();

                using (NpgsqlCommand command = new NpgsqlCommand("SELECT * FROM sp_get_all_orders(@ActiveOnly)", connection))
                {
                    command.Parameters.AddWithValue("@ActiveOnly", activeOnly);

                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            orders.Add(new Order
                            {
                                OrderId = reader.GetInt32("orderid"),
                                UserId = reader.GetInt32("userid"),
                                OrderDate = reader.GetDateTime("orderdate"),
                                TotalAmount = reader.GetDecimal("totalamount"),
                                DeliveryFee = reader.GetDecimal("deliveryfee"),
                                Discount = reader.GetDecimal("discount"),
                                Status = reader.GetString("status"),
                                EstimatedDeliveryTime = reader.IsDBNull("estimateddeliverytime") ? null : (DateTime?)reader.GetDateTime("estimateddeliverytime"),
                                DeliveryAddress = reader.GetString("deliveryaddress"),
                                CustomerName = reader.GetString("customername"),
                                CustomerPhone = reader.IsDBNull("customerphone") ? string.Empty : reader.GetString("customerphone")
                            });
                        }
                    }
                }

                foreach (var order in orders)
                {
                    order.OrderItems = GetOrderItems(order.OrderId);
                }
            }

            return orders;
        }

        private List<OrderItem> GetOrderItems(int orderId)
        {
            List<OrderItem> items = new List<OrderItem>();

            using (NpgsqlConnection connection = _databaseService.GetConnection())
            {
                connection.Open();

                using (NpgsqlCommand command = new NpgsqlCommand("SELECT * FROM sp_get_order_items(@OrderId)", connection))
                {
                    command.Parameters.AddWithValue("@OrderId", orderId);

                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            items.Add(new OrderItem
                            {
                                OrderItemId = reader.GetInt32("orderitemid"),
                                OrderId = reader.GetInt32("orderid"),
                                ProductId = reader.GetInt32("productid"),
                                Quantity = reader.GetInt32("quantity"),
                                UnitPrice = reader.GetDecimal("unitprice"),
                                ProductName = reader.GetString("productname")
                            });
                        }
                    }
                }
            }

            return items;
        }

        public bool UpdateOrderStatus(int orderId, string status)
        {
            using (NpgsqlConnection connection = _databaseService.GetConnection())
            {
                connection.Open();

                using (NpgsqlCommand command = new NpgsqlCommand("UPDATE orders SET status = @Status WHERE orderid = @OrderId", connection))
                {
                    command.Parameters.AddWithValue("@OrderId", orderId);
                    command.Parameters.AddWithValue("@Status", status);

                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
        }

        public bool UpdateProductQuantities(int orderId)
        {
            using (NpgsqlConnection connection = _databaseService.GetConnection())
            {
                connection.Open();

                using (NpgsqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string query = "SELECT update_product_quantities_for_order(@OrderId)";

                        using (NpgsqlCommand command = new NpgsqlCommand(query, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@OrderId", orderId);
                            bool success = (bool)command.ExecuteScalar();

                            if (!success)
                            {
                                transaction.Rollback();
                                return false;
                            }
                        }

                        transaction.Commit();
                        return true;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }
    }
}
