using Adapters;
using DietPlanning.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Globalization;
using System.Windows;
using System.Windows.Input;


namespace DietPlanning.ViewModels
{
    public class AddProductViewModel : BaseViewModel
{


        private string _carbsInput;
        public string CarbsInput
        {
            get => _carbsInput;
            set
            {
                if (_carbsInput != value)
                {
                    _carbsInput = value;
                    OnPropertyChanged(nameof(CarbsInput));

                    // Parse input using InvariantCulture to handle "." as the decimal separator
                    if (double.TryParse(_carbsInput.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                    {
                        Carbs = result;
                    }
                }
            }
        }
        private string _fatInput;
        public string FatInput
        {
            get => _fatInput;
            set
            {
                if (_fatInput != value)
                {
                    _fatInput = value;
                    OnPropertyChanged(nameof(FatInput));

                    // Parse input using InvariantCulture to handle "." as the decimal separator
                    if (double.TryParse(_fatInput.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                    {
                        Fat = result;
                    }
                }
            }
        }

        private string _proteinInput;
        public string ProteinInput
        {
            get => _proteinInput;
            set
            {
                if (_proteinInput != value)
                {
                    _proteinInput = value;
                    OnPropertyChanged(nameof(ProteinInput));

                    // Parse input using InvariantCulture to handle "." as the decimal separator
                    if (double.TryParse(_proteinInput.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                    {
                        Protein = result;
                    }
                }
            }
        }


        private string _name;
    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            OnPropertyChanged(nameof(Name));
        }
    }

    private double _protein;
    public double Protein
    {
        get => _protein;
        set
        {
            _protein = value;
            OnPropertyChanged(nameof(Protein));
        }
    }

    private double _carbs;
    public double Carbs
    {
        get => _carbs;
        set
        {
            _carbs = value;
            OnPropertyChanged(nameof(Carbs));
        }
    }

    private double _fat;
    public double Fat
    {
        get => _fat;
        set
        {
            _fat = value;
            OnPropertyChanged(nameof(Fat));
        }
    }

    private int _calories;
    public int Calories
    {
        get => _calories;
        set
        {
                _calories = value;
            OnPropertyChanged(nameof(Calories));
        }
    }

        private string _selectedCategory;
    public string SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            _selectedCategory = value;
            OnPropertyChanged(nameof(SelectedCategory));
        }
    }

    public ICommand SaveProductCommand { get; }
    public ObservableCollection<string> Categories { get; }



    public AddProductViewModel()
    {

        // Predefined categories
        Categories = new ObservableCollection<string>
        {
            "Carbs",
            "Protein",
            "Fat"
        };


        SaveProductCommand = new ViewModelCommands(SaveProductCommandExecuted);
    }

    private void SaveProductCommandExecuted(object obj)
    {
            string connectionstring = "Server=localhost\\SQLEXPRESS;Database=DietPlanningDB;Trusted_Connection=True;";
            // Save to database logic
            using (var connection = new SqlConnection(connectionstring))
            {
                var command = new SqlCommand(
                    "INSERT INTO Products (Name, Protein, Carbs, Fat, Category,Calories) VALUES (@Name, @Protein, @Carbs, @Fat, @Category,@Calories)",
                    connection);
                command.Parameters.AddWithValue("@Name", Name);
                command.Parameters.AddWithValue("@Protein", Protein);
                command.Parameters.AddWithValue("@Carbs", Carbs);
                command.Parameters.AddWithValue("@Fat", Fat);
                command.Parameters.AddWithValue("@Category", SelectedCategory);
                command.Parameters.AddWithValue("@Calories", Calories);

                connection.Open();
                command.ExecuteNonQuery();

                MessageBox.Show("Product Added to the Database", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                ResetData();
            }
        }

        private void ResetData()
        {
            CarbsInput= string.Empty;
            ProteinInput= string.Empty;
            FatInput= string.Empty;
            Calories=0;
            //Categories= string.Empty;
            Name= string.Empty;
        }
    }
}
