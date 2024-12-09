using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DebugScreen : MonoBehaviour
{
    World world;
    TextMeshProUGUI text;

    string divider = "\n=========\n";

    float frameRate;
    float timer;

    int halfWorldSizeInVoxels;
    int halfWorldSizeInChunks;

    void Start()
    {
        world = GameObject.Find("World").GetComponent<World>();
        text = GetComponent<TextMeshProUGUI>();

        halfWorldSizeInChunks = VoxelData.WORLD_SIZE_IN_CHUNKS / 2;
        halfWorldSizeInVoxels = VoxelData.WORLD_SIZE_IN_VOXELS / 2;
    }

    void Update()
    {
        string debugText = "Debug Screen\n";
        debugText += "FPS: " + frameRate + divider;

        debugText += "Coordinates:\n";
        debugText += "x: " + (Mathf.FloorToInt(world.player.transform.position.x) - halfWorldSizeInVoxels) + ", ";
        debugText += "y: " + Mathf.FloorToInt(world.player.transform.position.y) + ", ";
        debugText += "z: " + (Mathf.FloorToInt(world.player.transform.position.z) - halfWorldSizeInVoxels) + divider;

        debugText += "Chunk Coordinates:\n";
        debugText += "x: " + (world.playerChunkCoord.x - halfWorldSizeInChunks) + ", ";
        debugText += "z: " + (world.playerChunkCoord.z - halfWorldSizeInChunks) + divider;


        if (timer > 1f)
        {
            frameRate = (int)(1f / Time.unscaledDeltaTime);
            timer = 0;
        }
        else 
        {
            timer += Time.deltaTime;
        }

        string direction = "";
        switch (world.player.orientation)
        {
            case 0:
                direction = "South";
                break;
            case 5:
                direction = "West";
                break;
            case 1:
                direction = "North";
                break;
            case 4:
                direction = "East";
                break;
            default:
                direction = "idk, up/down?";
                break;
        }

        debugText += "Facing: ";
        debugText += direction + divider;

        text.text = debugText;
    }
}
