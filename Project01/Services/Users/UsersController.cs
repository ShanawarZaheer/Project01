using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Project01.Context;
using Project01.Migrations;
using Project01.Models;
using System.Data;

namespace Project01.Services.Users
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _dbContext;

        public UsersController(UserManager<ApplicationUser> userManager, AppDbContext dbContext)
        {
            _userManager = userManager;
            _dbContext = dbContext;
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpGet("all")]
        public IActionResult GetAllUsers()
        {
            var users = _userManager.Users.ToList();
            return Ok(users);
        }

        [Authorize(Policy = "Admin/User")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
             
            if (user == null)
                return NotFound();

            return Ok(user);
        }
       
        [HttpGet("userTokens")]
        public async Task<IActionResult> GetAllUserTokens()
        {
            var user =  _dbContext.Tokens.ToList();
             
            if (user == null)
                return NotFound();

            return Ok(user);
        }
        
     
    }
}
