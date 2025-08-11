using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Serilog;

namespace FileContentRenamer.Services
{
    public class PdfProcessor : IFileProcessor
    {
        public bool CanProcess(string filePath)
        {
            return Path.GetExtension(filePath).Equals(".pdf", StringComparison.InvariantCultureIgnoreCase);
        }

        public async Task<string> ExtractContentAsync(string filePath)
        {
            try
            {
                // Process PDF files using iText7
                using var pdfReader = new PdfReader(filePath);
                using var pdfDocument = new PdfDocument(pdfReader);
                
                var text = string.Empty;
                
                for (int i = 1; i <= Math.Min(pdfDocument.GetNumberOfPages(), 3); i++) // Process up to 3 pages
                {
                    var page = pdfDocument.GetPage(i);
                    var strategy = new SimpleTextExtractionStrategy();
                    text += PdfTextExtractor.GetTextFromPage(page, strategy);
                    
                    // If we found enough text (more than 100 chars), stop processing
                    if (text.Length > 100)
                        break;
                }
                
                return await Task.FromResult(text);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error extracting text from PDF");
                return string.Empty;
            }
        }
    }
}
