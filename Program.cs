using System;
using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

public class Program
{
    static Camera3D camera;
    static Model terrainModel;
    static Model terrainModel2;
    static float isolevel = 0.5f;
    static int gridSize = 33;
    static float noiseScale = 1f;

    public static void Main()
    {
        InitWindow(1280, 720, "3D Terrain with Marching Cubes");
        SetTargetFPS(60);

        // Setup camera
        camera = new Camera3D();
        camera.Position = new Vector3(50, 50, 50);
        camera.Target = new Vector3(0, 0, 0);
        camera.Up = new Vector3(0, 1, 0);
        camera.FovY = 45.0f;
        camera.Projection = CameraProjection.Perspective;

        // Generate terrain
        terrainModel = GenerateTerrain(Vector3.Zero);
        terrainModel2 = GenerateTerrain(new Vector3(32, 0, 0));

        while (!WindowShouldClose())
        {
            // Camera controls
            UpdateCamera(ref camera, CameraMode.Free);

            if (IsKeyPressed(KeyboardKey.R))
            {
                // Regenerate terrain
                UnloadModel(terrainModel);
                UnloadModel(terrainModel2);
                GenerateTerrain(Vector3.Zero);
                GenerateTerrain(new Vector3(32, 0, 0));
            }

            // Adjust isolevel
            if (IsKeyDown(KeyboardKey.Up)) isolevel += 0.01f;
            if (IsKeyDown(KeyboardKey.Down)) isolevel -= 0.01f;
            isolevel = Math.Clamp(isolevel, 0.1f, 0.9f);

            BeginDrawing();
            ClearBackground(Color.Black);

            BeginMode3D(camera);
            DrawModel(terrainModel, Vector3.Zero, 1.0f, Color.Green);
            DrawModel(terrainModel2, new Vector3(32, 0, 0), 1.0f, Color.Green);
            DrawGrid(100, 10.0f);
            EndMode3D();

            DrawText("3D Terrain with Marching Cubes", 10, 10, 20, Color.Lime);
            DrawText("Controls: WASD to move, Mouse to look, R to regenerate", 10, 40, 20, Color.Yellow);
            DrawText($"Isolevel: {isolevel:F2} (UP/DOWN to adjust)", 10, 70, 20, Color.Yellow);
            DrawText($"Grid Size: {gridSize} Noise Scale: {noiseScale:F2}", 10, 100, 20, Color.Yellow);
            DrawFPS(10, 130);

            EndDrawing();
        }

        UnloadModel(terrainModel);
        UnloadModel(terrainModel2);
        CloseWindow();
    }

    static Model GenerateTerrain(Vector3 worldPosition)
    {
        // Create 3D grid of points
        Point[,,] points = new Point[gridSize, gridSize, gridSize];

        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    Vector3 position = new Vector3(x - gridSize / 2, y - gridSize / 2, z - gridSize / 2);
                    float density = Perlin3D((worldPosition.X + x) * noiseScale, (worldPosition.Y + y) * noiseScale, (worldPosition.Z + z) * noiseScale);
                    //float density = y > gridSize / 2 ? 0f : 1f;
                    points[x, y, z] = new Point(position, density);
                }
            }
        }

        // Generate mesh
        MarchingCubes mc = new MarchingCubes(points, isolevel);
        Mesh mesh = mc.CreateMeshData(points);

        UploadMesh(ref mesh, false);

        return LoadModelFromMesh(mesh);
    }

    // 3D Perlin Noise implementation
    static float Perlin3D(float x, float y, float z)
    {
        //float ab = MathF.Cos(x * 0.1f) + MathF.Sin(y * 0.1f);
        //float bc = MathF.Sin(y * 0.1f) + MathF.Cos(z * 0.1f);
        //float ca = MathF.Cos(z * 0.1f) + MathF.Sin(x * 0.1f);

        //return (ab + bc + ca) / 3.0f;

        return perlinNoise.get3DPerlinNoise(new Vector3(x, y, z), 0.1f);
    }
}