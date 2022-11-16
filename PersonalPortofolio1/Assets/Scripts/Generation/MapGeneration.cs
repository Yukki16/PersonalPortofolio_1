using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public class MapGeneration : MonoBehaviour
{

    public enum DrawMode { NoiseMap, ColorMap, Mesh, FalloffMap };

    public DrawMode drawMode;

    public NoiseMap.NormalizeMode normalizeMode;

    public const int mapChunkSize = 239;
    [Range(0,6)]
    public int editorLOD;

    public int noiseSeed;
    public float noiseScale;

    public AnimationCurve meshHeightCurve;
    public float meshHeightMultiplier;

    [Range (0,25)]
    public int ocataves;
    [Range(0,1)]
    public float persistance;
    public float lacunarity;

    public Vector2 offset;

    public bool autoUpdate;
    public bool useFallout;

    public TerrainType[] terrainRegions;

    float[,] falloffMap;

    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQ = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQ = new Queue<MapThreadInfo<MeshData>>();

    private void Awake()
    {
        falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize);
    }
    MapData GenerateMapData(Vector2 center)
    {
        float[,] noiseMap = NoiseMap.GenerateNoiseMap(mapChunkSize + 2, mapChunkSize + 2, noiseScale, noiseSeed, ocataves, persistance, lacunarity, center + offset, normalizeMode);

        Color[] colorMap = new Color[mapChunkSize * mapChunkSize];
        for (int y = 0; y < mapChunkSize; y++)
        {
            for (int x = 0; x < mapChunkSize; x++)
            {
                if(useFallout)
                {
                    noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - falloffMap[x,y]);
                }
                float currentHeight = noiseMap[x, y];
                for (int i = 0; i < terrainRegions.Length; i++)
                {
                    if (currentHeight >= terrainRegions[i].height)
                    {
                        colorMap[y * mapChunkSize + x] = terrainRegions[i].color;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
        //DrawMapInEditor(noiseMap, colorMap);
        return new MapData(noiseMap, colorMap);
    }

    public void DrawMapInEditor()
    {
        MapData mapData = GenerateMapData(Vector2.zero);
        MapDisplay display = FindObjectOfType<MapDisplay>();
        if (drawMode == DrawMode.NoiseMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
        }
        else if (drawMode == DrawMode.ColorMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
        }
        else if (drawMode == DrawMode.Mesh)
        {
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, editorLOD), TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
        }
        else if(drawMode == DrawMode.FalloffMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(mapChunkSize)));
        }
    }

    public void RequestMapData(Vector2 center, Action<MapData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MapDataThread(center, callback);
        };

        new Thread(threadStart).Start();
    }

    void MapDataThread(Vector2 center, Action<MapData> callback)
    {
        MapData mapData = GenerateMapData(center);
        lock (mapDataThreadInfoQ)
        {
            mapDataThreadInfoQ.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MeshDataThread(mapData, lod, callback);
        };

        new Thread(threadStart).Start();
    }

    void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, lod);
        lock (meshDataThreadInfoQ)
        {
            meshDataThreadInfoQ.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    private void Update()
    {
        if(mapDataThreadInfoQ.Count > 0)
        {
            for (int i = 0; i < mapDataThreadInfoQ.Count; i++)
            {
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQ.Dequeue();
                threadInfo.callBack(threadInfo.parameter);
            }
        }

        if (meshDataThreadInfoQ.Count > 0)
        {
            for (int i = 0; i < meshDataThreadInfoQ.Count; i++)
            {
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQ.Dequeue();
                threadInfo.callBack(threadInfo.parameter);
            }
        }
    }

    private void OnValidate()
    {
        if(lacunarity < 1)
        {
            lacunarity = 1;
        }

        if(ocataves < 0)
        {
            ocataves = 0;
        }

        falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize);
    }

    struct MapThreadInfo<T>
    {
        public readonly Action<T> callBack;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callBack, T parameter)
        {
            this.callBack = callBack;
            this.parameter = parameter;
        }
    }

    
}

[System.Serializable]
public struct TerrainType
{
    public string name;
    public float height;
    public Color color;
}

public struct MapData
{
    public readonly float[,] heightMap;
    public readonly Color[] colorMap;

    public MapData(float[,] heightMap, Color[] colorMap)
    {
        this.heightMap = heightMap;
        this.colorMap = colorMap;
    }
}
