using System;
using System.Numerics;

public class Chunk	
{
    Vector3 worldPosition;
    Terrain terrain;

    public Chunk(Vector3 worldPosition)
	{
        this.worldPosition = worldPosition;
        terrain = new Terrain(worldPosition);
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
}
