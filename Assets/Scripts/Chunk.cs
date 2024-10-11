using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Chunk
{
    public ChunkCoord coord;

    GameObject chunkObject;
    MeshRenderer meshRenderer;
    MeshFilter meshFilter;

    private bool _isActive;
    public bool isVoxelMapPopulated = false;

    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    List<int> transparentTriangles = new List<int>();
    Material[] materials = new Material[2];
    List<Vector2> uvs = new List<Vector2>();
    int vertexIndex = 0;

    public byte[,,] voxelMap = new byte[VoxelData.CHUNK_WIDTH, VoxelData.CHUNK_HEIGHT, VoxelData.CHUNK_WIDTH];

    World world;

    public Chunk(ChunkCoord _coord, World _world, bool generateOnLoad) 
    {
        coord = _coord;
        world = _world;
        isActive = true;

        if (generateOnLoad)
        {
            Init();
        }
    }

    public void Init() 
    {
        chunkObject = new GameObject();
        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();

        materials[0] = world.material;
        materials[1] = world.transparentMaterial;
        meshRenderer.materials = materials;

        chunkObject.transform.SetParent(world.transform);
        chunkObject.transform.position = new Vector3(coord.x * VoxelData.CHUNK_WIDTH, 0, coord.z * VoxelData.CHUNK_WIDTH);
        chunkObject.name = "Chunk " + coord.x + ", " + coord.z;

        PopulateVoxelMap();
        UpdateChunkMesh();
    }

    void PopulateVoxelMap() 
    {
        for (int y = 0; y < VoxelData.CHUNK_HEIGHT; y++)
        {
            for (int x = 0; x < VoxelData.CHUNK_WIDTH; x++)
            {
                for (int z = 0; z < VoxelData.CHUNK_WIDTH; z++)
                {
                    voxelMap[x, y, z] = world.GetVoxel(new Vector3(x, y, z) + position);
                }
            }
        }

        isVoxelMapPopulated = true;
    }

    public bool isActive 
    { 
        get { return _isActive; }
        set { 
            _isActive = value;
            if (chunkObject != null)
            {
                chunkObject.SetActive(value);
            }
        }
    }

    public Vector3 position 
    {
        get { return chunkObject.transform.position; }
    }

    bool IsVoxelInChunk(int x, int y, int z) 
    {
        if (x < 0 || x > VoxelData.CHUNK_WIDTH - 1 ||
            y < 0 || y > VoxelData.CHUNK_HEIGHT - 1 ||
            z < 0 || z > VoxelData.CHUNK_WIDTH - 1)
        {
            return false;
        }
        return true;
    }

    public void EditVoxel(Vector3 pos, byte newID) 
    {
        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        x -= Mathf.FloorToInt(chunkObject.transform.position.x);
        z -= Mathf.FloorToInt(chunkObject.transform.position.z);

        voxelMap[x, y, z] = newID;

        UpdateSurroundingVoxels(x, y, z);

        UpdateChunkMesh();
    }

    void UpdateSurroundingVoxels(int x, int y, int z) 
    {
        Vector3 thisVoxel = new Vector3(x, y, z);

        for (int face = 0; face < 6; face++)
        {
            Vector3 checkVoxel = thisVoxel + VoxelData.faceChecks[face];

            if (!IsVoxelInChunk((int)checkVoxel.x, (int)checkVoxel.y, (int)checkVoxel.z))
            {
                world.GetChunkFromVector3(checkVoxel + position).UpdateChunkMesh();
            }
        }
    }

    bool CheckForVoxel(Vector3 pos) 
    {
        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        if (!IsVoxelInChunk(x, y, z)) 
        {
            return world.CheckIfVoxelTransparent(pos + position);
        }

        return world.blockTypes[voxelMap[x, y, z]].isTransparent;
    }

    public byte GetVoxelFromGlobalVector3(Vector3 pos) {
        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        x -= Mathf.FloorToInt(chunkObject.transform.position.x);
        z -= Mathf.FloorToInt(chunkObject.transform.position.z);

        return voxelMap[x, y, z];
    }

    void UpdateChunkMesh() 
    {
        ClearMeshData();

        for (int y = 0; y < VoxelData.CHUNK_HEIGHT; y++)
        {
            for (int x = 0; x < VoxelData.CHUNK_WIDTH; x++)
            {
                for (int z = 0; z < VoxelData.CHUNK_WIDTH; z++)
                {
                    if (world.blockTypes[voxelMap[x, y, z]].isSolid)
                    {
                        UpdateMeshData(new Vector3(x, y, z));
                    }
                }
            }
        }

        CreateMesh();
    }

    void ClearMeshData() 
    {
        vertexIndex = 0;
        vertices.Clear();
        triangles.Clear();
        transparentTriangles.Clear();
        uvs.Clear();
    }

    void UpdateMeshData(Vector3 pos) 
    {
        byte blockID = voxelMap[(int)pos.x, (int)pos.y, (int)pos.z];
        bool isTransparent = world.blockTypes[blockID].isTransparent;

        //6 faces on a cube
        for (int face = 0; face < 6; face++)
        {
            //only draw face if there's no neighbor
            if (CheckForVoxel(pos + VoxelData.faceChecks[face]))
            {
                vertices.Add(pos + VoxelData.voxelVertices[VoxelData.voxelTriangles[face, 0]]);
                vertices.Add(pos + VoxelData.voxelVertices[VoxelData.voxelTriangles[face, 1]]);
                vertices.Add(pos + VoxelData.voxelVertices[VoxelData.voxelTriangles[face, 2]]);
                vertices.Add(pos + VoxelData.voxelVertices[VoxelData.voxelTriangles[face, 3]]);

                AddTexture(world.blockTypes[blockID].GetTextureID(face));

                if (!isTransparent)
                {
                    triangles.Add(vertexIndex);
                    triangles.Add(vertexIndex + 1);
                    triangles.Add(vertexIndex + 2);

                    triangles.Add(vertexIndex + 2);
                    triangles.Add(vertexIndex + 1);
                    triangles.Add(vertexIndex + 3);
                }
                else 
                {
                    transparentTriangles.Add(vertexIndex);
                    transparentTriangles.Add(vertexIndex + 1);
                    transparentTriangles.Add(vertexIndex + 2);

                    transparentTriangles.Add(vertexIndex + 2);
                    transparentTriangles.Add(vertexIndex + 1);
                    transparentTriangles.Add(vertexIndex + 3);
                }

                vertexIndex += 4;
            }
        }
    }

    void CreateMesh() 
    {
        Mesh mesh = new Mesh
        {
            vertices = vertices.ToArray(),
            subMeshCount = 2,
            uv = uvs.ToArray(),
        };
        mesh.SetTriangles(triangles.ToArray(), 0);
        mesh.SetTriangles(transparentTriangles.ToArray(), 1);

        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
    }

    void AddTexture(int textureID) 
    {
        float y = textureID / VoxelData.TextureAtlasHeightInBlocks;
        float x = textureID - (y * VoxelData.TextureAtlasWidthInBlocks);

        x *= VoxelData.NormalizedBlockTextureWidth;
        y *= VoxelData.NormalizedBlockTextureHeight;

        y = 1f - y - VoxelData.NormalizedBlockTextureHeight;

        uvs.Add(new Vector2(x, y));
        uvs.Add(new Vector2(x, y + VoxelData.NormalizedBlockTextureHeight));
        uvs.Add(new Vector2(x + VoxelData.NormalizedBlockTextureWidth, y));
        uvs.Add(new Vector2(x + VoxelData.NormalizedBlockTextureWidth, y + VoxelData.NormalizedBlockTextureHeight));
    }
}


public class ChunkCoord 
{
    public int x;
    public int z;

    public ChunkCoord(int _x = 0, int _z = 0) 
    {
        x = _x;
        z = _z;
    }

    public ChunkCoord(Vector3 pos)
    {
        x = (Mathf.FloorToInt(pos.x) / VoxelData.CHUNK_WIDTH);
        z = (Mathf.FloorToInt(pos.z) / VoxelData.CHUNK_WIDTH);
    }

    public override bool Equals(object obj)
    {
        return obj is ChunkCoord && Equals((ChunkCoord) obj);
    }

    private bool Equals(ChunkCoord b) {
        return this == b;
    }

    public override int GetHashCode()
    {
        return x * 312 + z * 32;
    }

    public override string ToString()
    {
        return x.ToString() + ", " + z.ToString();
    }

    public static bool operator ==(ChunkCoord a, ChunkCoord b) 
    {
        return (a.x == b.x && a.z == b.z);
    }

    public static bool operator !=(ChunkCoord a, ChunkCoord b)
    {
        return (a.x != b.x || a.z != b.z);
    }
}