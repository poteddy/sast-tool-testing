using CommunityToolkit.Maui;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Syncfusion.Maui.Core.Hosting;
using Syncfusion.Maui.Toolkit.Hosting;
using System.Reflection;
using ToolTester.Application;
using ToolTester.Application.Common.Interfaces;
using ToolTester.Application.Common.Models;
using ToolTester.Infrastructure;
using ToolTester.Infrastructure.Extensions;
using ToolTester.Infrastructure.Persistance;

namespace ToolTester.Presentation
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {

            var builder = MauiApp.CreateBuilder();
            var a = Assembly.GetExecutingAssembly();
            var appSettings = $"{a.GetName().Name}.appsettings.json";
            using var stream = a.GetManifestResourceStream(appSettings);
            var config = new ConfigurationBuilder()
            .AddJsonStream(stream)
            .Build();
            builder.Configuration.AddConfiguration(config);

            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureSyncfusionToolkit()
                .ConfigureSyncfusionCore()
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

            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(syncfusionSetting.Registration_Key);

            
          
            var context = app.Services.GetRequiredService<ApplicationDbContext>();
            context.Database.EnsureCreated();
         
            return app;
        }
    }
    
}
