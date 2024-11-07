using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using UnityEngine;

public static class SaveSystem
{
    public static void SaveWorld(WorldData world)
    {
        string savePath = World.Instance.appPath + "/saves/" + world.worldName + "/";

        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }

        Debug.Log("Saving " + world.worldName);

        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = new FileStream(savePath + "world.data", FileMode.Create);

        formatter.Serialize(stream, world);
        stream.Close();

        Thread thread = new Thread(() => SaveChunks(world));
        thread.Start();
    }

    public static void SaveChunks(WorldData world)
    {
        List<ChunkData> chunks = new List<ChunkData>(world.modifiedChunks);
        world.modifiedChunks.Clear();

        int count = 0;
        foreach (ChunkData chunk in chunks)
        {
            SaveSystem.SaveChunk(chunk, world.worldName);
            count++;
        }

        Debug.Log(count + " chunks saved!");
    }

    public static void SaveChunk(ChunkData chunk, string worldName)
    {
        string chunkName = chunk.position.x + "," + chunk.position.y;

        string savePath = World.Instance.appPath + "/saves/" + worldName + "/chunks/";

        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }

        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = new FileStream(savePath + chunkName + ".data", FileMode.Create);

        formatter.Serialize(stream, chunk);
        stream.Close();
    }

    public static WorldData LoadWorld(string worldName, int seed = 0)
    { 
        string loadPath = World.Instance.appPath + "/saves/" + worldName + "/";

        if (File.Exists(loadPath + "world.data"))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(loadPath + "world.data", FileMode.Open);

            WorldData world = formatter.Deserialize(stream) as WorldData;
            stream.Close();

            //Have to create a new one because deserializing doesn't
            //use constructor to create dictionary
            return new WorldData(world);
        }
        else
        {
            Debug.Log(worldName + " not found! Creating new world.");

            WorldData world = new WorldData(worldName, seed);

            SaveWorld(world);

            return world;
        }
    }

    public static ChunkData LoadChunk(string worldName, Vector2Int pos)
    {
        string chunkName = pos.x + "," + pos.y;

        string loadPath = World.Instance.appPath + "/saves/" + worldName + "/chunks/" + chunkName + ".data";

        if (File.Exists(loadPath))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(loadPath, FileMode.Open);

            ChunkData chunkData = formatter.Deserialize(stream) as ChunkData;
            stream.Close();

            //Have to create a new one because deserializing doesn't
            //use constructor to create dictionary
            return chunkData;
        }
        else
        {
            return null;
        }
    }
}
