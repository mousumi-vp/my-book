using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using my_books.Data.Authentication;
using my_books.Data.Services;
using my_books.Data.ViewModel;
using my_books.Exceptions;
using System.Security.Policy;

namespace my_books.Controllers
{
    [Authorize(Roles = UserRoles.Publisher + "," + UserRoles.Admin)]
    [Route("api/[controller]")]
    [ApiController]
    public class PublishersController : ControllerBase
    {
        private readonly PublishersService _services;
        private readonly ILogger<PublishersController> _logger;
        private readonly MemoryService _memoryService;
        public PublishersController(PublishersService services, ILogger<PublishersController> logger, MemoryService memoryService)
        {
            _services = services;
            _logger = logger;
            _memoryService = memoryService;

        }
        [HttpGet("get-all-publishers")]
        public IActionResult GetAllPublishers(string sortBy, string searchString, int pageNumber)
        {
            try
            {
                _logger.LogInformation("This is just a log in GetAllPublishers()");
                var _result = _services.GetAllPublishers(sortBy, searchString, pageNumber);
                return Ok(_result);
            }
            catch (Exception)
            {
                return BadRequest("Sorry, we could not load the publishers");
            }
        }
        [HttpPost("add-publisher")]
        public IActionResult AddPublisher([FromBody] PublisherVM publisher)
        {
            try
            {
                var newPublisher = _services.AddPublisher(publisher);
                return Created(nameof(AddPublisher), newPublisher);
            }
            catch (PublisherNameException ex)
            {
                return BadRequest($"{ex.Message}, Publisher name: {ex.PublisherName}");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }
        [HttpGet("get-publisher-books-with-authors/{id}")]
        public IActionResult GetPublisherData(int id)
        {
            var _response = _services.GetPublisherData(id);
            return Ok(_response);
        }
        [HttpDelete("delete-publisher-by-id/{id}")]
        public IActionResult DeletePublisherById(int id)
        {
            try
            {
                _services.DeletePublisherById(id);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet("get-publisher-by-id/{id}")]
        public IActionResult GetPublisherById(int id)
        {
            string key = "publisherKey";
            var publisher = _memoryService.GetData<Publisher>(key);

            if (publisher == null)
            {
                var _response = _services.GetPublisherById(id);

                if (_response != null)
                {
                    _memoryService.SetData(key, _response, TimeSpan.FromMinutes(5));
                    return Ok(_response);
                }
                else
                {
                    return NotFound();
                }
                
            }
            else
            {
                return Ok(publisher);
            }
            
        }
    }
}
