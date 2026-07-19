using System.Text;
using TimeNator.Desktop.Models;

namespace TimeNator.Desktop.Tests;

public class NoiseGeneratorTests
{
    [Theory]
    [InlineData(NoiseKind.White)]
    [InlineData(NoiseKind.Pink)]
    [InlineData(NoiseKind.Brown)]
    public void Writes_a_valid_pcm_header_and_the_right_number_of_samples(NoiseKind kind)
    {
        var wav = NoiseGenerator.CreateWav(kind, TimeSpan.FromSeconds(2), 0.5);

        Assert.Equal("RIFF", Encoding.ASCII.GetString(wav, 0, 4));
        Assert.Equal("WAVE", Encoding.ASCII.GetString(wav, 8, 4));
        Assert.Equal(NoiseGenerator.SampleRate, BitConverter.ToInt32(wav, 24));
        Assert.Equal(2 * NoiseGenerator.SampleRate * 2, BitConverter.ToInt32(wav, 40));
        Assert.Equal(44 + 2 * NoiseGenerator.SampleRate * 2, wav.Length);
    }

    [Fact]
    public void Zero_volume_is_silence()
    {
        var wav = NoiseGenerator.CreateWav(NoiseKind.Pink, TimeSpan.FromSeconds(1), 0);

        Assert.All(wav.Skip(44), b => Assert.Equal(0, b));
    }
}
