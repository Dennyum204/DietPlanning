using Adapters;
using DietPlanning.Models;
using DietPlanning.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Input;


namespace DietPlanning.ViewModels
{
    public class EditProductViewModel : BaseViewModel
    {

        public ObservableCollection<Product> Products { get; set; }
        public ObservableCollection<string> Categories { get; set; }

        private Product _selectedProduct;

        public Product SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                _selectedProduct = value;
                OnPropertyChanged(nameof(SelectedProduct));
            }
        }

        public ICommand SaveChangesCommand { get; }

        public EditProductViewModel()
        {
            Products = LoadProducts();
            Categories = new ObservableCollection<string> { "Carbs", "Protein", "Fat" }; // Categories for dropdown
            SaveChangesCommand = new ViewModelCommands(SaveChangesCommandExecuted);
        }

        private void SaveChangesCommandExecuted(object obj)
        {
            if (SelectedProduct == null) return;

            using (var connection = new SqlConnection("Server=localhost\\SQLEXPRESS;Database=DietPlanningDB;Trusted_Connection=True;"))
            {
                connection.Open();

                // Check if a product with the same name exists
                var checkCommand = new SqlCommand("SELECT COUNT(*) FROM Products WHERE Name = @Name", connection);
                checkCommand.Parameters.AddWithValue("@Name", SelectedProduct.Name);

                var count = (int)checkCommand.ExecuteScalar();

                if (count > 0)
                {
                    // Update existing product
                    var updateCommand = new SqlCommand(
                        "UPDATE Products SET Protein = @Protein, Carbs = @Carbs, Fat = @Fat, Category = @Category, Calories = @Calories WHERE Name = @Name",
                        connection
                    );

                    updateCommand.Parameters.AddWithValue("@Name", SelectedProduct.Name);
                    updateCommand.Parameters.AddWithValue("@Protein", SelectedProduct.Protein);
                    updateCommand.Parameters.AddWithValue("@Carbs", SelectedProduct.Carbs);
                    updateCommand.Parameters.AddWithValue("@Fat", SelectedProduct.Fat);
                    updateCommand.Parameters.AddWithValue("@Category", SelectedProduct.Category);
                    updateCommand.Parameters.AddWithValue("@Calories", SelectedProduct.Calories);

                    updateCommand.ExecuteNonQuery();

                    System.Windows.MessageBox.Show("Product Updated Successfully", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // Insert as new product
                    var insertCommand = new SqlCommand(
                        "INSERT INTO Products (Name, Protein, Carbs, Fat, Category, Calories) VALUES (@Name, @Protein, @Carbs, @Fat, @Category, @Calories)",
                        connection
                    );

                    insertCommand.Parameters.AddWithValue("@Name", SelectedProduct.Name);
                    insertCommand.Parameters.AddWithValue("@Protein", SelectedProduct.Protein);
                    insertCommand.Parameters.AddWithValue("@Carbs", SelectedProduct.Carbs);
                    insertCommand.Parameters.AddWithValue("@Fat", SelectedProduct.Fat);
                    insertCommand.Parameters.AddWithValue("@Category", SelectedProduct.Category);
                    insertCommand.Parameters.AddWithValue("@Calories", SelectedProduct.Calories);

                    insertCommand.ExecuteNonQuery();

                    System.Windows.MessageBox.Show("New Product Added Successfully", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }

            // Refresh the product list
            UpdateProducts();
            
        }

        private ObservableCollection<Product> LoadProducts()
        {
            var products = new ObservableCollection<Product>();

            using (var connection = new SqlConnection("Server=localhost\\SQLEXPRESS;Database=DietPlanningDB;Trusted_Connection=True;"))
            {
                var command = new SqlCommand("SELECT Name, Protein, Carbs, Fat, Category, Calories FROM Products", connection);
                connection.Open();

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        products.Add(new Product
                        {
                            Name = reader["Name"].ToString(),
                            Protein = (double)reader["Protein"],
                            Carbs = (double)reader["Carbs"],
                            Fat = (double)reader["Fat"],
                            Category = reader["Category"].ToString(),
                            Calories = (int)reader["Calories"]
                        });
                    }
                }
            }

            return products;
        }
        private void UpdateProducts()
        {
            Products.Clear();

            foreach (var product in LoadProducts())
            {
                Products.Add(product);
            }
        }

    }
}
