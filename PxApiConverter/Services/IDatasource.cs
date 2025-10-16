using PCAxis.Paxiom;

namespace PxApiConverter.Services
{
    public interface IDatasource
    {
        string? ResolveTableId(string path);

        IPXModelBuilder? GetBuiler(string table, string language);
    }
}
