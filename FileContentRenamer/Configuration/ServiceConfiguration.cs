using Microsoft.Extensions.DependencyInjection;
using FileContentRenamer.Models;
using FileContentRenamer.Services;

namespace FileContentRenamer.Configuration
{
    public static class ServiceConfiguration
    {
        public static ServiceProvider ConfigureServices(AppConfig config)
        {
            var services = new ServiceCollection();

            // Register configuration with interface
            services.AddSingleton<IAppConfig>(config);
            services.AddSingleton(config); // Keep concrete type registration for backward compatibility

            // Register interfaces and their implementations
            services.AddTransient<IDateExtractor, DateExtractor>();
            services.AddTransient<IDirectoryOrganizer, DirectoryOrganizer>();
            services.AddTransient<IFilenameGenerator, FilenameGenerator>();
            services.AddTransient<IFileValidator, FileValidator>();

            // Register file processors
            services.AddTransient<IFileProcessor, PdfProcessor>();
            services.AddTransient<IFileProcessor, ImageProcessor>();
            services.AddTransient<IFileProcessor, TextProcessor>();

            // Register the list of processors
            services.AddTransient(provider =>
                provider.GetServices<IFileProcessor>().ToList());

            // Register main service
            services.AddTransient<IFileService, FileService>();

            return services.BuildServiceProvider();
        }
    }
}
