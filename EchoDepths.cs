using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;
using static Settings;

public class EchoDepths
{
    public static Camera3D camera;
    private static Chunk[,,] chunks = new Chunk[worldSize, worldSize, worldSize];

    private static Shader sonarShader;
    private static int waveProgressLoc;
    private static int waveMaxDistanceLoc;
    private static int waveWidthLoc;
    private static int cameraPositionLoc;
    private static int frameCount = 0;
    private static float waveProgress = 0.0f;

    public static void Main()
    {
        InitWindow(1280, 720, "3D Terrain with Marching Cubes");
        
        SetConfigFlags(ConfigFlags.VSyncHint);
        SetConfigFlags(ConfigFlags.Msaa4xHint);

        SetWindowFocused();
        SetTargetFPS(60);
        SetMouseCursor(MouseCursor.Crosshair);

        SetupCamera();
        LoadSonarShader();
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
            
            UpdateSonarEffect();

            DrawGrid(100, gridSize);
            EndMode3D();

            DrawText("3D Terrain with Marching Cubes", 10, 10, 20, Color.Lime);
            DrawText("Controls: WASD to move, Mouse to look", 10, 40, 20, Color.Yellow);
            DrawFPS(10, 130);

            EndDrawing();
            frameCount++;
        }

        foreach (Chunk chunk in chunks)
        {
            chunk.Unload();
        }

        UnloadShader(sonarShader);
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
                    chunks[x, y, z] = new Chunk(position, sonarShader);
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

    private static void LoadSonarShader()
    {
        // Vertex shader
        string vs = @"
        #version 330
        in vec3 vertexPosition;
        uniform mat4 mvp;
        uniform mat4 matModel;
        out vec3 fragWorldPos;

        void main()
        {
            vec4 worldPos = matModel * vec4(vertexPosition, 1.0);
            fragWorldPos = worldPos.xyz;
            gl_Position = mvp * worldPos;
        }";

        // Fragment shader with sonar effect
        string fs = @"
        #version 330
        in vec3 fragWorldPos;
        out vec4 finalColor;

        uniform vec3 cameraPosition;
        uniform float waveProgress;
        uniform float waveWidth;
        uniform float waveMaxDistance;

        void main()
        {
            // Base terrain color
            vec3 baseColor = vec3(0.0, 1.0, 0.0);
            
            // Calculate distance from camera
            float dist = distance(fragWorldPos, cameraPosition);
            float waveFront = waveProgress * waveMaxDistance;
            
            // Calculate sonar effect
            if (waveProgress > 0.0 && dist <= waveFront && dist >= waveFront - waveWidth)
            {
                // Calculate wave intensity
                float d = (waveFront - dist) / waveWidth;
                float intensity = smoothstep(0.0, 1.0, d);
                
                // Create sonar color (blue to cyan)
                vec3 sonarColor = mix(vec3(0.0, 0.0, 1.0), vec3(0.0, 1.0, 1.0), intensity);
                
                // Blend with base color
                finalColor = vec4(mix(baseColor, sonarColor, intensity), 1.0);
            }
            else
            {
                // Default terrain color
                finalColor = vec4(baseColor, 1.0);
            }
        }";

        // Load shader from memory
        sonarShader = LoadShaderFromMemory(vs, fs);
        
        // Get uniform locations
        waveProgressLoc = GetShaderLocation(sonarShader, "waveProgress");
        waveMaxDistanceLoc = GetShaderLocation(sonarShader, "waveMaxDistance");
        waveWidthLoc = GetShaderLocation(sonarShader, "waveWidth");
        cameraPositionLoc = GetShaderLocation(sonarShader, "cameraPosition");

        // Set constant shader values
        float waveMaxDistance = 100.0f;
        SetShaderValue(sonarShader, waveMaxDistanceLoc, waveMaxDistance, ShaderUniformDataType.Float);

        float waveWidth = 30.0f;
        SetShaderValue(sonarShader, waveWidthLoc, waveWidth, ShaderUniformDataType.Float);
    }

    private static void UpdateSonarEffect()
    {
        const int sonarFrequency = 180; // Trigger sonar every 180 frames

        // Trigger sonar every 60 frames
        if (frameCount % sonarFrequency == 0)
        {
            waveProgress = 0.01f; // Start the wave
        }

        // Update wave progress
        if (waveProgress > 0)
        {
            waveProgress += 1f / sonarFrequency;

            if (waveProgress > 1.0f)
            {
                waveProgress = 0.0f;
            }
        }

        // Update shader uniforms
        SetShaderValue(sonarShader, waveProgressLoc, waveProgress, ShaderUniformDataType.Float);

        Vector3 camPos = camera.Position;
        SetShaderValue(sonarShader, cameraPositionLoc, camPos, ShaderUniformDataType.Vec3);
    }
}