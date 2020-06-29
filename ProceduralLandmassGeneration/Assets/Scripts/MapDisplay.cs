using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapDisplay : MonoBehaviour
{
    public Renderer textureRenderer;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    public void DrawTexture(Texture2D a_texture)
    {
        textureRenderer.sharedMaterial.mainTexture = a_texture;
        textureRenderer.transform.localScale = new Vector3(a_texture.width, 1, a_texture.height);
    }

    public void DrawMesh(MeshData a_meshData, Texture2D a_texture)
    {
        meshFilter.sharedMesh = a_meshData.CreateMesh();
        meshRenderer.sharedMaterial.mainTexture = a_texture;
    }
}
