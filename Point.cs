using System.Numerics;

public struct Point
{
    public Vector3 position;
    public float density;

    public Point(Vector3 position, float density)
    {
        this.position = position;
        this.density = density;
    }
}