using Microsoft.AspNetCore.Identity;

namespace my_books.Data.Model
{
    public class ApplicationUser : IdentityUser
    {
        public string? Custom { get; set; }
    }
}
