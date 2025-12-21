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

            // Конвертация расходов в кг/с
            double G_m = parameters.MaterialFlowRate / 3600;
            double G_g = parameters.GasFlowRate / 3600;

            // Расчет теплоемкостных расходов
            double W_m = G_m * request.Material.SpecificHeat;
            double W_g = G_g * request.Gas.SpecificHeat;

            // Коэффициент теплообмена
            double alpha = CalculateHeatTransferCoefficient(request);
            result.HeatTransferCoefficient = alpha;

            // Поверхность теплообмена на единицу высоты
            double S_per_height = CalculateSpecificSurface(request);

            // Шаг по высоте
            double dh = parameters.Height / parameters.CalculationSteps;

            // Начальные условия
            double T_m = parameters.MaterialInletTemp;
            double T_g = parameters.GasInletTemp;

            // Очищаем списки
            result.Heights.Clear();
            result.MaterialTemperatures.Clear();
            result.GasTemperatures.Clear();
            result.TemperatureDifferences.Clear();

            for (int i = 0; i <= parameters.CalculationSteps; i++)
            {
                double height = i * dh;

                // Уравнения теплообмена для противоточного движения
                // Для материала (движется вниз):
                double dT_m_dh = (alpha * S_per_height * (T_g - T_m)) / W_m;

                // Для газа (движется вверх):
                double dT_g_dh = -(alpha * S_per_height * (T_g - T_m)) / W_g;

                // Интегрирование методом Эйлера
                T_m += dT_m_dh * dh;
                T_g += dT_g_dh * dh;

                // Корректировка пределов температуры
                T_m = Math.Max(T_m, parameters.MaterialInletTemp);
                T_g = Math.Min(T_g, parameters.GasInletTemp);

                // Проверка на пересечение температур
                if (T_m >= T_g)
                {
                    T_m = T_g - 1;
                }

                result.Heights.Add(height);
                result.MaterialTemperatures.Add(T_m);
                result.GasTemperatures.Add(T_g);
                result.TemperatureDifferences.Add(T_g - T_m);
            }

            // Расчет общего теплопереноса
            result.TotalHeatTransfer = W_g * (parameters.GasInletTemp - result.GasTemperatures[^1]);

            // Расчет эффективности
            double maxPossibleHeat = Math.Min(W_g, W_m) * (parameters.GasInletTemp - parameters.MaterialInletTemp);
            if (maxPossibleHeat > 0)
            {
                result.Efficiency = (result.TotalHeatTransfer / maxPossibleHeat) * 100;
            }
            else
            {
                result.Efficiency = 0;
            }

            result.CalculationTime = DateTime.Now;

            return result;
        }

        private double CalculateHeatTransferCoefficient(CalculationRequest request)
        {
            // Упрощенный расчет коэффициента теплообмена
            // На основе модели для плотного слоя

            double d = request.Material.ParticleSize;
            double rho_g = request.Gas.Density;
            double mu_g = request.Gas.Viscosity;
            double lambda_g = request.Gas.ThermalConductivity;
            double cp_g = request.Gas.SpecificHeat;

            // Скорость газа в свободном сечении
            double A = request.Parameters.CrossSection;
            double G_g = request.Parameters.GasFlowRate / 3600;
            double epsilon = request.Material.Porosity;
            double u = (G_g / (rho_g * A)) / epsilon;

            // Число Рейнольдса
            double Re = rho_g * u * d / mu_g;

            // Число Прандтля
            double Pr = mu_g * cp_g / lambda_g;

            // Критериальное уравнение для теплообмена в слое
            double Nu = 0.106 * Math.Pow(Re, 0.72) * Math.Pow(Pr, 0.33);

            return Nu * lambda_g / d;
        }

        private double CalculateSpecificSurface(CalculationRequest request)
        {
            // Удельная поверхность частиц
            double d = request.Material.ParticleSize;
            double epsilon = request.Material.Porosity;
            double A = request.Parameters.CrossSection;

            // Для сферических частиц
            double a = 6 * (1 - epsilon) / d;

            return a * A;
        }
    }
}