using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GridCell
{
    public GameObject cellObject;
    public MeshRenderer meshRenderer;
    public Vector2Int gridPosition;
    public Color originalColor;
}

public class GridVisualizer : MonoBehaviour
{
    [Header("Grid Visual Settings")]
    [SerializeField] private GameObject gridCellPrefab;
    [SerializeField] private Material gridMaterial;
    [SerializeField] private float gridHeight = 0.55f;  // ← CAMBIAR de 0.01f a 0.55f
    [SerializeField] private float borderWidth = 0.02f;

    [Header("Grid Colors")]
    [SerializeField] private Color defaultColor = new Color(0f, 1f, 0f, 0.7f);  // ← CAMBIAR: Verde más visible
    [SerializeField] private Color walkableColor = new Color(0f, 1f, 0f, 0.8f);
    [SerializeField] private Color notReachableColor = new Color(1f, 0f, 0f, 0.8f);
    [SerializeField] private Color spellRangeColor = new Color(0f, 0.5f, 1f, 0.8f);
    [SerializeField] private Color highlightColor = new Color(1f, 1f, 0f, 0.8f);

    private GridCell[,] gridCells;
    private Dictionary<Vector2Int, GameObject> gridTiles = new Dictionary<Vector2Int, GameObject>();
    private Dictionary<Vector2Int, Renderer> tileRenderers = new Dictionary<Vector2Int, Renderer>();

