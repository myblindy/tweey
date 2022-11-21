using Simplex;

namespace Twee.Core;

public record struct MapGenerationWave(int Seed, float Frequency, float Amplitude)
{
    internal Noise Noise { get; set; }
}
public readonly record struct MapGenerationBiome(double Height, double Moisture, double Heat)
{
    public bool Matches(double height, double moisture, double heat) =>
        height >= Height && moisture >= Moisture && heat >= Heat;

    public double GetDifferenceFromPoint(double height, double moisture, double heat) =>
        (height - Height) + (moisture - Moisture) + (Heat - heat);
}

public static class MapGeneration
{
    static float GenerateNoiseValue(int x, int y, MapGenerationWave[] waves)
    {
        float result = 0, normalization = 0;
        foreach (ref readonly var wave in waves.AsSpan())
        {
            result += wave.Amplitude * wave.Noise.CalcPixel2D(x, y, wave.Frequency) / 255f;
            normalization += wave.Amplitude;
        }

        return result / normalization;
    }

    public static (double Height, byte BiomeIndex)[,] Generate(int width, int height,
        MapGenerationWave[] heightWaves, MapGenerationWave[] moistureWaves, MapGenerationWave[] heatWaves,
        MapGenerationBiome[] biomes)
    {
        foreach (ref var wave in heightWaves.AsSpan())
            wave.Noise = new() { Seed = wave.Seed };
        foreach (ref var wave in moistureWaves.AsSpan())
            wave.Noise = new() { Seed = wave.Seed };
        foreach (ref var wave in heatWaves.AsSpan())
            wave.Noise = new() { Seed = wave.Seed };

        var result = new (double Height, byte TileIndex)[width, height];

        for (int y = 0; y < height; ++y)
            for (int x = 0; x < width; ++x)
            {
                var heightValue = GenerateNoiseValue(x, y, heightWaves);
                var moistureValue = GenerateNoiseValue(x, y, moistureWaves);
                var heatValue = GenerateNoiseValue(x, y, heatWaves);

                var bestBiomeIndex = 0;
                var bestBiomeError = double.MaxValue;

                for (var biomeIndex = 0; biomeIndex < biomes.Length; ++biomeIndex)
                    if (biomes[biomeIndex].Matches(heightValue, moistureValue, heatValue)
                        && biomes[biomeIndex].GetDifferenceFromPoint(heightValue, moistureValue, heatValue) is { } error && error < bestBiomeError)
                    {
                        (bestBiomeIndex, bestBiomeError) = (biomeIndex, error);
                    }

                if (bestBiomeIndex == 4) { }
                result[x, y] = (heightValue, (byte)bestBiomeIndex);
            }

        return result;
    }
}
