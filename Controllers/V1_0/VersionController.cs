using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace Logging.Controllers.V1_0.Version
{
    [Route("api/v1.0/[controller]")]
    [ApiController]
    public class VersionController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);

            return Ok(new
            {
                AssemblyVersion = assembly.GetName().Version?.ToString(),
                FileVersion = fileVersionInfo.FileVersion,
                ProductVersion = fileVersionInfo.ProductVersion,
                Environment = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
            });
        }
    }
}
