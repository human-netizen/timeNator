using Avalonia.Data.Converters;
using Avalonia.Media;

namespace TimeNator.Desktop.Converters;

public static class BoolConverters
{
    /// <summary>Full opacity when true, dimmed when false.</summary>
    public static readonly IValueConverter ToOpacity = new FuncValueConverter<bool, double>(on => on ? 1.0 : 0.25);

    /// <summary>A faint accent background when true, used to mark the viewer's own row.</summary>
    public static readonly IValueConverter ToHighlight = new FuncValueConverter<bool, IBrush>(on =>
        on ? new SolidColorBrush(Color.Parse("#339B59B6")) : new SolidColorBrush(Color.Parse("#0FFFFFFF")));
}