    public static GridVisualizer Instance { get; private set; }

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
        StartCoroutine(InitializeWhenReady());
    }

    IEnumerator InitializeWhenReady()
    {
        while (MapGenerator.Instance == null || MapGenerator.Instance.GetMapWidth() == 0)
        {
            yield return null;
        }

        yield return new WaitForSeconds(0.1f); // Esperar un poco más para asegurar todo está listo

        CreateGridVisual();
    }

    void CreateGridVisual()
    {
        if (MapGenerator.Instance == null)
        {
            Debug.LogError("MapGenerator.Instance es null!");
            return;
        }

        int width = MapGenerator.Instance.GetMapWidth();
        int height = MapGenerator.Instance.GetMapHeight();

        if (width == 0 || height == 0)
        {
            Debug.LogError("MapGenerator no tiene dimensiones válidas!");
            return;
        }

        gridCells = new GridCell[width, height];

        GameObject gridContainer = new GameObject("GridVisualizer");
        gridContainer.transform.parent = transform;
        gridContainer.transform.position = Vector3.zero; // ← Asegurar posición en origen

        int tilesCreated = 0;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (MapGenerator.Instance.IsWalkable(x, y))
                {
                    if (gridCellPrefab != null)
                    {
                        Vector3 worldPos = MapGenerator.Instance.GetWorldPosition(x, y);
                        worldPos.y = gridHeight; // ← Usar el nuevo gridHeight

                        GameObject cellObj = Instantiate(gridCellPrefab, worldPos, Quaternion.identity, gridContainer.transform);
                        cellObj.name = $"GridCell_{x}_{y}";

                        GridCell cell = new GridCell
                        {
                            cellObject = cellObj,
                            meshRenderer = cellObj.GetComponent<MeshRenderer>(),
                            gridPosition = new Vector2Int(x, y)
                        };

                        if (cell.meshRenderer != null)
                        {
                            cell.originalColor = cell.meshRenderer.material.color;
                        }

                        gridCells[x, y] = cell;
                        tilesCreated++;
                    }
                    else
                    {
                        CreateGridTile(x, y, gridContainer.transform);
                        tilesCreated++;
                    }
                }
            }
        }

        Debug.Log($"Grid visual creado: {width}x{height} celdas ({tilesCreated} tiles visibles creadas)");
    }

    void CreateGridTile(int x, int y, Transform parent)
    {
        Vector2Int gridPos = new Vector2Int(x, y);
        Vector3 worldPos = MapGenerator.Instance.GetWorldPosition(x, y);
        worldPos.y = gridHeight;

        GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Quad);
        tile.name = $"GridTile_{x}_{y}";
        tile.transform.position = worldPos;
        tile.transform.rotation = Quaternion.Euler(90, 0, 0);
        tile.transform.localScale = new Vector3(0.95f, 0.95f, 1f);
        tile.transform.parent = parent;

        // ← ASEGURAR QUE EL TILE SEA VISIBLE
        tile.layer = 0; // Default layer

        Renderer renderer = tile.GetComponent<Renderer>();

        // Usar shader URP si está disponible, sino usar Standard
        Material mat;
        if (gridMaterial != null)
        {
            mat = new Material(gridMaterial);
        }
        else
        {
            // Intentar usar shader URP primero
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            mat = new Material(shader);

            // Configurar transparencia
            mat.SetFloat("_Mode", 3); // Transparent mode
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHABLEND_ON");
            mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }

        mat.color = defaultColor;
        renderer.material = mat;

        // ← ASEGURAR QUE EL RENDERER ESTÉ HABILITADO
        renderer.enabled = true;

        gridTiles[gridPos] = tile;
        tileRenderers[gridPos] = renderer;

        // NO crear bordes por ahora para simplificar la visualización
        // CreateBorders(tile.transform, worldPos);

        Debug.Log($"Tile creado en: {worldPos} con color: {defaultColor}");
    }

    void CreateBorders(Transform tileTransform, Vector3 position)
    {
        CreateBorder(tileTransform, new Vector3(0, 0, 0.5f), new Vector3(1f, borderWidth, 1f));
        CreateBorder(tileTransform, new Vector3(0, 0, -0.5f), new Vector3(1f, borderWidth, 1f));
        CreateBorder(tileTransform, new Vector3(-0.5f, 0, 0), new Vector3(borderWidth, 1f, 1f));
        CreateBorder(tileTransform, new Vector3(0.5f, 0, 0), new Vector3(borderWidth, 1f, 1f));
    }

    void CreateBorder(Transform parent, Vector3 localPos, Vector3 scale)
    {
        GameObject border = GameObject.CreatePrimitive(PrimitiveType.Cube);
        border.transform.parent = parent;
        border.transform.localPosition = localPos;
        border.transform.localScale = scale;
        border.transform.localRotation = Quaternion.identity;

        Renderer renderer = border.GetComponent<Renderer>();
        renderer.material.color = Color.white;

        Destroy(border.GetComponent<Collider>());
    }

    public void ShowMovementRange(Vector2Int playerPos, int movementPoints)
    {
        if (PathfindingSystem.Instance == null)
        {
            Debug.LogWarning("PathfindingSystem.Instance es null");
            return;
        }

        ResetGridColors();
        List<Vector2Int> reachableTiles = PathfindingSystem.Instance.GetReachableTiles(playerPos, movementPoints);

        Debug.Log($"Mostrando {reachableTiles.Count} tiles alcanzables desde {playerPos}");

        foreach (Vector2Int tile in reachableTiles)
        {
            SetTileColor(tile, walkableColor);
        }
    }

    public void ShowPathToTarget(Vector2Int startPos, Vector2Int targetPos, int movementPoints)
    {
        if (PathfindingSystem.Instance == null) return;

        ResetGridColors();
        List<Vector2Int> path = PathfindingSystem.Instance.FindPath(startPos, targetPos);

        if (path != null)
        {
            for (int i = 0; i < path.Count; i++)
            {
                if (i <= movementPoints)
                {
                    SetTileColor(path[i], walkableColor);
                }
                else
                {
                    SetTileColor(path[i], notReachableColor);
                }
            }
        }
    }

    public void ShowSpellRange(Vector2Int casterPos, SpellData spell)
    {
        ResetGridColors();
        List<Vector2Int> spellTiles = GetSpellRangeTiles(casterPos, spell);

        foreach (Vector2Int tile in spellTiles)
        {
            SetTileColor(tile, spellRangeColor);
        }
    }

    List<Vector2Int> GetSpellRangeTiles(Vector2Int casterPos, SpellData spell)
    {
        List<Vector2Int> tiles = new List<Vector2Int>();

        switch (spell.spellType)
        {
            case SpellType.SingleTarget:
                for (int x = -spell.range; x <= spell.range; x++)
                {
                    for (int y = -spell.range; y <= spell.range; y++)
                    {
                        Vector2Int tile = new Vector2Int(casterPos.x + x, casterPos.y + y);
                        if (Mathf.Abs(x) + Mathf.Abs(y) <= spell.range)
                        {
                            tiles.Add(tile);
                        }
                    }
                }
                break;

            case SpellType.Line:
                for (int i = 1; i <= spell.range; i++)
                {
                    tiles.Add(new Vector2Int(casterPos.x + i, casterPos.y));
                    tiles.Add(new Vector2Int(casterPos.x - i, casterPos.y));
                    tiles.Add(new Vector2Int(casterPos.x, casterPos.y + i));
                    tiles.Add(new Vector2Int(casterPos.x, casterPos.y - i));
                }
                break;

            case SpellType.Area:
                for (int x = -spell.areaSize; x <= spell.areaSize; x++)
                {
                    for (int y = -spell.areaSize; y <= spell.areaSize; y++)
                    {
                        Vector2Int tile = new Vector2Int(casterPos.x + x, casterPos.y + y);
                        tiles.Add(tile);
                    }
                }
                break;
        }

        return tiles;
    }

    public void HighlightTile(Vector2Int gridPos)
    {
        SetTileColor(gridPos, highlightColor);
    }

    public void SetTileColor(Vector2Int gridPos, Color color)
    {
        if (tileRenderers.ContainsKey(gridPos))
        {
            tileRenderers[gridPos].material.color = color;
            Debug.Log($"Color de tile {gridPos} cambiado a {color}");
        }
        else
        {
            Debug.LogWarning($"No se encontró renderer para tile en {gridPos}");
        }
    }

    public void ResetGridColors()
    {
        foreach (var kvp in tileRenderers)
        {
            kvp.Value.material.color = defaultColor;
        }
    }

    public void ShowTile(Vector2Int gridPos, bool show)
    {
        if (gridTiles.ContainsKey(gridPos))
        {
            gridTiles[gridPos].SetActive(show);
        }
    }
}