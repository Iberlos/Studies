using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{
    const float viewerMoveThresholdForChunkUpdate = 25f;
    const float sqrdViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;
    public LevelOfDetailInfo[] detailLevels;
    public static float maxViewDistance;
    public Transform viwerTransform;
    public Material mapMaterial;
    public static Vector2 viwerPosition;
    Vector2 previousViwerPosition;
    static MapGenerator mapGenerator;
    int chunckSize;
    int chunksViwedInDistance;

    Dictionary<Vector2Int, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2Int, TerrainChunk>();
    static List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

    // Start is called before the first frame update
    void Start()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();

        maxViewDistance = detailLevels[detailLevels.Length - 1].visibleDistanceThreshold;
        chunckSize = MapGenerator.chunkSize - 1;
        chunksViwedInDistance = Mathf.RoundToInt(maxViewDistance / chunckSize);

        UpdateVisibleChunks();
    }

    private void Update()
    {
        viwerPosition = new Vector2(viwerTransform.position.x, viwerTransform.position.z);
        if ((previousViwerPosition - viwerPosition).sqrMagnitude > sqrdViewerMoveThresholdForChunkUpdate)
        {
            previousViwerPosition = viwerPosition;
            UpdateVisibleChunks();
        }
    }

    // Update is called once per frame
    void UpdateVisibleChunks()
    {
        foreach(TerrainChunk terrainChunk in terrainChunksVisibleLastUpdate)
        {
            terrainChunk.SetVisible(false);
        }
        terrainChunksVisibleLastUpdate.Clear();

        int currentCurrentChunkCoordx = Mathf.RoundToInt(viwerPosition.x / chunckSize);
        int currentCurrentChunkCoordy = Mathf.RoundToInt(viwerPosition.y / chunckSize);

        for(int offsetX = -chunksViwedInDistance;  offsetX <= chunksViwedInDistance; offsetX++)
        {
            for(int offsetY = -chunksViwedInDistance; offsetY <= chunksViwedInDistance; offsetY++)
            {
                Vector2Int viewedChunkCoord = new Vector2Int(currentCurrentChunkCoordx + offsetX, currentCurrentChunkCoordy + offsetY);

                if(terrainChunkDictionary.ContainsKey(viewedChunkCoord))
                {
                    terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
                }
                else
                {
                    terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunckSize, detailLevels, transform, mapMaterial));
                }
            }
        }
    }

    public class TerrainChunk
    {
        GameObject meshObject;
        Vector2 position;
        Bounds bounds;

        MapData mapData;
        bool mapDataReceived;

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;

        LevelOfDetailInfo[] detailLevels;
        LevelOfDetailMesh[] detailLevelMeshes;

        int previousLevelOfDetail = -1;

        public TerrainChunk(Vector2 a_coord, int size, LevelOfDetailInfo[] a_detailLevels, Transform parent, Material a_material)
        {
            detailLevels = a_detailLevels;

            position = a_coord * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();

            meshRenderer.material = a_material;

            meshObject.transform.position = positionV3;
            meshObject.transform.parent = parent;
            SetVisible(false);

            detailLevelMeshes = new LevelOfDetailMesh[detailLevels.Length];
            for(int i = 0; i<detailLevels.Length; i++)
            {
                detailLevelMeshes[i] = new LevelOfDetailMesh(detailLevels[i].lod, UpdateTerrainChunk);
            }

            mapGenerator.RequestMapData(OnMapDatareceived, position);
        }

        void OnMapDatareceived(MapData a_mapData)
        {
            mapData = a_mapData;
            mapDataReceived = true;

            Texture2D texture = TextureGenerator.TextureFromColorMap(mapData.colorMap, MapGenerator.chunkSize, MapGenerator.chunkSize);
            meshRenderer.material.mainTexture = texture;

            UpdateTerrainChunk();
        }

        void OnMeshDataReceived(MeshData a_meshData)
        {
            meshFilter.mesh = a_meshData.CreateMesh();
        }

        public void UpdateTerrainChunk()
        {
            if(mapDataReceived)
            {
                float viewerDistanceFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viwerPosition));
                bool visible = viewerDistanceFromNearestEdge <= maxViewDistance;

                if (visible)
                {
                    int lodIndex = 0;
                    for (int i = 0; i < detailLevels.Length - 1; i++)
                    {
                        if (viewerDistanceFromNearestEdge > detailLevels[i].visibleDistanceThreshold)
                        {
                            lodIndex = i + 1;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (lodIndex != previousLevelOfDetail)
                    {
                        LevelOfDetailMesh lodMesh = detailLevelMeshes[lodIndex];
                        if (lodMesh.hasMesh)
                        {
                            meshFilter.mesh = lodMesh.mesh;
                            previousLevelOfDetail = lodIndex;
                        }
                        else if (!lodMesh.hasRequestedMesh)
                        {
                            lodMesh.RequestMesh(mapData);
                        }
                    }

                    terrainChunksVisibleLastUpdate.Add(this);
                }

                SetVisible(visible);
            }
        }

        public void SetVisible(bool a_visible)
        {
            meshObject.SetActive(a_visible);
        }

        public bool IsVisible()
        {
            return meshObject.activeSelf;
        }
    }

    class LevelOfDetailMesh
    {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        int lod;
        System.Action updateCallback;

        public LevelOfDetailMesh(int a_lod, System.Action a_updateCallback)
        {
            lod = a_lod;
            updateCallback = a_updateCallback;
        }

        void OnMeshDataReceived(MeshData a_meshData)
        {
            mesh = a_meshData.CreateMesh();
            hasMesh = true;

            updateCallback();
        }

        public void RequestMesh(MapData a_mapData)
        {
            hasRequestedMesh = true;
            mapGenerator.RequestMeshData(a_mapData, lod,  OnMeshDataReceived);
        }
    }

    [System.Serializable]
    public struct LevelOfDetailInfo
    {
        public int lod;
        public float visibleDistanceThreshold;
    }
}
