using Adapters;
using DietPlanning.ViewModels;
using System;
using System.Windows.Input;

namespace DietPlanning.ViewModels
{
    public class MainViewModel : BaseViewModel
    {

        // Keep instances of ViewModels
        private readonly MealPlannerViewModel _mealPlannerViewModel;
        private readonly AddProductViewModel _addProductViewModel;
        private readonly RemoveProductViewModel _removeProductViewModel;
        private readonly EditProductViewModel _editProductViewModel;
        private readonly GoalPlannerViewModel _goalPlannerViewModel;
        private readonly WeeklySummaryViewModel _WeeklySummaryViewModel;


        public ICommand AddProductCommand { get; }
        public ICommand RemoveProductCommand { get; }
        public ICommand EditProductCommand { get; }
        public ICommand OpenMealPlannerCommand { get; }
        public ICommand OpenGoalPlannerCommand { get; }
        public ICommand OpenWeeklySummaryCommand { get; }

        private BaseViewModel _currentViewModel;
        public BaseViewModel CurrentViewModel
        {
            get => _currentViewModel;
            set
            {
                _currentViewModel = value;
                OnPropertyChanged(nameof(CurrentViewModel));
            }
        }

        public MainViewModel()
        {
            // Initialize ViewModels
            _mealPlannerViewModel = new MealPlannerViewModel();
            _addProductViewModel = new AddProductViewModel();
            _removeProductViewModel = new RemoveProductViewModel();
            _editProductViewModel = new EditProductViewModel();
            _goalPlannerViewModel = new GoalPlannerViewModel();
            _WeeklySummaryViewModel = new WeeklySummaryViewModel();


            // Set default view
            CurrentViewModel = _WeeklySummaryViewModel;


            // Initialize commands
            AddProductCommand = new ViewModelCommands(AddProductCommandExecuted);
            RemoveProductCommand = new ViewModelCommands(RemoveProductCommandExecuted);
            EditProductCommand = new ViewModelCommands(EditProductCommandExecuted);
            OpenMealPlannerCommand = new ViewModelCommands(OpenMealPlannerCommandExecuted);
            OpenGoalPlannerCommand = new ViewModelCommands(OpenGoalPlannerCommandExecuted);
            OpenWeeklySummaryCommand = new ViewModelCommands(OpenWeeklySummaryCommandExecuted);

        }

        private void OpenWeeklySummaryCommandExecuted(object obj)
        {
            CurrentViewModel = _WeeklySummaryViewModel;
            _WeeklySummaryViewModel.Reload(); ;
        }
        private void OpenGoalPlannerCommandExecuted(object obj)
        {
            CurrentViewModel = _goalPlannerViewModel;
        }

        private void OpenMealPlannerCommandExecuted(object obj)
        {
            CurrentViewModel = _mealPlannerViewModel;
        }

        private void EditProductCommandExecuted(object obj)
        {
            CurrentViewModel = _editProductViewModel;
        }

        private void RemoveProductCommandExecuted(object obj)
        {
            CurrentViewModel = _removeProductViewModel;
        }

        private void AddProductCommandExecuted(object obj)
        {
            CurrentViewModel = _addProductViewModel;
        }

    }

}