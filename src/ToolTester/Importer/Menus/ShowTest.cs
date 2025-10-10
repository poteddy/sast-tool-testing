using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using ToolTester.Application.Common.Interfaces;

namespace ToolTester.ConsoleApp.Menus
{
    internal class ShowTest
    {
        private readonly ILogger<Program> logger;
        private readonly IApplicationDbContext context;

        public ShowTest(ILogger<Program> logger, IApplicationDbContext context)
        {
            this.logger = logger;
            this.context = context;
        }

        public async Task Display()
        {
            Console.Write("Type CWE ID: ");

            var input = Console.ReadLine();
            if (int.TryParse(input, out int validoutput))
            {
                var cat = await context.CWECatalogs.AsNoTracking().FirstOrDefaultAsync(d => d.Id == validoutput);
                var rel = await context.Relationships.AsNoTracking().Where(d => d.CWEID == validoutput).ToListAsync();
                JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions()
                {
                    WriteIndented = true
                };
                StringBuilder sb = new StringBuilder();

                sb.AppendLine(JsonSerializer.Serialize(cat, jsonSerializerOptions));
                foreach (var item in rel)
                {
                    var childcat = context.CWECatalogs.AsNoTracking().FirstOrDefault(d => d.Id == item.RelatedCweID);
                    sb.AppendLine($"{item.CWEID} is a {item.Nature} of {item.RelatedCweID} = {childcat.Id} {childcat.Name}");
                }

                Console.WriteLine(sb);

            }

        }
    }
}
