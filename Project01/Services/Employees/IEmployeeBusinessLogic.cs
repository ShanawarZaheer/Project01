using Project01.DTOs;

namespace Project01.Services.Employees
{
    public interface IEmployeeBusinessLogic
    {
        Task<AppResponse> AddEmployee(EmployeeDTO model);
        Task<AppResponse> AddEmployeeInBulk(IFormFile file);
        Task<AppResponse> GetAllEmployees();
        Task<AppResponse> ExportEmployees();
    }
}
