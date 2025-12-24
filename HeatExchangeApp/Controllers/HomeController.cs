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
                    Description = $"Расчет по методике УрФУ. {result.Description}",
                    Result = result,
                    CreatedAt = DateTime.Now
                };

                _storage.SaveCalculation(savedCalc);

                return Json(new
                {
                    success = true,
                    result = new
                    {
                        heights = result.Heights,
                        materialTemperatures = result.MaterialTemperatures,
                        gasTemperatures = result.GasTemperatures,
                        temperatureDifferences = result.TemperatureDifferences,
                        heatTransferCoefficient = result.HeatTransferCoefficient,
                        totalHeatTransfer = result.TotalHeatTransfer,
                        efficiency = result.Efficiency,
                        methodDescription = result.Description,
                        alphaV = request.Parameters.VolumeHeatTransferCoefficient,
                        materialFlowRate_kg_s = request.Parameters.MaterialFlowRate / 3600,
                        gasFlowRate_kg_s = request.Parameters.GasFlowRate / 3600
                    },
                    id = savedCalc.Id
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetCalculation(Guid id)
        {
            try
            {
                var calculation = _storage.GetCalculation(id);
                if (calculation == null)
                {
                    return Json(new { success = false, error = "Расчет не найден" });
                }
                return Json(calculation);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult DeleteCalculation([FromBody] DeleteRequest deleteRequest) 
        {
            try
            {
                Console.WriteLine($"Удаление расчета: {deleteRequest.Id}");
                _storage.DeleteCalculation(deleteRequest.Id);
                return Json(new { success = true, message = "Расчет удален" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка удаления: {ex.Message}");
                return Json(new { success = false, error = ex.Message });
            }
        }

        public class DeleteRequest
        {
            public Guid Id { get; set; }
        }

        [HttpGet]
        public IActionResult ExportToCsv(Guid id)
        {
            try
            {
                var calculation = _storage.GetCalculation(id);
                if (calculation == null)
                    return NotFound();

                var csv = new StringBuilder();
                byte[] bom = Encoding.UTF8.GetPreamble();
                var stream = new MemoryStream();
                stream.Write(bom, 0, bom.Length);

                csv.AppendLine("Высота (м);Температура материала (°C);Температура газа (°C);Разность температур (°C)");

                var result = calculation.Result;
                for (int i = 0; i < result.Heights.Count; i++)
                {
                    csv.AppendLine($"{result.Heights[i]:F3};{result.MaterialTemperatures[i]:F1};{result.GasTemperatures[i]:F1};{result.TemperatureDifferences[i]:F1}");
                }

                byte[] csvBytes = Encoding.UTF8.GetBytes(csv.ToString());
                stream.Write(csvBytes, 0, csvBytes.Length);

                return File(stream.ToArray(), "text/csv; charset=utf-8",
                    $"calculation_{calculation.Name.Replace(" ", "_")}.csv");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при создании CSV файла: {ex.Message}");
            }
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
    }
}