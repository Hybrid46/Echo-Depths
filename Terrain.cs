using Raylib_cs;
using static Raylib_cs.Raylib;
using System.Numerics;
using static Settings;

public class Terrain
{
    const float modelScale = 1.0f;
    Model model;

    public Terrain(Vector3 worldPosition, Shader shader)
    {
        model = GenerateTerrain(worldPosition);
        SetMaterialShader(ref model, 0, ref shader);
    }

    public Model GenerateTerrain(Vector3 worldPosition)
    {
        Point[,,] points = new Point[gridSize, gridSize, gridSize];
        float halfGrid = gridSize / 2.0f;

        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    Vector3 position = new Vector3(x - halfGrid, y - halfGrid, z - halfGrid);
                    float density = perlinNoise.get3DPerlinNoise(new Vector3(
                        (worldPosition.X + x + perlinOffset.X) * perlinScale,
                        (worldPosition.Y + y + perlinOffset.Y) * perlinScale,
                        (worldPosition.Z + z + perlinOffset.Z) * perlinScale), perlinFrequency);

                    points[x, y, z] = new Point(position, density);
                }
            }
        }

        MarchingCubes mc = new MarchingCubes(points, isolevel);
        Mesh mesh = mc.CreateMeshData(points);
        
        UploadMesh(ref mesh, false);

        return LoadModelFromMesh(mesh);
    }

    public void Draw(Vector3 worldPosition)
    {
        DrawModel(model, worldPosition, modelScale, Color.Green);
    }

    public void Unload()
    {
        UnloadModel(model);
    }
}
