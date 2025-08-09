using Raylib_cs;
using System.Numerics;
using static Raylib_cs.Raylib;
using static Settings;

public class EchoDepths
{
    public static Camera3D camera;
    private static Chunk[,,] chunks = new Chunk[worldSize, worldSize, worldSize];

    private static Shader sonarShader;

    private static int fresnelPowerLoc;
    private static int fresnelIntensityLoc;

    private static int fogDensityLoc;

    private static int waveProgressLoc;
    private static int waveMaxDistanceLoc;
    private static int waveWidthLoc;
    private static int cameraPositionLoc;
    private static int frameCount = 0;
    private static float waveProgress = 0.0f;

    //private static RenderTexture2D target = LoadRenderTexture(GetScreenWidth(), GetScreenHeight());

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

            //BeginTextureMode(target);
            ClearBackground(Color.Black);

            BeginMode3D(camera);

            foreach (Chunk chunk in chunks)
            {
                chunk.Draw();
            }
            
            UpdateSonarEffect();

            DrawGrid(100, gridSize);
            EndMode3D();
            //EndTextureMode();

            //BeginShaderMode(shaderBloom);
            //Bloom post process effect
            //EndShaderMode();

            BeginDrawing();
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
        in vec3 vertexNormal;

        uniform mat4 mvp;
        uniform mat4 matModel;
        uniform mat4 matNormal;

        out vec3 fragWorldPos;
        out vec3 fragNormal;

        void main()
        {
            vec4 worldPos = matModel * vec4(vertexPosition, 1.0);
            fragWorldPos = worldPos.xyz;
            fragNormal = normalize(vec3(matNormal * vec4(vertexNormal, 1.0)));
            gl_Position = mvp * vec4(vertexPosition, 1.0);
        }";

        // Fragment shader
        string fs = @"
        #version 330
        in vec3 fragWorldPos;
        in vec3 fragNormal;
        out vec4 finalColor;

        uniform vec3 cameraPosition;
        uniform float waveProgress;
        uniform float waveWidth;
        uniform float waveMaxDistance;
        uniform float fogDensity;
        uniform float fresnelPower;
        uniform float fresnelIntensity;

        void main()
        {
            // Calculate view direction
            vec3 viewDir = normalize(cameraPosition - fragWorldPos);
    
            // Fresnel effect
            float fresnel = pow(1.0 - max(dot(normalize(fragNormal), viewDir), 0.0), fresnelPower);
            fresnel *= fresnelIntensity;
            vec3 fresnelColor = vec3(1.0, 1.0, 1.0);
    
            // Base color with Fresnel
            vec3 baseColor = mix(vec3(0.0, 1.0, 0.0), fresnelColor, fresnel);
    
            // Fog calculation
            float dist = distance(fragWorldPos, cameraPosition);
            float fogFactor = exp(-dist * fogDensity);
    
            // Sonar effect
            if (waveProgress > 0.0)
            {
                float waveFront = waveProgress * waveMaxDistance;
                if (dist <= waveFront && dist >= waveFront - waveWidth)
                {
                    float d = (waveFront - dist) / waveWidth;
                    float intensity = smoothstep(0.0, 1.0, d);
                    vec3 sonarColor = mix(vec3(0.0, 0.0, 1.0), vec3(0.0, 1.0, 1.0), intensity);
                    baseColor = mix(baseColor, sonarColor, intensity);
                }
            }
    
            // Apply fog
            vec3 fogColor = vec3(0.0, 0.2, 0.6);
            baseColor = mix(fogColor, baseColor, fogFactor);
    
            finalColor = vec4(baseColor, 1.0);
        }";

        // Load shader from memory
        sonarShader = LoadShaderFromMemory(vs, fs);
        
        // Get uniform locations
        waveProgressLoc = GetShaderLocation(sonarShader, "waveProgress");
        waveMaxDistanceLoc = GetShaderLocation(sonarShader, "waveMaxDistance");
        waveWidthLoc = GetShaderLocation(sonarShader, "waveWidth");
        cameraPositionLoc = GetShaderLocation(sonarShader, "cameraPosition");

        fresnelPowerLoc = GetShaderLocation(sonarShader, "fresnelPower");
        fresnelIntensityLoc = GetShaderLocation(sonarShader, "fresnelIntensity");

        fogDensityLoc = GetShaderLocation(sonarShader, "fogDensity");

        float fogDensity = 0.02f;
        SetShaderValue(sonarShader, fogDensityLoc, fogDensity, ShaderUniformDataType.Float);

        // Set constant shader values
        float waveMaxDistance = 100.0f;
        SetShaderValue(sonarShader, waveMaxDistanceLoc, waveMaxDistance, ShaderUniformDataType.Float);

        float waveWidth = 30.0f;
        SetShaderValue(sonarShader, waveWidthLoc, waveWidth, ShaderUniformDataType.Float);
        
        float fresnelPower = 4.0f;
        float fresnelIntensity = 1.0f;
        SetShaderValue(sonarShader, fresnelPowerLoc, fresnelPower, ShaderUniformDataType.Float);
        SetShaderValue(sonarShader, fresnelIntensityLoc, fresnelIntensity, ShaderUniformDataType.Float);
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