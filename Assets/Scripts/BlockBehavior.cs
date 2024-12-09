using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class BlockBehavior
{
    public static bool Active(VoxelState voxel)
    {
        string blockName = World.Instance.GetBlockNameFromID(voxel.id);

        switch (blockName)
        {
            case "Grass":
                byte dirtID = World.Instance.GetBlockIDFromName("Dirt");
                //in the order from VoxelData, skipping up/down
                if ((voxel.neighbors[0] != null && voxel.neighbors[0].id == dirtID) ||
                    (voxel.neighbors[1] != null && voxel.neighbors[1].id == dirtID) ||
                    (voxel.neighbors[4] != null && voxel.neighbors[4].id == dirtID) ||
                    (voxel.neighbors[5] != null && voxel.neighbors[5].id == dirtID))
                {
                    return true;
                }

                break;
        }

        return false;
    }

    public static void Behave(VoxelState voxel)
    {
        string blockName = World.Instance.GetBlockNameFromID(voxel.id);

        switch (blockName)
        {
            case "Grass":
                byte airID = World.Instance.GetBlockIDFromName("Air");
                byte dirtID = World.Instance.GetBlockIDFromName("Dirt");
                byte grassID = World.Instance.GetBlockIDFromName("Grass");
                //in the order from VoxelData
                //check if grass has block above it, if so make it dirt
                if (voxel.neighbors[2] != null && voxel.neighbors[2].id != airID)
                {
                    voxel.chunkData.chunk.RemoveActiveVoxel(voxel);
                    voxel.chunkData.ModifyVoxel(voxel.position, dirtID, 0);
                    return;
                }
                //spread grass to a random neighbor
                List<VoxelState> neighbors = new List<VoxelState>();
                if (voxel.neighbors[0] != null && voxel.neighbors[0].id == dirtID)
                {
                    neighbors.Add(voxel.neighbors[0]);
                }
                if (voxel.neighbors[1] != null && voxel.neighbors[1].id == dirtID)
                {
                    neighbors.Add(voxel.neighbors[1]);
                }
                if (voxel.neighbors[4] != null && voxel.neighbors[4].id == dirtID)
                {
                    neighbors.Add(voxel.neighbors[4]);
                }
                if (voxel.neighbors[5] != null && voxel.neighbors[5].id == dirtID)
                {
                    neighbors.Add(voxel.neighbors[5]);
                }

                if (neighbors.Count == 0)
                {
                    return;
                }

                int index = Random.Range(0, neighbors.Count);
                neighbors[index].chunkData.ModifyVoxel(neighbors[index].position, grassID, 0);

                break;
        }
    }
}
