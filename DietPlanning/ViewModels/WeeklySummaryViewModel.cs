using Adapters;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Kernel.Geom;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Input;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Layout.Properties;
using iText.Kernel.Colors;
using System.Windows.Media;

namespace DietPlanning.ViewModels
{
    public class WeeklySummaryViewModel : BaseViewModel
    {

        public Dictionary<string, DayMealGroup> WeeklySummary { get; set; }

        public WeeklySummaryViewModel()
        {
            Reload();

            ExportToPdfCommand = new ViewModelCommands(ExportToPdf);
        }

        public ICommand ExportToPdfCommand { get; }
        private void ExportToPdf(object obj)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "PDF file (*.pdf)|*.pdf",
                Title = "Save Weekly Summary as PDF"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;

                using (PdfWriter writer = new PdfWriter(filePath))
                {
                    using (PdfDocument pdf = new PdfDocument(writer))
                    {
                        Document document = new Document(pdf, PageSize.A4.Rotate()); // Landscape
                        document.SetMargins(20, 20, 20, 20);

                        // Load fonts
                        PdfFont boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                        PdfFont regularFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

                        // Title
                        document.Add(new Paragraph("Plano Dieta Semanal")
                            .SetFont(boldFont)
                            .SetFontSize(18)
                            .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER));

                        // Create table with 7 columns (one for each day)
                        Table table = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1, 1, 1, 1, 1, 1 }));
                        table.SetWidth(UnitValue.CreatePercentValue(100));


                     
                        // Add header row (Days of the week)
                        table.AddCell(CreateHeaderCell("Segunda"));
                        table.AddCell(CreateHeaderCell("Terça"));
                        table.AddCell(CreateHeaderCell("Quarta"));
                        table.AddCell(CreateHeaderCell("Quinta"));
                        table.AddCell(CreateHeaderCell("Sexta"));
                        table.AddCell(CreateHeaderCell("Sabado"));
                        table.AddCell(CreateHeaderCell("Domingo"));

                        // Meal types
                        string[] mealTypes = { "Breakfast", "MorningLunch", "Lunch", "Middle Day", "Dinner", "Before Bed" };
                        string[] mealTypesPT = { "PEQ. ALMOÇO / PÓS TREINO","MEIO DA MANHÃ", "ALMOÇO", "LANCHE", "JANTAR", "CEIA" };
                        int index = 0;

                        SolidColorBrush brush = (SolidColorBrush)System.Windows.Application.Current.FindResource("gridMealTypeHeader");                        
                        System.Windows.Media.Color wpfColor = brush.Color;
                        iText.Kernel.Colors.Color iTextColorSubHeader = new DeviceRgb(wpfColor.R, wpfColor.G, wpfColor.B);

                        foreach (var mealType in mealTypes)
                        {
                            // Meal type row spanning all 7 columns
                            Cell mealTypeCell = new Cell(1, 7)
                                .Add(new Paragraph(mealTypesPT[index]).SetFont(boldFont).SetFontSize(10))
                                .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                                .SetBackgroundColor(iTextColorSubHeader)
                                .SetFontColor(ColorConstants.WHITE);
                            table.AddCell(mealTypeCell);

                            // Add meals for each day
                            foreach (var day in new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" })
                            {
                                var meals = WeeklySummary.ContainsKey(day) ? GetMealsForType(WeeklySummary[day], mealType) : "";
                                table.AddCell(new Cell()
                                    .Add(new Paragraph(meals).SetFont(regularFont).SetFontSize(7))
                                    .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER));
                            }
                            index++;
                        }

                        document.Add(table);
                        document.Close();
                    }
                }

                MessageBox.Show("PDF Exported Successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // Helper function to create header cells with black background and white text
        private Cell CreateHeaderCell(string text)
        {
            SolidColorBrush brushHeader = (SolidColorBrush)System.Windows.Application.Current.FindResource("gridHeader");
            System.Windows.Media.Color wpfColorHeader = brushHeader.Color;
            iText.Kernel.Colors.Color iTextColorHeader = new DeviceRgb(wpfColorHeader.R, wpfColorHeader.G, wpfColorHeader.B);


            return new Cell()
                .Add(new Paragraph(text).SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD)).SetFontSize(12))
                .SetBackgroundColor(iTextColorHeader)
                .SetFontColor(ColorConstants.WHITE)
                .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER);
        }

        // Helper function to get meals for a given meal type
        private string GetMealsForType(DayMealGroup dayMealGroup, string mealType)
        {
            switch (mealType)
            {
                case "Breakfast": return string.Join("\n", dayMealGroup.Breakfast);
                case "MorningLunch": return string.Join("\n", dayMealGroup.MorningLunch);
                case "Lunch": return string.Join("\n", dayMealGroup.Lunch);
                case "Middle Day": return string.Join("\n", dayMealGroup.MiddleDay);
                case "Dinner": return string.Join("\n", dayMealGroup.Dinner);
                case "Before Bed": return string.Join("\n", dayMealGroup.BeforeBed);
                default: return "";
            }
        }
        public void Reload()
        {
            WeeklySummary = LoadWeeklySummary();
            OnPropertyChanged(nameof(WeeklySummary));
        }

        private Dictionary<string, DayMealGroup> LoadWeeklySummary()
        {
            var weeklyData = new Dictionary<string, DayMealGroup>
        {
            { "Monday", new DayMealGroup() },
            { "Tuesday", new DayMealGroup() },
            { "Wednesday", new DayMealGroup() },
            { "Thursday", new DayMealGroup() },
            { "Friday", new DayMealGroup() },
            { "Saturday", new DayMealGroup() },
            { "Sunday", new DayMealGroup() }
        };

            using (var connection = new SqlConnection("Server=localhost\\SQLEXPRESS;Database=DietPlanningDB;Trusted_Connection=True;"))
            {
                var command = new SqlCommand(@"
                SELECT Day, MealType, ProductName, Grams
                FROM MealPlans
                ORDER BY CASE
                    WHEN Day = 'Monday' THEN 1
                    WHEN Day = 'Tuesday' THEN 2
                    WHEN Day = 'Wednesday' THEN 3
                    WHEN Day = 'Thursday' THEN 4
                    WHEN Day = 'Friday' THEN 5
                    WHEN Day = 'Saturday' THEN 6
                    WHEN Day = 'Sunday' THEN 7
                END, MealType", connection);

                connection.Open();

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var day = reader["Day"].ToString();
                        var mealType = reader["MealType"].ToString();
                        var meal = $"{reader["ProductName"]} ({reader["Grams"]}g)";

                        if (weeklyData.ContainsKey(day))
                        {
                            weeklyData[day].AddMeal(mealType, meal);
                        }
                    }
                }
            }

            return weeklyData;
        }
    }

    public class DayMealGroup
    {
        public List<string> Breakfast { get; set; } = new List<string>();
        public List<string> MorningLunch { get; set; } = new List<string>();
        public List<string> Lunch { get; set; } = new List<string>();
        public List<string> MiddleDay { get; set; } = new List<string>();
        public List<string> Dinner { get; set; } = new List<string>();
        public List<string> BeforeBed { get; set; } = new List<string>();

        public void AddMeal(string mealType, string meal)
        {
            switch (mealType)
            {
                case "Breakfast": Breakfast.Add(meal); break;
                case "Morning Lunch": MorningLunch.Add(meal); break;
                case "Lunch": Lunch.Add(meal); break;
                case "Middle Day": MiddleDay.Add(meal); break;
                case "Dinner": Dinner.Add(meal); break;
                case "Before Bed": BeforeBed.Add(meal); break;
            }
        }
    }
}




