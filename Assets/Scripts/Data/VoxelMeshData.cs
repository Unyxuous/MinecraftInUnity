using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[CreateAssetMenu(fileName = "New Voxel Mesh Data", menuName = "Minecraft/Voxel Mesh Data")]
public class VoxelMeshData : ScriptableObject
{
    public string blockName;
    public FaceMeshData[] faces; //there will only ever be 6 faces. one for each direction.
}

[System.Serializable]
public class VertData 
{
    public Vector3 pos;
    public Vector2 uv;

    public VertData(Vector3 _pos, Vector2 _uv)
    {
        pos = _pos;
        uv = _uv;
    }

    public Vector3 GetRotatedPosition(Vector3 angles) 
    {
        Vector3 center = new Vector3(0.5f, 0.5f, 0.5f);
        Vector3 direction = pos - center;  //get the direction from the center to the current vertice
        direction = Quaternion.Euler(angles) * direction;  //rotate the direction by the angle
        return direction + center;
    }
}

[System.Serializable]
public class FaceMeshData
{
    public string direction;
    public VertData[] vertData;
    public int[] triangles;

    public VertData GetVertData(int index)
    {
        return vertData[index];
    }
}