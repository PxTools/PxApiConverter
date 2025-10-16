using PCAxis.Paxiom;
using PxWeb.Api2.Server.Models;
using System.Text;

namespace sq_migrate
{
    internal class ConvertUtil
    {
        public static VariablesSelection? Convert(List<PCAxis.Query.Query> queries, PXModel model)
        {

            try
            {
                // Init the saved query 
                var s = new PxWeb.Api2.Server.Models.VariablesSelection();
                s.Selection = new List<PxWeb.Api2.Server.Models.VariableSelection>();


                // Loop throw all the variables in the old svaed query and add them to the new saved query
                foreach (var query in queries)
                {
                    var selection = new PxWeb.Api2.Server.Models.VariableSelection();
                    selection.ValueCodes = new List<string>();
                    selection.VariableCode = query.Code;
                    if (query.Selection.Filter.StartsWith("agg:", StringComparison.OrdinalIgnoreCase))
                    {
                        selection.CodeList = string.Concat("agg_", query.Selection.Filter.AsSpan(query.Selection.Filter.IndexOf(':') + 1));
                        selection.ValueCodes.AddRange(query.Selection.Values.ToList());
                    }
                    else if (query.Selection.Filter.StartsWith("vs:", StringComparison.OrdinalIgnoreCase))
                    {
                        selection.CodeList = string.Concat("vs_", query.Selection.Filter.AsSpan(query.Selection.Filter.IndexOf(':') + 1));
                        selection.ValueCodes.AddRange(query.Selection.Values.ToList());
                    }
                    else if (string.Equals(query.Selection.Filter, "TOP", StringComparison.OrdinalIgnoreCase))
                    {
                        selection.ValueCodes.Add($"TOP({query.Selection.Values[0]})");
                    }
                    else if (string.Equals(query.Selection.Filter, "ALL", StringComparison.OrdinalIgnoreCase))
                    {
                        selection.ValueCodes.Add(query.Selection.Values[0]);
                    }
                    else
                    {
                        selection.ValueCodes.AddRange(query.Selection.Values.ToList());
                    }
                    s.Selection.Add(selection);
                }

                // Check if we have variables that might have been removed from the old saved query Eliminate SingleContents and variables that are not eliminaable
                var definedVariableCodes = new HashSet<string>(queries.Select(q => q.Code));
                foreach (var variable in model.Meta.Variables)
                {
                    if (!definedVariableCodes.Contains(variable.Code))
                    {
                        var selection = new PxWeb.Api2.Server.Models.VariableSelection();
                        selection.ValueCodes = new List<string>();
                        selection.VariableCode = variable.Code;
                        selection.ValueCodes.Add("*");
                        s.Selection.Add(selection);
                    }
                }

                return s;
            }
            catch (Exception ex)
            {
                //TODO Log error
            }

            return null;
        }


        public static (OutputFormatType, List<OutputFormatParamType>) TranslateOutputFormat(string outputFormat)
        {
            outputFormat = outputFormat.ToUpper();
            OutputFormatType format = OutputFormatType.PxEnum;
            var parameters = new List<OutputFormatParamType>();
            switch (outputFormat)
            {
                case "PX":
                    format = OutputFormatType.PxEnum;
                    break;

                case "CSV":
                    format = OutputFormatType.CsvEnum;
                    break;
                case "CSV2":
                    format = OutputFormatType.CsvEnum;
                    // Comma separated
                    parameters.Add(OutputFormatParamType.UseTextsEnum);
                    break;
                case "CSV3":
                    format = OutputFormatType.CsvEnum;
                    // Comma separated
                    parameters.Add(OutputFormatParamType.UseCodesEnum);
                    break;
                case "JSON":
                    format = OutputFormatType.JsonPxEnum;
                    break;
                case "JSON-STAT2":
                    format = OutputFormatType.JsonStat2Enum;
                    break;
                case "XLSX":
                    format = OutputFormatType.XlsxEnum;
                    parameters.Add(OutputFormatParamType.IncludeTitleEnum);
                    break;
                case "SDMX":
                    throw new ArgumentException($"Output format SDMX not longer supported");
                case "JSON-STAT":
                    throw new ArgumentException($"Output format JSON-STAT not longer supported");
                default:
                    throw new ArgumentException($"Output format {outputFormat} not supported");
            }

            return (format, parameters);
        }


        public static VariablePlacementType? GetPlacement(string outputFormat, PXModel model)
        {
            if (string.Equals(outputFormat, "CSV2", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(outputFormat, "CSV3", StringComparison.OrdinalIgnoreCase))
            {
                // Move everything to the stub last variable should be time then contents.
                return GetCsvPlacement(model.Meta);
            }
            return null;
        }


        private static VariablePlacementType GetCsvPlacement(PXMeta meta)
        {
            var placement = new VariablePlacementType();
            placement.Heading = new List<string>();
            placement.Stub = new List<string>();

            // Add all variables to the stub except time and contents

            placement.Stub.AddRange(meta.Variables
                .Where(v => (!v.IsTime) && (!v.IsContentVariable))
                .Select(v => v.Code));

            // Add time variable to stub
            var time = meta.Variables.FirstOrDefault(v => v.IsTime);
            if (time is not null)
            {
                placement.Stub.Add(time.Code);
            }

            // Add contents variable to stub
            var contents = meta.Variables.FirstOrDefault(v => v.IsContentVariable);
            if (contents is not null)
            {
                placement.Stub.Add(contents.Code);
            }

            return placement;
        }

        public static string ToUrlParamString(VariablesSelection? selections)
        {
            if (selections is null) return string.Empty;

            var sb = new StringBuilder();
            foreach (var selection in selections.Selection)
            {
                sb.Append($"&variableCodes[{selection.VariableCode}]={string.Join(',', selection.ValueCodes.Select(c => c.Contains(',') ? $"[{c}]" : c))}");
                if (!string.IsNullOrWhiteSpace(selection.CodeList))
                {
                    sb.Append($"&codeList[{selection.VariableCode}]={selection.CodeList}");
                }
            }
            return sb.ToString();
        }

    }
}
