using System.Text.RegularExpressions;
using Serilog;

namespace FileContentRenamer.Services
{
    public class DateExtractor : IDateExtractor
    {
        private static readonly Dictionary<string, string> SpanishMonths = new Dictionary<string, string>
        {
            { "enero", "01" }, { "febrero", "02" }, { "marzo", "03" }, { "abril", "04" },
            { "mayo", "05" }, { "junio", "06" }, { "julio", "07" }, { "agosto", "08" },
            { "septiembre", "09" }, { "octubre", "10" }, { "noviembre", "11" }, { "diciembre", "12" }
        };

        public string ExtractDateFromContent(string content)
        {
            if (string.IsNullOrEmpty(content))
                return string.Empty;

            // First, try Spanish date format: "dd de mes de yyyy"
            var spanishDateMatch = Regex.Match(content, @"(\d{1,2})\s+de\s+(\w+)\s+de\s+(\d{4})", RegexOptions.IgnoreCase);
            if (spanishDateMatch.Success)
            {
                var day = spanishDateMatch.Groups[1].Value;
                var monthName = spanishDateMatch.Groups[2].Value.ToLower();
                var year = spanishDateMatch.Groups[3].Value;
                
                var convertedDate = ConvertSpanishDate(day, monthName, year);
                if (!string.IsNullOrEmpty(convertedDate))
                {
                    Log.Information("Using Spanish date from content: {Date} (found: '{OriginalMatch}')", 
                        convertedDate, spanishDateMatch.Value);
                    return convertedDate;
                }
            }

            // Prioritize emission dates over due dates for better accuracy
            var emissionPatterns = new[]
            {
                @"EMISIÓN:\s*(\d{1,2}/\d{1,2}/\d{4})",
                @"Fecha\s+(\d{1,2}/\d{1,2}/\d{4})",
                @"FECHA:\s*(\d{1,2}/\d{1,2}/\d{4})",
                @"emisión\s*(\d{1,2}/\d{1,2}/\d{4})"
            };

            foreach (var pattern in emissionPatterns)
            {
                var emissionMatch = Regex.Match(content, pattern, RegexOptions.IgnoreCase);
                if (emissionMatch.Success)
                {
                    var date = StandardizeDate(emissionMatch.Groups[1].Value);
                    Log.Information("Using emission date from content: {Date} (found: '{OriginalMatch}')", 
                        date, emissionMatch.Value);
                    return date;
                }
            }

            // Then try due dates
            var duePatterns = new[]
            {
                @"vencimiento\s+(\d{1,2}/\d{1,2}/\d{4})",
                @"Vto\.?\s*:?\s*(\d{1,2}/\d{1,2}/\d{4})",
                @"vencimiento:\s*(\d{1,2}/\d{1,2}/\d{4})"
            };

            foreach (var pattern in duePatterns)
            {
                var dueMatch = Regex.Match(content, pattern, RegexOptions.IgnoreCase);
                if (dueMatch.Success)
                {
                    var date = StandardizeDate(dueMatch.Groups[1].Value);
                    Log.Information("Using due date from content: {Date} (found: '{OriginalMatch}')", 
                        date, dueMatch.Value);
                    return date;
                }
            }

            // Generic date patterns
            var genericPatterns = new[]
            {
                @"\b(\d{1,2}/\d{1,2}/\d{4})\b",
                @"\b(\d{1,2}-\d{1,2}-\d{4})\b",
                @"\b(\d{4}-\d{1,2}-\d{1,2})\b"
            };

            foreach (var pattern in genericPatterns)
            {
                var genericMatch = Regex.Match(content, pattern);
                if (genericMatch.Success)
                {
                    var date = StandardizeDate(genericMatch.Groups[1].Value);
                    Log.Information("Using generic date from content: {Date} (found: '{OriginalMatch}')", 
                        date, genericMatch.Value);
                    return date;
                }
            }

            return string.Empty;
        }

        public string ExtractDateFromFilename(string filename)
        {
            var match = Regex.Match(filename, @"(\d{4}-\d{2}-\d{2})");
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        private static string StandardizeDate(string dateString)
        {
            if (string.IsNullOrEmpty(dateString))
                return string.Empty;

            try
            {
                // Handle different date formats
                if (dateString.Contains('/'))
                {
                    var parts = dateString.Split('/');
                    if (parts.Length == 3)
                    {
                        // Assume dd/MM/yyyy format
                        var day = parts[0].PadLeft(2, '0');
                        var month = parts[1].PadLeft(2, '0');
                        var year = parts[2];
                        return $"{year}-{month}-{day}";
                    }
                }
                else if (dateString.Contains('-'))
                {
                    var parts = dateString.Split('-');
                    if (parts.Length == 3)
                    {
                        // Check if it's yyyy-MM-dd or dd-MM-yyyy
                        if (parts[0].Length == 4)
                        {
                            // yyyy-MM-dd format
                            return dateString;
                        }
                        else
                        {
                            // dd-MM-yyyy format
                            var day = parts[0].PadLeft(2, '0');
                            var month = parts[1].PadLeft(2, '0');
                            var year = parts[2];
                            return $"{year}-{month}-{day}";
                        }
                    }
                }

                return dateString;
            }
            catch
            {
                return dateString;
            }
        }

        private static string ConvertSpanishDate(string day, string monthName, string year)
        {
            if (SpanishMonths.TryGetValue(monthName, out string? monthNumber))
            {
                return $"{year}-{monthNumber}-{day.PadLeft(2, '0')}";
            }
            
            Log.Warning("Unknown Spanish month name: {MonthName}", monthName);
            return string.Empty;
        }
    }
}
