using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;

namespace Project01.Models
{
    public class Token
    {
        [Key]
        public int Id { get; set; }
        public string UserId { get; set; }
        public string TokenId { get; set; }
        public string? Ended { get; set; }
    }
}
