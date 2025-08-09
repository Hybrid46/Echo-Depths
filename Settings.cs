using System.Numerics;

public static class Settings
{
    public static float isolevel = 0.25f;
    public static int gridSize = 33;
    public static int chunkSize = gridSize - 1;
    public static int worldSize = 4;

    // Perlin noise parameters
    public static float perlinScale = 1f;
    public static float perlinFrequency = 0.1f;
    public static Vector3 perlinOffset = Vector3.Zero;

    public static float FOV = 60.0f;
}
