using Adapters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DietPlanning.ViewModels
{
    public class GoalPlannerViewModel : BaseViewModel
    {
        private decimal _actualWeight;
        public decimal ActualWeight
        {
            get => _actualWeight;
            set
            {
                _actualWeight = value;
                OnPropertyChanged(nameof(ActualWeight));
            }
        }

        private decimal _targetWeight;
        public decimal TargetWeight
        {
            get => _targetWeight;
            set
            {
                _targetWeight = value;
                OnPropertyChanged(nameof(TargetWeight));
            }
        }

        private int _dailyCalories;
        public int DailyCalories
        {
            get => _dailyCalories;
            set
            {
                _dailyCalories = value;
                OnPropertyChanged(nameof(DailyCalories));
                // Trigger recalculation of dependent properties
                OnPropertyChanged(nameof(DailyCarbs));
                OnPropertyChanged(nameof(DailyProtein));
                OnPropertyChanged(nameof(DailyFat));
            }
        }

        private decimal _fatPercentage;
        public decimal FatPercentage
        {
            get => _fatPercentage;
            set
            {
                _fatPercentage = value;
                OnPropertyChanged(nameof(FatPercentage));
                // Trigger recalculation of dependent properties
                OnPropertyChanged(nameof(DailyCarbs));
                OnPropertyChanged(nameof(DailyProtein));
                OnPropertyChanged(nameof(DailyFat));
            }
        }

        private decimal _proteinPercentage;
        public decimal ProteinPercentage
        {
            get => _proteinPercentage;
            set
            {
                _proteinPercentage = value;
                OnPropertyChanged(nameof(ProteinPercentage));
                // Trigger recalculation of dependent properties
                OnPropertyChanged(nameof(DailyCarbs));
                OnPropertyChanged(nameof(DailyProtein));
                OnPropertyChanged(nameof(DailyFat));
            }
        }

        private decimal _carbsPercentage;
        public decimal CarbsPercentage
        {
            get => _carbsPercentage;
            set
            {
                _carbsPercentage = value;
                OnPropertyChanged(nameof(CarbsPercentage));
                // Trigger recalculation of dependent properties
                OnPropertyChanged(nameof(DailyCarbs));
                OnPropertyChanged(nameof(DailyProtein));
                OnPropertyChanged(nameof(DailyFat));
            }
        }

        // Read-only calculated properties
        public int DailyCarbs => CalculateGrams(DailyCalories, CarbsPercentage, 4);
        public int DailyProtein => CalculateGrams(DailyCalories, ProteinPercentage, 4);
        public int DailyFat => CalculateGrams(DailyCalories, FatPercentage, 9);


        public ICommand SaveGoalCommand { get; }

        public GoalPlannerViewModel()
        {
            SaveGoalCommand = new ViewModelCommands(SaveGoalCommandExecuted);
            // Load data when the ViewModel is initialized
            LoadGoalFromDatabase();
        }

        private int CalculateGrams(int totalCalories, decimal percentage, int caloriesPerGram)
        {
            return (int)(totalCalories * percentage / 100 / caloriesPerGram);
        }


        private void SaveGoalCommandExecuted(object obj)
        {
            // Validation: Ensure percentages add up to 100
            if (FatPercentage + ProteinPercentage + CarbsPercentage != 100)
            {
                System.Windows.MessageBox.Show("The percentages of Fat, Protein, and Carbs must add up to 100.", "Validation Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }



            using (var connection = new SqlConnection("Server=localhost\\SQLEXPRESS;Database=DietPlanningDB;Trusted_Connection=True;"))
            {
                var command = new SqlCommand(@"
                INSERT INTO Goals 
                (ActualWeight, TargetWeight, DailyCalories, FatPercentage, ProteinPercentage, CarbsPercentage, DailyCarbs, DailyProtein, DailyFat)
                VALUES 
                (@ActualWeight, @TargetWeight, @DailyCalories, @FatPercentage, @ProteinPercentage, @CarbsPercentage, @DailyCarbs, @DailyProtein, @DailyFat)",
                    connection);

                command.Parameters.AddWithValue("@ActualWeight", ActualWeight);
                command.Parameters.AddWithValue("@TargetWeight", TargetWeight);
                command.Parameters.AddWithValue("@DailyCalories", DailyCalories);
                command.Parameters.AddWithValue("@FatPercentage", FatPercentage);
                command.Parameters.AddWithValue("@ProteinPercentage", ProteinPercentage);
                command.Parameters.AddWithValue("@CarbsPercentage", CarbsPercentage);
                command.Parameters.AddWithValue("@DailyCarbs", DailyCarbs);
                command.Parameters.AddWithValue("@DailyProtein", DailyProtein);
                command.Parameters.AddWithValue("@DailyFat", DailyFat);

                connection.Open();
                command.ExecuteNonQuery();
            }

            System.Windows.MessageBox.Show("Goal saved successfully!", "Success",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private void LoadGoalFromDatabase()
        {
            using (var connection = new SqlConnection("Server=localhost\\SQLEXPRESS;Database=DietPlanningDB;Trusted_Connection=True;"))
            {
                var command = new SqlCommand("SELECT TOP 1 * FROM Goals ORDER BY Id DESC", connection);
                connection.Open();

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        ActualWeight = Convert.ToDecimal(reader["ActualWeight"]);
                        TargetWeight = Convert.ToDecimal(reader["TargetWeight"]);
                        DailyCalories = Convert.ToInt32(reader["DailyCalories"]);
                        FatPercentage = Convert.ToDecimal(reader["FatPercentage"]);
                        ProteinPercentage = Convert.ToDecimal(reader["ProteinPercentage"]);
                        CarbsPercentage = Convert.ToDecimal(reader["CarbsPercentage"]);
                        OnPropertyChanged(nameof(DailyCarbs)); // Notify dependent properties
                        OnPropertyChanged(nameof(DailyProtein));
                        OnPropertyChanged(nameof(DailyFat));
                    }
                }
            }
        }
    }


}

  

