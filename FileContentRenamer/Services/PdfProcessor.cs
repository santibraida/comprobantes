using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Serilog;
using System.Text;

namespace FileContentRenamer.Services
{
    public class PdfProcessor : IFileProcessor
    {
        private const int MaxPagesToProcess = 3;
        private const int MinimumCharacterThreshold = 100;
        private const string PdfExtension = ".pdf";

        public bool CanProcess(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return false;

            return Path.GetExtension(filePath).Equals(PdfExtension, StringComparison.InvariantCultureIgnoreCase);
        }

        public async Task<string> ExtractContentAsync(string filePath)
        {
            try
            {
                using var pdfReader = new PdfReader(filePath);
                using var pdfDocument = new PdfDocument(pdfReader);

                var textBuilder = new StringBuilder();
                var strategy = new SimpleTextExtractionStrategy();

                // Process up to MaxPagesToProcess pages
                int totalPages = Math.Min(pdfDocument.GetNumberOfPages(), MaxPagesToProcess);
                for (int i = 1; i <= totalPages; i++)
                {
                    var page = pdfDocument.GetPage(i);
                    string pageText = PdfTextExtractor.GetTextFromPage(page, strategy);
                    textBuilder.Append(pageText);

                    // If we found enough text, stop processing
                    if (textBuilder.Length > MinimumCharacterThreshold)
                        break;
                }

                return textBuilder.ToString();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error extracting text from PDF");
                return string.Empty;
            }
        }
    }
}
