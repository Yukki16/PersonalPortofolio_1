using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve _heightCurve, int levelOfDetail)
    {
        AnimationCurve heightCurve = new AnimationCurve(_heightCurve.keys);
        
        int meshSimplification = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;

        int borderedSize = heightMap.GetLength(0);
        int meshSize = borderedSize - 2 * meshSimplification;
        int meshSizeUnsimplified = borderedSize - 2;

        float topLeftX = (meshSizeUnsimplified - 1) / -2f;
        float topLeftZ = (meshSizeUnsimplified - 1) / 2f;

        int verticesPerLine = (meshSize - 1) / meshSimplification + 1;

        MeshData meshData = new MeshData(borderedSize);

        int[,] vertexIndexMap = new int[borderedSize, borderedSize];
        int meshVertexIndex = 0;
        int borderedVertexIndex = -1;

        for (int y = 0; y < borderedSize; y += meshSimplification)
        {
            for (int x = 0; x < borderedSize; x += meshSimplification)
            {
                bool isBorderVertex = y == 0 || y == borderedSize - 1 || x == 0 || x == borderedSize - 1;
                if (isBorderVertex)
                {
                    vertexIndexMap[x, y] = borderedVertexIndex;
                    borderedVertexIndex--;
                }
                else
                {
                    vertexIndexMap[x, y] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }

        for (int y = 0; y < borderedSize; y += meshSimplification)
        {
            for (int x = 0; x < borderedSize; x += meshSimplification)
            {
                int vertexIndex = vertexIndexMap[x, y];
                Vector3 percent = new Vector2((x - meshSimplification) / (float)meshSize, (y - meshSimplification) / (float)meshSize);
                float height = heightCurve.Evaluate(heightMap[x, y]) * heightMultiplier;
                Vector3 vertexPosition = new Vector3(topLeftX + percent.x * meshSizeUnsimplified, height, topLeftZ - percent.y * meshSizeUnsimplified);

                meshData.AddVertex(vertexPosition, percent, vertexIndex);
                if (x < borderedSize - 1 && y < borderedSize - 1)
                {
                    int a = vertexIndexMap[x, y];
                    int b = vertexIndexMap[x + meshSimplification, y];
                    int c = vertexIndexMap[x, y + meshSimplification];
                    int d = vertexIndexMap[x + meshSimplification, y + meshSimplification];

                    meshData.AddTriangle(a, d, c);
                    meshData.AddTriangle(d, a, b);
                }

                vertexIndex++;

            }
        }
        return meshData;
    }
}

public class MeshData
{
    Vector3[] verticles;
    int[] triangles;

    public Vector2[] uvs;

    int trinagleIndex;
    int borderTrianglesIndex;

    Vector3[] borderVericles;
    int[] borderTriangles;
    public MeshData(int verticlesPerLine)
    {
        verticles = new Vector3[verticlesPerLine * verticlesPerLine];
        uvs = new Vector2[verticlesPerLine * verticlesPerLine];
        triangles = new int[(verticlesPerLine - 1) * (verticlesPerLine - 1) * 6];

        borderVericles = new Vector3[verticlesPerLine * 4 + 4];
        borderTriangles = new int[24 * verticlesPerLine];
    }

    public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex)
    {
        if (vertexIndex < 0)
        {
            borderVericles[-vertexIndex - 1] = vertexPosition;
        }
        else
        {
            verticles[vertexIndex] = vertexPosition;
            uvs[vertexIndex] = uv;
        }
    }
    public void AddTriangle(int a, int b, int c)
    {
        if (a < 0 || b < 0 || c < 0)
        {
            borderTriangles[borderTrianglesIndex] = a;
            borderTriangles[borderTrianglesIndex + 1] = b;
            borderTriangles[borderTrianglesIndex + 2] = c;
            borderTrianglesIndex += 3;
        }
        else
        {
            triangles[trinagleIndex] = a;
            triangles[trinagleIndex + 1] = b;
            triangles[trinagleIndex + 2] = c;
            trinagleIndex += 3;
        }
    }

    Vector3[] CalculateNormals()
    {
        Vector3[] normals = new Vector3[verticles.Length];
        int triangleCount = triangles.Length / 3;
        for (int i = 0; i < triangleCount; i++)
        {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = triangles[normalTriangleIndex];
            int vertexIndexB = triangles[normalTriangleIndex + 1];
            int vertexIndexC = triangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);

            normals[vertexIndexA] += triangleNormal;
            normals[vertexIndexB] += triangleNormal;
            normals[vertexIndexC] += triangleNormal;
        }

        int borderTriangleCount = borderTriangles.Length / 3;
        for (int i = 0; i < borderTriangleCount; i++)
        {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = borderTriangles[normalTriangleIndex];
            int vertexIndexB = borderTriangles[normalTriangleIndex + 1];
            int vertexIndexC = borderTriangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);

            if (vertexIndexA >= 0)
            {
                normals[vertexIndexA] += triangleNormal;
            }
            if (vertexIndexB >= 0)
            {
                normals[vertexIndexB] += triangleNormal;
            }

            if (vertexIndexC >= 0)
            {
                normals[vertexIndexC] += triangleNormal;
            }

        }

        for (int i = 0; i < normals.Length; i++)
        {
            normals[i].Normalize();
        }

        return normals;
    }

    Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC)
    {
        Vector3 pointA = (indexA < 0) ? borderVericles[-indexA - 1] :verticles[indexA];
        Vector3 pointB = (indexB < 0) ? borderVericles[-indexB - 1] : verticles[indexB];
        Vector3 pointC = (indexC < 0) ? borderVericles[-indexC - 1] : verticles[indexC];

        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;

        return Vector3.Cross(sideAB, sideAC).normalized;
    }
    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = verticles;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.normals = CalculateNormals();
        //mesh.RecalculateNormals();
        return mesh;
    }
}
