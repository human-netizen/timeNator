using CommunityToolkit.Mvvm.ComponentModel;

namespace TimeNator.Desktop.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
    /// <summary>Called by the navigator once the page is shown.</summary>
    public virtual Task ActivateAsync() => Task.CompletedTask;
}
