using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Syncfusion.Maui.Toolkit.Charts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolTester.Application.CWECatalogs.Queries;
using ToolTester.Application.Relationships.Queries;
using ToolTester.Presentation.Models;
using ToolTester.Presentation.Services;
using ToolTester.Presentation.Ulitlities;

namespace ToolTester.Presentation.PageModels
{
    public partial class RelationshipsPageModel :BaseViewModel
    {
        private ObservableCollection<Relationship> _items = [];


        private bool _isNavigatedTo;
        private bool _dataLoaded;
        private readonly ModalErrorHandler _errorHandler;
        private readonly IMediator _mediator;

        
        private ObservableCollection<GroupRelations> _groups = [];
        public ObservableCollection<GroupRelations> Groups
        {
            get => _groups;
            set => SetProperty(ref _groups, value); // SetProperty handles property change notification
        }
        public ObservableCollection<Relationship> Items
        {
            get => _items;
            set => SetProperty(ref _items, value); // SetProperty handles property change notification
        }
          public RelationshipsPageModel(ModalErrorHandler errorHandler, IMediator mediator)
        {
            _errorHandler = errorHandler;
            _mediator = mediator;
        }

        public async Task LoadItemsAsync()
        {
        var query = new GetRelationshipsWithPaginationQuery()
            {
               PageSize =10000
            };
            var result = await _mediator.Send(query);
            ObservableCollection<Relationship> items = new ObservableCollection<Relationship>();
            ObservableCollection<GroupRelations> groupitems = new ObservableCollection<GroupRelations>();

            foreach (var group in result.Items.GroupBy(d=>d.Nature))
            {
                var key = group.Key;
                var grouprelation = new GroupRelations();

                grouprelation.Nature = key;
                grouprelation.Items = new List<Relationship>();
               
                foreach (var item in group)
                {
                    var rel = new Relationship()
                    {
                        Id = item.Id,
                        CWEID = item.CWEID,
                        ChainId = item.ChainId,
                        Nature = item.Nature,
                        Oridinal = item.Oridinal,
                        OrderSpecified = item.OrderSpecified,
                        RelatedCweID = item.RelatedCweID
                    };
                  
                    items.Add(rel);
                    grouprelation.Items.Add(rel);
                }
                groupitems.Add(grouprelation);
            }
            //Maui requires this to do initial load. 
            Items = new ObservableCollection<Relationship>(items);
            Groups = new ObservableCollection<GroupRelations>(groupitems);
            // ObservableCollection<GroupRelations> groupds = new ObservableCollection<GroupRelations>((IEnumerable<GroupRelations>)result.Items.GroupBy(d => d.Nature));
        }

        [RelayCommand]
        private void NavigatedTo() =>
        _isNavigatedTo = true;

        [RelayCommand]
        private void NavigatedFrom() =>
            _isNavigatedTo = false;


        [RelayCommand]
        private async Task Appearing()
        {
            if (!_dataLoaded)
            {

                //await InitData(_seedDataService);
                _dataLoaded = true;
                await Refresh();
            }
            // This means we are being navigated to
            else if (!_isNavigatedTo)
            {
                await Refresh();
            }
        }

        [RelayCommand]
        private async Task Refresh()
        {
            try
            {
                IsRefreshing = true;
                LoadItemsAsync().FireAndForgetSafeAsync(_errorHandler);
            }
            catch (Exception e)
            {

            }
            finally
            {
                IsRefreshing = false;
            }
        }

        [RelayCommand]
        private Task AddTask()
              => Shell.Current.GoToAsync($"task");

        [RelayCommand]
        private Task NavigateToProject(CweCatalogDetailsPage project)
            => Shell.Current.GoToAsync($"project?id={project.ID}");

    }

    public class GroupRelations()
    {
        public string Nature { get; set; }
        public List<Relationship> Items {  get; set; }
    }
   
}
