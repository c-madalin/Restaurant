using Npgsql;
using RestaurantApp.Models;
using System;
using System.Collections.Generic;
using System.Data;

namespace RestaurantApp.Services
{
    public class CategoryService
    {
        private readonly DatabaseService _databaseService;

        public CategoryService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public List<Category> GetAllCategories()
        {
            List<Category> categories = new List<Category>();

            using (NpgsqlConnection connection = _databaseService.GetConnection())
            {
                connection.Open();

                using (NpgsqlCommand command = new NpgsqlCommand("SELECT * FROM sp_get_all_categories()", connection))
                {
                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            categories.Add(new Category
                            {
                                CategoryId = reader.GetInt32("categoryid"),
                                Name = reader.GetString("name"),
                                Description = reader.IsDBNull("description") ? string.Empty : reader.GetString("description")
                            });
                        }
                    }
                }
            }

            return categories;
        }

        public bool AddCategory(Category category)
        {
            using (NpgsqlConnection connection = _databaseService.GetConnection())
            {
                connection.Open();

                using (NpgsqlCommand command = new NpgsqlCommand("SELECT sp_add_category(@Name, @Description)", connection))
                {
                    command.Parameters.AddWithValue("@Name", category.Name);
                    command.Parameters.AddWithValue("@Description",
                        string.IsNullOrEmpty(category.Description) ? DBNull.Value : (object)category.Description);

                    bool result = (bool)command.ExecuteScalar();
                    return result;
                }
            }
        }

        public bool UpdateCategory(Category category)
        {
            using (NpgsqlConnection connection = _databaseService.GetConnection())
            {
                connection.Open();

                using (NpgsqlCommand command = new NpgsqlCommand("SELECT sp_update_category(@CategoryId, @Name, @Description)", connection))
                {
                    command.Parameters.AddWithValue("@CategoryId", category.CategoryId);
                    command.Parameters.AddWithValue("@Name", category.Name);
                    command.Parameters.AddWithValue("@Description",
                        string.IsNullOrEmpty(category.Description) ? DBNull.Value : (object)category.Description);

                    bool result = (bool)command.ExecuteScalar();
                    return result;
                }
            }
        }

        public bool DeleteCategory(int categoryId)
        {
            using (NpgsqlConnection connection = _databaseService.GetConnection())
            {
                connection.Open();

                using (NpgsqlCommand command = new NpgsqlCommand("SELECT sp_delete_category(@CategoryId)", connection))
                {
                    command.Parameters.AddWithValue("@CategoryId", categoryId);
                    bool result = (bool)command.ExecuteScalar();
                    return result;
                }
            }
        }

        public Category GetCategoryByName(string name)
        {
            using (NpgsqlConnection connection = _databaseService.GetConnection())
            {
                connection.Open();

                using (NpgsqlCommand command = new NpgsqlCommand("SELECT * FROM sp_get_category_by_name(@Name)", connection))
                {
                    command.Parameters.AddWithValue("@Name", name);

                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Category
                            {
                                CategoryId = reader.GetInt32("categoryid"),
                                Name = reader.GetString("name"),
                                Description = reader.IsDBNull("description") ? string.Empty : reader.GetString("description")
                            };
                        }
                    }
                }
            }

            return null;
        }
    }
}