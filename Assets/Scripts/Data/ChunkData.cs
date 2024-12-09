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

                    map[x, y, z] = new VoxelState(World.Instance.GetVoxel(voxelGlobalPos), this, new Vector3Int(x, y, z));

                    for (int face = 0; face < 6; face++)
                    {
                        Vector3Int neighborV3 = new Vector3Int(x, y, z) + VoxelData.faceChecks[face];
                        if (IsVoxelInChunk(neighborV3))
                        {
                            map[x, y, z].neighbors[face] = VoxelFromV3Int(neighborV3);
                        }
                        else
                        { 
                            map[x, y, z].neighbors[face] = World.Instance.worldData.GetVoxel(voxelGlobalPos + VoxelData.faceChecks[face]);
                        }
                    }
                }
            }
        }

        Lighting.RecalculateNaturalLight(this);
        World.Instance.worldData.AddToModifiedChunks(this);
    }

    public void ModifyVoxel(Vector3Int pos, byte _id, int _orientation)
    {
        if (map[pos.x, pos.y, pos.z].id == _id)
        {
            return;
        }

        VoxelState voxel = map[pos.x, pos.y, pos.z];
        BlockType newVoxel = World.Instance.blockTypes[_id];
        byte oldOpacity = voxel.properties.opacity;
        voxel.id = _id;
        voxel.orientation = _orientation;

        if ((pos.y == VoxelData.CHUNK_HEIGHT || 
            map[pos.x, pos.y + 1, pos.z].light == 15) && 
            voxel.properties.opacity != oldOpacity)
        {
            Lighting.CastNaturalLight(this, pos.x, pos.z, pos.y + 1);
        }

        if (voxel.properties.isActive && BlockBehavior.Active(voxel))
        {
            voxel.chunkData.chunk.AddActiveVoxel(voxel);
        }

        for (int i = 0; i < 6; i++)
        {
            if (voxel.neighbors[i] != null)
            {
                if (voxel.neighbors[i].properties.isActive && BlockBehavior.Active(voxel.neighbors[i]))
                {
                    voxel.neighbors[i].chunkData.chunk.AddActiveVoxel(voxel.neighbors[i]);
                }
            }
        }

        World.Instance.worldData.AddToModifiedChunks(this);

        if (chunk != null)
        {
            World.Instance.AddChunkToUpdate(chunk);
        }
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
