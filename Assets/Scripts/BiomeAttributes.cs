using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BiomeAttributes", menuName = "Terrain/Biome Attribute")]
public class BiomeAttributes : ScriptableObject
{
    [Header("Biome Settings")]
    public string biomeName;
    public float offset;
    public float scale;


    //max height after noise is applied (added to solidGroundHeight)
    public int terrainHeight;
    public int surfaceBlockDepth;

    public float terrainScale;

    public byte surfaceBlock;
    public byte subSurfaceBlock;

    [Header("Major Flora")]
    public int majorFloraIndex;
    public float majorFloraZoneOffset = 0f;
    public float majorFloraZoneScale = 0.4f;
    [Range(0.1f, 1f)]
    public float majorFloraZoneThreshold = 0.6f;
    public float majorFloraPlacementOffset = 0f;
    public float majorFloraPlacementScale = 0.005f;
    [Range(0.1f, 1f)]
    public float majorFloraPlacementThreshold = 0.9f;
    public int maxMajorFloraHeight = 12;
    public int minMajorFloraHeight = 5;
    public bool placeMajorFlora = true;

    public Ore[] ores;
}


[System.Serializable]
public class Ore {
    public string name;
    public byte blockID;

    public int minHeight;
    public int maxHeight;

    public float scale;
    public float threshold;
    public float noiseOffset;
}