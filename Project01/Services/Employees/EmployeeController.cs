using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Project01.DTOs;
using System.Text.RegularExpressions;

namespace Project01.Services.Employees
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeController : ControllerBase
    {
        public readonly IEmployeeBusinessLogic _employeeBusinessLogic;
        public EmployeeController(IEmployeeBusinessLogic employeeBusinessLogic)
        {
            _employeeBusinessLogic = employeeBusinessLogic;
        }

        //[Authorize(Policy = "AdminOnly")]
        [HttpPost("addEmployee")]
        public async Task<IActionResult> AddEmployee(EmployeeDTO model)
        {
            string cnicPattern = "^[0-9]{13}$";
            if(!Regex.IsMatch(model.Cnic, cnicPattern))
            {
                return BadRequest("Invalid Cnic");
            }
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            var add = await _employeeBusinessLogic.AddEmployee(model);
            return Ok(add);
        }

        //[Authorize(Policy = "AdminOnly")]
        [HttpPost("addEmployeeInBulk")]
        public async Task<IActionResult> AddEmployeeInBulk(IFormFile file)
        {
            var add = await _employeeBusinessLogic.AddEmployeeInBulk(file);
            return Ok(add);
        }
        
        //[Authorize(Policy = "AdminOnly")]
        [HttpGet("GetAllEmployees")]
        public async Task<IActionResult> GetAllEmployees()
        {
            var list = await _employeeBusinessLogic.GetAllEmployees();
            return Ok(list);
        }
        
        //[Authorize(Policy = "AdminOnly")]
        [HttpGet("ExportEmployees")]
        public async Task<IActionResult> ExportEmployees()
        {
            var list = await _employeeBusinessLogic.ExportEmployees();
            return Ok(list);
        }




    }
}
