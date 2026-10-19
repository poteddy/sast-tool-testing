using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using ToolTester.Domain.Entities;
using ToolTester.Presentation.Services;

namespace ToolTester.Presentation.PageModels;

public partial class ToolDeclaredCwesPageModel : BaseViewModel
{
    private readonly IToolDeclaredCoverageService _service;

    public ToolDeclaredCwesPageModel(IToolDeclaredCoverageService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        DeclaredCoverages = new ObservableCollection<ToolDeclaredCwe>();
        Tools = new ObservableCollection<Tool>();
    }

    public ObservableCollection<Tool> Tools { get; }

    [ObservableProperty]
    private Tool? _selectedTool;

    public ObservableCollection<ToolDeclaredCwe> DeclaredCoverages { get; }

    [ObservableProperty]
    private string _newCweIdText = string.Empty;

    [RelayCommand]
    public async Task RefreshAsync()
    {
        IsBusy = true;
        try
        {
            Tools.Clear();
            var tools = await _service.GetAllToolsAsync();
            foreach (var t in tools)
                Tools.Add(t);

            if (SelectedTool is null && Tools.Count > 0)
                SelectedTool = Tools[0];

            await LoadDeclaredForSelectedTool();
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSelectedToolChanged(Tool? value)
    {
        _ = LoadDeclaredForSelectedTool();
    }

    private async Task LoadDeclaredForSelectedTool()
    {
        DeclaredCoverages.Clear();
        if (SelectedTool is null) return;

        var declared = await _service.GetDeclaredForToolAsync(SelectedTool.Id);
        foreach (var d in declared)
            DeclaredCoverages.Add(d);
    }

    [RelayCommand]
    public async Task AddDeclaredCweAsync()
    {
        if (SelectedTool is null) return;
        if (!int.TryParse(NewCweIdText, out var cweId)) return;

        var dto = new ToolDeclaredCwe { ToolId = SelectedTool.Id, CweId = cweId };
        var added = await _service.AddDeclaredAsync(dto);
        if (added is not null)
            DeclaredCoverages.Add(added);

        NewCweIdText = string.Empty;
    }

    [RelayCommand]
    public async Task RemoveDeclaredCweAsync(ToolDeclaredCwe declared)
    {
        if (declared is null) return;
        var ok = await _service.RemoveDeclaredAsync(declared.Id);
        if (ok)
            DeclaredCoverages.Remove(declared);
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        if (SelectedTool is null) return;
        // Persist any changes if service requires explicit save.
        await _service.SaveChangesAsync();
        await RefreshAsync();
    }
}