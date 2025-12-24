using HeatExchangeApp.Core.Models; 
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace HeatExchangeApp.Web.Services
{
    public class JsonCalculationStorage : ICalculationStorage
    {
        private readonly string _storagePath;
        private List<SavedCalculation> _calculations;

        public JsonCalculationStorage(IWebHostEnvironment env)
        {
            _storagePath = Path.Combine(env.ContentRootPath, "Data", "calculations.json");
            LoadCalculations();
        }

        private void LoadCalculations()
        {
            if (File.Exists(_storagePath))
            {
                var json = File.ReadAllText(_storagePath);
                _calculations = JsonSerializer.Deserialize<List<SavedCalculation>>(json)
                    ?? new List<SavedCalculation>();
            }
            else
            {
                _calculations = new List<SavedCalculation>();
            }
        }

        private void SaveCalculations()
        {
            var directory = Path.GetDirectoryName(_storagePath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(_calculations,
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_storagePath, json);
        }

        public void SaveCalculation(SavedCalculation calculation)
        {
            var existing = _calculations.FirstOrDefault(c => c.Id == calculation.Id);
            if (existing != null)
            {
                _calculations.Remove(existing);
            }
            _calculations.Add(calculation);
            SaveCalculations();
        }

        public SavedCalculation GetCalculation(Guid id)
        {
            return _calculations.FirstOrDefault(c => c.Id == id);
        }

        public List<SavedCalculation> GetAllCalculations()
        {
            return _calculations.OrderByDescending(c => c.CreatedAt).ToList();
        }

        public void DeleteCalculation(Guid id)
        {
            var calculation = GetCalculation(id);
            if (calculation != null)
            {
                _calculations.Remove(calculation);
                SaveCalculations();
            }
        }
    }
}