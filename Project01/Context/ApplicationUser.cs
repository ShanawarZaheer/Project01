using Microsoft.AspNetCore.Identity;

namespace Project01.Context
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

}
