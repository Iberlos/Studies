using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{
    public enum NormalizeMode { LOCAL, GLOBAL};

    public static float[,] GenerateNoiseMap(int a_mapWidth, int a_mapHeigt, int seed, float a_scale, int a_octaves, float a_persistance, float a_lacunarity, Vector2 a_offset, NormalizeMode a_normalizeMode)
    {
        float[,] noiseMap = new float[a_mapWidth, a_mapHeigt];

        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[a_octaves];

        float maxPossibleHeight = 0;
        float amplitude = 1;
        float frequency = 1;

        for (int i = 0; i < a_octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000);
            float offsetY = prng.Next(-100000, 100000);
            octaveOffsets[i] = new Vector2(offsetX,offsetY);

            maxPossibleHeight += amplitude;
            amplitude *= a_persistance;
        }

        float minLocalNoiseHeight = float.MaxValue;
        float maxLocalNoiseHeight = float.MinValue;

        float halfWidth = a_mapWidth / 2f;
        float halfHeight = a_mapHeigt / 2f;

        for (int x = 0; x < a_mapWidth; x++)
        {
            for (int y = 0; y < a_mapHeigt; y++)
            {
                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i <a_octaves; i++)
                {
                    amplitude *= a_persistance;
                    frequency *= a_lacunarity;

                    float sampleX = (x - halfWidth + octaveOffsets[i].x - a_offset.x) * a_scale * frequency ;
                    float sampleY = (y - halfHeight + octaveOffsets[i].y - a_offset.y) * a_scale * frequency ;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;


                }

                if(noiseHeight > maxLocalNoiseHeight)
                {
                    maxLocalNoiseHeight = noiseHeight;
                }else if(noiseHeight < minLocalNoiseHeight)
                {
                    minLocalNoiseHeight = noiseHeight;
                }

                noiseMap[x, y] = noiseHeight;
            }
        }

        for (int x = 0; x < a_mapWidth; x++)
        {
            for (int y = 0; y < a_mapHeigt; y++)
            {
                if(a_normalizeMode == NormalizeMode.LOCAL)
                {
                    noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
                }
                else
                {
                    float normalizedHeight = (noiseMap[x, y] + 1) / (2f * maxPossibleHeight / 3.2f);
                    noiseMap[x, y] = normalizedHeight;
                }
            }
        }

        return noiseMap;
    }
}
