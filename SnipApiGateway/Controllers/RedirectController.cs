using Microsoft.AspNetCore.Mvc;

namespace SnipApiGateway.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RedirectController : Controller
    {
        [HttpGet]
        [Route("/v1/Swagger")]
        public IActionResult RedirectToAuthServerSwagger()
        {
            // Cambia la URL por la del Swagger del AuthServer
            return Redirect("https://localhost:7180/swagger/index.html");
        }
    }
}
