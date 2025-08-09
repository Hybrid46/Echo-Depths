using Raylib_cs;
using System.Numerics;

public class Chunk	
{
    Vector3 worldPosition;
    Terrain terrain;

    public Chunk(Vector3 worldPosition, Shader shader)
	{
        this.worldPosition = worldPosition;
        terrain = new Terrain(worldPosition, shader);
    }

    public void Draw()
    {
        if (terrain != null)
        {
            terrain.Draw(worldPosition);
        }
    }

    public void Unload()
    {
        terrain.Unload();
    }

    public bool IsVisible(Camera3D camera, float drawDistance)
    {
        BoundingBox bounds = new BoundingBox(
            worldPosition - new Vector3(Settings.chunkSize / 2f),
            worldPosition + new Vector3(Settings.chunkSize / 2f)
        );

        return Raylib.CheckCollisionBoxSphere(bounds, camera.Position, drawDistance);
    }
}
