using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public static class TerrainGeneration
{
    //Variables for heightmap noise
    public static int heightMapSeed = 0;
    static readonly FastNoiseLite heightMapNoise;
    static readonly float heightXOrg = 0.0f;
    static readonly float heightYOrg = 0.0f;
    static readonly float heightZOrg = 0.0f;
    static readonly float heightFrequency = 1.0f;
    static float heightWaveLength;

    static TerrainGeneration()
    {
        heightMapNoise = new FastNoiseLite(heightMapSeed);
    }

    public static float Get2DNoise(Vector2Int pos, float offset, float scale)
    {
        heightMapNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2S);
        heightWaveLength = (float)VoxelData.WORLD_SIZE_IN_VOXELS / heightFrequency;

        float xCoord = (heightXOrg + pos.x + offset) / scale;
        float zCoord = (heightZOrg + pos.y + offset) / scale;

        return ((heightMapNoise.GetNoise(xCoord, zCoord) / 2.0f) + 0.5f);
    }

    public static bool Get3DNoise(Vector3Int pos, float offset, float scale, float threshold) { 
        heightMapNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2S);

        float xCoord = (heightXOrg + pos.x + offset) / scale;
        float yCoord = (heightYOrg + pos.y + offset) / scale;
        float zCoord = (heightZOrg + pos.z + offset) / scale;

        return ((heightMapNoise.GetNoise(xCoord, yCoord, zCoord) / 2.0f) + 0.5f) > threshold;
    }
}
