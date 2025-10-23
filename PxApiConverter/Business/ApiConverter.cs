using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PCAxis.Query;
using PxApiConverter.Models;
using PxApiConverter.Services;
using sq_migrate;

namespace PxApiConverter.Business
{
    public class ApiConverter : IApiConverter
    {
        private readonly ILogger<ApiConverter> _logger;

        private readonly IDatasource _datasource;

        private readonly string _sourceBaseUrl;
        private readonly string _targetBaseUrl;

        public ApiConverter(ILogger<ApiConverter> logger, IOptions<PxApiOptions> options, IDatasource datasource)
        {
            _logger = logger;
            _datasource = datasource;
            _sourceBaseUrl = options.Value.SourceUrlPrefix ?? string.Empty;
            _targetBaseUrl = options.Value.TargetUrlPrefix ?? string.Empty;
        }

        public Task<ConvertResultModel> ConvertPxApi1ToPxApi2Async(string pxApi1Url, string body)
        {
            // Basic input validation
            if (string.IsNullOrWhiteSpace(pxApi1Url))
            {
                _logger.LogInformation("Conversion attempted with empty URL.");
                throw new ArgumentException("URL is required", nameof(pxApi1Url));
            }

            if (string.IsNullOrWhiteSpace(body))
            {
                _logger.LogInformation("Conversion attempted with empty body.");
                throw new ArgumentException("Body is required", nameof(pxApi1Url));
            }

            // Validate that query is in right format
            var queries = JsonConvert.DeserializeObject<TableQuery>(body);

            if (queries == null)
            {
                _logger.LogInformation("Failed to deserialize body TableQuery");
                throw new ArgumentException("Invalid body format", nameof(body));
            }

            // Removed the prefix from the url to get the relative path
            var path = pxApi1Url.Remove(0, _sourceBaseUrl.Length);
            var lang = GetLanguageFromUrl(path);
            var tableId = _datasource.ResolveTableId(path);
            var builder = _datasource.GetBuiler(path, lang);

            // Verify that the table exists
            if (builder == null)
            {
                _logger.LogInformation("No builder found for path {Path}", path);
                throw new ArgumentException("Table not found", nameof(pxApi1Url));
            }


            // Convert the old query to a new query
            builder.BuildForSelection();
            var (outputFormat, outputFormatParams) = ConvertUtil.TranslateOutputFormat(queries.Response.Format);
            var placement = ConvertUtil.GetPlacement(queries.Response.Format, builder.Model);
            var selection = ConvertUtil.Convert(queries.Query.ToList(), builder.Model);

            if (selection is null)
            {
                _logger.LogInformation("Failed to convert selection for path {Path}", path);
                throw new ArgumentException("Failed to convert selection", nameof(body));
            }
            var dataUrl = @$"{_targetBaseUrl}/tables/{tableId}/data?lang={lang}&outputFormat={EnumConverter.ToEnumString(outputFormat)}";

            if (outputFormatParams.Count > 0)
            {
                var formatParams = string.Join(",", outputFormatParams.Select(p => EnumConverter.ToEnumString(p)));
                dataUrl += $"&outputFormatParams={formatParams}";
            }

            var getUrl = dataUrl;

            // Add placement if any
            if (placement is not null)
            {
                var stubVariables = string.Join(",", placement.Stub.Select(v => v.Contains(',') ? $"[{v}]" : v));
                var headingVariables = string.Join(",", placement.Heading.Select(v => v.Contains(',') ? $"[{v}]" : v));
                getUrl += $"&stub={stubVariables}&heading={headingVariables}";
            }

            // Add the selection as url parameters
            getUrl += ConvertUtil.ToUrlParamString(selection);

            selection.Placement = placement;

            var postBody = JsonConvert.SerializeObject(selection, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            var result = new ConvertResultModel
            {
                GetUrl = getUrl,
                PostUrl = dataUrl,
                PostBody = postBody
            };


            return Task.FromResult(result);
        }

        private static string GetLanguageFromUrl(string url)
        {
            var lang = url.Split('/', StringSplitOptions.RemoveEmptyEntries).First();
            return lang;
        }
    }
}
