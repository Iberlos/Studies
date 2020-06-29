using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[,] a_heightMap, float a_maxHeight, AnimationCurve a_regionMultiplierCurve, int a_levelOfDetail)
    {
        AnimationCurve regionMultiplierCurve = new AnimationCurve(a_regionMultiplierCurve.keys);

        int width = a_heightMap.GetLength(0);
        int height = a_heightMap.GetLength(1);

        float topLeftX = (width - 1) / -2f;
        float topLeftZ = (height - 1) / 2f;

        int meshSimplificationIncrement = (a_levelOfDetail == 0) ? 1: a_levelOfDetail * 2;
        int verticesPerLine = (width - 1) / meshSimplificationIncrement + 1;

        MeshData meshData = new MeshData(verticesPerLine, verticesPerLine);
        int vertexIndex = 0;

        for(int x = 0; x < width; x+= meshSimplificationIncrement)
        {
            for(int y = 0; y < height; y+= meshSimplificationIncrement)
            {
                meshData.vertices[vertexIndex] = new Vector3(-x - topLeftX, regionMultiplierCurve.Evaluate(a_heightMap[x, y]) * a_maxHeight, topLeftZ - y);
                meshData.uvs[vertexIndex] = new Vector2(x / (float)width, y / (float)height);

                if(x < width-1 && y < height-1)
                {
                    meshData.AddTraingle(vertexIndex, vertexIndex+ verticesPerLine + 1, vertexIndex+ verticesPerLine);
                    meshData.AddTraingle(vertexIndex + verticesPerLine + 1, vertexIndex, vertexIndex + 1);
                }

                vertexIndex++;
            }
        }

        return meshData;
    }
}

public class MeshData
{
    public Vector3[] vertices;
    public int[] triangles;
    public Vector2[] uvs;

    int triangleIndex;

    public MeshData(int a_meshWidth, int a_meshHeight)
    {
        vertices = new Vector3[a_meshWidth * a_meshHeight];
        uvs = new Vector2[a_meshWidth * a_meshHeight];
        triangles = new int[(a_meshWidth - 1) * (a_meshHeight - 1) * 6];
    }

    public void AddTraingle(int a, int b, int c)
    {
        triangles[triangleIndex] = a;
        triangles[triangleIndex+1] = b;
        triangles[triangleIndex+2] = c;
        triangleIndex += 3;
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        return mesh;
    }
}