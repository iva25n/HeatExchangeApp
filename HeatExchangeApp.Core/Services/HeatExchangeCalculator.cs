using HeatExchangeApp.Core.Models;
using System;
using System.Collections.Generic;

namespace HeatExchangeApp.Core.Services
{
    public class HeatExchangeCalculator : IHeatExchangeCalculator
    {
        public CalculationResult Calculate(CalculationRequest request)
        {
            var result = new CalculationResult();
            var parameters = request.Parameters;


            // Конвертация расходов: кг/ч -> кг/с 
            double G_m_kg_per_sec = parameters.MaterialFlowRate / 3600.0;
            double G_g_kg_per_sec = parameters.GasFlowRate / 3600.0;

            // Теплоемкости:  Дж/(кг·°C) -> кДж/(кг·°C)
            double C_m_kJ = request.Material.SpecificHeat / 1000.0;
            double C_g_kJ = request.Gas.SpecificHeat / 1000.0;

            // Объемный расход газа Vг (м³/с) = массовый расход / плотность
            double V_g = G_g_kg_per_sec / request.Gas.Density;

            // Теплоемкости потоков (кВт/°C) = расход * удельная теплоемкость
            double W_m = G_m_kg_per_sec * C_m_kJ;  // кВт/°C для материала
            double W_g = V_g * (request.Gas.Density * C_g_kJ); // кВт/°C для газа

            // Отношение теплоемкостей m 
            double m = W_m / W_g;

            double alpha_v = parameters.VolumeHeatTransferCoefficient;
            if (alpha_v <= 0)
            {
                alpha_v = 2460.0; 
            }

            double S = parameters.CrossSection; // площадь сечения, м²
            double H0 = parameters.Height;      // полная высота слоя, м

            // Объемная теплоемкость газа в Дж/(м³·°C)
            double C_g_vol_J = request.Gas.Density * request.Gas.SpecificHeat;

            // Полная относительная высота Y₀
            double Y0 = (alpha_v * S * H0) / (V_g * C_g_vol_J);

            int steps = parameters.CalculationSteps;
            double delta_h = H0 / steps;

            double t_in = parameters.MaterialInletTemp; // t'
            double T_in = parameters.GasInletTemp;      // T'

            // Коэффициент для экспоненты: (m-1)/m
            double exp_coef = (m - 1.0) / m;

            // Знаменатель в формулах: 1 - m*exp[(m-1)Y₀/m]
            double denominator = 1.0 - m * Math.Exp(exp_coef * Y0);

            for (int i = 0; i <= steps; i++)
            {
                double height = i * delta_h;

                // Относительная высота Y для текущей точки
                double Y = (alpha_v * S * height) / (V_g * C_g_vol_J);

                // Вычисляем экспоненты
                double exp_term = Math.Exp(exp_coef * Y);

                // Безразмерная температура материала ϑ (формула 17)
                double theta_v = (1.0 - exp_term) / denominator;

                // Безразмерная температура газа θ (формула 18)
                double theta = (1.0 - m * exp_term) / denominator;

                // Абсолютные температуры (°C)
                double t = t_in + (T_in - t_in) * theta_v;  // t = t' + (T'-t')·ϑ
                double T = t_in + (T_in - t_in) * theta;    // T = t' + (T'-t')·θ

                // Ограничиваем значения
                if (T < t) T = t + 0.1;
                if (t > T) t = T - 0.1;

                // Заполняем результаты
                result.Heights.Add(height);
                result.MaterialTemperatures.Add(t);
                result.GasTemperatures.Add(T);
                result.TemperatureDifferences.Add(Math.Abs(T - t));
            }


            // Коэффициент теплоотдачи
            result.HeatTransferCoefficient = alpha_v;

            // Полный теплоперенос (Вт) = W_g * (Tвход_газа - Tвыход_газа)
            double T_g_out = result.GasTemperatures[^1];
            result.TotalHeatTransfer = W_g * 1000 * (T_in - T_g_out);

            // Эффективность (%) = фактический теплоперенос / максимально возможный
            double maxPossibleHeat = Math.Min(W_g, W_m) * 1000 * Math.Abs(T_in - t_in);
            if (maxPossibleHeat > 0)
            {
                result.Efficiency = (result.TotalHeatTransfer / maxPossibleHeat) * 100;
            }
            else
            {
                result.Efficiency = 0;
            }

            result.CalculationTime = DateTime.Now;

            // Дополнительная информация
            result.Description = $"m = {m:F3}, Y₀ = {Y0:F3}, αv = {alpha_v} Вт/(м³·°C)";

            return result;
        }
    }
}