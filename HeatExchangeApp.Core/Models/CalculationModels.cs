using System;
using System.Collections.Generic;

namespace HeatExchangeApp.Core.Models
{
    public class MaterialProperties
    {
        public string Name { get; set; } = "Железная руда";
        public double Density { get; set; } = 3500; // кг/м³
        public double SpecificHeat { get; set; } = 900; // Дж/(кг·°C)
        public double ParticleSize { get; set; } = 0.02; // м
        public double Porosity { get; set; } = 0.4; // пористость
    }

    public class GasProperties
    {
        public string Name { get; set; } = "Воздух";
        public double Density { get; set; } = 1.2; // кг/м³
        public double SpecificHeat { get; set; } = 1005; // Дж/(кг·°C)
        public double Viscosity { get; set; } = 1.8e-5; // Па·с
        public double ThermalConductivity { get; set; } = 0.026; // Вт/(м·°C)
    }

    public class LayerParameters
    {
        public double Height { get; set; } = 2.0; // м
        public double CrossSection { get; set; } = 1.0; // м²
        public double MaterialFlowRate { get; set; } = 1000; // кг/ч
        public double GasFlowRate { get; set; } = 500; // кг/ч
        public double MaterialInletTemp { get; set; } = 20; // °C
        public double GasInletTemp { get; set; } = 800; // °C
        public int CalculationSteps { get; set; } = 100;
    }

    public class CalculationResult
    {
        public List<double> Heights { get; set; } = new List<double>();
        public List<double> MaterialTemperatures { get; set; } = new List<double>();
        public List<double> GasTemperatures { get; set; } = new List<double>();
        public List<double> TemperatureDifferences { get; set; } = new List<double>();
        public double HeatTransferCoefficient { get; set; }
        public double TotalHeatTransfer { get; set; } // Вт
        public double Efficiency { get; set; } // %
        public DateTime CalculationTime { get; set; }
    }

    public class CalculationRequest
    {
        public MaterialProperties Material { get; set; } = new MaterialProperties();
        public GasProperties Gas { get; set; } = new GasProperties();
        public LayerParameters Parameters { get; set; } = new LayerParameters();
        public string Name { get; set; } = "Новый расчет";
        public string Description { get; set; } = string.Empty;
    }

    public class SavedCalculation : CalculationRequest
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public CalculationResult Result { get; set; } = new CalculationResult();
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}