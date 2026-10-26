using Microsoft.AspNetCore.Mvc;

namespace OCRS.Controllers;

public sealed class SOADemoController : Controller
{
    [HttpGet("/SOADemo")]
    public IActionResult Index() => View();
}
