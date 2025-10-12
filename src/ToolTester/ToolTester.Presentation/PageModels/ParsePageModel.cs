using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ToolTester.Domain.Coomon.Interfaces;

namespace ToolTester.Presentation.PageModels
{
    public partial class ParsePageModel : BaseViewModel
    {
        private readonly IParsingService _parsingService;
        private readonly IReportingService _reportingService;

        public IAsyncRelayCommand SarifParserCommand { get; }
        public ParsePageModel(IParsingService parsingService, IReportingService reportingService)
        {
            SarifParserCommand = new AsyncRelayCommand(RunSarifParser);
            _parsingService = parsingService;
            _reportingService = reportingService;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private async Task RunSarifParser()
        {
            int temptoolid = 1;
            // Your C# code to be executed when the command is invoked
            var filepath = await PickAndShowImage();
            if (!string.IsNullOrEmpty(filepath))
            {
                var result = await _parsingService.Parse(temptoolid, filepath);
                Console.WriteLine($"Parsed {result} items");

            //    await _reportingService.GenerateReport(temptoolid);

            }

        }
     
        async Task<string> PickAndShowImage()
        {
            var result = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Please select an sarif file",
                
            });

            if (result != null)
            {
                return result.FullPath;
            }
            return string.Empty;
        }
    }
}
