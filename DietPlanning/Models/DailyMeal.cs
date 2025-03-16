using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DietPlanning.Models
{
    public class DailyMeal
    {
        public string Day { get; set; }
        public string MealType { get; set; }
        public List<Product> Products { get; set; } = new List<Product>();
    }
}
