using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using OfficeOpenXml;
using Project01.Clients.SMTP;
using Project01.Context;
using Project01.DTOs;
using Project01.Migrations;
using Project01.Models;
using System.Data;
using System.Text.RegularExpressions;

namespace Project01.Services.Employees
{
    public class EmployeeBusinessLogic : IEmployeeBusinessLogic
    {
        private readonly AppDbContext _dbContext;

        public EmployeeBusinessLogic(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<AppResponse> AddEmployee(EmployeeDTO model)
        {
            var response = new AppResponse();
            var employee = await _dbContext.Employees.FirstOrDefaultAsync(x => x.Cnic == model.Cnic && x.Ended == null);
            if (employee != null)
            {
                response.ResCode = 1;
                response.ResMsg = "Already Exist";
                response.ResBody = null;
                return response;
            }
            var emp = new Employee();
            emp.FirstName = model.FirstName;
            emp.LastName = model.LastName;
            emp.Cnic = model.Cnic;
            emp.Created = DateTime.Now;
            emp.CreatedBy = "";
            _dbContext.Employees.Add(emp);
            var result = await _dbContext.SaveChangesAsync();
            if (result > 1)
            {
                response.ResCode = 2;
                response.ResMsg = "Failed";
                response.ResBody = null;
                return response;
            }
            response.ResCode = 100;
            response.ResMsg = "Success";
            response.ResBody = model;
            return response;
        }

        public async Task<AppResponse> AddEmployeeInBulk(IFormFile file)
        {
            var response = new AppResponse();
            string cnicPattern = "^[0-9]{13}$";
            using var package = new ExcelPackage(file.OpenReadStream());
            var worksheet = package.Workbook.Worksheets[0];
            var fileName = file.FileName;
            var totalRecords = worksheet.Dimension.Rows - 1;
            var fileSize = file.Length.ToString();

            var Failed = new List<Employee>();
            var SuccessList = new List<Employee>();
            var Invalid = new List<Employee>();


            var bit = CheckFile(worksheet);
            if (bit == false)
            {
                response.ResCode = 2;
                response.ResMsg = "File Not Correct";
                response.ResBody = null;
                return response;
            }
            for (int row = 2; row <= worksheet.Dimension.Rows; row++)
            {
                var item = new Employee
                {
                    FirstName = string.IsNullOrEmpty(worksheet.Cells[row, 1].Text.ToString()) ? "" : worksheet.Cells[row, 1].Text.ToString(),
                    LastName = string.IsNullOrEmpty(worksheet.Cells[row, 2].Text.ToString()) ? "" : worksheet.Cells[row, 2].Text.ToString(),
                    Cnic = string.IsNullOrEmpty(worksheet.Cells[row, 3].Text.ToString()) ? "" : worksheet.Cells[row, 3].Text.ToString(),
                    Created = DateTime.Now,
                    CreatedBy = "",
                };
                
                if (!item.FirstName.IsNullOrEmpty() && !item.LastName.IsNullOrEmpty() && Regex.IsMatch(item.Cnic, cnicPattern))
                {
                    var emp = await _dbContext.Employees.FirstOrDefaultAsync(x => x.Cnic == item.Cnic && x.Ended == null);
                    if (emp != null)
                    {
                        Failed.Add(item);
                    }
                    else
                    {
                        _dbContext.Employees.Add(item);
                        var count = await _dbContext.SaveChangesAsync();
                        if(count>1)
                        {
                            Failed.Add(item);
                        }
                        else
                        {
                            SuccessList.Add(item);
                        }

                    }
                }else
                {
                    Invalid.Add(item);
                }
            }
            response.ResCode = 100;
            response.ResMsg = "Success";
            response.ResBody = new
            {
                failedlist = Failed,
                SuccessList = SuccessList,
                invalidlist = Invalid
            };
            return response;
        }

        public async Task<AppResponse> GetAllEmployees()
        {
            var response = new AppResponse();
            var list = _dbContext.Employees.Where(x=> x.Ended == null).ToList();
            if (list.Count<1)
            {
                response.ResCode = 1;
                response.ResMsg = "Empty List";
                response.ResBody = null;
                return response;
            }
            
            response.ResCode = 100;
            response.ResMsg = "Success";
            response.ResBody = list;
            return response;
        }

        public async Task<AppResponse> ExportEmployees()
        {
            var response = new AppResponse();
            var list = _dbContext.Employees.Where(x => x.Ended == null).ToList();

            if (list.Count < 1)
            {
                response.ResCode = 1;
                response.ResMsg = "Empty List";
                response.ResBody = null;
                return response;
            }
            byte[] excelFile = GenerateExcelFile(list);
            string base64ExcelFile = Convert.ToBase64String(excelFile);

            response.ResCode = 100;
            response.ResMsg = "Success";
            response.ResBody = base64ExcelFile;
            return response;
        }


        private byte[] GenerateExcelFile(List<Employee> employees)
        {
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Employees");

                // Add headers
                worksheet.Cells[1, 1].Value = "Id";
                worksheet.Cells[1, 2].Value = "FirstName";
                worksheet.Cells[1, 3].Value = "LastName";
                worksheet.Cells[1, 4].Value = "Cnic";
                worksheet.Cells[1, 5].Value = "Created";
                worksheet.Cells[1, 6].Value = "CreatedBy";
                worksheet.Cells[1, 7].Value = "Ended";
                worksheet.Cells[1, 8].Value = "EndedBy";

                // Bold headers
                using (var range = worksheet.Cells[1, 1, 1, 8])
                {
                    range.Style.Font.Bold = true;
                }

                // Add data
                for (int i = 0; i < employees.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = employees[i].Id;
                    worksheet.Cells[i + 2, 2].Value = employees[i].FirstName;
                    worksheet.Cells[i + 2, 3].Value = employees[i].LastName;
                    worksheet.Cells[i + 2, 4].Value = employees[i].Cnic;
                    worksheet.Cells[i + 2, 5].Value = employees[i].Created?.ToString("yyyy-MM-dd");
                    worksheet.Cells[i + 2, 6].Value = employees[i].CreatedBy;
                    worksheet.Cells[i + 2, 7].Value = employees[i].Ended?.ToString("yyyy-MM-dd");
                    worksheet.Cells[i + 2, 8].Value = employees[i].EndedBy;
                }

                // Auto fit columns
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                // Save to stream
                return package.GetAsByteArray();
            }
        }   
        private byte[] GenerateExcelFile_V1(List<Employee> employees)
        {
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Employees");

                worksheet.Cells["A1"].LoadFromCollection(employees, true);
                var headerRow = worksheet.Cells["A1:H1"];
                headerRow.Style.Font.Bold = true;
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                
                // Save to stream
                return package.GetAsByteArray();
            }
        }
        private bool CheckFile(ExcelWorksheet worksheet)
        {
            var bit = false;
            for (int i = 2; i < 5; i++)
            {
                var firstName = string.IsNullOrEmpty(worksheet.Cells[i, 1].Text.ToString()) ? "" : worksheet.Cells[i, 1].Text.ToString();
                var lastName = string.IsNullOrEmpty(worksheet.Cells[i, 2].Text.ToString()) ? "" : worksheet.Cells[i, 2].Text.ToString();
                var cnic = string.IsNullOrEmpty(worksheet.Cells[i, 3].Text.ToString()) ? "" : worksheet.Cells[i, 3].Text.ToString();
                if (!firstName.IsNullOrEmpty() && !lastName.IsNullOrEmpty() && !cnic.IsNullOrEmpty())
                {
                    bit = true;
                }

            }
            return bit;
        }




    }
}
