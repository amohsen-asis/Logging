using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Reflection;

namespace Logging.Controllers.V1_0;

[Route("api/v1.0/[controller]")]
[ApiController]
public class VersionController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;

    public VersionController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    [HttpGet]
    public IActionResult GetVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var fileVersion = FileVersionInfo.GetVersionInfo(assembly.Location).FileVersion;
        var assemblyVersion = assembly.GetName().Version?.ToString();

        return Ok(new
        {
            AssemblyVersion = assemblyVersion,
            FileVersion = fileVersion,
            ProductVersion = version,
            Environment = _environment.EnvironmentName
        });
    }
}
