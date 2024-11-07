using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ChunkData
{
    int x;
    int z;

    [HideInInspector]
    public VoxelState[,,] map = new VoxelState[VoxelData.CHUNK_WIDTH, VoxelData.CHUNK_HEIGHT, VoxelData.CHUNK_WIDTH];


    [System.NonSerialized]
    public Chunk chunk;
    Queue<VoxelState> lightToPropogate = new Queue<VoxelState>();
    public void AddLightForPropogation(VoxelState voxel)
    {
        lightToPropogate.Enqueue(voxel);
    }


    public Vector2Int position 
    {
        get 
        {
            return new Vector2Int(x, z);
        }

        set 
        {
            x = value.x;
            z = value.y;
        }
    }

    public ChunkData(Vector2Int pos)
    {
        position = pos;
    }

    public ChunkData(int _x, int _z)
    {
        x = _x;
        z = _z;
    }

    public void Populate()
    {
        for (int y = 0; y < VoxelData.CHUNK_HEIGHT; y++)
        {
            for (int x = 0; x < VoxelData.CHUNK_WIDTH; x++)
            {
                for (int z = 0; z < VoxelData.CHUNK_WIDTH; z++)
                {
                    Vector3Int voxelGlobalPos = new Vector3Int(x + position.x, y, z + position.y);

                    map[x, y, z] = new VoxelState(World.Instance.GetVoxel(voxelGlobalPos), this);

                    for (int i = 0; i < 6; i++)
                    {
                        Vector3Int neighborV3 = new Vector3Int(x, y, z) + VoxelData.faceChecks[i];
                        if (IsVoxelInChunk(neighborV3))
                        {
                            map[x, y, z].neighbors[i] = VoxelFromV3Int(neighborV3);
                        }
                        else
                        { 
                            map[x, y, z].neighbors[i] = World.Instance.worldData.GetVoxel(voxelGlobalPos + VoxelData.faceChecks[i]);
                        }
                    }
                }
            }
        }

        Lighting.RecalculateNaturalLight(this);
        World.Instance.worldData.AddToModifiedChunks(this);
    }

    public bool IsVoxelInChunk(int x, int y, int z)
    {
        if (x < 0 || x > VoxelData.CHUNK_WIDTH - 1 ||
            y < 0 || y > VoxelData.CHUNK_HEIGHT - 1 ||
            z < 0 || z > VoxelData.CHUNK_WIDTH - 1)
        {
            return false;
        }
        return true;
    }

    public bool IsVoxelInChunk(Vector3Int pos)
    {
        return IsVoxelInChunk(pos.x, pos.y, pos.z);
    }

    public VoxelState VoxelFromV3Int(Vector3Int pos)
    {
        return map[pos.x, pos.y, pos.z];
    }
}
