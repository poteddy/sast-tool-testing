using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Syncfusion.Maui.Toolkit.Hosting;
using ToolTester.Application;
using ToolTester.Infrastructure;
using ToolTester.Infrastructure.Persistance;
namespace ToolTester.Presentation
{
    public static class MauiProgram
    {
        public  static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureSyncfusionToolkit()
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
    		
            builder.Services.AddLogging(configure =>
            {
                configure.SetMinimumLevel(LogLevel.Trace);
                configure.AddFilter("Microsoft.EntityFrameworkCore.Database", LogLevel.Warning);
                configure.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
                configure.AddDebug(); // Adds the console logger
                                        // You can also add other providers like AddDebug(), AddEventLog(), etc.
            });

#endif
            builder.Services.AddInfrastructureServices()
                .AddApplicationServices();
            builder.Services.AddSingleton<ProjectRepository>();
            builder.Services.AddSingleton<TaskRepository>();
            builder.Services.AddSingleton<CategoryRepository>();
            builder.Services.AddSingleton<TagRepository>();
            builder.Services.AddSingleton<SeedDataService>();
            builder.Services.AddSingleton<ModalErrorHandler>();
            builder.Services.AddSingleton<MainPageModel>();
            builder.Services.AddSingleton<ProjectListPageModel>();
            builder.Services.AddSingleton<ManageMetaPageModel>();

            builder.Services.AddTransientWithShellRoute<ProjectDetailPage, ProjectDetailPageModel>("project");
            builder.Services.AddTransientWithShellRoute<TaskDetailPage, TaskDetailPageModel>("task");
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
            var catalog = ToolTester.Infrastructure.Extensions.XMLExtensions.ReadXML(mitrecatfilepath);

            var context = builder.Services.BuildServiceProvider().GetRequiredService<ApplicationDbContext>();
            context.Database.EnsureCreated();

            
            ApplicationDbContextSeed.SeedCWECatalog(context, catalog);
          
            return  builder.Build();
        }
    }
}
