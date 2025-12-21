using HeatExchangeApp.Core.Models;

namespace HeatExchangeApp.Core.Services
{
    public interface IHeatExchangeCalculator
    {
        CalculationResult Calculate(CalculationRequest request);
    }
}