using Microsoft.AspNetCore.Mvc;
using Retrive_Data_Async.Models;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;

namespace Retrive_Data_Async.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration _configuration;
        public HomeController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public IActionResult Index()
        {

            return View();
        }
        public ActionResult Synchronous()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            var customerData = GetTableData<Customer>("GetCustomerData");
            var employeeData = GetTableData<Employee>("GetEmployeeData");
            var studentData = GetTableData<Student>("GetStudentData");

            stopwatch.Stop();
            ViewBag.ElapsedTime = stopwatch.ElapsedMilliseconds;

            return View("DisplayTables", new DisplayTablesViewModel
            {
                CustomerData = customerData,
                EmployeeData = employeeData,
                StudentData = studentData
            });
        }
        private List<T> GetTableData<T>(string storedProcedure)
        {
            string ConnectionString = _configuration.GetConnectionString("connectionstring");
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(storedProcedure, connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        List<T> result = new List<T>();
                        while (reader.Read())
                        {
                            result.Add(MapReaderToModel<T>(reader));
                        }
                        return result;
                    }
                }
            }
        }
        private T MapReaderToModel<T>(SqlDataReader reader)
        {
            var model = Activator.CreateInstance<T>();

            var properties = typeof(T).GetProperties();

            foreach (var property in properties)
            {
                var columnName = property.Name; // Assuming column names match property names

                if (reader[columnName] != DBNull.Value)
                {
                    var value = Convert.ChangeType(reader[columnName], property.PropertyType);
                    property.SetValue(model, value, null);
                }
            }

            return model;
        }

        public async Task<ActionResult> Asynchronous()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            var customerTask = GetTableDataAsync<Customer>("GetCustomerData");
            var employeeTask = GetTableDataAsync<Employee>("GetEmployeeData");
            var studentTask = GetTableDataAsync<Student>("GetStudentData");

            await Task.WhenAll(customerTask, employeeTask, studentTask);

            stopwatch.Stop();
            ViewBag.ElapsedTime = stopwatch.ElapsedMilliseconds;

            return View("DisplayTables", new DisplayTablesViewModel
            {
                CustomerData = customerTask.Result,
                EmployeeData = employeeTask.Result,
                StudentData = studentTask.Result
            });
        }

        private async Task<List<T>> GetTableDataAsync<T>(string storedProcedure)
        {
            string ConnectionString = _configuration.GetConnectionString("connectionstring");
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                await connection.OpenAsync();
                using (SqlCommand command = new SqlCommand(storedProcedure, connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        List<T> result = new List<T>();
                        while (await reader.ReadAsync())
                        {
                            result.Add(MapReaderToModel<T>(reader));
                        }
                        return result;
                    }
                }
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}