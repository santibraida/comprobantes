using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FileContentRenamer.Models;
using FileContentRenamer.Services;
using FileContentRenamer.Configuration;
using Serilog;
using System.Reflection;

namespace FileContentRenamer.Tests.Helpers
{
    public class TestBase : IDisposable
    {
        private const string DefaultTestContent = "Test content";
        private const string DefaultPdfContent = "Test PDF content";
        private const string DefaultSearchPattern = "*.*";

        protected IServiceProvider ServiceProvider { get; }
        protected string TestFilesPath { get; }
        protected AppConfig TestConfig { get; }

        public TestBase()
        {
            // Setup test directory
            TestFilesPath = Path.Combine(Path.GetTempPath(), "FileContentRenamerTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(TestFilesPath);

            // Setup configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.test.json", optional: false)
                .Build();

            // Setup services
            var services = new ServiceCollection();

            // Configure logging for tests
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateLogger();

            // Load app config
            TestConfig = new AppConfig();
            configuration.GetSection("AppConfig").Bind(TestConfig);
            TestConfig.BasePath = TestFilesPath; // Override with test path

            services.AddSingleton(TestConfig);

            // Register services manually like in ServiceConfiguration
            services.AddTransient<IDateExtractor, DateExtractor>();
            services.AddTransient<IDirectoryOrganizer, DirectoryOrganizer>();
            services.AddTransient<IFilenameGenerator, FilenameGenerator>();
            services.AddTransient<IFileValidator, FileValidator>();
            services.AddTransient<IFileProcessor, PdfProcessor>();
            services.AddTransient<IFileProcessor, ImageProcessor>();
            services.AddTransient<IFileProcessor, TextProcessor>();
            services.AddTransient(provider => provider.GetServices<IFileProcessor>().ToList());
            services.AddTransient<IFileService, FileService>();

            ServiceProvider = services.BuildServiceProvider();
        }

        protected void CreateTestFile(string fileName, string content = DefaultTestContent)
        {
            var filePath = Path.Combine(TestFilesPath, fileName);
            EnsureDirectoryExists(filePath);
            File.WriteAllText(filePath, content);
        }

        protected void CreateTestPdfFile(string fileName, string content = DefaultPdfContent)
        {
            var filePath = Path.Combine(TestFilesPath, fileName);
            EnsureDirectoryExists(filePath);
            // Create a simple text file with .pdf extension for testing
            // In real tests, you might want to create actual PDF files
            File.WriteAllText(filePath, content);
        }

        private static void EnsureDirectoryExists(string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        protected string GetTestFilePath(string fileName)
        {
            return Path.Combine(TestFilesPath, fileName);
        }

        protected bool FileExists(string fileName)
        {
            return File.Exists(Path.Combine(TestFilesPath, fileName));
        }

        protected string ReadTestFile(string fileName)
        {
            return File.ReadAllText(Path.Combine(TestFilesPath, fileName));
        }

        protected string[] GetTestFiles(string searchPattern = DefaultSearchPattern)
        {
            return Directory.GetFiles(TestFilesPath, searchPattern, SearchOption.AllDirectories);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    if (Directory.Exists(TestFilesPath))
                    {
                        Directory.Delete(TestFilesPath, true);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Error cleaning up test directory: {TestFilesPath}", TestFilesPath);
                }

                if (ServiceProvider is IDisposable disposableProvider)
                {
                    disposableProvider.Dispose();
                }
            }
        }
    }
}
