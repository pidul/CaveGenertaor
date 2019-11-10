using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CaveMeshGenerator : MonoBehaviour
{
    public SquareGrid squareGrid;
    public MeshFilter walls;

    List<Vector3> vertices;
    List<int> triangles;

    Dictionary<int, List<Triangle>> triangleDic = new Dictionary<int, List<Triangle>>();
    List<List<int>> outlines = new List<List<int>>();
    HashSet<int> checkedVertices = new HashSet<int>();

    public void GenerateMesh(int[,] map, float squareSize)
    {
        outlines.Clear();
        checkedVertices.Clear();
        triangleDic.Clear();

        squareGrid = new SquareGrid(map, squareSize);

        vertices = new List<Vector3>();
        triangles = new List<int>();

        for (int x = 0; x < squareGrid.squares.GetLength(0); ++x)
        {
            for (int y = 0; y < squareGrid.squares.GetLength(1); ++y)
            {
                TriangulateSquare(squareGrid.squares[x, y]);
            }
        }

        Mesh mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        CreateWallMesh();
    }

    void CreateWallMesh()
    {
        CalculateMeshOutlines();

        List<Vector3> wallVertices = new List<Vector3>();
        List<int> wallTriangles = new List<int>();
        Mesh wallMesh = new Mesh();
        float wallHeight = 5;

        foreach (List<int> outline in outlines)
        {
            for(int i = 0; i < outline.Count - 1; ++i)
            {
                int startIndex = wallVertices.Count;
                wallVertices.Add(vertices[outline[i]]);
                wallVertices.Add(vertices[outline[i + 1]]);
                wallVertices.Add(vertices[outline[i]] - Vector3.up * wallHeight);
                wallVertices.Add(vertices[outline[i + 1]] - Vector3.up * wallHeight);

                wallTriangles.Add(startIndex);
                wallTriangles.Add(startIndex + 2);
                wallTriangles.Add(startIndex + 3);

                wallTriangles.Add(startIndex + 3);
                wallTriangles.Add(startIndex + 1);
                wallTriangles.Add(startIndex);
            }
        }
        wallMesh.vertices = wallVertices.ToArray();
        wallMesh.triangles = wallTriangles.ToArray();
        walls.mesh = wallMesh;
    }

    void TriangulateSquare(Square square)
    {
        switch(square.m_Config)
        {
            case 0:
                break;

            case 1:
                MeshFromPoints(square.m_CentreLeft, square.m_CentreBottom, square.m_BottomLeft);
                break;
            case 2:
                MeshFromPoints(square.m_BottomRight, square.m_CentreBottom, square.m_CentreRight);
                break;
            case 4:
                MeshFromPoints(square.m_TopRight, square.m_CentreRight, square.m_CentreTop);
                break;
            case 8:
                MeshFromPoints(square.m_TopLeft, square.m_CentreTop, square.m_CentreLeft);
                break;

            case 3:
                MeshFromPoints(square.m_CentreRight, square.m_BottomRight, square.m_BottomLeft, square.m_CentreLeft);
                break;
            case 6:
                MeshFromPoints(square.m_CentreTop, square.m_TopRight, square.m_BottomRight, square.m_CentreBottom);
                break;
            case 9:
                MeshFromPoints(square.m_TopLeft, square.m_CentreTop, square.m_CentreBottom, square.m_BottomLeft);
                break;
            case 12:
                MeshFromPoints(square.m_TopLeft, square.m_TopRight, square.m_CentreRight, square.m_CentreLeft);
                break;
            case 5:
                MeshFromPoints(square.m_CentreTop, square.m_TopRight, square.m_CentreRight, square.m_CentreBottom, square.m_BottomLeft, square.m_CentreLeft);
                break;
            case 10:
                MeshFromPoints(square.m_TopLeft, square.m_CentreTop, square.m_CentreRight, square.m_BottomRight, square.m_CentreBottom, square.m_CentreLeft);
                break;

            case 7:
                MeshFromPoints(square.m_CentreTop, square.m_TopRight, square.m_BottomRight, square.m_BottomLeft, square.m_CentreLeft);
                break;
            case 11:
                MeshFromPoints(square.m_TopLeft, square.m_CentreTop, square.m_CentreRight, square.m_BottomRight, square.m_BottomLeft);
                break;
            case 13:
                MeshFromPoints(square.m_TopLeft, square.m_TopRight, square.m_CentreRight, square.m_CentreBottom, square.m_BottomLeft);
                break;
            case 14:
                MeshFromPoints(square.m_TopLeft, square.m_TopRight, square.m_BottomRight, square.m_CentreBottom, square.m_CentreLeft);
                break;

            case 15:
                MeshFromPoints(square.m_TopLeft, square.m_TopRight, square.m_BottomRight, square.m_BottomLeft);
                checkedVertices.Add(square.m_TopLeft.m_VertexIndex);
                checkedVertices.Add(square.m_TopRight.m_VertexIndex);
                checkedVertices.Add(square.m_BottomRight.m_VertexIndex);
                checkedVertices.Add(square.m_BottomLeft.m_VertexIndex);
                break;
        }
    }

    void MeshFromPoints(params Node[] points)
    {
        AssignVertices(points);

        if(points.Length >= 3)
        {
            CreateTriangle(points[0], points[1], points[2]);
        }
        if(points.Length >= 4)
        {
            CreateTriangle(points[0], points[2], points[3]);
        }
        if (points.Length >= 5)
        {
            CreateTriangle(points[0], points[3], points[4]);
        }
        if (points.Length >= 6)
        {
            CreateTriangle(points[0], points[4], points[5]);
        }
    }

    void AssignVertices(Node[] points)
    {
        for(int i = 0; i < points.Length; ++i)
        {
            if(points[i].m_VertexIndex == -1)
            {
                points[i].m_VertexIndex = vertices.Count;
                vertices.Add(points[i].m_Position);
            }
        }
    }

    void CreateTriangle(Node a, Node b, Node c)
    {
        triangles.Add(a.m_VertexIndex);
        triangles.Add(b.m_VertexIndex);
        triangles.Add(c.m_VertexIndex);

        Triangle triangle = new Triangle(a.m_VertexIndex, b.m_VertexIndex, c.m_VertexIndex);

        AddTriangleToDictionary(triangle.vertexIndexA, triangle);
        AddTriangleToDictionary(triangle.vertexIndexB, triangle);
        AddTriangleToDictionary(triangle.vertexIndexC, triangle);

    }

    void AddTriangleToDictionary(int vertexIndexKey, Triangle triangle)
    {
        if (triangleDic.ContainsKey(vertexIndexKey))
        {
            triangleDic[vertexIndexKey].Add(triangle);
        }
        else
        {
            List<Triangle> triangles = new List<Triangle>();
            triangles.Add(triangle);
            triangleDic.Add(vertexIndexKey, triangles);
        }
    }

    void CalculateMeshOutlines()
    {
        for(int vertexIndex = 0; vertexIndex < vertices.Count; ++vertexIndex)
        {
            if (!checkedVertices.Contains(vertexIndex))
            {
                int newOutlineVertex = GetConnectedOutlineVertex(vertexIndex);
                if (newOutlineVertex != -1)
                {
                    checkedVertices.Add(vertexIndex);

                    List<int> newOutline = new List<int>();
                    newOutline.Add(vertexIndex);
                    outlines.Add(newOutline);
                    FollowOutline(newOutlineVertex, outlines.Count - 1);
                    outlines[outlines.Count - 1].Add(vertexIndex);
                }
            }
        }
    }

    // TODO: change to non recursive
    void FollowOutline(int vertexIndex, int outlineIndex)
    {
        outlines[outlineIndex].Add(vertexIndex);
        checkedVertices.Add(vertexIndex);
        int nextVertexIndex = GetConnectedOutlineVertex(vertexIndex);

        if (nextVertexIndex != -1)
        {
            FollowOutline(nextVertexIndex, outlineIndex);
        }
    }

    int GetConnectedOutlineVertex(int vertexIndex)
    {
        List<Triangle> trianglesContainingVertex = triangleDic[vertexIndex];

        for (int i = 0; i < trianglesContainingVertex.Count; ++i)
        {
            Triangle triangle = trianglesContainingVertex[i];

            for (int j = 0; j < 3; ++j)
            {
                int vertexB = triangle[j];
                
                if (vertexB == vertexIndex || checkedVertices.Contains(vertexB))
                {
                    continue;
                }

                if (IsOutlineEdge(vertexIndex, vertexB))
                {
                    return vertexB;
                }
            }
        }

        return -1;
    }

    bool IsOutlineEdge(int vertexA, int vertexB)
    {
        List<Triangle> trianglesContainingVertexA = triangleDic[vertexA];
        int sharedTriangleCount = 0;
        for (int i = 0; i < trianglesContainingVertexA.Count; ++i)
        {
            if (trianglesContainingVertexA[i].Contains(vertexB))
            {
                ++sharedTriangleCount;
                if (sharedTriangleCount == 2)
                {
                    return false;
                }
            }
        }
        return true;
    }

    struct Triangle
    {
        public int vertexIndexA;
        public int vertexIndexB;
        public int vertexIndexC;
        int[] vertices;

        public Triangle(int a, int b, int c)
        {
            vertexIndexA = a;
            vertexIndexB = b;
            vertexIndexC = c;

            vertices = new int[3];
            vertices[0] = a;
            vertices[1] = b;
            vertices[2] = c;
        }

        public int this[int i]
        {
            get
            {
                return vertices[i];
            }
        }

        public bool Contains(int vertexIndex)
        {
            return vertexIndex == vertexIndexA ||
                    vertexIndex == vertexIndexB ||
                    vertexIndex == vertexIndexC;
        }
    }

    public class SquareGrid
    {
        public Square[,] squares;

        public SquareGrid(int[,] map, float squareSize)
        {
            int nodeCountX = map.GetLength(0);
            int nodeCountY = map.GetLength(1);
            float mapWidth = nodeCountX * squareSize;
            float mapHeight = nodeCountY * squareSize;

            ControlNode[,] controlNodes = new ControlNode[nodeCountX, nodeCountY];

            for(int x = 0; x < nodeCountX; ++x)
            {
                for (int y = 0; y < nodeCountY; ++y)
                {
                    Vector3 position = new Vector3(-mapWidth / 2 + x * squareSize + squareSize / 2,
                                                0,
                                                -mapHeight / 2 + y * squareSize + squareSize / 2);
                    controlNodes[x, y] = new ControlNode(position, map[x, y] == 1, squareSize);
                }
            }

            squares = new Square[nodeCountX - 1, nodeCountY - 1];
            for (int x = 0; x < nodeCountX - 1; ++x)
            {
                for (int y = 0; y < nodeCountY - 1; ++y)
                {
                    squares[x, y] = new Square(controlNodes[x, y + 1],
                                               controlNodes[x + 1, y + 1],
                                               controlNodes[x + 1, y],
                                               controlNodes[x, y]);
                }
            }
        }
    }

    public class Square
    {
        public ControlNode m_TopLeft, m_TopRight, m_BottomLeft, m_BottomRight;
        public Node m_CentreTop, m_CentreRight, m_CentreBottom, m_CentreLeft;
        public int m_Config;

        public Square(
            ControlNode topLeft,
            ControlNode topRight,
            ControlNode bottomRight,
            ControlNode bottomLeft
            )
        {
            m_TopLeft = topLeft;
            m_TopRight = topRight;
            m_BottomLeft = bottomLeft;
            m_BottomRight = bottomRight;

            m_CentreTop = m_TopLeft.m_RightNode;
            m_CentreRight = m_BottomRight.m_AboveNode;
            m_CentreBottom = m_BottomLeft.m_RightNode;
            m_CentreLeft = m_BottomLeft.m_AboveNode;

            m_Config = ((m_TopLeft.m_IsActive) ? 8 : 0) |
                       ((m_TopRight.m_IsActive) ? 4 : 0) |
                       ((m_BottomRight.m_IsActive) ? 2 : 0) |
                       (m_BottomLeft.m_IsActive ? 1 : 0);
        }
    }

    public class Node
    {
        public Vector3 m_Position;
        public int m_VertexIndex = -1;

        public Node(Vector3 pos)
        {
            m_Position = pos;
        }
    }

    public class ControlNode : Node
    {
        public bool m_IsActive;
        public Node m_AboveNode, m_RightNode;

        public ControlNode(Vector3 pos, bool isActive, float squareSize) : base(pos)
        {
            m_IsActive = isActive;
            m_AboveNode = new Node(pos + Vector3.forward * squareSize / 2f);
            m_RightNode = new Node(pos + Vector3.right * squareSize / 2f);
        }
    }
}
