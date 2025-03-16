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
    public class RemoveProductViewModel : BaseViewModel
    {
        public ObservableCollection<Product> Products { get; set; }
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

        public ICommand RemoveProductCommand { get; }

        public RemoveProductViewModel()
        {
            Products = LoadProducts();
            RemoveProductCommand = new ViewModelCommands(RemoveProductCommandExecuted);
        }

        private void RemoveProductCommandExecuted(object obj)
{
    if (SelectedProduct == null)
    {
        System.Windows.MessageBox.Show("Please select a product to remove.", "Error",
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        return;
    }

    using (var connection = new SqlConnection("Server=localhost\\SQLEXPRESS;Database=DietPlanningDB;Trusted_Connection=True;"))
    {
        connection.Open();

        // Delete related MealPlans records
        var deleteMealPlansCommand = new SqlCommand("DELETE FROM MealPlans WHERE ProductName = @ProductName", connection);
        deleteMealPlansCommand.Parameters.AddWithValue("@ProductName", SelectedProduct.Name);
        deleteMealPlansCommand.ExecuteNonQuery();

        // Delete the product from Products table
        var deleteProductCommand = new SqlCommand("DELETE FROM Products WHERE Name = @ProductName", connection);
        deleteProductCommand.Parameters.AddWithValue("@ProductName", SelectedProduct.Name);
        deleteProductCommand.ExecuteNonQuery();
    }

    // Remove the product from the ObservableCollection
    Products.Remove(SelectedProduct);
    System.Windows.MessageBox.Show("Product removed successfully.", "Success",
        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
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


    }
}
