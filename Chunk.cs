using Raylib_cs;
using System;
using System.Numerics;

public class Chunk	
{
    Vector3 worldPosition;
    Terrain terrain;
    Shader shader;

    public Chunk(Vector3 worldPosition, Shader shader)
	{
        this.worldPosition = worldPosition;
        this.shader = shader;
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
}
