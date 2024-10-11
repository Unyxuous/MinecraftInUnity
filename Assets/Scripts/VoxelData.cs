using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class VoxelData
{
    public static readonly int WORLD_SIZE_IN_CHUNKS = 100;
    public static readonly int CHUNK_WIDTH = 16;
    public static readonly int CHUNK_HEIGHT = 128;
    public static int WORLD_SIZE_IN_VOXELS 
    {
        get { return WORLD_SIZE_IN_CHUNKS * CHUNK_WIDTH; }
    }

    public static readonly int TextureAtlasWidthInBlocks = 4;
    public static readonly int TextureAtlasHeightInBlocks = 4;
    public static float NormalizedBlockTextureWidth 
    {
        get { return 1f / TextureAtlasWidthInBlocks; }
    }
    
    public static float NormalizedBlockTextureHeight 
    {
        get { return 1f / TextureAtlasHeightInBlocks; }
    }

    public static readonly int VIEW_DISTANCE_IN_CHUNKS = 5;

    public static readonly Vector3[] voxelVertices = 
    { 
        new Vector3(0, 0, 0),
        new Vector3(1, 0, 0),
        new Vector3(1, 1, 0),
        new Vector3(0, 1, 0),
        new Vector3(0, 0, 1),
        new Vector3(1, 0, 1),
        new Vector3(1, 1, 1),
        new Vector3(0, 1, 1)
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

    public static readonly Vector2[] voxelUvs = 
    {
        new Vector2(0, 0),
        new Vector2(0, 1),
        new Vector2(1, 0),
        new Vector2(1, 1)
    };


    public static readonly Vector3[] faceChecks = 
    {
        new Vector3(0, 0, -1), //same order as voxelTriangles
        new Vector3(0, 0, 1),
        new Vector3(0, 1, 0),
        new Vector3(0, -1, 0),
        new Vector3(-1, 0, 0),
        new Vector3(1, 0, 0)
    };
}
