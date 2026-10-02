using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ToolTester.Application.Common.Interfaces;
using ToolTester.Domain.Entities;
using ToolTester.Presentation.Models;

namespace ToolTester.Presentation.PageModels
{
    public class ScansPageModel : BaseViewModel
    {
        private readonly IScanService _scanService;

        public ObservableCollection<ScanModel> Scans { get; } = new();
        ScanModel? _selectedScan;
        public ScanModel? SelectedScan
        {
            get => _selectedScan;
            set
            {
                _selectedScan = value;
                OnPropertyChanged();
            }
        }

        public IAsyncRelayCommand AddCommand { get; }
        public IAsyncRelayCommand RefreshCommand { get; }
        public IAsyncRelayCommand<ScanModel> EditCommand { get; }
        public IAsyncRelayCommand<ScanModel> DeleteCommand { get; }

        public ScansPageModel(IScanService scanService)
        {
            _scanService = scanService ?? throw new ArgumentNullException(nameof(scanService));

            AddCommand = new AsyncRelayCommand(AddAsync);
            RefreshCommand = new AsyncRelayCommand(RefreshAsync);
            EditCommand = new AsyncRelayCommand<ScanModel>(EditAsync);
            DeleteCommand = new AsyncRelayCommand<ScanModel>(DeleteAsync);

            _ = RefreshAsync();
        }

        async Task RefreshAsync()
        {
            try
            {
                Scans.Clear();
                var items = await _scanService.GetAllAsync(CancellationToken.None);

                // Map domain Scan -> presentation ScanModel
                foreach (var s in items)
                {
                    Scans.Add(new ScanModel
                    {
                        Id = s.Id,
                        Name = s.Name ?? string.Empty,
                        Description = string.Empty, // Domain Scan doesn't have Description - leave default or extend domain if needed
                        CreatedAt = DateTime.UtcNow  // no CreatedAt in domain - use default or extend domain if desired
                    });
                }
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert("Error", $"Failed to load scans: {ex.Message}", "OK");
            }
        }

        async Task AddAsync()
        {
            var name = await App.Current.MainPage.DisplayPromptAsync("New scan", "Name:");
            if (string.IsNullOrWhiteSpace(name))
                return;

            var description = await App.Current.MainPage.DisplayPromptAsync("New scan", "Description (optional):") ?? string.Empty;

            try
            {
                var domainScan = new Scan
                {
                    Name = name,
                    // ToolId must be set appropriately; default to 0 if unknown - consider prompting or selecting a Tool
                    ToolId = 0
                };

                var created = await _scanService.CreateAsync(domainScan, CancellationToken.None);

                var model = new ScanModel
                {
                    Id = created.Id,
                    Name = created.Name ?? string.Empty,
                    Description = description,
                    CreatedAt = DateTime.UtcNow
                };

                Scans.Add(model);
                SelectedScan = model;
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert("Error", $"Failed to create scan: {ex.Message}", "OK");
            }
        }

        async Task EditAsync(ScanModel? item)
        {
            if (item == null) return;

            var newName = await App.Current.MainPage.DisplayPromptAsync("Edit scan", "Name:", initialValue: item.Name);
            if (string.IsNullOrWhiteSpace(newName)) return;
            var newDesc = await App.Current.MainPage.DisplayPromptAsync("Edit scan", "Description:", initialValue: item.Description) ?? item.Description;

            try
            {
                // Load domain entity, update and persist
                var domain = await _scanService.GetByIdAsync(item.Id, CancellationToken.None);
                if (domain == null)
                {
                    await App.Current.MainPage.DisplayAlert("Error", "Scan not found.", "OK");
                    return;
                }

                domain.Name = newName;
                // Domain has no Description; keep it in presentation model only

                await _scanService.UpdateAsync(domain, CancellationToken.None);

                // Update presentation model
                item.Name = newName;
                item.Description = newDesc;
                OnPropertyChanged(nameof(Scans));
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert("Error", $"Failed to update scan: {ex.Message}", "OK");
            }
        }

        async Task DeleteAsync(ScanModel? item)
        {
            if (item == null) return;

            var confirm = await App.Current.MainPage.DisplayAlert("Delete", $"Delete '{item.Name}'?", "Yes", "No");
            if (!confirm) return;

            try
            {
                await _scanService.DeleteAsync(item.Id, CancellationToken.None);
                Scans.Remove(item);
                if (SelectedScan == item) SelectedScan = null;
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert("Error", $"Failed to delete scan: {ex.Message}", "OK");
            }
        }
    }
}