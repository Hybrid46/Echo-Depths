using Raylib_cs;
using System.Diagnostics;
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
    private static float waveProgress = 0.0f;

    //Gameplay Loop variables
    static float accumulator = 0f;
    static int ups = 0;
    static int upsCount = 0;
    static double upsTimer = 0;

    // Performance metrics
    static double totalFrameTimeMs = 0;
    static double minFrameTime = double.MaxValue;
    static double maxFrameTime = 0;
    static double avgFrameTime = 0;
    static int frameCount = 0;
    static Stopwatch frameTimer = new Stopwatch();

    //private static RenderTexture2D target = LoadRenderTexture(GetScreenWidth(), GetScreenHeight());

    public static void Main()
    {
        LoadSettings();

        InitWindow(screenWidth == 0 ? GetScreenWidth() : screenWidth, screenWidth == 0 ? GetScreenWidth() : screenWidth, "3D Terrain with Marching Cubes");

        SetConfigFlags(ConfigFlags.VSyncHint);
        SetConfigFlags(ConfigFlags.Msaa4xHint);

        HideCursor();
        SetWindowPosition(0, 0);
        if (borderlessWindowed) ToggleBorderlessWindowed();

        SetWindowFocused();
        SetTargetFPS(targetFPS);
        SetMouseCursor(MouseCursor.Crosshair);

        SetupCamera();
        LoadSonarShader();
        InitializeRandomPerlinOffset();
        GenerateTerrain();

        frameTimer.Start();
        Stopwatch gameTimer = Stopwatch.StartNew();
        double lastTime = gameTimer.Elapsed.TotalSeconds;
        double currentTime = lastTime;

        while (!Raylib.WindowShouldClose())
        {
            // Calculate frame time
            currentTime = gameTimer.Elapsed.TotalSeconds;
            double frameTime = currentTime - lastTime;
            lastTime = currentTime;

            // Convert to milliseconds for metrics
            totalFrameTimeMs = frameTime * 1000.0;

            // Update performance stats
            if (totalFrameTimeMs < minFrameTime) minFrameTime = totalFrameTimeMs;
            if (totalFrameTimeMs > maxFrameTime) maxFrameTime = totalFrameTimeMs;

            frameCount++;
            if (frameCount >= 60)
            {
                avgFrameTime = (avgFrameTime * 0.9) + (totalFrameTimeMs * 0.1);
                frameCount = 0;
            }

            // Accumulate time for UPS
            accumulator += (float)frameTime;
            double updateStartTime = gameTimer.Elapsed.TotalSeconds;

            // Fixed timestep updates for game logic
            while (accumulator >= fixedDeltaTime)
            {
                using (PerformanceMonitor.Measure(t => PerformanceMonitor.FixedUpdateTime = t))
                {
                    FixedUpdate();
                }

                accumulator -= fixedDeltaTime;
                upsCount++;
            }

            // Update UPS counter every second
            upsTimer += frameTime;
            if (upsTimer >= 1.0)
            {
                ups = upsCount;
                upsCount = 0;
                upsTimer -= 1.0;
            }

            // Only render if we're not running behind on updates
            if (accumulator < fixedDeltaTime * 3)
            {
                RenderUpdate();
            }

            frameTimer.Restart();
        }

        foreach (Chunk chunk in chunks)
        {
            chunk.Unload();
        }

        UnloadShader(sonarShader);
        CloseWindow();
    }

    private static void FixedUpdate()
    {
        // Update game logic here -> key press etc
    }

    private static void RenderUpdate()
    {
        BeginBlendMode(BlendMode.Alpha);
        UpdateCamera(ref camera, CameraMode.Free);

        SetMousePosition(GetScreenWidth() / 2, GetScreenHeight() / 2);

        using (PerformanceMonitor.Measure(t => PerformanceMonitor.RenderTime = t))
        {
            //BeginTextureMode(target);
            ClearBackground(Color.Black);

            BeginMode3D(camera);

            foreach (Chunk chunk in chunks)
            {
                chunk.Draw();
            }

            UpdateSonarEffect();

            //DrawGrid(100, gridSize);
            EndMode3D();
            //EndTextureMode();
        }

        using (PerformanceMonitor.Measure(t => PerformanceMonitor.PostProcessTime = t))
        {
            //BeginShaderMode(shaderBloom);
            //Bloom post process effect
            //EndShaderMode();
        }

        BeginDrawing();
        DrawPerformanceMetrics();
        EndDrawing();
    }

    static void DrawPerformanceMetrics()
    {
        int startX = 10;
        int startY = 40;
        int lineHeight = 20;
        int line = 1;

        // Draw background panel
        Raylib.DrawRectangle(startX - 5, startY - 5, 320, 230, new Color(0, 0, 0, 180));
        Raylib.DrawRectangleLines(startX - 5, startY - 5, 320, 230, Color.DarkGray);

        // Draw metrics
        Raylib.DrawText($"{nameof(PerformanceMonitor.FixedUpdateTime)}: {PerformanceMonitor.FixedUpdateTime:F2} ms", startX, startY, lineHeight, Color.Green);
        Raylib.DrawText($"{nameof(PerformanceMonitor.RenderTime)}: {PerformanceMonitor.RenderTime:F2} ms", startX, startY + lineHeight * line++, lineHeight, Color.Green);
        Raylib.DrawText($"{nameof(PerformanceMonitor.PostProcessTime)}: {PerformanceMonitor.PostProcessTime:F2} ms", startX, startY + lineHeight * line++, lineHeight, Color.Green);

        Raylib.DrawText($"Total Frame time: {totalFrameTimeMs:F2} ms", startX, startY + lineHeight * line++, lineHeight, Color.Yellow);
        Raylib.DrawText($"FPS: {Raylib.GetFPS()}", startX, startY + lineHeight * line++, lineHeight, Color.Yellow);
        Raylib.DrawText($"UPS: {ups}", startX, startY + lineHeight * line++, lineHeight, Color.SkyBlue);

        // Min/Max/Avg
        Raylib.DrawText($"Min: {minFrameTime:F2} ms", startX, startY + lineHeight * line++, lineHeight, Color.Orange);
        Raylib.DrawText($"Max: {maxFrameTime:F2} ms", startX, startY + lineHeight * line++, lineHeight, Color.Orange);
        Raylib.DrawText($"Avg: {avgFrameTime:F2} ms", startX, startY + lineHeight * line++, lineHeight, Color.Orange);
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
            vec3 viewDir = normalize(cameraPosition - fragWorldPos);

            // Backface culling - discard if facing away from camera
            if (dot(fragNormal, viewDir) <= 0.0) {
                discard;
            }
    
            // Fresnel effect
            float fresnel = pow(1.0 - max(dot(normalize(fragNormal), viewDir), 0.0), fresnelPower) * fresnelIntensity;
            vec4 fresnelColor = vec4(0.5, 0.0, 0.0, 0.0) * fresnel;
    
            vec4 baseColor = vec4(0.0, 0.0, 0.0, 0.0);
    
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
                    vec4 sonarColor = mix(vec4(0.5, 0.0, 0.0, 1.0), vec4(0.5, 0.25, 0.0, 1.0), intensity);
                    baseColor = mix(baseColor, sonarColor + fresnelColor, intensity);
                }
            }
    
            // Apply fog
            vec4 fogColor = vec4(0.0, 0.0, 0.5, 0.0);
            finalColor = mix(fogColor, baseColor, fogFactor);
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

        float waveWidth = 50.0f;
        SetShaderValue(sonarShader, waveWidthLoc, waveWidth, ShaderUniformDataType.Float);

        float fresnelPower = 4.0f;
        float fresnelIntensity = 1.0f;
        SetShaderValue(sonarShader, fresnelPowerLoc, fresnelPower, ShaderUniformDataType.Float);
        SetShaderValue(sonarShader, fresnelIntensityLoc, fresnelIntensity, ShaderUniformDataType.Float);
    }

    private static void UpdateSonarEffect()
    {
        const int sonarFrequency = 60; // Trigger sonar every 180 frames

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