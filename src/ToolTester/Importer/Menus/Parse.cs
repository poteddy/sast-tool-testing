using Importer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Data;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ToolTester.Application.Common.Interfaces;
using ToolTester.Domain.Coomon.Interfaces;
using ToolTester.Domain.Entities;
using ToolTester.Infrastructure.Persistance;
using ToolTester.Parsers.Sarif.Interfaces;
using static System.Net.Mime.MediaTypeNames;

namespace ToolTester.Importer.Menus
{
    internal class Parse
    {
        private readonly ILogger<Program> _logger;
        private readonly IServiceProvider serviceProvider;

        public Parse(IServiceProvider serviceProvider, ILogger<Program> logger)
        {
            this.serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task<int> Display()
        {
            Console.Write("Type Parser: ");

            var input = Console.ReadLine().ToLower();
            if (input == "Sarif".ToLower())
            {
                ToolTester.Parsers.Sarif.Parser parser = new ToolTester.Parsers.Sarif.Parser();

                Console.WriteLine("input file: ");
                var filepath = Console.In.ReadLine();

                Console.WriteLine("Tool");

                var parseservice = serviceProvider.GetRequiredService<IParsingService>();
              var result = await  parseservice.Parse(1,filepath);
                Console.WriteLine($"Parsed {result} items");
                return result;

            }

            return 0;
        }
    }
}
