using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WorldData
{
    public string worldName = "Prototype";
    public int seed;

    [System.NonSerialized]
    public Dictionary<Vector2Int, ChunkData> chunks = new Dictionary<Vector2Int, ChunkData>();
    [System.NonSerialized]
    public List<ChunkData> modifiedChunks = new List<ChunkData>();

    public WorldData(string _worldName, int _seed)
    {
        worldName = _worldName;
        seed = _seed;
    }

    public WorldData(WorldData savedWorld)
    {
        worldName = savedWorld.worldName;
        seed = savedWorld.seed;
    }

    public void AddToModifiedChunks(ChunkData chunk)
    {
        if (!modifiedChunks.Contains(chunk))
        {
            modifiedChunks.Add(chunk);
        }
    }

    public ChunkData RequestChunk(Vector2Int coord, bool create)
    {
        ChunkData c;

        lock (World.Instance.ChunkListThreadLock)
        {
            if (chunks.ContainsKey(coord))
            {
                c = chunks[coord];
            }
            else if (!create)
            {
                c = null;
            }
            else
            {
                LoadChunk(coord);
                c = chunks[coord];
            }
        }

        return c;
    }

    public void LoadChunk(Vector2Int coord)
    {
        //chunk already loaded
        if (chunks.ContainsKey(coord))
        {
            return;
        }

        //chunk not loaded, but is saved to system
        ChunkData chunk = SaveSystem.LoadChunk(worldName, coord);
        if (chunk != null)
        {
            chunks.Add(coord, chunk);
            return;
        }

        //chunk doesn't exist, create new one
        chunks.Add(coord, new ChunkData(coord));
        chunks[coord].Populate();
    }

    public void UnloadChunk(Vector2Int coord)
    {
        if (chunks.ContainsKey(coord))
        {
            chunks.Remove(coord);
            return;
        }
    }

    bool IsVoxelInWorld(Vector3Int pos)
    {
        return (pos.x >= 0 && pos.x < VoxelData.WORLD_SIZE_IN_VOXELS &&
            pos.y >= 0 && pos.y < VoxelData.CHUNK_HEIGHT &&
            pos.z >= 0 && pos.z < VoxelData.WORLD_SIZE_IN_VOXELS);
    }

    public void SetVoxel(Vector3Int pos, byte val)
    {
        if (!IsVoxelInWorld(pos))
        {
            return;
        }

        int x = (pos.x / VoxelData.CHUNK_WIDTH) * VoxelData.CHUNK_WIDTH;
        int z = (pos.z / VoxelData.CHUNK_WIDTH) * VoxelData.CHUNK_WIDTH;

        ChunkData chunk = RequestChunk(new Vector2Int(x, z), true);

        Vector3Int voxel = new Vector3Int(pos.x - x, pos.y, pos.z - z);

        chunk.map[voxel.x, voxel.y, voxel.z].id = val;

        AddToModifiedChunks(chunk);
    }

    public VoxelState GetVoxel(Vector3Int pos)
    {
        if (!IsVoxelInWorld(pos))
        {
            return null;
        }

        int x = (pos.x / VoxelData.CHUNK_WIDTH) * VoxelData.CHUNK_WIDTH;
        int z = (pos.z / VoxelData.CHUNK_WIDTH) * VoxelData.CHUNK_WIDTH;

        ChunkData chunk = RequestChunk(new Vector2Int(x, z), true);

        Vector3Int voxel = new Vector3Int(pos.x - x, pos.y, pos.z - z);

        return chunk.map[voxel.x, voxel.y, voxel.z];
    }
}
