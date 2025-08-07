using System;
using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;
using static Settings;

public class Program
{
    public static Camera3D camera;
    private static Chunk[,,] chunks = new Chunk[worldSize, worldSize, worldSize];

    public static void Main()
    {
        InitWindow(1280, 720, "3D Terrain with Marching Cubes");
        SetTargetFPS(60);

        SetMouseCursor(MouseCursor.Crosshair);

        SetupCamera();

        GenerateTerrain();

        while (!WindowShouldClose())
        {
            // Camera controls
            UpdateCamera(ref camera, CameraMode.Free);
            SetMousePosition(GetScreenWidth() / 2, GetScreenHeight() / 2);

            BeginDrawing();
            ClearBackground(Color.Black);

            BeginMode3D(camera);

            foreach (Chunk chunk in chunks)
            {
                chunk.Draw();
            }

            DrawGrid(100, 10.0f);
            EndMode3D();

            DrawText("3D Terrain with Marching Cubes", 10, 10, 20, Color.Lime);
            DrawText("Controls: WASD to move, Mouse to look", 10, 40, 20, Color.Yellow);
            DrawFPS(10, 130);

            EndDrawing();
        }

        // Unload chunks and close window
        foreach (Chunk chunk in chunks)
        {
            chunk.Unload();
        }

        CloseWindow();
    }

    private static void GenerateTerrain()
    {
        for (int z = 0; z < worldSize; z++)
        {
            for (int y = 0; y < worldSize; y++)
            {
                for (int x = 0; x < worldSize; x++)
                {
                    Vector3 position = new Vector3(x * chunkSize, y * chunkSize, z * chunkSize);
                    chunks[x, y, z] = new Chunk(position);
                }
            }
        }
    }

    private static void SetupCamera()
    {
        camera = new Camera3D();
        camera.Position = new Vector3(50, 50, 50);
        camera.Target = new Vector3(0, 0, 0);
        camera.Up = new Vector3(0, 1, 0);
        camera.FovY = 60.0f;
        camera.Projection = CameraProjection.Perspective;
    }
}