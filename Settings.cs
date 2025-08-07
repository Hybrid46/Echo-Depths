using System;

public static class Settings
{
    public static float isolevel = 0.25f;
    public static int gridSize = 33;
    public static int chunkSize = gridSize - 1;

    // Perlin noise parameters
    public static float noiseScale = 1f;
    public static float frequency = 0.1f;

    public static int worldSize = 5;
}
