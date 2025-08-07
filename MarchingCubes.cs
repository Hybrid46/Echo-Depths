using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

public class MarchingCubes
{
    private Vector3[] _vertices;
    private int[] _triangles;
    private float _isolevel;

    private int _vertexIndex;

    private Vector3[] _vertexList;
    private Point[] _initPoints;
    private int[,,] _cubeIndexes;

    public MarchingCubes(Point[,,] points, float isolevel)
    {
        _isolevel = isolevel;
        _vertexIndex = 0;

        _vertexList = new Vector3[12];
        _initPoints = new Point[8];
        _cubeIndexes = new int[points.GetLength(0) - 1, points.GetLength(1) - 1, points.GetLength(2) - 1];
    }

    private Vector3 VertexInterpolate(Vector3 p1, Vector3 p2, float v1, float v2)
    {
        if (MathF.Abs(_isolevel - v1) < 0.000001f)
        {
            return p1;
        }
        if (MathF.Abs(_isolevel - v2) < 0.000001f)
        {
            return p2;
        }
        if (MathF.Abs(v1 - v2) < 0.000001f)
        {
            return p1;
        }

        float mu = (_isolevel - v1) / (v2 - v1);

        Vector3 p = p1 + mu * (p2 - p1);

        return p;
    }

    private void March(Point[] points, int cubeIndex)
    {
        int edgeIndex = LookupTables.EdgeTable[cubeIndex];

        _vertexList = GenerateVertexList(points, edgeIndex);

        int[] row = LookupTables.TriangleTable[cubeIndex];

        for (int i = 0; i < row.Length; i += 3)
        {
            _vertices[_vertexIndex] = _vertexList[row[i + 0]];
            _triangles[_vertexIndex] = _vertexIndex;
            _vertexIndex++;

            _vertices[_vertexIndex] = _vertexList[row[i + 1]];
            _triangles[_vertexIndex] = _vertexIndex;
            _vertexIndex++;

            _vertices[_vertexIndex] = _vertexList[row[i + 2]];
            _triangles[_vertexIndex] = _vertexIndex;
            _vertexIndex++;
        }
    }

    private Vector3[] GenerateVertexList(Point[] points, int edgeIndex)
    {
        for (int i = 0; i < 12; i++)
        {
            if ((edgeIndex & (1 << i)) != 0)
            {
                int[] edgePair = LookupTables.EdgeIndexTable[i];
                int edge1 = edgePair[0];
                int edge2 = edgePair[1];

                Point point1 = points[edge1];
                Point point2 = points[edge2];

                _vertexList[i] = VertexInterpolate(point1.position, point2.position, point1.density, point2.density);
            }
        }

        return _vertexList;
    }

    private int CalculateCubeIndex(Point[] points, float iso)
    {
        int cubeIndex = 0;

        for (int i = 0; i < 8; i++)
            if (points[i].density > iso)
                cubeIndex |= 1 << i;

        return cubeIndex;
    }

    public Mesh CreateMeshData(Point[,,] points)
    {
        _cubeIndexes = GenerateCubeIndexes(points);
        int vertexCount = GenerateVertexCount(_cubeIndexes);

        _vertices = new Vector3[vertexCount];
        _triangles = new int[vertexCount];

        for (int x = 0; x < points.GetLength(0) - 1; x++)
        {
            for (int y = 0; y < points.GetLength(1) - 1; y++)
            {
                for (int z = 0; z < points.GetLength(2) - 1; z++)
                {
                    int cubeIndex = _cubeIndexes[x, y, z];
                    if (cubeIndex == 0 || cubeIndex == 255) continue;

                    March(GetPoints(x, y, z, points), cubeIndex);
                }
            }
        }

        ushort[] indices = new ushort[vertexCount];

        for (int t = 0; t < _triangles.Length; t += 3)
        {
            indices[t] = (ushort)_triangles[t];
            indices[t + 1] = (ushort)_triangles[t + 1];
            indices[t + 2] = (ushort)_triangles[t + 2];
        }

        float[] vertexPositionArray = new float[_vertices.Length * 3];

        _vertexIndex = 0;
        foreach (Vector3 vertex in _vertices)
        {
            vertexPositionArray[_vertexIndex++] = vertex.X;
            vertexPositionArray[_vertexIndex++] = vertex.Y;
            vertexPositionArray[_vertexIndex++] = vertex.Z;
        }

        return GenerateMesh(_vertices, _triangles);
    }

    private Mesh GenerateMesh(Vector3[] _vertices, int[] _triangles)
    {
        Mesh mesh = new Mesh(_vertices.Length, _triangles.Length / 3);
        mesh.AllocVertices();
        Span<Vector3> vertices = mesh.VerticesAs<Vector3>();

        for (int t = 0; t < _triangles.Length; t += 3)
        {
            vertices[t] = _vertices[_triangles[t]];
            vertices[t + 1] = _vertices[_triangles[t + 1]];
            vertices[t + 2] = _vertices[_triangles[t + 2]];
        }

        return mesh;
    }

    private Point[] GetPoints(int x, int y, int z, Point[,,] points)
    {
        for (int i = 0; i < 8; i++)
        {
            Point p = points[x + CubePointsX[i], y + CubePointsY[i], z + CubePointsZ[i]];
            _initPoints[i] = p;
        }

        return _initPoints;
    }

    private int[,,] GenerateCubeIndexes(Point[,,] points)
    {
        for (int x = 0; x < points.GetLength(0) - 1; x++)
        {
            for (int y = 0; y < points.GetLength(1) - 1; y++)
            {
                for (int z = 0; z < points.GetLength(2) - 1; z++)
                {
                    _initPoints = GetPoints(x, y, z, points);

                    _cubeIndexes[x, y, z] = CalculateCubeIndex(_initPoints, _isolevel);
                }
            }
        }

        return _cubeIndexes;
    }

    private int GenerateVertexCount(int[,,] cubeIndexes)
    {
        int vertexCount = 0;

        for (int x = 0; x < cubeIndexes.GetLength(0); x++)
        {
            for (int y = 0; y < cubeIndexes.GetLength(1); y++)
            {
                for (int z = 0; z < cubeIndexes.GetLength(2); z++)
                {
                    int cubeIndex = cubeIndexes[x, y, z];
                    int[] row = LookupTables.TriangleTable[cubeIndex];
                    vertexCount += row.Length;
                }
            }
        }

        return vertexCount;
    }

    public static readonly Vector3[] CubePoints =
    {
        new Vector3(0, 0, 0),
        new Vector3(1, 0, 0),
        new Vector3(1, 0, 1),
        new Vector3(0, 0, 1),
        new Vector3(0, 1, 0),
        new Vector3(1, 1, 0),
        new Vector3(1, 1, 1),
        new Vector3(0, 1, 1)
    };

    public static readonly int[] CubePointsX =
    {
        0,
        1,
        1,
        0,
        0,
        1,
        1,
        0
    };

    public static readonly int[] CubePointsY =
    {
        0,
        0,
        0,
        0,
        1,
        1,
        1,
        1
    };

    public static readonly int[] CubePointsZ =
    {
        0,
        0,
        1,
        1,
        0,
        0,
        1,
        1
    };
}