using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Structure
{
    public static Queue<VoxelMod> GenerateMajorFlora(int index, World world, Vector3Int pos, int minTrunkHeight, int maxTrunkHeight)
    {
        switch (index)
        {
            case 0:
                return MakeTree(world, pos, minTrunkHeight, maxTrunkHeight);
            case 1:
                return MakeCactus(world, pos, minTrunkHeight, maxTrunkHeight);
        }

        return new Queue<VoxelMod>();
    }

    public static Queue<VoxelMod> MakeTree(World world, Vector3Int pos, int minTrunkHeight, int maxTrunkHeight) 
    {
        Queue<VoxelMod> queue = new Queue<VoxelMod>();
        byte log = world.GetBlockIDFromName("OakLog");
        byte leaf = world.GetBlockIDFromName("OakLeaf");

        int height = (int)(maxTrunkHeight * TerrainGeneration.Get2DNoise(new Vector2Int(pos.x, pos.z), 250f, 3f));

        if (height < minTrunkHeight)
        {
            height = minTrunkHeight;
        }

        for (int i = 1; i < height; i++)
        {
            queue.Enqueue(new VoxelMod(new Vector3Int(pos.x, pos.y + i, pos.z), log));
        }

        for (int x = -3; x < 4; x++)
        {
            for (int y = 0; y < 7; y++)
            {
                for (int z = -3; z < 4; z++)
                { 
                    queue.Enqueue(new VoxelMod(new Vector3Int(pos.x + x, pos.y + height + y, pos.z + z), leaf));        
                }
            }
        }

        return queue;
    }

    public static Queue<VoxelMod> MakeCactus(World world, Vector3Int pos, int minTrunkHeight, int maxTrunkHeight)
    {
        Queue<VoxelMod> queue = new Queue<VoxelMod>();
        byte cactus = world.GetBlockIDFromName("Cactus");

        int height = (int)(maxTrunkHeight * TerrainGeneration.Get2DNoise(new Vector2Int(pos.x, pos.z), 20f, 2f));

        if (height < minTrunkHeight)
        {
            height = minTrunkHeight;
        }

        for (int i = 1; i <= height; i++)
        {
            queue.Enqueue(new VoxelMod(new Vector3Int(pos.x, pos.y + i, pos.z), cactus));
        }

        return queue;
    }
}
