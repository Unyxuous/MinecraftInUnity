using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BiomeAttributes", menuName = "Terrain/Biome Attribute")]
public class BiomeAttributes : ScriptableObject
{
    public string biomeName;

    //from here down is solid blocks
    public int solidGroundHeight;
    //max height after noise is applied (added to solidGroundHeight)
    public int terrainHeight;
    public int surfaceBlockDepth;

    public float terrainScale;

    public byte groundBlock;

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