using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Clouds : MonoBehaviour
{
    public int cloudHeight = 100;
    public int cloudDepth = 1;

    [SerializeField]
    private Texture2D cloudPattern = null;
    [SerializeField]
    private Material cloudMaterial = null;
    bool[,] cloudData;

    int cloudTexWidth;

    int cloudTileSize;
    Vector3Int offset;

    Dictionary<Vector2Int, GameObject> clouds = new Dictionary<Vector2Int, GameObject>();

    [SerializeField]
    private World world = null;

    private void Start()
    {
        cloudTexWidth = cloudPattern.width;
        cloudTileSize = VoxelData.CHUNK_WIDTH;
        offset = new Vector3Int(-(cloudTexWidth / 2), 0, -(cloudTexWidth / 2));
        
        transform.position = new Vector3(VoxelData.WORLD_CENTER, cloudHeight, VoxelData.WORLD_CENTER);

        LoadCloudData();
        CreateClouds();
    }

    private void LoadCloudData() 
    {
        cloudData = new bool[cloudTexWidth, cloudTexWidth];
        Color[] cloudTex = cloudPattern.GetPixels();

        for (int x = 0; x < cloudTexWidth; x++)
        {
            for (int z = 0; z < cloudTexWidth; z++)
            {
                cloudData[x, z] = (cloudTex[z * cloudTexWidth + x].a > 0);
            }
        }
    }

    private void CreateClouds()
    {
        if (world.settings.clouds == CloudStyle.Off)
        {
            return;
        }

        for (int x = 0; x < cloudTexWidth; x += cloudTileSize)
        {
            for (int z = 0; z < cloudTexWidth; z += cloudTileSize)
            {
                Mesh cloudMesh;
                if (world.settings.clouds == CloudStyle.Fancy)
                {
                    cloudMesh = CreateFastCloudMesh(x, z);
                }
                else
                {
                    cloudMesh = CreateFancyCloudMesh(x, z);
                }

                Vector3 pos = new Vector3(x, cloudHeight, z);
                clouds.Add(CloudTilePosFromV3(pos), CreateCloudTile(cloudMesh, pos));
            }
        }
    }

    public void UpdateClouds() 
    {
        if (world.settings.clouds == CloudStyle.Off)
        {
            return;
        }

        for (int x = 0; x < cloudTexWidth; x += cloudTileSize)
        {
            for (int z = 0; z < cloudTexWidth; z += cloudTileSize)
            {
                Vector3 pos = world.player.transform.position + new Vector3(x, 0, z) + offset;
                pos = new Vector3(RoundToCloud(pos.x), cloudHeight, RoundToCloud(pos.z));
                Vector2Int cloudPos = CloudTilePosFromV3(pos);

                clouds[cloudPos].transform.position = pos;
            }
        }
    }

    private int RoundToCloud(float val)
    {
        return Mathf.FloorToInt(val / cloudTileSize) * cloudTileSize;
    }

    private Mesh CreateFastCloudMesh(int x, int z)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector3> normals = new List<Vector3>();
        int vertCount = 0;

        for (int xIncrement = 0; xIncrement < cloudTileSize; xIncrement++)
        {
            for (int zIncrement = 0; zIncrement < cloudTileSize; zIncrement++)
            {
                int xVal = x + xIncrement;
                int zVal = z + zIncrement;

                if (cloudData[xVal, zVal])
                {
                    vertices.Add(new Vector3(xIncrement,     0, zIncrement));
                    vertices.Add(new Vector3(xIncrement,     0, zIncrement + 1));
                    vertices.Add(new Vector3(xIncrement + 1, 0, zIncrement + 1));
                    vertices.Add(new Vector3(xIncrement + 1, 0, zIncrement));

                    normals.Add(Vector3.down);
                    normals.Add(Vector3.down);
                    normals.Add(Vector3.down);
                    normals.Add(Vector3.down);

                    triangles.Add(vertCount + 1);
                    triangles.Add(vertCount);
                    triangles.Add(vertCount + 2);

                    triangles.Add(vertCount + 2);
                    triangles.Add(vertCount);
                    triangles.Add(vertCount + 3);

                    vertCount += 4;
                }
            }
        }

        Mesh mesh = new Mesh
        {
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray(),
            normals = normals.ToArray()
        };

        return mesh;
    }

    private Mesh CreateFancyCloudMesh(int x, int z)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector3> normals = new List<Vector3>();
        int vertCount = 0;

        for (int xIncrement = 0; xIncrement < cloudTileSize; xIncrement++)
        {
            for (int zIncrement = 0; zIncrement < cloudTileSize; zIncrement++)
            {
                int xVal = x + xIncrement;
                int zVal = z + zIncrement;

                if (cloudData[xVal, zVal])
                {
                    for (int face = 0; face < 6; face++)
                    {
                        if (!CheckCloudData(new Vector3Int(xVal, 0, zVal) + VoxelData.faceChecks[face]))
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                Vector3Int vert = new Vector3Int(xIncrement, 0, zIncrement);
                                vert += VoxelData.voxelVertices[VoxelData.voxelTriangles[face, i]];
                                vert.y *= cloudDepth;
                                vertices.Add(vert);
                            }

                            for (int i = 0; i < 4; i++)
                            {
                                normals.Add(VoxelData.faceChecks[face]);
                            }

                            triangles.Add(vertCount);
                            triangles.Add(vertCount + 1);
                            triangles.Add(vertCount + 2);

                            triangles.Add(vertCount + 2);
                            triangles.Add(vertCount + 1);
                            triangles.Add(vertCount + 3);

                            vertCount += 4;
                        }
                    }
                }
            }
        }

        Mesh mesh = new Mesh
        {
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray(),
            normals = normals.ToArray()
        };

        return mesh;
    }

    private bool CheckCloudData(Vector3Int pos)
    {
        if (pos.y != 0)
        {
            return false;
        }

        int x = pos.x;
        int z = pos.z;

        if (pos.x < 0)
        {
            x = cloudTexWidth - 1;
        }
        else if (pos.x > cloudTexWidth - 1)
        {
            x = 0;
        }
        if (pos.z < 0)
        {
            z = cloudTexWidth - 1;
        }
        else if (pos.z > cloudTexWidth - 1)
        {
            z = 0;
        }

        return cloudData[x, z];
    }

    private GameObject CreateCloudTile(Mesh mesh, Vector3 pos)
    {
        GameObject newCloudTile = new GameObject
        {
            name = "Cloud Tile: " + pos.x.ToString() + ',' + pos.z.ToString()
        };
        newCloudTile.transform.position = pos;
        newCloudTile.transform.parent = transform;

        MeshFilter mF = newCloudTile.AddComponent<MeshFilter>();
        MeshRenderer mR = newCloudTile.AddComponent<MeshRenderer>();

        mR.material = cloudMaterial;
        mF.mesh = mesh;

        return newCloudTile;
    }

    private Vector2Int CloudTilePosFromV3(Vector3 pos) 
    {
        return new Vector2Int(CloudTileCoordFromFloat(pos.x), CloudTileCoordFromFloat(pos.z));
    }

    private int CloudTileCoordFromFloat(float val) 
    {
        //Gets location as a percentage of the overall texture
        float a = val / (float)cloudTexWidth;
        a -= Mathf.FloorToInt(a);

        //Transforms it to an actual location in the texture
        return Mathf.FloorToInt((float)cloudTexWidth * a);
    }
}

public enum CloudStyle { 
    Off, 
    Fast, 
    Fancy
};
