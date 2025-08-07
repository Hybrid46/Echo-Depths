using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;
using System.Collections.Generic;

public class MarchingCubes
{
    private float _isolevel;
    private List<Vector3> _verticesList;
    private List<int> _indicesList;
    private Dictionary<long, int> _vertexDictionary;

    public MarchingCubes(Point[,,] points, float isolevel)
    {
        _isolevel = isolevel;
    }

    private Vector3 VertexInterpolate(Vector3 p1, Vector3 p2, float v1, float v2)
    {
        if (MathF.Abs(_isolevel - v1) < 0.000001f) return p1;
        if (MathF.Abs(_isolevel - v2) < 0.000001f) return p2;
        if (MathF.Abs(v1 - v2) < 0.000001f) return p1;

        float mu = (_isolevel - v1) / (v2 - v1);
        return p1 + mu * (p2 - p1);
    }

    public Mesh CreateMeshData(Point[,,] points)
    {
        _verticesList = new List<Vector3>();
        _indicesList = new List<int>();
        _vertexDictionary = new Dictionary<long, int>();

        int gridSizeX = points.GetLength(0);
        int gridSizeY = points.GetLength(1);
        int gridSizeZ = points.GetLength(2);

        for (int x = 0; x < gridSizeX - 1; x++)
        {
            for (int y = 0; y < gridSizeY - 1; y++)
            {
                for (int z = 0; z < gridSizeZ - 1; z++)
                {
                    Point[] cubePoints = GetPoints(x, y, z, points);
                    int cubeIndex = CalculateCubeIndex(cubePoints, _isolevel);
                    if (cubeIndex == 0 || cubeIndex == 255) continue;

                    int edgeIndex = LookupTables.EdgeTable[cubeIndex];
                    int[] cubeEdgeVertices = new int[12];
                    for (int i = 0; i < 12; i++)
                        cubeEdgeVertices[i] = -1;

                    for (int i = 0; i < 12; i++)
                    {
                        if ((edgeIndex & (1 << i)) != 0)
                        {
                            int[] edgePair = LookupTables.EdgeIndexTable[i];
                            int v1 = edgePair[0];
                            int v2 = edgePair[1];

                            int x1 = x + CubePointsX[v1];
                            int y1 = y + CubePointsY[v1];
                            int z1 = z + CubePointsZ[v1];
                            int x2 = x + CubePointsX[v2];
                            int y2 = y + CubePointsY[v2];
                            int z2 = z + CubePointsZ[v2];

                            int index1 = FlattenIndex(x1, y1, z1, gridSizeX, gridSizeY);
                            int index2 = FlattenIndex(x2, y2, z2, gridSizeX, gridSizeY);

                            long min = Math.Min(index1, index2);
                            long max = Math.Max(index1, index2);
                            long key = (min << 32) | (uint)max;

                            if (!_vertexDictionary.TryGetValue(key, out int vertexIndex))
                            {
                                Vector3 vertexPos = VertexInterpolate(
                                    cubePoints[v1].position,
                                    cubePoints[v2].position,
                                    cubePoints[v1].density,
                                    cubePoints[v2].density
                                );
                                _verticesList.Add(vertexPos);
                                vertexIndex = _verticesList.Count - 1;
                                _vertexDictionary.Add(key, vertexIndex);
                            }
                            cubeEdgeVertices[i] = vertexIndex;
                        }
                    }

                    int[] row = LookupTables.TriangleTable[cubeIndex];
                    for (int i = 0; i < row.Length; i += 3)
                    {
                        _indicesList.Add(cubeEdgeVertices[row[i]]);
                        _indicesList.Add(cubeEdgeVertices[row[i + 1]]);
                        _indicesList.Add(cubeEdgeVertices[row[i + 2]]);
                    }
                }
            }
        }

        Mesh mesh = new Mesh(_verticesList.Count, _indicesList.Count / 3);
        mesh.AllocVertices();
        mesh.AllocIndices();

        Console.WriteLine($"Vertices: {_verticesList.Count}, Indices: {_indicesList.Count}");

        Span<Vector3> verticesSpan = mesh.VerticesAs<Vector3>();
        for (int i = 0; i < _verticesList.Count; i++)
            verticesSpan[i] = _verticesList[i];

        Span<ushort> indicesSpan = mesh.IndicesAs<ushort>();
        for (int i = 0; i < _indicesList.Count; i++)
            indicesSpan[i] = (ushort)_indicesList[i];

        return mesh;
    }

    private int FlattenIndex(int x, int y, int z, int gridSizeX, int gridSizeY)
    {
        return x + y * gridSizeX + z * gridSizeX * gridSizeY;
    }

    private int CalculateCubeIndex(Point[] points, float iso)
    {
        int cubeIndex = 0;
        for (int i = 0; i < 8; i++)
            if (points[i].density > iso)
                cubeIndex |= 1 << i;
        return cubeIndex;
    }

    private Point[] GetPoints(int x, int y, int z, Point[,,] points)
    {
        Point[] cubePoints = new Point[8];
        for (int i = 0; i < 8; i++)
        {
            cubePoints[i] = points[
                x + CubePointsX[i],
                y + CubePointsY[i],
                z + CubePointsZ[i]
            ];
        }
        return cubePoints;
    }

    public static readonly int[] CubePointsX = { 0, 1, 1, 0, 0, 1, 1, 0 };
    public static readonly int[] CubePointsY = { 0, 0, 0, 0, 1, 1, 1, 1 };
    public static readonly int[] CubePointsZ = { 0, 0, 1, 1, 0, 0, 1, 1 };
}