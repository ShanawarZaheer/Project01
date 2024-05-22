using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Project01.Clients.SMTP;
using Project01.Context;
using Project01.DTOs;
using Project01.Models;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using static Project01.Services.AuthService.AuthbusinessLogic;

namespace Project01.Services.AuthService
{
    public class AuthbusinessLogic : IAuthBusinessLogic
    {

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly AppDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;
        private readonly IMemoryCache _memoryCache;

        public AuthbusinessLogic(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager,
            AppDbContext dbContext, IConfiguration configuration, IEmailSender emailSender, IMemoryCache memoryCache)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _dbContext = dbContext;
            _configuration = configuration;
            _emailSender = emailSender;
            _memoryCache = memoryCache;
        }

        public async Task<AppResponse> Authenticate(LoginModel model)
        {
            var response = new AppResponse();
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                response.ResCode = 1;
                response.ResMsg = "User Does Not Exist";
                response.ResBody = null;
                return response;
            }

            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
            if (!result.Succeeded)
            {
                response.ResCode = 1;
                response.ResMsg = "Login Failed";
                response.ResBody = null;
                return response;
            }
            var roles = await _userManager.GetRolesAsync(user);
            response.ResCode = 100;
            response.ResMsg = "Success";
            response.ResBody = GenerateJwtToken(user, roles);
            return response;
        }
        public async Task<AppResponse> Logout(string token)
        {
            var response = new AppResponse();
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            var tokenId = jwtToken.Claims.First(claim => claim.Type == JwtRegisteredClaimNames.Jti).Value;

            var userToken = _dbContext.Tokens.SingleOrDefault(t => t.TokenId == tokenId);
            if (userToken != null)
            {
                userToken.Ended = DateTime.Now.ToString();
                await _dbContext.SaveChangesAsync();
            }
            response.ResCode = 100;
            response.ResMsg = "Success";
            response.ResBody = null;
            return response;
        }
        public async Task<AppResponse> Register(RegisterModel model)
        {
            var response = new AppResponse();
            var user = new ApplicationUser { UserName = model.Email, Email = model.Email, FirstName = model.FirstName, LastName = model.LastName };
            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, model.Role);
                response.ResCode = 100;
                response.ResMsg = "Success";
                response.ResBody = user;
                return response;
            }
            var res = result.Errors.ToList();
            response.ResCode = 1;
            response.ResMsg = "Failure";
            response.ResBody = res;
            return response;
        }
        public async Task<AppResponse> UpdateUser(UpdateUserModel model)
        {
            var response = new AppResponse();
            var user = await _userManager.FindByIdAsync(model.userId);
            if (user == null)
            {
                response.ResCode = 1;
                response.ResMsg = "User Does Not Exist";
                response.ResBody = null;
                return response;
            }
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;

            var update = await _userManager.UpdateAsync(user);
            if (!update.Succeeded)
            {
                response.ResCode = 2;
                response.ResMsg = "Failure";
                response.ResBody = null;
                return response;
            }
            response.ResCode = 100;
            response.ResMsg = "Success";
            response.ResBody = user;
            return response;
        }
        private string GenerateJwtToken(ApplicationUser user, IList<string> roles)
        {
            var tokenId = Guid.NewGuid().ToString();
            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(JwtRegisteredClaimNames.Jti, tokenId),
        };
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiryMinutes = Convert.ToInt32(_configuration["JwtExpireMinutes"]);
            var expires = DateTime.Now.AddMinutes(expiryMinutes);
            var token = new JwtSecurityToken(
                _configuration["JwtIssuer"],
                _configuration["JwtIssuer"],
                claims,
                expires: expires,
                signingCredentials: creds
            );
            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            var userToken = new Token
            {
                UserId = user.Id,
                TokenId = tokenId,
                Ended = null,
            };

            _dbContext.Tokens.Add(userToken);
            var a = _dbContext.SaveChanges();

            return tokenString;
        }
        public async Task<AppResponse> ForgotPassword(ForgotPasswordDto model)
        {
            var response = new AppResponse();
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                response.ResCode = 1;
                response.ResMsg = "User Does Not Exist";
                response.ResBody = null;
                return response;
            }

            var otp = GenerateOTP();
            _memoryCache.Set(user.Email, otp, TimeSpan.FromMinutes(10));

            await _emailSender.SendEmailAsync(model.Email, "OTP", $"Your OTP code is: {otp}");


            response.ResCode = 100;
            response.ResMsg = "OTP sent to email " + model.Email;
            response.ResBody = null;
            return response;
        }
        public async Task<AppResponse> ResetPassword(ResetPasswordDto model)
        {
            var response = new AppResponse();
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                response.ResCode = 1;
                response.ResMsg = "User Does Not Exist";
                response.ResBody = null;
                return response;
            }

            if (!_memoryCache.TryGetValue(model.Email, out string cachedOtp) || cachedOtp != model.OTP)
            {
                response.ResCode = 2;
                response.ResMsg = "Invalid or expired OTP.";
                response.ResBody = null;
                return response;
            }
            _memoryCache.Remove(model.Email);
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, model.Password);
            if (result.Succeeded)
            {
                response.ResCode = 3;
                response.ResMsg = "Failed";
                response.ResBody = null;
                return response;
            }
            response.ResCode = 100;
            response.ResMsg = "Success";
            response.ResBody = null;

            return response;
        }
        private string GenerateOTP()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                var otpBytes = new byte[4];
                rng.GetBytes(otpBytes);
                return BitConverter.ToUInt32(otpBytes, 0).ToString("D6");
            }
        }


    }
}