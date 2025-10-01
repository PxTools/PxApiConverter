using PxApiConverter.Models;

namespace PxApiConverter.Business
{
    public interface IApiConverter
    {
        Task<ConvertResultModel> ConvertPxApi1ToPxApi2Async(string pxApi1Url, string body);
    }
}
