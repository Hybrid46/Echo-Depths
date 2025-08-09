using System.Globalization;
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

    // Display settings
    public static int display = 0;
    public static int screenWidth = 0;
    public static int screenHeight = 0;
    public static bool fullscreen = false;
    public static bool borderlessWindowed = true;

    // Performance settings
    public static int targetFPS = 60;
    public static int targetUPS = 60;
    public static float fixedDeltaTime = 1.0f / 60.0f;
    public static float drawDistance = 100.0f;

    // Gameplay settings
    public static float moveSpeed = 3.0f;
    public static float rotationSpeed = 3.0f;
    public static float mouseRotationSpeed = 4.0f;

    private const string SETTINGS_FILE = "settings.cfg";

    private static void LoadDefaults()
    {
        display = 0;
        screenWidth = 0;
        screenHeight = 0;
        fullscreen = false;
        borderlessWindowed = true;
        targetFPS = 60;
        targetUPS = 60;
        moveSpeed = 3.0f;
        rotationSpeed = 3.0f;
        mouseRotationSpeed = 4.0f;
        FOV = 60f;
        fixedDeltaTime = 1.0f / targetUPS;
        drawDistance = 100.0f;
    }

    public static void LoadSettings()
    {
        if (!File.Exists(SETTINGS_FILE))
        {
            Console.WriteLine("Settings file not found. Creating with default values.");
            LoadDefaults();
            SaveSettings();  // Create file with defaults
            return;
        }

        Dictionary<string, string> loadedSettings = new Dictionary<string, string>();

        try
        {
            foreach (string line in File.ReadAllLines(SETTINGS_FILE))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                string[] parts = line.Split('=', 2);

                if (parts.Length == 2)
                {
                    loadedSettings[parts[0].Trim()] = parts[1].Trim();
                }
            }

            // Parse display settings
            if (loadedSettings.TryGetValue(nameof(display), out string displayStr) &&
                int.TryParse(displayStr, out int displayValue)) display = displayValue;

            if (loadedSettings.TryGetValue(nameof(screenWidth), out string widthStr) &&
                int.TryParse(widthStr, out int width)) screenWidth = width;

            if (loadedSettings.TryGetValue(nameof(screenHeight), out string heightStr) &&
                int.TryParse(heightStr, out int height)) screenHeight = height;

            if (loadedSettings.TryGetValue(nameof(fullscreen), out string fullscreenStr))
                fullscreen = Convert.ToBoolean(fullscreenStr);

            if (loadedSettings.TryGetValue(nameof(borderlessWindowed), out string borderlessStr))
                borderlessWindowed = Convert.ToBoolean(borderlessStr);

            // Parse performance settings
            if (loadedSettings.TryGetValue(nameof(targetFPS), out string fpsStr) &&
                int.TryParse(fpsStr, out int fps)) targetFPS = fps;

            if (loadedSettings.TryGetValue(nameof(targetUPS), out string upsStr) &&
                int.TryParse(upsStr, out int ups)) targetUPS = ups;

            // Parse gameplay settings - FIXED: use different variable names to avoid conflicts
            if (loadedSettings.TryGetValue(nameof(moveSpeed), out string moveSpeedStr) &&
                float.TryParse(moveSpeedStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float moveSpeedVal))
                moveSpeed = moveSpeedVal;

            if (loadedSettings.TryGetValue(nameof(rotationSpeed), out string rotSpeedStr) &&
                float.TryParse(rotSpeedStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float rotSpeedVal))
                rotationSpeed = rotSpeedVal;

            if (loadedSettings.TryGetValue(nameof(mouseRotationSpeed), out string mouseRotSpeedStr) &&
                float.TryParse(mouseRotSpeedStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float mouseRotSpeedVal))
                mouseRotationSpeed = mouseRotSpeedVal;

            if (loadedSettings.TryGetValue(nameof(FOV), out string FOVStr) &&
                float.TryParse(FOVStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float FOVVal))
                FOV = FOVVal;

            if (loadedSettings.TryGetValue(nameof(drawDistance), out string drawDistanceStr) &&
                float.TryParse(drawDistanceStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float drawDistanceVal))
                drawDistance = drawDistanceVal;

            // Update derived values
            fixedDeltaTime = 1.0f / Math.Max(targetUPS, 1);

            Console.WriteLine("Settings loaded successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading settings: {ex.Message}. Loading defaults.");
            LoadDefaults();
        }
    }

    public static void SaveSettings()
    {
        try
        {
            using (StreamWriter writer = new StreamWriter(SETTINGS_FILE))
            {
                writer.WriteLine($"# Game settings configuration");
                writer.WriteLine();

                // Display settings
                writer.WriteLine($"# Display Settings");
                writer.WriteLine($"{nameof(display)}={display}");
                writer.WriteLine($"{nameof(screenWidth)}={screenWidth}");
                writer.WriteLine($"{nameof(screenHeight)}={screenHeight}");
                writer.WriteLine($"{nameof(fullscreen)}={fullscreen.ToString().ToLower()}");
                writer.WriteLine($"{nameof(borderlessWindowed)}={borderlessWindowed.ToString().ToLower()}");
                writer.WriteLine();

                // Performance settings
                writer.WriteLine($"# Performance Settings");
                writer.WriteLine($"{nameof(targetFPS)}={targetFPS}");
                writer.WriteLine($"{nameof(targetUPS)}={targetUPS}");
                writer.WriteLine($"{nameof(drawDistance)}={drawDistance}");
                writer.WriteLine();

                // Gameplay settings
                writer.WriteLine($"# Gameplay Settings");
                writer.WriteLine($"{nameof(moveSpeed)}={moveSpeed.ToString(CultureInfo.InvariantCulture)}");
                writer.WriteLine($"{nameof(rotationSpeed)}={rotationSpeed.ToString(CultureInfo.InvariantCulture)}");
                writer.WriteLine($"{nameof(mouseRotationSpeed)}={mouseRotationSpeed.ToString(CultureInfo.InvariantCulture)}");
                writer.WriteLine($"{nameof(FOV)}={FOV.ToString(CultureInfo.InvariantCulture)}");
            }
            Console.WriteLine("Settings saved successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving settings: {ex.Message}");
        }
    }
}
