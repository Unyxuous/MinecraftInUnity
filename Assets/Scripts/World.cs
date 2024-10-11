using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.Rendering;

public class World : MonoBehaviour
{
    public BiomeAttributes biome;

    public Transform player;
    public Vector3 spawnPosition;

    public Material material;
    public Material transparentMaterial;
    public BlockType[] blockTypes;

    Chunk[,] chunks = new Chunk[VoxelData.WORLD_SIZE_IN_CHUNKS, VoxelData.WORLD_SIZE_IN_CHUNKS];
    HashSet<ChunkCoord> activeChunks = new HashSet<ChunkCoord>();
    public ChunkCoord playerChunkCoord;
    ChunkCoord playerLastChunkCoord;

    List<ChunkCoord> chunksToCreate = new List<ChunkCoord>();
    private bool isCreatingChunks;

    public GameObject debugScreen;

    private void Start()
    {
        spawnPosition = new Vector3((VoxelData.WORLD_SIZE_IN_CHUNKS * VoxelData.CHUNK_WIDTH) / 2f, VoxelData.CHUNK_HEIGHT - 50f, (VoxelData.WORLD_SIZE_IN_CHUNKS * VoxelData.CHUNK_WIDTH) / 2f);
        GenerateWorld();
        playerLastChunkCoord = GetChunkCoordFromVector3(player.position);
    }

    private void Update()
    {
        playerChunkCoord = GetChunkCoordFromVector3(player.position);
        if (playerChunkCoord.x != playerLastChunkCoord.x || playerChunkCoord.z != playerLastChunkCoord.z)
        {
            playerLastChunkCoord = playerChunkCoord;
            CheckViewDistance();
        }

        if (chunksToCreate.Count > 0 && !isCreatingChunks) {
            StartCoroutine(CreateChunks());
        }

        if (Input.GetButtonDown("Debug"))
        {
            debugScreen.SetActive(!debugScreen.activeSelf);
        }
    }

    void GenerateWorld()
    {
        for (int x = (VoxelData.WORLD_SIZE_IN_CHUNKS / 2) - VoxelData.VIEW_DISTANCE_IN_CHUNKS; x < (VoxelData.WORLD_SIZE_IN_CHUNKS / 2) + VoxelData.VIEW_DISTANCE_IN_CHUNKS; x++)
        {
            for (int z = (VoxelData.WORLD_SIZE_IN_CHUNKS / 2) - VoxelData.VIEW_DISTANCE_IN_CHUNKS; z < (VoxelData.WORLD_SIZE_IN_CHUNKS / 2) + VoxelData.VIEW_DISTANCE_IN_CHUNKS; z++)
            {
                ChunkCoord newChunkCoord = new ChunkCoord(x, z);
                chunks[x, z] = new Chunk(newChunkCoord, this, true);
                activeChunks.Add(newChunkCoord);
            }
        }

        player.position = spawnPosition;
    }

    IEnumerator CreateChunks() {
        isCreatingChunks = true;

        while (chunksToCreate.Count > 0)
        {
            chunks[chunksToCreate[0].x, chunksToCreate[0].z].Init();
            chunksToCreate.RemoveAt(0);
            yield return null;
        }

        isCreatingChunks = false;
    }

    void CheckViewDistance() 
    {
        int chunkX = Mathf.FloorToInt(player.position.x / VoxelData.CHUNK_WIDTH);
        int chunkZ = Mathf.FloorToInt(player.position.z / VoxelData.CHUNK_WIDTH);

        List<ChunkCoord> previouslyActiveChunks = new List<ChunkCoord>(activeChunks);

        for (int x = chunkX - VoxelData.VIEW_DISTANCE_IN_CHUNKS; x < chunkX + VoxelData.VIEW_DISTANCE_IN_CHUNKS; x++) 
        {
            for (int z = chunkZ - VoxelData.VIEW_DISTANCE_IN_CHUNKS; z < chunkZ + VoxelData.VIEW_DISTANCE_IN_CHUNKS; z++)
            {
                ChunkCoord thisChunk = new ChunkCoord(x, z);
                if (IsChunkInWorld(thisChunk)) 
                {
                    if (chunks[x, z] == null)
                    {
                        chunks[x, z] = new Chunk(thisChunk, this, false);
                        chunksToCreate.Add(thisChunk);
                    }
                    else if (!chunks[x, z].isActive)
                    {
                        chunks[x, z].isActive = true;
                    }
                    activeChunks.Add(thisChunk);
                }

                if (previouslyActiveChunks.Contains(thisChunk)) 
                {
                    previouslyActiveChunks.Remove(thisChunk);
                }
            }
        }

        foreach (ChunkCoord c in previouslyActiveChunks)
        {
            chunks[c.x, c.z].isActive = false;
            activeChunks.Remove(c);
        }
    }

