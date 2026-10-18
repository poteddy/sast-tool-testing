using ToolTester.Application.Common.Interfaces;

namespace ToolTester.Infrastructure.Services
{
    public class DateTimeService : IDateTime
    {
        public DateTime Now => DateTime.Now;
    }
}
