using System;
using System.Globalization;
using Nancy;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Linq;

namespace LiftPassPricing
{
    public class Prices : NancyModule
    {
        private const string QueryCostByType = "SELECT cost FROM base_price " + //
                                               "WHERE type = @type";
        private const string InsertBasePrice = "INSERT INTO base_price (type, cost) VALUES (@type, @cost) " + //
                                               "ON DUPLICATE KEY UPDATE cost = @cost;";
        private const string QueryHolidays = "SELECT * FROM holidays";
        public readonly MySqlConnection connection;

        public Prices()
        {
            connection = new MySqlConnection
            {
                ConnectionString = @"Database=lift_pass;Data Source=localhost;User Id=root;Password=mysql"
            };
            connection.Open();

            Put("/prices", _ =>
            {
                int liftPassCost = int.Parse(Request.Query["cost"]);
                string liftPassType = Request.Query["type"];

                using (var command = new MySqlCommand( //
                       InsertBasePrice, connection))
                {
                    command.Parameters.AddWithValue("@type", liftPassType);
                    command.Parameters.AddWithValue("@cost", liftPassCost);
                    command.Prepare();
                    command.ExecuteNonQuery();
                }

                return "";
            });

            base.Get("/prices", _ =>
            {
                int? age = base.Request.Query["age"] != null ? int.Parse(base.Request.Query["age"]) : null;
                string type = Request.Query["type"];
                string dateParam = base.Request.Query["date"];
                DateTime? date = null;
                if  (dateParam != null) 
                {
                    date = DateTime.ParseExact(dateParam, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                };

                double baseCost = GetBaseCost(type);

                int reduction;
                
                if (age != null && age < 6)
                {
                    return "{ \"cost\": 0}";
                }
                else
                {
                    
                    reduction = 0;

                    if (!"night".Equals(type))
                    {
                        if (date != null)
                        {
                            bool isHoliday = this.isHoliday(date.Value);

                            if (!isHoliday && (int)date?.DayOfWeek == 1)
                            {
                                reduction = 35;
                            }
                        }

                        // TODO apply reduction for others
                        if (age != null && age < 15)
                        {
                            return "{ \"cost\": " + (int)Math.Ceiling(baseCost * .7) + "}";
                        }
                        else
                        {
                            if (age == null)
                            {
                                double cost = baseCost * (1 - reduction / 100.0);
                                return "{ \"cost\": " + (int)Math.Ceiling(cost) + "}";
                            }
                            else
                            {
                                if (age > 64)
                                {
                                    double cost = baseCost * .75 * (1 - reduction / 100.0);
                                    return "{ \"cost\": " + (int)Math.Ceiling(cost) + "}";
                                }
                                else
                                {
                                    double cost = baseCost * (1 - reduction / 100.0);
                                    return "{ \"cost\": " + (int)Math.Ceiling(cost) + "}";
                                }
                            }
                        }
                    }
                    else
                    {
                        if (age != null && age >= 6)
                        {
                            if (age > 64)
                            {
                                return "{ \"cost\": " + (int)Math.Ceiling(baseCost * .4) + "}";
                            }
                            else
                            {
                                return "{ \"cost\": " + baseCost + "}";
                            }
                        }
                        else
                        {
                            return "{ \"cost\": 0}";
                        }
                    }
                }
            });

            After += ctx =>
            {
                ctx.Response.ContentType = "application/json";
            };

        }

        public bool isHoliday(DateTime date)
        {
            using (var holidayCmd = new MySqlCommand(QueryHolidays, connection))
            {
                holidayCmd.Prepare();
                using (var holidays = holidayCmd.ExecuteReader())
                {
                    while (holidays.Read())
                    {
                        var holiday = holidays.GetDateTime("holiday");

                        if (date.Year == holiday.Year &&
                            date.Month == holiday.Month &&
                            date.Date == holiday.Date)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public double GetBaseCost(string type)
        {
            using (var costCmd = new MySqlCommand(QueryCostByType, connection))
            {
                costCmd.Parameters.AddWithValue("@type", type);
                costCmd.Prepare();
                return (int)costCmd.ExecuteScalar();
            }
        }
    }
}
