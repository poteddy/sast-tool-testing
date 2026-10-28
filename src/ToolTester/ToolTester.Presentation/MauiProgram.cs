using CommunityToolkit.Maui;
using LiveChartsCore.SkiaSharpView.Maui;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;
using Syncfusion.Maui.Toolkit.Hosting;
using System.Reflection;
using ToolTester.Application;
using ToolTester.Application.Common.Interfaces;
using ToolTester.Application.Common.Models;
using ToolTester.Infrastructure;
using ToolTester.Infrastructure.Persistance;

#if MAUI_DEVFLOW
using Microsoft.Maui.DevFlow.Agent;
#endif

namespace ToolTester.Presentation
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {

            var builder = MauiApp.CreateBuilder();
#if MAUI_DEVFLOW
            builder.AddMauiDevFlowAgent();
#endif
            var a = Assembly.GetExecutingAssembly();
            var appSettings = $"{a.GetName().Name}.appsettings.json";
            using var stream = a.GetManifestResourceStream(appSettings);
            var config = new ConfigurationBuilder()
            .AddJsonStream(stream)
            .Build();
            builder.Configuration.AddConfiguration(config);

            var mitreCatalogPath = EnsureSeedAssetCopiedAsync("cwec_v4.17.xml")
                .GetAwaiter()
                .GetResult();
            var julietProjectPath = EnsureSeedAssetCopiedAsync(
                    "2017-10-01-juliet-test-suite-for-c-cplusplus-v1-3.zip")
                .GetAwaiter()
                .GetResult();
            builder.Configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["MitreCatalogSetting:Path"] = mitreCatalogPath,
                    ["JulietProjectSetting:Path"] = julietProjectPath
                });

            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureSyncfusionToolkit()
                .UseSkiaSharp()
                .UseLiveCharts()
                .ConfigureMauiHandlers(handlers =>
                {
#if IOS || MACCATALYST
    				handlers.AddHandler<Microsoft.Maui.Controls.CollectionView, Microsoft.Maui.Controls.Handlers.Items2.CollectionViewHandler2>();
#endif
                })
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("SegoeUI-Semibold.ttf", "SegoeSemibold");
                    fonts.AddFont("FluentSystemIcons-Regular.ttf", FluentUI.FontFamily);
                });

#if DEBUG
            builder.Logging.AddDebug();
            builder.Services.AddLogging(configure => configure.AddDebug());
#endif
            builder.Services.AddInfrastructureServices()
               .AddApplicationServices()
               .AddPresentationServices();

            var app = builder.Build();
            var syncfusionSetting = config.GetRequiredSection("SyncfusionSetting").Get<SyncfusionSetting>();




            var context = app.Services.GetRequiredService<ApplicationDbContext>();
            context.Database.EnsureCreated();
            var seed = app.Services.GetRequiredService<IApplicationDbContextSeed>();
            seed.SeedCWECatalog()
                .GetAwaiter()
                .GetResult();
            return app;
        }

        private static async Task<string> EnsureSeedAssetCopiedAsync(
            string assetName)
        {
            var destinationPath = Path.Combine(
                FileSystem.AppDataDirectory,
                assetName);

            if (File.Exists(destinationPath))
            {
                return destinationPath;
            }

            Directory.CreateDirectory(FileSystem.AppDataDirectory);
            var temporaryPath = $"{destinationPath}.tmp";
            try
            {
                await using var source =
                    await FileSystem.OpenAppPackageFileAsync(assetName)
                        .ConfigureAwait(false);
                await using (var destination = File.Create(temporaryPath))
                {
                    await source.CopyToAsync(destination)
                        .ConfigureAwait(false);
                }

                File.Move(temporaryPath, destinationPath);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }

            return destinationPath;
        }
    }
}
