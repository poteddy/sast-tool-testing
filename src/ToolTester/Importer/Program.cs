namespace Importer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ToolTester.Application;
using ToolTester.Importer.Extensions;
using ToolTester.Importer.Menus;
using ToolTester.Infrastructure;
using ToolTester.Infrastructure.Persistance;

public class Program
{
    public static async Task Main(string[] args)
    {

        //configure console logging
        var serviceCollection = new ServiceCollection();

        // Add logging to the service collection and configure the console provider
        serviceCollection.AddLogging(configure =>
        {
            configure.SetMinimumLevel(LogLevel.Trace);
            configure.AddFilter("Microsoft.EntityFrameworkCore.Database", LogLevel.Warning);
            configure.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
            configure.AddConsole(); // Adds the console logger
                                    // You can also add other providers like AddDebug(), AddEventLog(), etc.
        });
        serviceCollection.AddInfrastructureServices()
                .AddApplicationServices();

        var serviceProvider = serviceCollection.BuildServiceProvider();


        // Obtain an ILogger instance to start logging
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("This is an information message logged to the console.");
        logger.LogWarning("This is a warning message.");
        logger.LogError("This is an error message.");

        // See https://aka.ms/new-console-template for more information
        Console.WriteLine("Hello, World!");
        Console.WriteLine("pulling Mitre CWE catalog from res folder of repo base");
        var mitrecatfilepath = "..\\..\\..\\..\\..\\..\\res\\cwec_v4.17.xml";
        if (!File.Exists(mitrecatfilepath))
        {
            do
            {
                Console.WriteLine("Pleae provide Mitre cwec_v4.17.xml path or exit");
                mitrecatfilepath = Console.ReadLine();
            }while (mitrecatfilepath != "exit" && !File.Exists(mitrecatfilepath));
           
        }
        if (mitrecatfilepath == "exit")
        {
            Console.WriteLine("Exiting...");
            Environment.Exit(0);
        }
        var catalog = await XMLExtensions.ReadXML(mitrecatfilepath);

        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();

        logger.LogInformation("Loading Database");
        await ApplicationDbContextSeed.SeedCWECatalog(context, catalog);
        logger.LogInformation("Loading complete");
        // Keep the console open in a console application to see logs
        // if the application exits quickly
        await ShowMenus();
        while (true)
        {
            try
            {



                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    Console.Clear();
                    switch (key.Key)
                    {
                        case ConsoleKey.X:
                            Environment.Exit(0);
                            break;

                        case ConsoleKey.T:
                            var shoTestMenu = ActivatorUtilities.CreateInstance<ShowTest>(serviceProvider);
                          await shoTestMenu.Display();
                            break;
                        case ConsoleKey.P:
                            var parseMenu = ActivatorUtilities.CreateInstance<Parse>(serviceProvider);
                            await parseMenu.Display();
                            break;

                        case ConsoleKey.V:
                            var viewMenu = ActivatorUtilities.CreateInstance<View>(serviceProvider);
                            await viewMenu.Display();
                            break;
                            //case ConsoleKey.UpArrow:
                            //    if (Console.CursorTop > 0)
                            //    {
                            //        Console.SetCursorPosition(Console.CursorLeft - 1,
                            //            Console.CursorTop - 1);
                            //        Console.Write('*');
                            //    }
                            //    break;
                            //case ConsoleKey.DownArrow:
                            //    if (Console.CursorTop < Console.BufferHeight)
                            //    {
                            //        Console.SetCursorPosition(Console.CursorLeft - 1,
                            //            Console.CursorTop + 1);
                            //        Console.Write('*');
                            //    }
                            //    break;
                            //case ConsoleKey.LeftArrow:
                            //    if (Console.CursorLeft > 1)
                            //    {
                            //        Console.SetCursorPosition(Console.CursorLeft - 2,
                            //            Console.CursorTop);
                            //        Console.Write('*');
                            //    }
                            //    break;
                            //case ConsoleKey.RightArrow:
                            //    if (Console.CursorLeft < Console.WindowWidth - 1)
                            //    {
                            //        Console.Write('*');
                            //    }
                            //    break;

                    }

                    await ShowMenus();

                }
            }
            catch (Exception ex)
            {

                logger.LogError(ex.ToString());
            }
        }
    
    }
    private static async Task ShowMenus()
    {

        Console.WriteLine("Commands");
        Console.WriteLine("##########");
        Console.WriteLine("x - Exit");
        Console.WriteLine("m - Show menu");
        Console.WriteLine("t - Show CWE information");
        Console.WriteLine("p - Parse and store vulns");

        Console.WriteLine("##########");
    }
}
