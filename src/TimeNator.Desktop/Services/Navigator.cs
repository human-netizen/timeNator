using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using TimeNator.Desktop.ViewModels;

namespace TimeNator.Desktop.Services;

public interface INavigator
{
    ViewModelBase? Current { get; }
    void GoTo<T>() where T : ViewModelBase;
}

/// <summary>Swaps the page shown in the main window. The ViewLocator picks the view.</summary>
public partial class Navigator(IServiceProvider services) : ObservableObject, INavigator
{
    [ObservableProperty] public partial ViewModelBase? Current { get; private set; }

    public void GoTo<T>() where T : ViewModelBase
    {
        (Current as IDisposable)?.Dispose();
        Current = services.GetRequiredService<T>();
    }
}