    public bool CheckForVoxel(Vector3 pos) {
        ChunkCoord thisChunk = new ChunkCoord(pos);

        if (!IsVoxelInWorld(pos))
        {
            return false;
        }

        if (chunks[thisChunk.x, thisChunk.z] != null && chunks[thisChunk.x, thisChunk.z].isVoxelMapPopulated)
        {
            return blockTypes[chunks[thisChunk.x, thisChunk.z].GetVoxelFromGlobalVector3(pos)].isSolid;
        }

        return blockTypes[GetVoxel(pos)].isSolid;
    }

    public bool CheckIfVoxelTransparent(Vector3 pos)
    {
        ChunkCoord thisChunk = new ChunkCoord(pos);

        if (!IsVoxelInWorld(pos))
        {
            return false;
        }

        if (chunks[thisChunk.x, thisChunk.z] != null && chunks[thisChunk.x, thisChunk.z].isVoxelMapPopulated)
        {
            return blockTypes[chunks[thisChunk.x, thisChunk.z].GetVoxelFromGlobalVector3(pos)].isTransparent;
        }

        return blockTypes[GetVoxel(pos)].isTransparent;
    }

    ChunkCoord GetChunkCoordFromVector3(Vector3 pos) 
    {
        int x = Mathf.FloorToInt(pos.x / VoxelData.CHUNK_WIDTH);
        int z = Mathf.FloorToInt(pos.z / VoxelData.CHUNK_WIDTH);

        return new ChunkCoord(x, z);
    }

    public Chunk GetChunkFromVector3(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / VoxelData.CHUNK_WIDTH);
        int z = Mathf.FloorToInt(pos.z / VoxelData.CHUNK_WIDTH);

        return chunks[x, z];
    }

    public byte GetVoxel(Vector3 pos) 
    {
        int yPos = Mathf.FloorToInt(pos.y);

        //IMMUTABLE PASS
        //outside of world is air
        if (!IsVoxelInWorld(pos)) 
        {
            return 0;
        }
        //bedrock layer
        if (yPos == 0) 
        {
            return 1;
        }

        //basic terrain pass
        int terrainHeight = Mathf.FloorToInt(biome.terrainHeight * TerrainGeneration.Get2DNoise(new Vector2(pos.x, pos.z), 0, biome.terrainScale)) + biome.solidGroundHeight;
        byte voxelValue = 0;
        
        if (yPos == terrainHeight)
        {
            voxelValue = 2;
        }
        else if (yPos < terrainHeight && yPos > terrainHeight - biome.surfaceBlockDepth)
        {
            voxelValue = biome.groundBlock;
        }
        else if (yPos > terrainHeight)
        {
            return 0;
        }
        else
        {
            voxelValue = 8;
        }

        //Underground pass for ores/dirt patches/etc.
        if (voxelValue == 8) 
        {
            foreach (Ore ore in biome.ores) 
            {
                if (yPos > ore.minHeight && yPos < ore.maxHeight)
                {
                    if (TerrainGeneration.Get3DNoise(pos, ore.noiseOffset, ore.scale, ore.threshold))
                    {
                        voxelValue = ore.blockID;
                    }
                }
            }
        }

        return voxelValue;
    }

    bool IsChunkInWorld(ChunkCoord coord) 
    {
        return (coord.x > 0 && coord.x < VoxelData.WORLD_SIZE_IN_CHUNKS - 1 &&
            coord.z > 0 && coord.z < VoxelData.WORLD_SIZE_IN_CHUNKS - 1);
    }

    bool IsVoxelInWorld(Vector3 pos) 
    {
        return (pos.x >= 0 && pos.x < VoxelData.WORLD_SIZE_IN_VOXELS &&
            pos.y >= 0 && pos.y < VoxelData.CHUNK_HEIGHT &&
            pos.z >= 0 && pos.z < VoxelData.WORLD_SIZE_IN_VOXELS);
    }
}

[System.Serializable]
public class BlockType 
{
    public string blockName;
    public bool isSolid;
    public bool isTransparent;
    public Sprite icon;

    [Header("Texture Values")]
    public int backFaceTexture;
    public int frontFaceTexture;
    public int topFaceTexture;
    public int bottomFaceTexture;
    public int leftFaceTexture;
    public int rightFaceTexture;

    //Back, Front, Top, Bottom, Left, Right (same as in VoxelData)
    public int GetTextureID(int faceIndex) 
    {
        switch (faceIndex) 
        {
            case 0:
                return backFaceTexture;
            case 1:
                return frontFaceTexture;
            case 2:
                return topFaceTexture;
            case 3:
                return bottomFaceTexture;
            case 4:
                return leftFaceTexture;
            case 5:
                return rightFaceTexture;
            default:
                Debug.Log("Error in GetTextureID, invalid face index");
                return 0;
        }
    }
}