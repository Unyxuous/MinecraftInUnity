using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class VoxelData
{
    public static readonly int WORLD_SIZE_IN_CHUNKS = 100;
    public static readonly int CHUNK_WIDTH = 16;
    public static readonly int CHUNK_HEIGHT = 128;

    //Lighting values
    public static float minLightLevel = 0.01f;
    public static float maxLightLevel = 0.8f;

    public static float unitOfLight 
    {
        get
        {
            //based on minecraft's light levels
            return 1f / 16f;
        }
    }
    public static byte sunLightLevel = 15;

    public static int WORLD_SIZE_IN_VOXELS 
    {
        get { return WORLD_SIZE_IN_CHUNKS * CHUNK_WIDTH; }
    }

    public static int WORLD_CENTER {
        get { return (WORLD_SIZE_IN_CHUNKS * CHUNK_WIDTH) / 2; }
    }

    public static readonly int TextureAtlasWidthInBlocks = 16;
    public static readonly int TextureAtlasHeightInBlocks = 16;
    public static float NormalizedBlockTextureWidth 
    {
        get { return 1f / TextureAtlasWidthInBlocks; }
    }
    
    public static float NormalizedBlockTextureHeight 
    {
        get { return 1f / TextureAtlasHeightInBlocks; }
    }

    public static readonly Vector3Int[] voxelVertices = 
    { 
        new Vector3Int(0, 0, 0),
        new Vector3Int(1, 0, 0),
        new Vector3Int(1, 1, 0),
        new Vector3Int(0, 1, 0),
        new Vector3Int(0, 0, 1),
        new Vector3Int(1, 0, 1),
        new Vector3Int(1, 1, 1),
        new Vector3Int(0, 1, 1)
    };

    public static readonly int[,] voxelTriangles = 
    {
        { 0, 3, 1, 2 }, //back face
        { 5, 6, 4, 7 }, //front face
        { 3, 7, 2, 6 }, //top face
        { 1, 5, 0, 4 }, //bottom face
        { 4, 7, 0, 3 }, //left face
        { 1, 2, 5, 6 }, //right face
    };

    public static readonly Vector2Int[] voxelUvs = 
    {
        new Vector2Int(0, 0),
        new Vector2Int(0, 1),
        new Vector2Int(1, 0),
        new Vector2Int(1, 1)
    };


    public static readonly Vector3Int[] faceChecks = 
    {
        new Vector3Int(0, 0, -1), //same order as voxelTriangles
        new Vector3Int(0, 0, 1),
        new Vector3Int(0, 1, 0),
        new Vector3Int(0, -1, 0),
        new Vector3Int(-1, 0, 0),
        new Vector3Int(1, 0, 0)
    };

    public static readonly int[] revFaceCheckIndex =
    { 
        1, 0, 3, 2, 5, 4
    };
}
