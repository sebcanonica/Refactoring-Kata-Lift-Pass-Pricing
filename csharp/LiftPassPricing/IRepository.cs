using System;

namespace LiftPassPricing
{
    public interface IRepository
    {
        double GetBaseCost(string type);
        bool isHoliday(DateTime date);
    }
}