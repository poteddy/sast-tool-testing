using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ToolTester.Application.Common.Interfaces;
using ToolTester.ConsoleApp;

namespace ToolTester.ConsoleApp.Menus
{
    internal class Parse : IDisposable
    {
        private readonly ILogger<Program> _logger;
        private readonly IServiceProvider _serviceProvider;
        private bool disposedValue;

        public Parse(IServiceProvider serviceProvider, ILogger<Program> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task<int> Display()
        {

            Console.WriteLine("Select Parser number");
            Console.WriteLine("1 -- Sarif");
            var temptoolid = 0;
            var key = Console.ReadKey();
            switch (key.Key)
            {
                case ConsoleKey.NumPad1:
                case ConsoleKey.D1:
                    {
                        temptoolid = 1;
                        Parsers.Sarif.Parser parser = new Parsers.Sarif.Parser();

                        Console.WriteLine("input file: ");
                        var filepath = Console.In.ReadLine();
                      
                        var parseservice = _serviceProvider.GetRequiredService<IParsingService>();
                        var result = await parseservice.Parse(temptoolid, filepath);
                        Console.WriteLine($"Parsed {result} items");
                        parseservice.Dispose();

                        var reportservice = _serviceProvider.GetRequiredService<IReportingService>();
                        await reportservice.GenerateReport(result,temptoolid);
                        reportservice.Dispose();

                        return result;

                    }



            }

            return 0;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {

                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~Parse()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
