using ECommerce.Service.Response;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseApiController : ControllerBase
    {
        protected IActionResult HandleResult<T>(DataResponse<T> result)
        {
            if (result == null)
                return NotFound();

            if (result.Success && result.Data == null)
                return NotFound();

            if (result.Success)
                return Ok(result);

            return BadRequest(result);
        }
    }
}