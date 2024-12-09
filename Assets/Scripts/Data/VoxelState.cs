using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class VoxelState
{
    public byte id;
    public int orientation;

    [System.NonSerialized]
    public VoxelNeighbors neighbors;
    [System.NonSerialized]
    public ChunkData chunkData;
    [System.NonSerialized]
    public Vector3Int position;
    public BlockType properties
    {
        get
        {
            return World.Instance.blockTypes[id];
        }
    }

    public Vector3Int globalPosition
    {
        get
        {
            return new Vector3Int(position.x + chunkData.position.x, position.y, position.z + chunkData.position.y);
        }
    }

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
                byte oldLightValue = _light;
                byte oldCastValue = castLight;

                _light = value;

                if (_light < oldLightValue)
                {
                    List<int> neighborsToDarken = new List<int>();

                    for (int face = 0; face < 6; face++)
                    {
                        if (neighbors[face] != null)
                        {
                            if (neighbors[face].light <= oldCastValue)
                            {
                                neighborsToDarken.Add(face);
                            }
                            else
                            {
                                neighbors[face].PropogateLight();
                            }
                        }
                    }

                    foreach (int neighbor in neighborsToDarken)
                    {
                        neighbors[neighbor].light = 0;
                    }

                    if (chunkData.chunk != null)
                    {
                        World.Instance.AddChunkToUpdate(chunkData.chunk);
                    }
                }
                else if (_light > 1)
                {
                    PropogateLight();
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

    public VoxelState(byte _id, ChunkData _chunkData, Vector3Int _position)
    {
        id = _id;
        orientation = 1;
        chunkData = _chunkData;
        position = _position;
        neighbors = new VoxelNeighbors(this);
        light = 0;
    }

    public void PropogateLight()
    {
        if (light < 2)
        {
            return;
        }

        for (int face = 0; face < 6; face++)
        {
            if (neighbors[face] != null)
            {
                if (neighbors[face].light < castLight)
                {
                    neighbors[face].light = castLight;
                }
            }

            if (chunkData.chunk != null)
            {
                World.Instance.AddChunkToUpdate(chunkData.chunk);
            }
        }
    }
}


public class VoxelNeighbors
{
    public readonly VoxelState parent;
    private VoxelState[] _neighbors = new VoxelState[6];

    public int Length
    {
        get
        {
            return _neighbors.Length;
        }
    }
    public VoxelState this[int index]
    {
        get
        {
            if (_neighbors == null)
            {
                _neighbors[index] = World.Instance.worldData.GetVoxel(parent.globalPosition + VoxelData.faceChecks[index]);
                ReturnNeighbor(index);
            }
            return _neighbors[index];
        }

        set
        {
            _neighbors[index] = value;
            ReturnNeighbor(index);
        }
    }

    public VoxelNeighbors(VoxelState _parent)
    {
        parent = _parent;
    }

    void ReturnNeighbor(int index)
    {
        if (_neighbors[index] == null)
        {
            return;
        }

        //prevents infinite recursion by not letting blocks be processed twice
        if (_neighbors[index].neighbors[VoxelData.revFaceCheckIndex[index]] != parent)
        {
            _neighbors[index].neighbors[VoxelData.revFaceCheckIndex[index]] = parent;
        }
    }
}