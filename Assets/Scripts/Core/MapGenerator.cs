using UnityEngine;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour
{
    [Header("Map Configuration")]
    [SerializeField] private int mapWidth = 10;
    [SerializeField] private int mapHeight = 10;
    [SerializeField] private float tileSize = 1f;

    [Header("Prefab References")]
    [SerializeField] private List<GameObject> walkablePrefabs;
    [SerializeField] private List<GameObject> nonWalkablePrefabs;

    [Header("Map Layout")]
    [TextArea(10, 20)]
    [SerializeField] private string mapLayoutString = @"
0,0,0,0,0,0,0,0,0,0
0,1,1,1,1,1,1,1,1,0
0,1,1,2,1,1,3,1,1,0
0,1,1,1,1,1,1,1,1,0
0,1,1,1,4,4,1,1,1,0
0,1,1,1,4,4,1,1,1,0
0,1,1,1,1,1,1,1,1,0
0,1,3,1,1,1,1,2,1,0
0,1,1,1,1,1,1,1,1,0
0,0,0,0,0,0,0,0,0,0";

    private int[,] mapLayout;
    private GameObject[,] tileObjects;
    private bool[,] walkableGrid;  // ← Esta es la variable correcta

    public static MapGenerator Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        ParseMapLayout();
        GenerateMap();
    }

    void ParseMapLayout()
    {
        string[] lines = mapLayoutString.Trim().Split('\n');
        mapHeight = lines.Length;
        mapWidth = lines[0].Split(',').Length;

        mapLayout = new int[mapWidth, mapHeight];
        walkableGrid = new bool[mapWidth, mapHeight];
        tileObjects = new GameObject[mapWidth, mapHeight];

        for (int y = 0; y < mapHeight; y++)
        {
            string[] values = lines[y].Trim().Split(',');
            for (int x = 0; x < mapWidth; x++)
            {
                mapLayout[x, y] = int.Parse(values[x].Trim());
            }
        }
    }

    void GenerateMap()
    {
        GameObject mapContainer = new GameObject("Map");

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                Vector3 position = new Vector3(x * tileSize, 0, y * tileSize);
                GameObject tilePrefab = GetTilePrefab(mapLayout[x, y]);

                if (tilePrefab != null)
                {
                    GameObject tile = Instantiate(tilePrefab, position, Quaternion.identity, mapContainer.transform);
                    tile.name = $"Tile_{x}_{y}";
                    tileObjects[x, y] = tile;

                    // Determinar si es caminable (1 = walkable)
                    walkableGrid[x, y] = mapLayout[x, y] == 1;
                }
            }
        }

        Debug.Log($"Mapa generado: {mapWidth}x{mapHeight}");
    }

    GameObject GetTilePrefab(int tileType)
    {
        switch (tileType)
        {
            case 0:
                return GetRandomPrefab(nonWalkablePrefabs);
            case 1:
                return GetRandomPrefab(walkablePrefabs);
            case 2:
                return GetPrefabByName("CuboNWROCK1");
            case 3:
                return GetPrefabByName("CuboNWTREE1");
            case 4:
                return GetPrefabByName("CuboNWROCK2");
            default:
                return GetRandomPrefab(walkablePrefabs);
        }
    }

    GameObject GetRandomPrefab(List<GameObject> prefabList)
    {
        if (prefabList != null && prefabList.Count > 0)
        {
            return prefabList[Random.Range(0, prefabList.Count)];
        }
        return null;
    }

    GameObject GetPrefabByName(string name)
    {
        foreach (var prefab in nonWalkablePrefabs)
        {
            if (prefab != null && prefab.name.Contains(name))
                return prefab;
        }
        foreach (var prefab in walkablePrefabs)
        {
            if (prefab != null && prefab.name.Contains(name))
                return prefab;
        }
        return null;
    }

    public bool IsWalkable(int x, int y)
    {
        // Validar que walkableGrid esté inicializado
        if (walkableGrid == null)
        {
            Debug.LogWarning("WalkableGrid no está inicializado aún");
            return false;
        }

        // Validar límites
        if (x < 0 || x >= mapWidth || y < 0 || y >= mapHeight)
            return false;

        // Retornar si la celda es caminable
        return walkableGrid[x, y];
    }

    public int GetMapWidth()
    {
        return mapWidth;
    }

    public int GetMapHeight()
    {
        return mapHeight;
    }

    public Vector3 GetWorldPosition(int x, int y)
    {
        return new Vector3(x * tileSize, 0, y * tileSize);
    }

    public Vector2Int GetGridPosition(Vector3 worldPosition)
    {
        int x = Mathf.RoundToInt(worldPosition.x / tileSize);
        int y = Mathf.RoundToInt(worldPosition.z / tileSize);
        return new Vector2Int(x, y);
    }
}