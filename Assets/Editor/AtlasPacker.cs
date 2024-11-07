using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Codice.Client.Common;
using System.Linq;
using Unity.VisualScripting;
using System.IO;

public class AtlasPacker : EditorWindow
{
    int blockSize = 16;
    int atlasSizeInBlocks = 16;
    int atlasSize;

    List<Object> rawTextures = new List<Object>();
    List<Texture2D> sortedTextures = new List<Texture2D>();
    Texture2D atlas;

    [MenuItem("MinecraftClone/Atlas Packer")]
    public static void ShowWindow() 
    {
        EditorWindow.GetWindow(typeof(AtlasPacker));
    }

    private void OnGUI()
    {
        atlasSize = blockSize * atlasSizeInBlocks;

        GUILayout.Label("MinecraftClone Texture Atlas Packer", EditorStyles.boldLabel);

        blockSize = EditorGUILayout.IntField("Block Size", blockSize);
        atlasSizeInBlocks = EditorGUILayout.IntField("Atlas Size (in blocks)", atlasSizeInBlocks);

        if (GUILayout.Button("Load Textures")) 
        {
            LoadTextures();

            PackAtlas();
        }

        if (GUILayout.Button("Clear Textures"))
        {
            atlas = new Texture2D(atlasSize, atlasSize);
        }

        if (GUILayout.Button("Save Atlas"))
        {
            byte[] bytes = atlas.EncodeToPNG();

            try
            {
                File.WriteAllBytes(Application.dataPath + "/Textures/Packed_Atlas.png", bytes);
            }
            catch 
            {
                Debug.Log("Atlas Packer: couldn't save atlas to file.");
            }
        }

        GUILayout.Label(atlas);
    }

    void LoadTextures() 
    {
        rawTextures.Clear();
        sortedTextures.Clear();

        rawTextures = Resources.LoadAll("AtlasPacker", typeof(Texture2D)).ToList();

        for (int i = 0; i < rawTextures.Count; i++)
        {
            Texture2D texture = (Texture2D)rawTextures[i];
            if (texture.width == blockSize && texture.height == blockSize)
            {
                sortedTextures.Add(texture);
            }
            else
            { 
                Debug.Log("Atlas Packer: " + texture.name + " incorrect size. texture not loaded.");
            }
        }

        Debug.Log("Atlas Packer: " + sortedTextures.Count + " textures loaded successfully.");
    }

    void PackAtlas() 
    {
        atlas = new Texture2D(atlasSize, atlasSize);

        Color[] pixels = new Color[atlasSize * atlasSize];

        for (int x = 0; x < atlasSize; x++)
        {
            for (int y = 0; y < atlasSize; y++)
            {
                int currentBlockX = x / blockSize;
                int currentBlockY = y / blockSize;
                int index = currentBlockY * atlasSizeInBlocks + currentBlockX;

                int currentPixelX = x - (currentBlockX * blockSize);

                if (index < sortedTextures.Count)
                {
                    pixels[(atlasSize - y - 1) * atlasSize + x] = sortedTextures[index].GetPixel(currentPixelX, blockSize - y - 1);
                }
                else
                {
                    pixels[(atlasSize - y - 1) * atlasSize + x] = new Color(0f, 0f, 0f, 0f);
                }
            }
        }

        atlas.SetPixels(pixels);
        atlas.Apply();
    }
}
