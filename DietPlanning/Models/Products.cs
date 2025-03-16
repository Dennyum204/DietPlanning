using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DietPlanning.Models
{
    public class Product
    {
        public string Name { get; set; }
        public double Protein { get; set; }
        public double Carbs { get; set; }
        public double Fat { get; set; }
        public int Calories { get; set; }
        public string Category { get; set; }
    }
}
