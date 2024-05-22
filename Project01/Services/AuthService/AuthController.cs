using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project01.Clients.SMTP;
using Project01.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;

namespace Project01.Services.AuthService
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthBusinessLogic _authService;

        public AuthController(IAuthBusinessLogic authService)
        {
            _authService = authService;
        }

        [HttpPost("signUp")]
        public async Task<IActionResult> SignUp([FromBody] RegisterModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _authService.Register(model);
                if (user.ResCode != 100)
                {
                    return Ok(user);
                }

                var loginModel = new LoginModel
                {
                    Email = model.Email,
                    Password = model.Password,
                    RememberMe = false,
                };
                var token = await _authService.Authenticate(loginModel);
                return Ok(token);
            }
            else
            {
                return BadRequest();
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (ModelState.IsValid)
            {
                var token = await _authService.Authenticate(model);
                return Ok(token);
            }
            else
            {
                return BadRequest();
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var token = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

            if (token == null)
            {
                return BadRequest("No token found.");
            }
            var logout = await _authService.Logout(token);
            return Ok(logout);

        }

        [Authorize]  
        [HttpPost("updateUser")]
        public async Task<IActionResult> UpdateUser([FromBody]UpdateUserModel model)
        {
            if (ModelState.IsValid)
            {
                var update = await _authService.UpdateUser(model);

                return Ok(update);
            }
            return BadRequest();
        }

        [HttpPost("forgotPassword")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto model)
        {
            if (string.IsNullOrEmpty(model.Email))
            {
                return BadRequest("Email is required.");
            }
            var result = await _authService.ForgotPassword(model);
            return Ok(result);
        }

        [HttpPost("resetPassword")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto model)
        {
            if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.OTP) || string.IsNullOrEmpty(model.Password))
            {
                return BadRequest("Invalid request.");
            }
            var result = await _authService.ResetPassword(model);
            return Ok(result);
        }



    }
}
