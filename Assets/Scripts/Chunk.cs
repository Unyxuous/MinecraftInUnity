using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEditor;

public class Chunk
{
    public ChunkCoord coord;
    public Vector3Int position;

    ChunkData chunkData;

    GameObject chunkObject;
    MeshRenderer meshRenderer;
    MeshFilter meshFilter;

    public bool isInitialized;
    private bool _isActive;
    private bool _isLoaded;

    int vertexIndex = 0;
    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    List<int> transparentTriangles = new List<int>();
    Material[] materials = new Material[2];
    List<Vector2> uvs = new List<Vector2>();
    List<Color> colors = new List<Color>();
    List<Vector3> normals = new List<Vector3>();

    public Chunk(ChunkCoord _coord) 
    {
        coord = _coord;
    
        chunkObject = new GameObject();
        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();

        materials[0] = World.Instance.material;
        materials[1] = World.Instance.transparentMaterial;
        meshRenderer.materials = materials;

        chunkObject.transform.SetParent(World.Instance.transform);
        chunkObject.transform.position = new Vector3(coord.x * VoxelData.CHUNK_WIDTH, 0, coord.z * VoxelData.CHUNK_WIDTH);
        chunkObject.name = "Chunk " + coord.x + ", " + coord.z;

        position = new Vector3Int((int)chunkObject.transform.position.x, (int)chunkObject.transform.position.y, (int)chunkObject.transform.position.z);

        chunkData = World.Instance.worldData.RequestChunk(new Vector2Int(position.x, position.z), true);
        chunkData.chunk = this;

        isInitialized = true;
    }

    public bool isActive 
    { 
        get 
        { 
            return _isActive; 
        }
        set 
        { 
            _isActive = value;
            if (chunkObject != null)
            {
                chunkObject.SetActive(value);
            }
        }
    }

    public bool isLoaded
    {
        get
        {
            return _isLoaded;
        }
        set
        {
            _isLoaded = value;
        }
    }

    public void EditVoxel(Vector3 pos, byte newID) 
    {
        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        x -= Mathf.FloorToInt(chunkObject.transform.position.x);
        z -= Mathf.FloorToInt(chunkObject.transform.position.z);

        chunkData.map[x, y, z].id = newID;
        World.Instance.worldData.AddToModifiedChunks(chunkData);

        World.Instance.AddChunkToUpdate(this, true);
        UpdateSurroundingVoxels(x, y, z);
    }

    void UpdateSurroundingVoxels(int x, int y, int z) 
    {
        Vector3 thisVoxel = new Vector3(x, y, z);

        for (int face = 0; face < 6; face++)
        {
            Vector3 checkVoxel = thisVoxel + VoxelData.faceChecks[face];

            if (!chunkData.IsVoxelInChunk((int)checkVoxel.x, (int)checkVoxel.y, (int)checkVoxel.z))
            {
                World.Instance.AddChunkToUpdate(World.Instance.GetChunkFromVector3(checkVoxel + position), true);
            }
        }
    }

    VoxelState CheckForVoxel(Vector3Int pos) 
    {
        if (!chunkData.IsVoxelInChunk(pos.x, pos.y, pos.z)) 
        {
            return World.Instance.GetVoxelState(pos + position);
        }

        return chunkData.map[pos.x, pos.y, pos.z];
    }

    public VoxelState GetVoxelFromGlobalVector3(Vector3Int pos) {
        pos.x -= Mathf.FloorToInt(position.x);
        pos.z -= Mathf.FloorToInt(position.z);

        return chunkData.map[pos.x, pos.y, pos.z];
    }

    public void UpdateChunk() 
    {
        ClearMeshData();

        for (int y = 0; y < VoxelData.CHUNK_HEIGHT; y++)
        {
            for (int x = 0; x < VoxelData.CHUNK_WIDTH; x++)
            {
                for (int z = 0; z < VoxelData.CHUNK_WIDTH; z++)
                {
                    if (chunkData.map[x, y, z].properties.isSolid)
                    {
                        UpdateMeshData(new Vector3Int(x, y, z));
                    }
                }
            }
        }

        World.Instance.chunksToDraw.Enqueue(this);
    }

    void ClearMeshData() 
    {
        vertexIndex = 0;
        vertices.Clear();
        triangles.Clear();
        transparentTriangles.Clear();
        uvs.Clear();
        colors.Clear();
        normals.Clear();
    }

    void UpdateMeshData(Vector3Int pos) 
    {
        VoxelState voxel = chunkData.map[pos.x, pos.y, pos.z];
        bool isTransparent = voxel.properties.renderNeighborFaces;

        //6 faces on a cube
        for (int face = 0; face < 6; face++)
        {
            VoxelState neighbor = CheckForVoxel(pos + VoxelData.faceChecks[face]);

            //only draw face if there's no neighbor
            if (neighbor != null && neighbor.properties.renderNeighborFaces)
            {
                vertices.Add(pos + VoxelData.voxelVertices[VoxelData.voxelTriangles[face, 0]]);
                vertices.Add(pos + VoxelData.voxelVertices[VoxelData.voxelTriangles[face, 1]]);
                vertices.Add(pos + VoxelData.voxelVertices[VoxelData.voxelTriangles[face, 2]]);
                vertices.Add(pos + VoxelData.voxelVertices[VoxelData.voxelTriangles[face, 3]]);

                for (int i = 0; i < 4; i++)
                {
                    normals.Add(VoxelData.faceChecks[face]);
                }

                AddTexture(voxel.properties.GetTextureID(face));

                float lightLevel = neighbor.lightAsFloat;

                colors.Add(new Color(0, 0, 0, lightLevel));
                colors.Add(new Color(0, 0, 0, lightLevel));
                colors.Add(new Color(0, 0, 0, lightLevel));
                colors.Add(new Color(0, 0, 0, lightLevel));

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

    public void CreateMesh() 
    {
        Mesh mesh = new Mesh
        {
            vertices = vertices.ToArray(),
            subMeshCount = 2,
            uv = uvs.ToArray()
        };
        mesh.SetTriangles(triangles.ToArray(), 0);
        mesh.SetTriangles(transparentTriangles.ToArray(), 1);
        mesh.colors = colors.ToArray();
        mesh.normals = normals.ToArray();

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

    public ChunkCoord(Vector3Int pos)
    {
        x = (pos.x / VoxelData.CHUNK_WIDTH);
        z = (pos.z / VoxelData.CHUNK_WIDTH);
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