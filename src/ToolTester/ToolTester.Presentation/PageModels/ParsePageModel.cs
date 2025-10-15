using CommunityToolkit.Mvvm.Input;
using MediatR;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ToolTester.Application.Common.Interfaces;
using ToolTester.Application.Tools.Queries;
using ToolTester.Presentation.Models;

namespace ToolTester.Presentation.PageModels
{
    public partial class ParsePageModel : BaseViewModel
    {
        private readonly IParsingService _parsingService;
        private readonly IReportingService _reportingService;
        private readonly IMediator _mediator;

        public IAsyncRelayCommand SarifParserCommand { get; }
        public ObservableCollection<MyButtonDataItem> ButtonItems { get; set; }
        public ICommand ButtonClickedCommand { get; }

        public ParsePageModel(IParsingService parsingService, IReportingService reportingService,IMediator mediator)
        {
             _parsingService = parsingService;
            _reportingService = reportingService;
            _mediator = mediator;
            ButtonItems = new ObservableCollection<MyButtonDataItem>();
            ButtonClickedCommand = new AsyncRelayCommand<MyButtonDataItem>( ExecuteButtonClickedCommand);

            var buttonresult = _mediator.Send(new GetToolsWithPaginationQuery()).Result;
            // Populate your list dynamically
            foreach (var tool in buttonresult.Items)
            {
                ButtonItems.Add(new MyButtonDataItem { ButtonText = tool.Name, CommandParameter = tool.Id });
                
            }
           
            // ... add more items as needed
        }

        private async Task ExecuteButtonClickedCommand(MyButtonDataItem item)
        {
            var temptoolid = (int)item.CommandParameter;
            var filepath = await PickAndShowImage();
            if (!string.IsNullOrEmpty(filepath))
            {
                var result = await _parsingService.Parse(temptoolid, filepath);
                Console.WriteLine($"Parsed {result} items");

                await _reportingService.GenerateReport(result, temptoolid);

            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
