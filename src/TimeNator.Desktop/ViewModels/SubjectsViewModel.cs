using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeNator.Desktop.Services;
using TimeNator.Shared.Dtos;

namespace TimeNator.Desktop.ViewModels;

public partial class SubjectsViewModel(SubjectCatalog catalog) : ViewModelBase
{
    public static IReadOnlyList<string> Palette { get; } =
        ["#E74C3C", "#E67E22", "#F1C40F", "#2ECC71", "#1ABC9C", "#3498DB", "#9B59B6", "#EC407A", "#95A5A6"];

    public ObservableCollection<SubjectResponse> Subjects => catalog.Subjects;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCommand))]
    public partial string NewName { get; set; } = "";

    [ObservableProperty] public partial string NewColor { get; set; } = Palette[5];
    [ObservableProperty] public partial string? Error { get; set; }

    [RelayCommand]
    private Task LoadAsync() => RunAsync(() => catalog.RefreshAsync());

    [RelayCommand]
    private void PickColor(string color) => NewColor = color;

    [RelayCommand(CanExecute = nameof(CanAdd))]
    private Task AddAsync() => RunAsync(async () =>
    {
        await catalog.AddAsync(NewName.Trim(), NewColor);
        NewName = "";
    });

    private bool CanAdd() => !string.IsNullOrWhiteSpace(NewName);

    [RelayCommand]
    private Task DeleteAsync(SubjectResponse subject) => RunAsync(() => catalog.ArchiveAsync(subject));

    private async Task RunAsync(Func<Task> action)
    {
        try
        {
            await action();
            Error = null;
        }
        catch (ApiException ex)
        {
            Error = ex.Message;
        }
    }
}
