using Adapters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace DietPlanning.ViewModels
{
    public class MealPlannerViewModel : BaseViewModel
    {
        public ObservableCollection<DayMealPlan> Days { get; set; }
        public ObservableCollection<string> Products { get; set; }
        public ObservableCollection<string> MealTypes { get; set; }
        public Brush Background { get; set; } = Brushes.White; // Default color
        public bool IsSelected { get; set; }
        public DaySummary DaySummary { get; private set; }

        private string _mealType;
        public string MealType
        {
            get => _mealType;
            set
            {
                if (_mealType != value)
                {
                    _mealType = value;
                    OnPropertyChanged(nameof(MealType));
                    UpdateMealTypeInDatabase(); // Save changes to the database
                }
            }
        }

        private void UpdateMealTypeInDatabase()
        {
            if (SelectedMealPlan == null) return;

            using (var connection = new SqlConnection("Server=localhost\\SQLEXPRESS;Database=DietPlanningDB;Trusted_Connection=True;"))
            {
                var command = new SqlCommand(@"
            UPDATE MealPlans
            SET 
                MealType = @MealType,
                Protein = @Protein,
                Carbs = @Carbs,
                Fat = @Fat,
                Calories = @Calories
            WHERE ProductName = @ProductName AND Day = @Day", connection);

                // Use values directly from SelectedMealPlan
                command.Parameters.AddWithValue("@MealType", SelectedMealPlan.MealType);
                command.Parameters.AddWithValue("@Protein", SelectedMealPlan.Protein);
                command.Parameters.AddWithValue("@Carbs", SelectedMealPlan.Carbs);
                command.Parameters.AddWithValue("@Fat", SelectedMealPlan.Fat);
                command.Parameters.AddWithValue("@Calories", SelectedMealPlan.Calories);
                command.Parameters.AddWithValue("@ProductName", SelectedMealPlan.ProductName);
                command.Parameters.AddWithValue("@Day", SelectedDay.Day);

                connection.Open();
                command.ExecuteNonQuery();
            }
        }



        public MealPlannerViewModel()
        {
            Days = LoadDays();
            Products = LoadProducts();
            FilteredProducts = new ObservableCollection<string>(Products); // Initialize filtered list
            DaySummary = new DaySummary(); // Initialize DaySummary

            LoadGoalsFromDatabase(); // Load goals on initialization

            AddProductCommand = new ViewModelCommands(AddProductCommandExecuted);
            RemoveProductCommand = new ViewModelCommands(RemoveProductCommandExecuted);

            MealTypes = new ObservableCollection<string>
            {
                "Breakfast",
                "Morning Lunch",
                "Lunch",
                "Middle Day",
                "Dinner",
                "Before Bed"
            };

            // Set default day (e.g., Monday)
            SelectedDay = Days.FirstOrDefault();

            // Subscribe to changes in MealPlans
            foreach (var day in Days)
            {
                foreach (var mealPlan in day.MealPlans)
                {
                    mealPlan.MealTypeChanged += OnMealTypeChanged;
                }
            }

        }

        private void OnMealTypeChanged(object sender, EventArgs e)
        {
            if (sender is MealPlan changedMealPlan)
            {
                using (var connection = new SqlConnection("Server=localhost\\SQLEXPRESS;Database=DietPlanningDB;Trusted_Connection=True;"))
                {
                    var command = new SqlCommand(@"
                UPDATE MealPlans
                SET MealType = @MealType
                WHERE  Id = @Id", connection);

                    command.Parameters.AddWithValue("@MealType", changedMealPlan.MealType);
                    command.Parameters.AddWithValue("@ProductName", changedMealPlan.ProductName);
                    command.Parameters.AddWithValue("@Day", SelectedDay.Day);
                    command.Parameters.AddWithValue("@Id", changedMealPlan.Id);

                    connection.Open();
                    command.ExecuteNonQuery();
                }

                // Optionally, you can log or display a message that the change was saved
                Console.WriteLine($"MealType for product '{changedMealPlan.ProductName}' on {SelectedDay.Day} updated to '{changedMealPlan.MealType}'.");
            }
            UpdateDaySummary();
        }


        private void LoadGoalsFromDatabase()
        {
            using (var connection = new SqlConnection("Server=localhost\\SQLEXPRESS;Database=DietPlanningDB;Trusted_Connection=True;"))
            {
                var command = new SqlCommand("SELECT TOP 1 DailyCarbs, DailyFat, DailyProtein FROM Goals ORDER BY Id DESC", connection);
                connection.Open();

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        DailyCarbs = Convert.ToInt32(reader["DailyCarbs"]);
                        DailyFat = Convert.ToInt32(reader["DailyFat"]);
                        DailyProtein = Convert.ToInt32(reader["DailyProtein"]);
                    }
                }
            }
        }

        private ObservableCollection<DayMealPlan> LoadDays()
        {
            return new ObservableCollection<DayMealPlan>
            {
                new DayMealPlan("Monday"),
                new DayMealPlan("Tuesday"),
                new DayMealPlan("Wednesday"),
                new DayMealPlan("Thursday"),
                new DayMealPlan("Friday"),
                new DayMealPlan("Saturday"),
                new DayMealPlan("Sunday")
            };
        }

        private ObservableCollection<string> LoadProducts()
        {
            var products = new ObservableCollection<string>();

            using (var connection = new SqlConnection("Server=localhost\\SQLEXPRESS;Database=DietPlanningDB;Trusted_Connection=True;"))
            {
                var command = new SqlCommand("SELECT Name FROM Products", connection);
                connection.Open();

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        products.Add(reader["Name"].ToString());
                    }
                }
            }

            return products;
        }

        #region Properties
        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged(nameof(SearchText));
                    FilterProducts(); // Call filter logic when search text changes
                }
            }
        }

        private ObservableCollection<string> _filteredProducts;
        public ObservableCollection<string> FilteredProducts
        {
            get => _filteredProducts;
            set
            {
                _filteredProducts = value;
                OnPropertyChanged(nameof(FilteredProducts));
            }
        }

        private void FilterProducts()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredProducts = new ObservableCollection<string>(Products); // Show all products if search is empty
            }
            else
            {
                FilteredProducts = new ObservableCollection<string>(
                    Products.Where(p => p.IndexOf(SearchText, StringComparison.InvariantCultureIgnoreCase) >= 0)
                );
            }
        }

        private string _selectedMealType;
        public string SelectedMealType
        {
            get => _selectedMealType;
            set
            {
                _selectedMealType = value;
                OnPropertyChanged(nameof(SelectedMealType));
            }
        }

        private string _selectedProduct;
        public string SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                if (_selectedProduct != value)
                {
                    _selectedProduct = value;
                    Console.WriteLine($"SelectedProduct changed to: {_selectedProduct}");
                    OnPropertyChanged(nameof(SelectedProduct));
                }
            }
        }

        private DayMealPlan _selectedDay;
        public DayMealPlan SelectedDay
        {
            get => _selectedDay;
            set
            {
                if (_selectedDay != value)
                {
                    // Detach previous CollectionChanged handler
                    if (_selectedDay?.MealPlans != null)
                    {
                        _selectedDay.MealPlans.CollectionChanged -= OnMealPlansCollectionChanged;

                        
                    }

                    _selectedDay = value;

                    // Attach new CollectionChanged handler
                    if (_selectedDay?.MealPlans != null)
                    {
                        _selectedDay.MealPlans.CollectionChanged += OnMealPlansCollectionChanged;

                    }

                    // Notify changes for SelectedDay and totals
                    OnPropertyChanged(nameof(SelectedDay));
                    OnPropertyChanged(nameof(TotalCarbs));
                    OnPropertyChanged(nameof(TotalFat));
                    OnPropertyChanged(nameof(TotalProtein));
                    OnPropertyChanged(nameof(TotalCalories));
                    UpdateDaySummary(); // Update the summary when the selected day changes

                }
            }
        }
        private void UpdateDaySummary()
        {
            if (SelectedDay == null) return;

            DaySummary.Breakfast = new ObservableCollection<string>(SelectedDay.MealPlans
                .Where(mp => mp.MealType == "Breakfast")
                .Select(mp => $"{mp.ProductName} - {mp.Grams}g"));

            DaySummary.MidMorningSnack = new ObservableCollection<string>(SelectedDay.MealPlans
                .Where(mp => mp.MealType == "Morning Lunch")
                .Select(mp => $"{mp.ProductName} - {mp.Grams}g"));

            DaySummary.Lunch = new ObservableCollection<string>(SelectedDay.MealPlans
                .Where(mp => mp.MealType == "Lunch")
                .Select(mp => $"{mp.ProductName} - {mp.Grams}g"));
            DaySummary.MiddleDaySnack = new ObservableCollection<string>(SelectedDay.MealPlans
                .Where(mp => mp.MealType == "Middle Day")
                .Select(mp => $"{mp.ProductName} - {mp.Grams}g"));

            DaySummary.Dinner = new ObservableCollection<string>(SelectedDay.MealPlans
                .Where(mp => mp.MealType == "Dinner")
                .Select(mp => $"{mp.ProductName} - {mp.Grams}g"));

            DaySummary.BeforeBed = new ObservableCollection<string>(SelectedDay.MealPlans
               .Where(mp => mp.MealType == "Before Bed")
               .Select(mp => $"{mp.ProductName} - {mp.Grams}g"));


            // Repeat for other meal types...
            OnPropertyChanged(nameof(DaySummary));
        }
        private void OnMealPlansCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // Trigger recalculation of totals
            OnPropertyChanged(nameof(TotalCarbs));
            OnPropertyChanged(nameof(TotalFat));
            OnPropertyChanged(nameof(TotalProtein));
            OnPropertyChanged(nameof(TotalCalories));


            OnPropertyChanged(nameof(IsCarbsWithinRange));
            OnPropertyChanged(nameof(IsFatWithinRange));
            OnPropertyChanged(nameof(IsProteinWithinRange));
            UpdateDaySummary();
        }

        private void OnMealPlanGramsChanged(object sender, EventArgs e)
        {
            // Update totals when Grams changes

            OnPropertyChanged(nameof(TotalCarbs));
            OnPropertyChanged(nameof(TotalFat));
            OnPropertyChanged(nameof(TotalProtein));
            OnPropertyChanged(nameof(TotalCalories));
            UpdateDaySummary();


        }

        public double TotalCarbs => SelectedDay?.MealPlans.Sum(mp => mp.Carbs) ?? 0;
        public double TotalFat => SelectedDay?.MealPlans.Sum(mp => mp.Fat) ?? 0;
        public double TotalProtein => SelectedDay?.MealPlans.Sum(mp => mp.Protein) ?? 0;
        public double TotalCalories => SelectedDay?.MealPlans.Sum(mp => mp.Calories) ?? 0;

        // Daily goals from database
        private int _dailyCarbs;
        public int DailyCarbs
        {
            get => _dailyCarbs;
            set
            {
                _dailyCarbs = value;
                OnPropertyChanged(nameof(DailyCarbs));
                OnPropertyChanged(nameof(IsCarbsWithinRange));
            }
        }

        private int _dailyFat;
        public int DailyFat
        {
            get => _dailyFat;
            set
            {
                _dailyFat = value;
                OnPropertyChanged(nameof(DailyFat));
                OnPropertyChanged(nameof(IsFatWithinRange));
            }
        }

        private int _dailyProtein;
        public int DailyProtein
        {
            get => _dailyProtein;
            set
            {
                _dailyProtein = value;
                OnPropertyChanged(nameof(DailyProtein));
                OnPropertyChanged(nameof(IsProteinWithinRange));
            }
        }

        // Boolean properties for comparison
        public bool IsCarbsWithinRange => IsWithinRange(TotalCarbs, DailyCarbs);
        public bool IsFatWithinRange => IsWithinRange(TotalFat, DailyFat);
        public bool IsProteinWithinRange => IsWithinRange(TotalProtein, DailyProtein);

        private bool IsWithinRange(double total, int goal)
        {
            const double threshold = 0.05; // 5% range
            return Math.Abs(total - goal) / goal <= threshold;
        }

        private MealPlan _selectedMealPlan;
        public MealPlan SelectedMealPlan
        {
            get => _selectedMealPlan;
            set
            {
                _selectedMealPlan = value;
                OnPropertyChanged(nameof(SelectedMealPlan));
            }
        }



        #endregion



        #region Commands
        public ICommand AddProductCommand { get; }

        private void AddProductCommandExecuted(object obj)
        {
            if (SelectedDay == null || SelectedProduct == null || SelectedMealType == null) return;

            var productValues = GetProductNutritionalValues(SelectedProduct);

            var grams = 100; // Default grams value
            var protein = productValues.Protein / 100 * grams;
            var carbs = productValues.Carbs / 100 * grams;
            var fat = productValues.Fat / 100 * grams;
            var calories = productValues.Calories / 100 * grams;

            // Create a new MealPlan with these values
            var newMealPlan = new MealPlan
            {
                MealType = SelectedMealType,
                ProductName = SelectedProduct,
                ProteinPerGram = productValues.Protein / 100, // Convert to per gram
                CarbsPerGram = productValues.Carbs / 100,
                FatPerGram = productValues.Fat / 100,
                CaloriesPerGram = productValues.Calories / 100,
                Grams = grams // Default value
            };

            // Subscribe to the MealTypeChanged event
            newMealPlan.MealTypeChanged += OnMealTypeChanged;

            // Add the new MealPlan to the collection
            SelectedDay.MealPlans.Add(newMealPlan);

            SaveMealPlanToDatabase(SelectedDay.Day, SelectedMealType, SelectedProduct, grams, protein, carbs, fat, calories);

            SelectedProduct = null;
            UpdateDaySummary();
        }
        private (double Protein, double Carbs, double Fat, double Calories) GetProductNutritionalValues(string productName)
        {
            using (var connection = new SqlConnection("Server=localhost\\SQLEXPRESS;Database=DietPlanningDB;Trusted_Connection=True;"))
            {
                var command = new SqlCommand("SELECT Protein, Carbs, Fat, Calories FROM Products WHERE Name = @ProductName", connection);
                command.Parameters.AddWithValue("@ProductName", productName);
                connection.Open();

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return (
                            Protein: Convert.ToDouble(reader["Protein"]),
                            Carbs: Convert.ToDouble(reader["Carbs"]),
                            Fat: Convert.ToDouble(reader["Fat"]),
                            Calories: Convert.ToDouble(reader["Calories"])
                        );
                    }
                }
            }

            return (0, 0, 0, 0); // Default values if not found
        }

        private void SaveMealPlanToDatabase(string day, string mealType, string productName, int grams, double protein, double carbs, double fat, double calories)
        {
            using (var connection = new SqlConnection("Server=localhost\\SQLEXPRESS;Database=DietPlanningDB;Trusted_Connection=True;"))
            {
                var command = new SqlCommand(@"
            INSERT INTO MealPlans (Day, MealType, ProductName, Grams, Protein, Carbs, Fat, Calories)
            VALUES (@Day, @MealType, @ProductName, @Grams, @Protein, @Carbs, @Fat, @Calories)", connection);

                command.Parameters.AddWithValue("@Day", day);
                command.Parameters.AddWithValue("@MealType", mealType);
                command.Parameters.AddWithValue("@ProductName", productName);
                command.Parameters.AddWithValue("@Grams", grams);
                command.Parameters.AddWithValue("@Protein", protein);
                command.Parameters.AddWithValue("@Carbs", carbs);
                command.Parameters.AddWithValue("@Fat", fat);
                command.Parameters.AddWithValue("@Calories", calories);

                connection.Open();
                command.ExecuteNonQuery();
            }
        }



        public ICommand RemoveProductCommand { get; }
        private void RemoveProductCommandExecuted(object obj)
        {
            if (SelectedMealPlan == null || SelectedDay == null)
            {
                System.Windows.MessageBox.Show("Please select a meal plan to remove.", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            // Remove the meal plan from the ObservableCollection

            // Remove from the database
            using (var connection = new SqlConnection("Server=localhost\\SQLEXPRESS;Database=DietPlanningDB;Trusted_Connection=True;"))
            {
                var command = new SqlCommand(
                    "DELETE FROM MealPlans WHERE Day = @Day AND MealType = @MealType AND ProductName = @ProductName",
                    connection
                );
                command.Parameters.AddWithValue("@Day", SelectedDay.Day);
                command.Parameters.AddWithValue("@MealType", SelectedMealPlan.MealType);
                command.Parameters.AddWithValue("@ProductName", SelectedMealPlan.ProductName);

                connection.Open();
                command.ExecuteNonQuery();

                SelectedDay.MealPlans.Remove(SelectedMealPlan);
            }

            System.Windows.MessageBox.Show("Meal plan removed successfully.", "Success",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        #endregion

    }

    public class DayMealPlan : INotifyPropertyChanged
    {
        public string Day { get; set; }
        public ObservableCollectionWithItemNotify<MealPlan> MealPlans { get; set; }

        public DayMealPlan(string day)
        {
            Day = day;
            MealPlans = LoadMealPlansForDay(day); // Automatically listens for item changes
        }

        private ObservableCollectionWithItemNotify<MealPlan> LoadMealPlansForDay(string day)
        {
            
            var mealPlans = new ObservableCollectionWithItemNotify<MealPlan>();

            using (var connection = new SqlConnection("Server=localhost\\SQLEXPRESS;Database=DietPlanningDB;Trusted_Connection=True;"))
            {
                var command = new SqlCommand(@"
            SELECT 
                Id,
                MealType, 
                ProductName, 
                Grams, 
                Protein, 
                Carbs, 
                Fat, 
                Calories
            FROM MealPlans
            WHERE Day = @Day", connection);

                command.Parameters.AddWithValue("@Day", day);
                connection.Open();

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        

                        mealPlans.Add(new MealPlan
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            MealType = reader["MealType"].ToString(),
                            ProductName = reader["ProductName"].ToString(),
                            Grams = Convert.ToInt32(reader["Grams"]),
                            ProteinPerGram = Convert.ToDouble(reader["Protein"]) / Convert.ToInt32(reader["Grams"]),
                            CarbsPerGram = Convert.ToDouble(reader["Carbs"]) / Convert.ToInt32(reader["Grams"]),
                            FatPerGram = Convert.ToDouble(reader["Fat"]) / Convert.ToInt32(reader["Grams"]),
                            CaloriesPerGram = Convert.ToDouble(reader["Calories"]) / Convert.ToInt32(reader["Grams"])
                        });
                    }
                }
            }

            return mealPlans;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


    public class MealPlan : INotifyPropertyChanged
    {
        public int Id { get; set; } // Unique identifier for each meal plan

        public string ProductName { get; set; }

        private string _mealType;
        public string MealType
        {
            get => _mealType;
            set
            {
                if (_mealType != value)
                {
                    _mealType = value;
                    OnPropertyChanged(nameof(MealType));
                    // Trigger event when MealType changes
                    MealTypeChanged?.Invoke(this, EventArgs.Empty); // Raise the event
                }
            }
        }
        // Event to notify changes in MealType
        public event EventHandler MealTypeChanged;

        public double ProteinPerGram { get; set; }
        public double CarbsPerGram { get; set; }
        public double FatPerGram { get; set; }
        public double CaloriesPerGram { get; set; }

        private int _grams;
        public int Grams
        {
            get => _grams;
            set
            {
                if (_grams != value)
                {
                    _grams = value;
                    OnPropertyChanged(nameof(Grams));
                    OnPropertyChanged(nameof(Protein));
                    OnPropertyChanged(nameof(Carbs));
                    OnPropertyChanged(nameof(Fat));
                    OnPropertyChanged(nameof(Calories));
                    if (_grams != 0 && Calories != 0)
                    {
                        SaveGramsToDatabase();
                    }
                }
            }
        }

        public double Protein => ProteinPerGram * Grams;
        public double Carbs => CarbsPerGram * Grams;
        public double Fat => FatPerGram * Grams;
        public double Calories => CaloriesPerGram * Grams;

        
        private void SaveGramsToDatabase()
        {
            using (var connection = new SqlConnection("Server=localhost\\SQLEXPRESS;Database=DietPlanningDB;Trusted_Connection=True;"))
            {
                var command = new SqlCommand(@"
                   UPDATE MealPlans
                SET Grams = @Grams, 
                    Protein = @Protein, 
                    Carbs = @Carbs, 
                    Fat = @Fat, 
                    Calories = @Calories
                 WHERE Id = @Id", connection);

                command.Parameters.AddWithValue("@Grams", Grams);
                command.Parameters.AddWithValue("@Protein", Protein);
                command.Parameters.AddWithValue("@Carbs", Carbs);
                command.Parameters.AddWithValue("@Fat", Fat);
                command.Parameters.AddWithValue("@Calories", Calories);
                command.Parameters.AddWithValue("@ProductName", ProductName);
                command.Parameters.AddWithValue("@MealType", MealType);
                command.Parameters.AddWithValue("@Id", Id);

                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class DaySummary : INotifyPropertyChanged
    {
        private ObservableCollection<string> _breakfast;
        public ObservableCollection<string> Breakfast
        {
            get => _breakfast;
            set { _breakfast = value; OnPropertyChanged(nameof(Breakfast)); }
        }

        private ObservableCollection<string> _midMorningSnack;
        public ObservableCollection<string> MidMorningSnack
        {
            get => _midMorningSnack;
            set { _midMorningSnack = value; OnPropertyChanged(nameof(MidMorningSnack)); }
        }

        private ObservableCollection<string> _lunch;
        public ObservableCollection<string> Lunch
        {
            get => _lunch;
            set { _lunch = value; OnPropertyChanged(nameof(Lunch)); }
        }
        private ObservableCollection<string> _middleDaySnack;
        public ObservableCollection<string> MiddleDaySnack
        {
            get => _middleDaySnack;
            set { _middleDaySnack = value; OnPropertyChanged(nameof(MiddleDaySnack)); }
        }

        private ObservableCollection<string> _dinner;
        public ObservableCollection<string> Dinner
        {
            get => _dinner;
            set { _dinner = value; OnPropertyChanged(nameof(Dinner)); }
        }
        private ObservableCollection<string> _beforeBed;
        public ObservableCollection<string> BeforeBed
        {
            get => _beforeBed;
            set { _beforeBed = value; OnPropertyChanged(nameof(BeforeBed)); }
        }

        // Add properties for other meal types...

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


    public class ObservableCollectionWithItemNotify<T> : ObservableCollection<T> where T : INotifyPropertyChanged
    {
        protected override void InsertItem(int index, T item)
        {
            base.InsertItem(index, item);
            if (item != null)
            {
                item.PropertyChanged += Item_PropertyChanged;
            }
        }

        protected override void RemoveItem(int index)
        {
            if (index >= 0 && index < Count)
            {
                var item = this[index];
                if (item != null)
                {
                    item.PropertyChanged -= Item_PropertyChanged;
                }
            }
            base.RemoveItem(index);
        }

        protected override void ClearItems()
        {
            foreach (var item in this)
            {
                if (item != null)
                {
                    item.PropertyChanged -= Item_PropertyChanged;
                }
            }
            base.ClearItems();
        }

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Raise CollectionChanged event when any item changes
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
