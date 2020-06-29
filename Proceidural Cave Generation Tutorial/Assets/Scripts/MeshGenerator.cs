using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshGenerator : MonoBehaviour
{
    public SquareGrid squareGrid;
    public MeshFilter walls;
    List<Vector3> vertices;
    List<int> triangles;

    Dictionary<int, List<Triangle>> triangleDictionary = new Dictionary<int, List<Triangle>>();
    List<List<int>> outlines = new List<List<int>>();
    HashSet<int> checkedVertices = new HashSet<int>();

    public void GenerateMesh(int[,] a_map, float a_squareSize = 1)
    {
        triangleDictionary.Clear();
        outlines.Clear();
        checkedVertices.Clear();

        squareGrid = new SquareGrid(a_map, a_squareSize);

        vertices = new List<Vector3>();
        triangles = new List<int>();

        for(int x=0; x<squareGrid.squares.GetLength(0); x++)
        {
            for(int y = 0; y < squareGrid.squares.GetLength(1); y++)
            {
                TriangulateSquare(squareGrid.squares[x,y]);
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
        float wallHeight = GetComponent<MapGenerator>().wallHeight;

        foreach (List<int> outline in outlines)
        {
            for (int i = 0; i < outline.Count - 1; i++)
            {
                int startIndex = wallVertices.Count;
                wallVertices.Add(vertices[outline[i]]); //LeftVertex
                wallVertices.Add(vertices[outline[i + 1]]); //RightVertex
                wallVertices.Add(vertices[outline[i]] - Vector3.up * wallHeight); //BottomLeftVertex
                wallVertices.Add(vertices[outline[i + 1]] - Vector3.up * wallHeight); //BottomRightVertex

                //wound counter clockwise
                wallTriangles.Add(startIndex + 0);
                wallTriangles.Add(startIndex + 2);
                wallTriangles.Add(startIndex + 3);
                wallTriangles.Add(startIndex + 3);
                wallTriangles.Add(startIndex + 1);
                wallTriangles.Add(startIndex + 0);
            }
        }
        wallMesh.vertices = wallVertices.ToArray();
        wallMesh.triangles = wallTriangles.ToArray();
        walls.mesh = wallMesh;

    }

    void TriangulateSquare(Square a_square)
    {
        switch(a_square.configuration)
        {
            #region No tri
            case 0:
                break;
            #endregion
            #region One point
            case 1:
                MeshFromPoints(a_square.centerLeft, a_square.centerBottom, a_square.bottomLeft);
                break;
            case 2:
                MeshFromPoints(a_square.bottomRight, a_square.centerBottom, a_square.centerRight);
                break;
            case 4:
                MeshFromPoints(a_square.topRight, a_square.centerRight, a_square.centerTop);
                break;
            case 8:
                MeshFromPoints(a_square.topLeft, a_square.centerTop, a_square.centerLeft);
                break;
            #endregion
            #region Two points
            case 3:
                MeshFromPoints(a_square.centerRight, a_square.bottomRight, a_square.bottomLeft, a_square.centerLeft);
                break;
            case 6:
                MeshFromPoints(a_square.centerTop, a_square.topRight, a_square.bottomRight, a_square.centerBottom);
                break;
            case 9:
                MeshFromPoints(a_square.topLeft, a_square.centerTop, a_square.centerBottom, a_square.bottomLeft);
                break;
            case 12:
                MeshFromPoints(a_square.topLeft, a_square.topRight, a_square.centerRight, a_square.centerLeft);
                break;
            case 5:
                MeshFromPoints(a_square.centerTop, a_square.topRight, a_square.centerRight, a_square.centerBottom, a_square.bottomLeft, a_square.centerLeft);
                break;
            case 10:
                MeshFromPoints(a_square.topLeft, a_square.centerTop, a_square.centerRight, a_square.bottomRight, a_square.centerBottom, a_square.centerLeft);
                break;
            #endregion
            #region Three points
            case 7:
                MeshFromPoints( a_square.topRight, a_square.bottomRight, a_square.bottomLeft, a_square.centerLeft, a_square.centerTop);
                break;
            case 11:
                MeshFromPoints(a_square.topLeft, a_square.centerTop, a_square.centerRight, a_square.bottomRight, a_square.bottomLeft);
                break;
            case 13:
                MeshFromPoints( a_square.topRight, a_square.centerRight, a_square.centerBottom, a_square.bottomLeft, a_square.topLeft);
                break;
            case 14:
                MeshFromPoints(a_square.topLeft, a_square.topRight, a_square.bottomRight, a_square.centerBottom, a_square.centerLeft);
                break;
            #endregion
            #region Four Points
            case 15:
                MeshFromPoints(a_square.topLeft, a_square.topRight, a_square.bottomRight, a_square.bottomLeft);
                checkedVertices.Add(a_square.topLeft.vertexIndex);
                checkedVertices.Add(a_square.topRight.vertexIndex);
                checkedVertices.Add(a_square.bottomRight.vertexIndex);
                checkedVertices.Add(a_square.bottomLeft.vertexIndex);
                break;
            #endregion
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
        if(points.Length >=5)
        {
            CreateTriangle(points[0], points[3], points[4]);
        }
        if (points.Length >= 6)
        {
            CreateTriangle(points[0], points[4], points[5]);
        }
    }

    void AssignVertices(Node[] a_points)
    {
        for(int i=0; i<a_points.Length; i++)
        {
            if(a_points[i].vertexIndex == -1)
            {
                a_points[i].vertexIndex = vertices.Count;
            }
            vertices.Add(a_points[i].position); //weird
        }
    }

    void CreateTriangle(Node a_a, Node a_b, Node a_c)
    {
        triangles.Add(a_a.vertexIndex);
        triangles.Add(a_b.vertexIndex);
        triangles.Add(a_c.vertexIndex);

        Triangle triangle = new Triangle(a_a.vertexIndex,a_b.vertexIndex,a_c.vertexIndex);
        AddTriangleToDictionary(triangle.vectexIndexA, triangle);
        AddTriangleToDictionary(triangle.vectexIndexB, triangle);
        AddTriangleToDictionary(triangle.vectexIndexC, triangle);
    }

    void AddTriangleToDictionary(int a_vertexIndexKey, Triangle a_triangle)
    {
        if (triangleDictionary.ContainsKey(a_vertexIndexKey))
        {
            triangleDictionary[a_vertexIndexKey].Add(a_triangle);
        }
        else
        {
            List<Triangle> triangleList = new List<Triangle>();
            triangleList.Add(a_triangle);
            triangleDictionary.Add(a_vertexIndexKey, triangleList);
        }
    }

    void CalculateMeshOutlines()
    {
        for(int vertexIndex =0; vertexIndex < vertices.Count; vertexIndex++)
        {
            int newOutlineVertex = GetConnectedOutlineVertex(vertexIndex);
            if(newOutlineVertex != -1)
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

    void FollowOutline(int a_vertexIndex, int a_outlineIndex)
    {
        outlines[a_outlineIndex].Add(a_vertexIndex);
        checkedVertices.Add(a_vertexIndex);
        int nextVertexIndex = GetConnectedOutlineVertex(a_vertexIndex);

        if(nextVertexIndex != -1)
        {
            FollowOutline(nextVertexIndex, a_outlineIndex);
        }
    }

    int GetConnectedOutlineVertex(int a_vertexIndex)
    {
        List<Triangle> trianglesContainingVertex;
        if (triangleDictionary.ContainsKey(a_vertexIndex))
        {
            trianglesContainingVertex = triangleDictionary[a_vertexIndex];
        }
        else
        {
            return -1;
        }


        foreach(Triangle tri in trianglesContainingVertex)
        {
            for(int j = 0; j<3; j++)
            {
                int vertexB = tri[j];

                if (vertexB!=a_vertexIndex && !checkedVertices.Contains(vertexB))
                {
                    if (IsOutlineEdge(a_vertexIndex, vertexB))
                    {
                        return vertexB;
                    }
                }
            }
        }

        return -1;
    }

    bool IsOutlineEdge(int a_vertexA, int a_vertexB)
    {
        List<Triangle> trianglesContainingVertexA = triangleDictionary[a_vertexA];
        int sharedTriangleCount = 0;

        foreach(Triangle tri in trianglesContainingVertexA)
        {
            if(tri.ContainsVertex(a_vertexB))
            {
                sharedTriangleCount++;
                if(sharedTriangleCount > 1)
                {
                    break;
                }
            }
        }

        return sharedTriangleCount == 1;
    }

    struct Triangle
    {
        public int vectexIndexA, vectexIndexB, vectexIndexC;
        int[] vertices;

        public Triangle(int a_a, int a_b, int a_c)
        {
            vectexIndexA = a_a;
            vectexIndexB = a_b;
            vectexIndexC = a_c;

            vertices = new int[3];
            vertices[0] = vectexIndexA;
            vertices[1] = vectexIndexB;
            vertices[2] = vectexIndexC;
        }

        public int this[int i]
        {
            get
            {
                return vertices[i];
            }
        }

        public bool ContainsVertex(int a_vertexIndex)
        {
            return a_vertexIndex == vectexIndexA || a_vertexIndex == vectexIndexB || a_vertexIndex == vectexIndexC;
        }
    }

    public class SquareGrid
    {
        public ControlNode[,] controlNodes;
        public Square[,] squares;

        public SquareGrid(int[,] a_map, float a_squareSize)
        {
            int _nodeCountX = a_map.GetLength(0);
            int _nodeCountY = a_map.GetLength(1);
            float _mapWitdh = _nodeCountX * a_squareSize;
            float _mapHeight = _nodeCountY * a_squareSize;

            controlNodes = new ControlNode[_nodeCountX, _nodeCountY];

            for(int x=0; x<_nodeCountX; x++)
            {
                for(int y=0; y<_nodeCountY; y++)
                {
                    Vector3 pos = new Vector3(-_mapWitdh / 2 + x * a_squareSize + a_squareSize / 2, 0, -_mapHeight / 2 + y * a_squareSize + a_squareSize / 2);
                    controlNodes[x, y] = new ControlNode(pos, a_map[x, y] == 1, a_squareSize);
                }
            }

            squares = new Square[_nodeCountX - 1, _nodeCountY - 1];
            for (int x = 0; x < _nodeCountX-1; x++)
            {
                for (int y = 0; y < _nodeCountY-1; y++)
                {
                    squares[x, y] = new Square(controlNodes[x, y + 1], controlNodes[x + 1, y + 1], controlNodes[x + 1, y], controlNodes[x, y]);
                }
            }
        }
    }

    public class Square
    {
        public ControlNode topLeft, topRight, bottomLeft, bottomRight;
        public Node centerTop, centerRight, centerLeft, centerBottom;
        public int configuration;

        public Square(ControlNode a_topLeft, ControlNode a_topRight, ControlNode a_bottomRight, ControlNode a_bottomLeft)
        {
            topLeft = a_topLeft;
            topRight = a_topRight;
            bottomLeft = a_bottomLeft;
            bottomRight = a_bottomRight;

            centerTop = topLeft.right;
            centerRight = bottomRight.above;
            centerBottom = bottomLeft.right;
            centerLeft = bottomLeft.above;

            if (topLeft.active) configuration += 8;
            if (topRight.active) configuration += 4;
            if (bottomRight.active) configuration += 2;
            if (bottomLeft.active) configuration += 1;
        }
    }

    public class Node
    {
        public Vector3 position;
        public int vertexIndex = -1;

        public Node(Vector3 a_position)
        {
            position = a_position;
        }
    }

    public class ControlNode: Node
    {
        public bool active;
        public Node right, above;

        public ControlNode(Vector3 a_position, bool a_active, float a_saquareSize): base(a_position)
        {
            active = a_active;
            above = new Node(position + Vector3.forward * a_saquareSize / 2);
            right = new Node(position + Vector3.right * a_saquareSize / 2);
        }
    }
}
