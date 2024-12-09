using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;
using Color = UnityEngine.Color;

public class World : MonoBehaviour
{
    public static World _instance;
    public static World Instance 
    {
        get 
        {
            return _instance;
        }
    }

    public WorldData worldData;

    public Settings settings;

    [Header("World Generation Values")]
    public BiomeAttributes[] biomes;

    [Range(0f, 1f)]
    public float globalLightLevel;
    public Color day;
    public Color night;

    [Header("Player")]
    public Player player;
    public Vector3Int spawnPosition;

    [Header("Materials")]
    public Material material;
    public Material transparentMaterial;
    public Material waterMaterial;
    public BlockType[] blockTypes;

    Chunk[,] chunks = new Chunk[VoxelData.WORLD_SIZE_IN_CHUNKS, VoxelData.WORLD_SIZE_IN_CHUNKS];
    HashSet<ChunkCoord> activeChunks = new HashSet<ChunkCoord>();
    HashSet<ChunkCoord> loadedChunks = new HashSet<ChunkCoord>();
    public ChunkCoord playerChunkCoord;
    ChunkCoord playerLastChunkCoord;

    private List<Chunk> chunksToUpdate = new List<Chunk>();
    bool applyingModifications = false;

    public Queue<Chunk> chunksToDraw = new Queue<Chunk>();
    Queue<Queue<VoxelMod>> modifications = new Queue<Queue<VoxelMod>>();

    private bool _inUI = false;

    public Clouds clouds;

    public GameObject debugScreen;
    public GameObject creativeInventory;
    public GameObject cursorSlot;

    Thread ChunkUpdateThread;
    public object ChunkUpdateThreadLock = new object();
    public object ChunkListThreadLock = new object();

    public string appPath;

