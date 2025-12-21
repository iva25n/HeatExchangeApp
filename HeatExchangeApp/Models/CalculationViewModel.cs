using System.Collections.Generic;
using HeatExchangeApp.Core.Models;

namespace HeatExchangeApp.Web.Models
{
    public class CalculationViewModel
    {
        public CalculationRequest Request { get; set; } = new CalculationRequest();
        public List<SavedCalculation> SavedCalculations { get; set; } = new List<SavedCalculation>();
    }
}