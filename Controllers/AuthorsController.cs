using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using my_books.Data.Services;
using my_books.Data.ViewModel;

namespace my_books.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthorsController : ControllerBase
    {
        private readonly AuthorsService _services;
        public AuthorsController(AuthorsService services)
        {
            _services=services;
        }
        [HttpPost("add-author")]
        public IActionResult AddAuthor([FromBody]AuthorVM author)
        {
            _services.AddAuthor(author);
            return Ok();
        }
        [HttpGet("get-author-with-books-by-id/{id}")]
        public IActionResult GetAuthorWithBooks(int id)
        {
            var response = _services.GetAuthorWithBooks(id);
            return Ok(response);
        }
    }
}
