using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using my_books.Data.Authentication;
using my_books.Data.Services;
using my_books.Data.ViewModel;

namespace my_books.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BooksController : ControllerBase
    {
        private readonly BooksServices _services;
        public BooksController(BooksServices services)
        {
            _services=services;
        }
        [HttpPost("add-book")]
        public IActionResult AddBooks([FromBody]BookVM book)
        {
            _services.AddBooks(book);
            return Ok();
        }
        [Authorize(Roles = UserRoles.Author)]
        [HttpGet("get-all-book")]
        public IActionResult GetAllBooks()
        {
            var books=_services.GetAllBooks();
            return Ok(books);
        }
        [Authorize(Roles = UserRoles.Admin)]
        [HttpGet("get-book-by-id/{id}")]
        public IActionResult GetBookById(int id)
        {
            var book = _services.GetBookById(id);
            return Ok(book);
        }
        [HttpPut("update-book-by-id/{id}")]
        public IActionResult UpdateBookById(int id, [FromBody] BookVM book)
        {
            var updatedBook = _services.UpdateBookById(id, book);
            return Ok(updatedBook);
        }

        [HttpDelete("delete-book-by-id/{id}")]
        public IActionResult DeleteBookById(int id)
        {
            _services.DeleteBookById(id);
            return Ok();
        }
    }
}
