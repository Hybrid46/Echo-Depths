using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;
using static Settings;

public class EchoDepths
{
    public static Camera3D camera;
    private static Chunk[,,] chunks = new Chunk[worldSize, worldSize, worldSize];

    public static void Main()
    {
        InitWindow(1280, 720, "3D Terrain with Marching Cubes");
        SetWindowFocused();
        SetTargetFPS(60);
        SetMouseCursor(MouseCursor.Crosshair);

        SetupCamera();

        InitializeRandomPerlinOffset();
        GenerateTerrain();

        while (!WindowShouldClose())
        {
            UpdateCamera(ref camera, CameraMode.Free);

            SetMousePosition(GetScreenWidth() / 2, GetScreenHeight() / 2);

            BeginDrawing();
            ClearBackground(Color.Black);

            BeginMode3D(camera);

            foreach (Chunk chunk in chunks)
            {
                chunk.Draw();
            }

            DrawGrid(100, gridSize);
            EndMode3D();

            DrawText("3D Terrain with Marching Cubes", 10, 10, 20, Color.Lime);
            DrawText("Controls: WASD to move, Mouse to look", 10, 40, 20, Color.Yellow);
            DrawFPS(10, 130);

            EndDrawing();
        }

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
        camera = new Camera3D()
        {
            Position = new Vector3(50, 50, 50),
            Target = new Vector3(0, 0, 0),
            Up = new Vector3(0, 1, 0),
            FovY = FOV,
            Projection = CameraProjection.Perspective
        };
    }

    private static void InitializeRandomPerlinOffset()
    {
        Random rnd = new Random();

        perlinOffset = new Vector3(
            rnd.NextSingle() * 10000000f,
            rnd.NextSingle() * 10000000f,
            rnd.NextSingle() * 10000000f
        );
    }
}