using HeatExchangeApp.Core.Models;
using System;
using System.Collections.Generic;

namespace HeatExchangeApp.Web.Services
{
    public interface ICalculationStorage
    {
        void SaveCalculation(SavedCalculation calculation);
        SavedCalculation GetCalculation(Guid id);
        List<SavedCalculation> GetAllCalculations();
        void DeleteCalculation(Guid id);
    }
}