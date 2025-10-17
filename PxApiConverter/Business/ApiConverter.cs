using System.Net;
using PxApiConverter.Models;

namespace PxApiConverter.Business
{
    public class ApiConverter : IApiConverter
    {
        private readonly ILogger<ApiConverter> _logger;
        public ApiConverter(ILogger<ApiConverter> logger)
        {
            _logger = logger;
        }

        public Task<ConvertResultModel> ConvertPxApi1ToPxApi2Async(string pxApi1Url, string body)
        {
            if (string.IsNullOrWhiteSpace(pxApi1Url))
            {
                _logger.LogWarning("Conversion attempted with empty URL.");
                throw new ArgumentException("URL is required", nameof(pxApi1Url));
            }

            body ??= string.Empty;
            var trimmedBody = body.Trim();

            using var scope = _logger.BeginScope(new Dictionary<string, object?>
            {
                ["PxApi1Url"] = pxApi1Url,
                ["BodyLength"] = trimmedBody.Length
            });

            _logger.LogInformation("Starting conversion (HasBody={HasBody}).", !string.IsNullOrEmpty(trimmedBody));

            // Simplistic placeholder conversion logic; replace with real mapping later
            var getUrl = pxApi1Url;
            if (!string.IsNullOrEmpty(trimmedBody))
            {
                var encoded = WebUtility.UrlEncode(trimmedBody);
                getUrl += (pxApi1Url.Contains('?') ? '&' : '?') + "query=" + encoded;
                _logger.LogDebug("GET URL augmented with query parameter.");
            }

            string postUrl = pxApi1Url; // For now same endpoint assumed
            string postBody = trimmedBody; // unchanged

            var result = new ConvertResultModel
            {
                GetUrl = getUrl,
                PostUrl = postUrl,
                PostBody = postBody
            };

            _logger.LogInformation("Conversion completed.");

            return Task.FromResult(result);
        }
    }
}
