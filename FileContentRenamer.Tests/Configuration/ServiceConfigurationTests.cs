using FileContentRenamer.Configuration;
using FileContentRenamer.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FileContentRenamer.Tests.Configuration
{
    public class ServiceConfigurationTests : IDisposable
    {
        private readonly string _tempDirectory;

        public ServiceConfigurationTests()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);
        }

        [Fact]
        public void ConfigureServices_ShouldRegisterAllServices()
        {
            // Arrange
            var appConfig = new AppConfig
            {
                BasePath = _tempDirectory,
                FileExtensions = [".pdf"],
                NamingRules = new NamingRules()
            };

            // Act
            var serviceProvider = ServiceConfiguration.ConfigureServices(appConfig);

            // Assert
            serviceProvider.GetService<AppConfig>().Should().Be(appConfig);
            serviceProvider.GetService<FileContentRenamer.Services.IDateExtractor>().Should().NotBeNull();
            serviceProvider.GetService<FileContentRenamer.Services.IFilenameGenerator>().Should().NotBeNull();
            serviceProvider.GetService<FileContentRenamer.Services.IFileValidator>().Should().NotBeNull();
            serviceProvider.GetService<FileContentRenamer.Services.IDirectoryOrganizer>().Should().NotBeNull();
            serviceProvider.GetService<FileContentRenamer.Services.IFileService>().Should().NotBeNull();
        }

        [Fact]
        public void ConfigureServices_ShouldRegisterFileProcessors()
        {
            // Arrange
            var appConfig = new AppConfig
            {
                BasePath = _tempDirectory,
                FileExtensions = [".pdf", ".jpg", ".txt"],
                NamingRules = new NamingRules()
            };

            // Act
            var serviceProvider = ServiceConfiguration.ConfigureServices(appConfig);

            // Assert
            var processors = serviceProvider.GetServices<FileContentRenamer.Services.IFileProcessor>();
            processors.Should().NotBeEmpty();
            processors.Should().HaveCount(3); // PdfProcessor, ImageProcessor, TextProcessor
        }

        [Fact]
        public void ConfigureServices_WithValidConfig_ShouldRegisterSingletonAppConfig()
        {
            // Arrange
            var appConfig = new AppConfig
            {
                BasePath = _tempDirectory,
                FileExtensions = [".pdf"],
                NamingRules = new NamingRules()
            };

            // Act
            var serviceProvider = ServiceConfiguration.ConfigureServices(appConfig);

            // Assert - should be singleton
            var config1 = serviceProvider.GetService<AppConfig>();
            var config2 = serviceProvider.GetService<AppConfig>();
            config1.Should().BeSameAs(config2);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }
    }
}
