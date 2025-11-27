using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PxApiConverter.Business;
using PxApiConverter.Models;
using System.Diagnostics;

namespace PxApiConverter.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly PxApiOptions _options;
        private readonly IApiConverter _apiConverter;

        public HomeController(ILogger<HomeController> logger, IOptions<PxApiOptions> options, IApiConverter apiConverter)
        {
            _logger = logger;
            _options = options.Value;
            _apiConverter = apiConverter;
        }

        public IActionResult Index()
        {
            ViewData["UrlPrefix"] = _options.SourceUrlPrefix ?? string.Empty;
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        [ValidateAntiForgeryToken] 
        public async Task<IActionResult> ConvertResult([FromForm] string url, [FromForm] string body)
        {
            var prefix = _options.SourceUrlPrefix ?? string.Empty;
            if (string.IsNullOrWhiteSpace(url))
            {
                return BadRequest(new { error = "Url is required" });
            }
            if (body != null && body.Length > 10_000)
                return BadRequest(new { error = "JSON too long." });

            if(url.Length > 500)
                return BadRequest(new { error = "URL too long." });

            // URL-validation
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return BadRequest(new { error = "Invalid URL." });
            }

            // Only http/https
            if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
            {
                return BadRequest(new { error = "Only HTTP or HTTPS allowed." });
            }
            //URL must start with
            if (!string.IsNullOrEmpty(prefix) && !url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { error = $"URL must start with {prefix}" });
            }

            try
            {
                var converted = await _apiConverter.ConvertPxApi1ToPxApi2Async(url, body);

                // postBody may be empty if no JSON given
                var postBody = string.IsNullOrWhiteSpace(converted.PostBody) ? string.Empty : converted.PostBody;

                return Json(new
                {
                    getResult = converted.GetUrl,
                    postUrl = converted.PostUrl,
                    postBody
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Conversion failed");
                return BadRequest(new { error = "Conversion failed. Check that the format is correct and that the query works in PxWebApi1." });
            }
        }
    }
}
