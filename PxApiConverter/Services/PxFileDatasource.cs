using PCAxis.Paxiom;

namespace PxApiConverter.Services
{
    public class PxFileDatasource : IDatasource
    {
        private readonly string _databasePath;
        private readonly Dictionary<string, string> _lookup;
        public PxFileDatasource(string databasePath)
        {
            // Removes the root folder from the database path
            _databasePath = Path.GetDirectoryName(databasePath) ?? "";
        }



        public IPXModelBuilder? GetBuiler(string table, string language)
        {
            var builder = new PCAxis.Paxiom.PXFileBuilder();

            var pathWithoutLanguage = string.Join('\\', table.Split('/').Skip(1));

            var pathToFile = System.IO.Path.Combine(_databasePath, pathWithoutLanguage);

            if (!File.Exists(pathToFile))
            {
                return null;
            }

            builder.SetPath(pathToFile);
            builder.SetPreferredLanguage(language);
            return builder;
        }

        public string? ResolveTableId(string path)
        {
            var builder = new PCAxis.Paxiom.PXFileBuilder();
            var pathWithoutLanguage = string.Join('\\', path.Split('/').Skip(1));

            var pathToFile = System.IO.Path.Combine(_databasePath, pathWithoutLanguage);

            if (!File.Exists(pathToFile))
            {
                return null;
            }

            builder.SetPath(pathToFile);
            builder.BuildForSelection();
            return builder.Model.Meta.Matrix;
        }


    }
}
