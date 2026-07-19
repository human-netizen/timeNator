using CommunityToolkit.Mvvm.ComponentModel;
using TimeNator.Desktop.Interop;
using TimeNator.Desktop.Models;
using TimeNator.Desktop.Services;

namespace TimeNator.Desktop.ViewModels;

public record SoundOption(string Name, NoiseKind? Kind);

/// <summary>Picks a background noise and keeps it looping until switched off.</summary>
public partial class BackgroundAudioViewModel(ILoopingSound sound) : ViewModelBase
{
    private static readonly TimeSpan ClipLength = TimeSpan.FromSeconds(30);
    private const double Volume = 0.35;

    public static IReadOnlyList<SoundOption> Options { get; } =
    [
        new("No sound", null),
        new("White noise", NoiseKind.White),
        new("Pink noise (rain)", NoiseKind.Pink),
        new("Brown noise (rumble)", NoiseKind.Brown)
    ];

    [ObservableProperty] public partial SoundOption Selected { get; set; } = Options[0];

    partial void OnSelectedChanged(SoundOption value)
    {
        if (value.Kind is not { } kind)
        {
            sound.Stop();
            return;
        }

        // Generated once and cached; a 30-second clip loops without an audible seam.
        var path = Path.Combine(AppPaths.DataDirectory, $"noise-{kind.ToString().ToLowerInvariant()}.wav");
        if (!File.Exists(path))
        {
            Directory.CreateDirectory(AppPaths.DataDirectory);
            File.WriteAllBytes(path, NoiseGenerator.CreateWav(kind, ClipLength, Volume));
        }
        sound.Play(path);
    }
}
