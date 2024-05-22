using Project01.DTOs;

namespace Project01.Services.AuthService
{
    public interface IAuthBusinessLogic
    {
        Task<AppResponse> Authenticate(LoginModel model);
        Task<AppResponse> Register(RegisterModel model);
        Task<AppResponse> Logout(string token);
        Task<AppResponse> UpdateUser(UpdateUserModel model);
        Task<AppResponse> ForgotPassword(ForgotPasswordDto model);
        Task<AppResponse> ResetPassword(ResetPasswordDto model);
    }
}
