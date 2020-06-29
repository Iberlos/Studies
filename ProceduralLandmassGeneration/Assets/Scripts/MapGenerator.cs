using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode { NoiseMap, ColorMap, Mesh}

    public DrawMode drawMode;

    public Noise.NormalizeMode normalizeMode;

    public const int chunkSize = 241;
    public float maxHeight;
    public float noiseScale;

    [Range(0,6)]
    public int EditorLevelOfDetail;

    [Range(1,10)]
    public int octaves;
    [Range(0.00001f,1.0f)]
    public float persistance;
    [Range(-28.9f, 28.9f)]
    public float lacunarity;

    public int seed;
    public Vector2 offset;

    public TerrainType[] regions;
    public AnimationCurve regionMultiplierCurve;

    public bool autoUpdate;

    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    public void DrawMapInEditor()
    {
        MapDisplay display = FindObjectOfType<MapDisplay>();

        MapData mapData = GenerateMapData(Vector2.zero);
        if (drawMode == DrawMode.NoiseMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
        }
        else if (drawMode == DrawMode.ColorMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromColorMap(mapData.colorMap, chunkSize, chunkSize));
        }
        else if (drawMode == DrawMode.Mesh)
        {
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, maxHeight, regionMultiplierCurve, EditorLevelOfDetail), TextureGenerator.TextureFromColorMap(mapData.colorMap, chunkSize, chunkSize));
        }
    }

    public void RequestMapData(Action<MapData> a_callBack, Vector2 a_center)
    {
        ThreadStart threadStart = delegate
        {
            MapDataThread(a_callBack, a_center);
        };

        new Thread(threadStart).Start();
    }

    void MapDataThread(Action<MapData> a_callBack, Vector2 a_center)
    {
        MapData mapData = GenerateMapData( a_center);
        lock (mapDataThreadInfoQueue)
        {
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(a_callBack, mapData));
        }
    }

    public void RequestMeshData(MapData a_mapData, int a_lod, Action<MeshData> a_callBack)
    {
        ThreadStart threadStart = delegate
        {
            MeshDataThread(a_mapData, a_lod, a_callBack);
        };

        new Thread(threadStart).Start();
    }

    public void MeshDataThread(MapData a_mapData, int a_lod, Action<MeshData> a_callBack)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(a_mapData.heightMap, maxHeight, regionMultiplierCurve, a_lod);
        lock(meshDataThreadInfoQueue)
        {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(a_callBack, meshData));
        }
    }

    private void Update()
    {
        if(mapDataThreadInfoQueue.Count > 0)
        {
            for(int i =0; i<mapDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
                threadInfo.callBack(threadInfo.parameter);
            }
        }

        if (meshDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < meshDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callBack(threadInfo.parameter);
            }
        }
    }

    MapData GenerateMapData(Vector2 a_center)
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(chunkSize, chunkSize, seed, noiseScale, octaves, persistance, lacunarity, a_center + offset, normalizeMode);
        Color[] colorMap = new Color[chunkSize * chunkSize];
        for(int x = 0; x < chunkSize; x++)
        {
            for(int y = 0; y <chunkSize; y++)
            {
                float currentHeight = noiseMap[x, y];
                for(int i = 0; i<regions.Length; i++)
                {
                    if(currentHeight <= regions[i].height)
                    {
                        colorMap[y * chunkSize + x] = regions[i].color;
                        break;
                    }
                }
            }
        }

        return new MapData(noiseMap, colorMap);
    }

    private void OnValidate()
    {
        if (octaves < 1)
        {
            octaves = 1;
        }
    }

    struct MapThreadInfo<T>
    {
        public readonly Action<T> callBack;
        public readonly T parameter;

        public MapThreadInfo (Action<T> a_callBack, T a_parameter)
        {
            callBack = a_callBack;
            parameter = a_parameter;
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

    public MapData(float[,] a_heightMap, Color[] a_colorMap)
    {
        heightMap = a_heightMap;
        colorMap = a_colorMap;
    }
}