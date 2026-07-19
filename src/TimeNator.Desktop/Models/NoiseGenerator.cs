namespace TimeNator.Desktop.Models;

public enum NoiseKind
{
    White,
    Pink,
    Brown
}

/// <summary>
/// Synthesises study noise as a 16-bit mono WAV. White is flat, pink falls 3 dB per
/// octave (softer, rain-like), brown falls 6 dB per octave (a low rumble).
/// </summary>
public static class NoiseGenerator
{
    public const int SampleRate = 22050;

    public static byte[] CreateWav(NoiseKind kind, TimeSpan length, double volume, int seed = 1)
    {
        var samples = Generate(kind, (int)(length.TotalSeconds * SampleRate), Math.Clamp(volume, 0, 1), seed);

        using var stream = new MemoryStream(44 + samples.Length * 2);
        using var writer = new BinaryWriter(stream);
        writer.Write("RIFF"u8);
        writer.Write(36 + samples.Length * 2);
        writer.Write("WAVE"u8);
        writer.Write("fmt "u8);
        writer.Write(16);                 // fmt chunk size
        writer.Write((short)1);           // PCM
        writer.Write((short)1);           // mono
        writer.Write(SampleRate);
        writer.Write(SampleRate * 2);     // byte rate
        writer.Write((short)2);           // block align
        writer.Write((short)16);          // bits per sample
        writer.Write("data"u8);
        writer.Write(samples.Length * 2);
        foreach (var sample in samples)
            writer.Write(sample);
        writer.Flush();
        return stream.ToArray();
    }

    private static short[] Generate(NoiseKind kind, int count, double volume, int seed)
    {
        var random = new Random(seed);
        var output = new short[count];
        double b0 = 0, b1 = 0, b2 = 0, brown = 0;

        for (var i = 0; i < count; i++)
        {
            var white = random.NextDouble() * 2 - 1;
            double value;
            switch (kind)
            {
                case NoiseKind.Pink:
                    // Paul Kellet's economy pink filter.
                    b0 = 0.99765 * b0 + white * 0.0990460;
                    b1 = 0.96300 * b1 + white * 0.2965164;
                    b2 = 0.57000 * b2 + white * 1.0526913;
                    value = (b0 + b1 + b2 + white * 0.1848) * 0.2;
                    break;
                case NoiseKind.Brown:
                    brown = (brown + 0.02 * white) / 1.02;
                    value = brown * 3.5;
                    break;
                default:
                    value = white * 0.5;
                    break;
            }

            output[i] = (short)(Math.Clamp(value * volume, -1, 1) * short.MaxValue);
        }

        return output;
    }
}
