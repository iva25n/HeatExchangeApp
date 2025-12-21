using Microsoft.AspNetCore.Mvc;
using HeatExchangeApp.Core.Models;
using HeatExchangeApp.Core.Services;
using HeatExchangeApp.Web.Models;
using HeatExchangeApp.Web.Services;
using System;
using System.Text;

namespace HeatExchangeApp.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHeatExchangeCalculator _calculator;
        private readonly ICalculationStorage _storage;

        public HomeController(IHeatExchangeCalculator calculator, ICalculationStorage storage)
        {
            _calculator = calculator;
            _storage = storage;
        }

        public IActionResult Index()
        {
            var model = new CalculationViewModel
            {
                Request = new CalculationRequest(),
                SavedCalculations = _storage.GetAllCalculations()
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult Calculate([FromBody] CalculationRequest request)
        {
            try
            {
                var result = _calculator.Calculate(request);
                var savedCalc = new SavedCalculation
                {
                    Id = Guid.NewGuid(),
                    Material = request.Material,
                    Gas = request.Gas,
                    Parameters = request.Parameters,
                    Name = request.Name,
                    Description = request.Description,
                    Result = result,
                    CreatedAt = DateTime.Now
                };

                _storage.SaveCalculation(savedCalc);

                return Json(new
                {
                    success = true,
                    result = result,
                    id = savedCalc.Id
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }

        [HttpGet]
        public IActionResult GetCalculation(Guid id)
        {
            var calculation = _storage.GetCalculation(id);
            if (calculation == null)
                return NotFound();

            return Json(calculation);
        }

        [HttpPost]
        public IActionResult DeleteCalculation(Guid id)
        {
            try
            {
                _storage.DeleteCalculation(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult ExportToCsv(Guid id)
        {
            var calculation = _storage.GetCalculation(id);
            if (calculation == null)
                return NotFound();

            var csv = GenerateCsv(calculation);
            var bytes = Encoding.UTF8.GetBytes(csv);

            return File(bytes, "text/csv", $"calculation_{id}.csv");
        }

        [HttpGet]
        public IActionResult ExportAll()
        {
            var calculations = _storage.GetAllCalculations();
            var json = System.Text.Json.JsonSerializer.Serialize(calculations,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

            var bytes = Encoding.UTF8.GetBytes(json);
            return File(bytes, "application/json", "all_calculations.json");
        }

        private string GenerateCsv(SavedCalculation calculation)
        {
            var result = calculation.Result;
            var csv = new StringBuilder();
            csv.AppendLine("Высота (м),Температура материала (°C),Температура газа (°C),Разность температур (°C)");

            for (int i = 0; i < result.Heights.Count; i++)
            {
                csv.AppendLine($"{result.Heights[i]:F3},{result.MaterialTemperatures[i]:F1},{result.GasTemperatures[i]:F1},{result.TemperatureDifferences[i]:F1}");
            }

            return csv.ToString();
        }
    }
}
