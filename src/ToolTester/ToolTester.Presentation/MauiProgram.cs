using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Syncfusion.Maui.Core.Hosting;
using Syncfusion.Maui.Toolkit.Hosting;
using ToolTester.Application;
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
            var mitrecatfilepath = "..\\..\\..\\..\\..\\..\\..\\res\\cwec_v4.17.xml";
            if (!File.Exists(mitrecatfilepath))
            {
                do
                {
                    Console.WriteLine("Pleae provide Mitre cwec_v4.17.xml path or exit");
                    mitrecatfilepath = Console.ReadLine();
                } while (mitrecatfilepath != "exit" && !File.Exists(mitrecatfilepath));

            }
            if (mitrecatfilepath == "exit")
            {
                Console.WriteLine("Exiting...");
                Environment.Exit(0);
            }
            var catalog =  XMLExtensions.ReadXML(mitrecatfilepath);
            var context = app.Services.GetRequiredService<ApplicationDbContext>();
            context.Database.EnsureCreated();

            context.SeedCWECatalog(catalog);
            return app;
        }
    }
}