    public void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }

        appPath = Application.persistentDataPath;
    }

    private void Start()
    {
        Debug.Log("seed: " + TerrainGeneration.heightMapSeed);

        worldData = SaveSystem.LoadWorld("Prototype");

        string jsonImport = File.ReadAllText(Application.dataPath + "/settings.cfg");
        settings = JsonUtility.FromJson<Settings>(jsonImport);

        Shader.SetGlobalFloat("minGlobalLightLevel", VoxelData.minLightLevel);
        Shader.SetGlobalFloat("maxGlobalLightLevel", VoxelData.maxLightLevel);

        LoadWorld();

        SetGlobalLightValue();

        spawnPosition = new Vector3Int(VoxelData.WORLD_CENTER, VoxelData.CHUNK_HEIGHT - 50, VoxelData.WORLD_CENTER);

        player.transform.position = spawnPosition;
        CheckLoadDistance();
        CheckViewDistance();

        playerLastChunkCoord = GetChunkCoordFromVector3(player.transform.position);

        if (settings.enableThreading)
        {
            ChunkUpdateThread = new Thread(new ThreadStart(ThreadedUpdate));
            ChunkUpdateThread.Start();
        }

        StartCoroutine(Tick());
    }

    private void Update()
    {
        playerChunkCoord = GetChunkCoordFromVector3(player.transform.position);

        if (playerChunkCoord.x != playerLastChunkCoord.x || playerChunkCoord.z != playerLastChunkCoord.z)
        {
            playerLastChunkCoord = playerChunkCoord;
            CheckLoadDistance();
            CheckViewDistance();
        }

        if (chunksToDraw.Count > 0)
        {
            chunksToDraw.Dequeue().CreateMesh();
        }

        if (!settings.enableThreading)
        {
            if (!applyingModifications)
            {
                ApplyModifications();
            }

            if (chunksToUpdate.Count > 0)
            {
                UpdateChunks();
            }
        }

        if (Input.GetButtonDown("Debug"))
        {
            debugScreen.SetActive(!debugScreen.activeSelf);
        }
    }

    private void OnDisable()
    {
        if (settings.enableThreading)
        {
            ChunkUpdateThread.Abort();
        }
    }

    IEnumerator Tick()
    {
        while (true)
        {
            foreach (ChunkCoord c in activeChunks)
            {
                chunks[c.x, c.z].TickUpdate();
            }

            yield return new WaitForSeconds(VoxelData.tickLength);
        }
    }

    public bool inUI
    {
        get
        {
            return _inUI;
        }

        set
        {
            _inUI = value;
            if (_inUI)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                creativeInventory.SetActive(true);
                cursorSlot.SetActive(true);
            }
            else 
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                creativeInventory.SetActive(false);
                cursorSlot.SetActive(false);
            }
        }
    }

    public void SetGlobalLightValue() {
        Shader.SetGlobalFloat("GlobalLightLevel", globalLightLevel);
        Camera.main.backgroundColor = Color.Lerp(night, day, globalLightLevel);
    }

    void LoadWorld()
    {
        for (int x = (VoxelData.WORLD_SIZE_IN_CHUNKS / 2) - settings.loadDistance; x < (VoxelData.WORLD_SIZE_IN_CHUNKS / 2) + settings.loadDistance; x++)
        {
            for (int z = (VoxelData.WORLD_SIZE_IN_CHUNKS / 2) - settings.loadDistance; z < (VoxelData.WORLD_SIZE_IN_CHUNKS / 2) + settings.loadDistance; z++)
            {
                Vector2Int coord = new Vector2Int(x, z);
                worldData.LoadChunk(coord);
            }
        }
    }

    public void AddChunkToUpdate(Chunk chunk)
    {
        AddChunkToUpdate(chunk, false);
    }

    public void AddChunkToUpdate(Chunk chunk, bool insert)
    {
        lock (ChunkUpdateThreadLock)
        {
            if (!chunksToUpdate.Contains(chunk))
            {
                if (insert)
                {
                    chunksToUpdate.Insert(0, chunk);
                }
                else
                {
                    chunksToUpdate.Add(chunk);
                }
            }
        }
    }

    void UpdateChunks() 
    {
        lock (ChunkUpdateThreadLock)
        {
            if (chunksToUpdate[0].isInitialized)
            {
                chunksToUpdate[0].UpdateChunk();

                if (!activeChunks.Contains(chunksToUpdate[0].coord))
                {
                    activeChunks.Add(chunksToUpdate[0].coord);
                }

                chunksToUpdate.RemoveAt(0);
            }
        }
    }

    void ThreadedUpdate() 
    {
        while (true)
        {
            if (!applyingModifications)
            {
                ApplyModifications();
            }

            if (chunksToUpdate.Count > 0)
            {
                UpdateChunks();
            }
        }
    }

    void ApplyModifications() 
    {
        applyingModifications = true;

        while (modifications.Count > 0)
        {
            Queue<VoxelMod> queue = modifications.Dequeue();

            while (queue.Count > 0)
            {
                VoxelMod mod = queue.Dequeue();

                worldData.SetVoxel(mod.pos, mod.id, mod.orientation);
            }
        }

        applyingModifications = false;
    }

    void CheckViewDistance() 
    {
        clouds.UpdateClouds();

        int chunkX = (int)player.transform.position.x / VoxelData.CHUNK_WIDTH;
        int chunkZ = (int)player.transform.position.z / VoxelData.CHUNK_WIDTH;

        List<ChunkCoord> previouslyActiveChunks = new List<ChunkCoord>(activeChunks);

        activeChunks.Clear();

        for (int x = chunkX - settings.viewDistance; x < chunkX + settings.viewDistance; x++) 
        {
            for (int z = chunkZ - settings.viewDistance; z < chunkZ + settings.viewDistance; z++)
            {
                ChunkCoord thisChunk = new ChunkCoord(x, z);
                if (IsChunkInWorld(thisChunk)) 
                {
                    if (!chunks[x, z].isActive)
                    {
                        chunks[x, z].isActive = true;
                        lock (ChunkUpdateThreadLock)
                        {
                            chunksToUpdate.Add(chunks[x, z]);
                        }
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
        }
    }

    void CheckLoadDistance()
    {
        int chunkX = (int)player.transform.position.x / VoxelData.CHUNK_WIDTH;
        int chunkZ = (int)player.transform.position.z / VoxelData.CHUNK_WIDTH;

        List<ChunkCoord> previouslyLoadedChunks = new List<ChunkCoord>(loadedChunks);

        loadedChunks.Clear();

        for (int x = chunkX - settings.loadDistance; x < chunkX + settings.loadDistance; x++)
        {
            for (int z = chunkZ - settings.loadDistance; z < chunkZ + settings.loadDistance; z++)
            {
                ChunkCoord thisChunk = new ChunkCoord(x, z);
                if (IsChunkInWorld(thisChunk))
                {
                    if (chunks[x, z] == null)
                    {
                        chunks[x, z] = new Chunk(thisChunk);

                    }
                    worldData.LoadChunk(new Vector2Int(x, z));
                    chunks[x, z].isLoaded = true;
                    loadedChunks.Add(thisChunk);
                }

                if (previouslyLoadedChunks.Contains(thisChunk))
                {
                    previouslyLoadedChunks.Remove(thisChunk);
                }
            }
        }

        foreach (ChunkCoord c in previouslyLoadedChunks)
        {
            worldData.UnloadChunk(new Vector2Int(c.x, c.z));
            chunks[c.x, c.z].isLoaded = false;
        }
    }

    public bool CheckForVoxel(Vector3Int pos) 
    {
        VoxelState voxel = worldData.GetVoxel(pos);

        if (voxel != null && blockTypes[voxel.id].isSolid)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public VoxelState GetVoxelState(Vector3Int pos)
    {
        return worldData.GetVoxel(pos);
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

    public byte GetVoxel(Vector3Int pos) 
    {
        byte voxelValue = GetBlockIDFromName("Air");
        Vector2Int xz = new Vector2Int(pos.x, pos.z);

        //IMMUTABLE PASS
        //outside of world is air
        if (!IsVoxelInWorld(pos)) 
        {
            return voxelValue;
        }
        //bedrock layer
        if (pos.y == 0) 
        {
            return GetBlockIDFromName("Bedrock");
        }

        //biome selection pass
        int solidGroundHeight = 42;

        int count = 0;
        float sumOfHeights = 0f;
        float strongestWeight = 0f;
        int strongestBiomeIndex = 0;

        for (int i = 0; i < biomes.Length; i++)
        {
            float weight = TerrainGeneration.Get2DNoise(xz, biomes[i].offset, biomes[i].scale);

            if (weight > strongestWeight)
            {
                strongestWeight = weight;
                strongestBiomeIndex = i;
            }

            //Get height of current biome and multiply by its weight
            float height = biomes[i].terrainHeight * TerrainGeneration.Get2DNoise(xz, biomes[i].offset, biomes[i].terrainScale) * weight;

            if (height > 0)
            {
                sumOfHeights += height;
                count++;
            }
        }

        BiomeAttributes biome = biomes[strongestBiomeIndex];

        sumOfHeights /= count;

        //basic terrain pass
        int terrainHeight = Mathf.FloorToInt(sumOfHeights + solidGroundHeight);
        
        if (pos.y == terrainHeight)
        {
            voxelValue = biome.surfaceBlock;
        }
        else if (pos.y < terrainHeight && pos.y > terrainHeight - biome.surfaceBlockDepth)
        {
            voxelValue = biome.subSurfaceBlock;
        }
        else if (pos.y > terrainHeight)
        {
            if (pos.y < VoxelData.SEA_LEVEL)
            {
                voxelValue = GetBlockIDFromName("Water");
            }

            return voxelValue;
        }
        else
        {
            voxelValue = GetBlockIDFromName("Stone");
        }

        //Underground pass for ores/dirt patches/etc.
        if (voxelValue == GetBlockIDFromName("Stone")) 
        {
            foreach (Ore ore in biome.ores) 
            {
                if (pos.y > ore.minHeight && pos.y < ore.maxHeight)
                {
                    if (TerrainGeneration.Get3DNoise(pos, ore.noiseOffset, ore.scale, ore.threshold))
                    {
                        voxelValue = ore.blockID;
                    }
                }
            }
        }

        //Major Flora pass
        if (pos.y == terrainHeight && biome.placeMajorFlora)
        {
            if (TerrainGeneration.Get2DNoise(xz, biome.majorFloraZoneOffset, biome.majorFloraZoneScale) > biome.majorFloraZoneThreshold)
            {
                if (TerrainGeneration.Get2DNoise(xz, biome.majorFloraPlacementOffset, biome.majorFloraPlacementScale) > biome.majorFloraPlacementThreshold)
                {
                    modifications.Enqueue(Structure.GenerateMajorFlora(biome.majorFloraIndex, this, pos, biome.minMajorFloraHeight, biome.maxMajorFloraHeight));
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

    bool IsVoxelInWorld(Vector3Int pos) 
    {
        return (pos.x >= 0 && pos.x < VoxelData.WORLD_SIZE_IN_VOXELS &&
            pos.y >= 0 && pos.y < VoxelData.CHUNK_HEIGHT &&
            pos.z >= 0 && pos.z < VoxelData.WORLD_SIZE_IN_VOXELS);
    }

    public byte GetBlockIDFromName(string name)
    {
        for (byte i = 0; i < blockTypes.Length; i++)
        {
            if (blockTypes[i].blockName == name) 
            {
                return i;
            }
        }

        Debug.Log("Could not find block with the name: " + name);
        return 0;
    }

    public string GetBlockNameFromID(byte id)
    {
        if (blockTypes.Length < id)
        { 
            Debug.Log("Could not find block with the id: " + id);
            return "";
        }

        return blockTypes[id].blockName;
    }
}

[System.Serializable]
public class BlockType 
{
    public string blockName;
    public VoxelMeshData meshData;
    public bool isSolid;
    public bool isLiquid;
    public bool isActive;
    public bool renderNeighborFaces;
    public byte opacity;
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

public class VoxelMod
{
    public Vector3Int pos;
    public byte id;
    public int orientation;

    public VoxelMod()
    {
        pos = new Vector3Int();
        id = 0;
        orientation = 1;
    }

    public VoxelMod(Vector3Int _pos, byte _id, int _orientation = 1)
    {
        pos = _pos;
        id = _id;
    }
}

[System.Serializable]
public class Settings {
    [Header("Game Data")]
    public string version = "0.0.1";

    [Header("Performance")]
    public int loadDistance = 16;
    public int viewDistance = 8;
    public CloudStyle clouds = CloudStyle.Fast;
    public bool enableThreading = true;

    [Header("Controls")]
    [Range(0.7f, 10f)]
    public float mouseSensitivity = 2.0f;
}