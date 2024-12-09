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
    List<int> waterTriangles = new List<int>();
    List<int> transparentTriangles = new List<int>();
    Material[] materials = new Material[3];
    List<Vector2> uvs = new List<Vector2>();
    List<Color> colors = new List<Color>();
    List<Vector3> normals = new List<Vector3>();

    List<VoxelState> activeVoxels = new List<VoxelState>();

    public Chunk(ChunkCoord _coord) 
    {
        coord = _coord;
    
        chunkObject = new GameObject();
        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();

        materials[0] = World.Instance.material;
        materials[1] = World.Instance.transparentMaterial;
        materials[2] = World.Instance.waterMaterial;
        meshRenderer.materials = materials;

        chunkObject.transform.SetParent(World.Instance.transform);
        chunkObject.transform.position = new Vector3(coord.x * VoxelData.CHUNK_WIDTH, 0, coord.z * VoxelData.CHUNK_WIDTH);
        chunkObject.name = "Chunk " + coord.x + ", " + coord.z;

        position = new Vector3Int((int)chunkObject.transform.position.x, (int)chunkObject.transform.position.y, (int)chunkObject.transform.position.z);

        chunkData = World.Instance.worldData.RequestChunk(new Vector2Int(position.x, position.z), true);
        chunkData.chunk = this;

        for (int y = 0; y < VoxelData.CHUNK_HEIGHT; y++)
        {
            for (int x = 0; x < VoxelData.CHUNK_WIDTH; x++)
            {
                for (int z = 0; z < VoxelData.CHUNK_WIDTH; z++)
                {
                    VoxelState voxel = chunkData.map[x, y, z];
                    if (voxel.properties.isActive)
                    {
                        AddActiveVoxel(voxel);
                    }
                }
            }
        }

        World.Instance.AddChunkToUpdate(this);

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

    public void AddActiveVoxel(VoxelState voxel)
    {
        if (!activeVoxels.Contains(voxel))
        {
            activeVoxels.Add(voxel);
        }
    }

    public void RemoveActiveVoxel(VoxelState voxel)
    {
        for (int i = 0; i < activeVoxels.Count; i++)
        {
            if (activeVoxels[i] == voxel)
            {
                activeVoxels.RemoveAt(i);
                return;
            }
        }
    }

    public void EditVoxel(Vector3 pos, byte newID) 
    {
        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        x -= Mathf.FloorToInt(chunkObject.transform.position.x);
        z -= Mathf.FloorToInt(chunkObject.transform.position.z);

        chunkData.ModifyVoxel(new Vector3Int(x, y, z), newID, World.Instance.player.orientation);

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

    public VoxelState GetVoxelFromGlobalVector3(Vector3Int pos) {
        pos.x -= Mathf.FloorToInt(position.x);
        pos.z -= Mathf.FloorToInt(position.z);

        return chunkData.map[pos.x, pos.y, pos.z];
    }

    public void TickUpdate()
    {
        Debug.Log(chunkObject.name + " currently has " + activeVoxels.Count + " active blocks");

        for (int i = activeVoxels.Count - 1; i >= 0; i--)
        {
            if (!BlockBehavior.Active(activeVoxels[i]))
            {
                RemoveActiveVoxel(activeVoxels[i]);
            }
            else 
            { 
                BlockBehavior.Behave(activeVoxels[i]);
            }
        }
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
        waterTriangles.Clear();
        uvs.Clear();
        colors.Clear();
        normals.Clear();
    }

    void UpdateMeshData(Vector3Int pos) 
    {
        VoxelState voxel = chunkData.map[pos.x, pos.y, pos.z];
        bool isTransparent = voxel.properties.renderNeighborFaces;

        float rotation = 0f;
        switch (voxel.orientation)
        {
            case 0:
                rotation = 180f;
                break;
            case 5:
                rotation = 270f;
                break;
            case 1:
                rotation = 0f;
                break;
            case 4:
                rotation = 90f;
                break;
            default:
                rotation = 0f;
                break;
        }

        //6 faces on a cube
        for (int face = 0; face < 6; face++)
        {
            //hacky way to fix neighbor face rendering based on rotation
            int translatedFace = face;
            if (voxel.orientation != 1)
            {
                if (voxel.orientation == 0)
                {
                    if      (face == 0) translatedFace = 1;
                    else if (face == 1) translatedFace = 0;
                    else if (face == 4) translatedFace = 5;
                    else if (face == 5) translatedFace = 4;
                }
                else if (voxel.orientation == 4)
                {
                    if      (face == 0) translatedFace = 4;
                    else if (face == 1) translatedFace = 5;
                    else if (face == 4) translatedFace = 1;
                    else if (face == 5) translatedFace = 0;
                }
                else if (voxel.orientation == 5)
                {
                    if      (face == 0) translatedFace = 5;
                    else if (face == 1) translatedFace = 4;
                    else if (face == 4) translatedFace = 0;
                    else if (face == 5) translatedFace = 1;
                }
            }

            VoxelState neighbor = chunkData.map[pos.x, pos.y, pos.z].neighbors[translatedFace];

            if (neighbor != null && neighbor.properties.renderNeighborFaces && !(voxel.properties.isLiquid && chunkData.map[pos.x, pos.y +1, pos.z].properties.isLiquid))
            {
                FaceMeshData currFace = voxel.properties.meshData.faces[face];
                float lightLevel = neighbor.lightAsFloat;
                int faceVertCount = 0;

                for (int i = 0; i < currFace.vertData.Length; i++)
                {
                    VertData vertData = currFace.GetVertData(i);
                    vertices.Add(pos + vertData.GetRotatedPosition(new Vector3(0, rotation, 0)));
                    normals.Add(VoxelData.faceChecks[face]);
                    colors.Add(new Color(0, 0, 0, lightLevel));

                    if (voxel.properties.isLiquid)
                    {
                        uvs.Add(voxel.properties.meshData.faces[face].vertData[i].uv);
                    }
                    else
                    {
                        AddTexture(voxel.properties.GetTextureID(face), vertData.uv);
                    }

                    faceVertCount++;
                }

                if (!voxel.properties.renderNeighborFaces)
                {
                    for (int i = 0; i < currFace.triangles.Length; i++)
                    {
                        triangles.Add(vertexIndex + currFace.triangles[i]);
                    }
                }
                else
                {
                    if (voxel.properties.isLiquid)
                    {
                        for (int i = 0; i < currFace.triangles.Length; i++)
                        {
                            waterTriangles.Add(vertexIndex + currFace.triangles[i]);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < currFace.triangles.Length; i++)
                        {
                            transparentTriangles.Add(vertexIndex + currFace.triangles[i]);
                        }
                    }
                }

                vertexIndex += faceVertCount;
            }
        }
    }

    public void CreateMesh() 
    {
        Mesh mesh = new Mesh
        {
            vertices = vertices.ToArray(),
            subMeshCount = 3,
            uv = uvs.ToArray()
        };
        mesh.SetTriangles(triangles.ToArray(), 0);
        mesh.SetTriangles(transparentTriangles.ToArray(), 1);
        mesh.SetTriangles(waterTriangles.ToArray(), 2);
        mesh.colors = colors.ToArray();
        mesh.normals = normals.ToArray();

        meshFilter.mesh = mesh;
    }

    void AddTexture(int textureID, Vector2 uv) 
    {
        float y = textureID / VoxelData.TextureAtlasHeightInBlocks;
        float x = textureID - (y * VoxelData.TextureAtlasWidthInBlocks);

        x *= VoxelData.NormalizedBlockTextureWidth;
        y *= VoxelData.NormalizedBlockTextureHeight;

        y = 1f - y - VoxelData.NormalizedBlockTextureHeight;

        x += VoxelData.NormalizedBlockTextureWidth * uv.x;
        y += VoxelData.NormalizedBlockTextureHeight * uv.y;

        uvs.Add(new Vector2(x, y));
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