using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class VoxelState
{
    public byte id;

    [System.NonSerialized]
    public VoxelState[] neighbors = new VoxelState[6];
    [System.NonSerialized]
    public ChunkData chunkData;

    [System.NonSerialized]
    private byte _light;
    public byte light
    {
        get
        {
            return _light;
        }
        set
        {
            if (value != _light)
            {
                _light = value;

                if (_light > 1)
                {
                    chunkData.AddLightForPropogation(this);
                }
            }
        }
    }

    public float lightAsFloat
    {
        get
        {
            return (float)light * VoxelData.unitOfLight;
        }
    }

    public VoxelState(byte _id, ChunkData _chunkData)
    {
        id = _id;
        chunkData = _chunkData;
        light = 0;
    }

    public byte castLight 
    {
        get
        {
            int lightLevel = _light - properties.opacity - 1;

            if (lightLevel < 0)
            {
                lightLevel = 0;
            }

            return (byte)lightLevel;
        }
    }

    public BlockType properties
    {
        get
        {
            return World.Instance.blockTypes[id];
        }
    }
}
