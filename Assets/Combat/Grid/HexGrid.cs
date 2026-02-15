using UnityEngine;

public class HexGrid : MonoBehaviour
{
    [Header("Hex Rendering Options")]
    [SerializeField] Color baseColor;
    [SerializeField] Color hexColor;
    [SerializeField][Range(0.0f, 1.0f)] float lineThickness = 0.1f;
    [SerializeField][Min(1)] int hexScale = 5;
    [SerializeField][Min(1)] Vector2Int gridDim = new Vector2Int(8, 8);
    [SerializeField] bool clipEdges = true;

    [Header("Combat Prefabs")]
    [SerializeField] GameObject playerPrefab;
    [SerializeField] GameObject fallbackEnemyPrefab;

    private Vector2Int gridSize;
    private Vector3 gridOrigin;
    private Vector3 hexSize;

    private Vector3 mouseWorldPos;
    private Vector3 activeHexPos;

    private Material hexMat;
    private Camera cam;

    private GameObject playerInstance;
    private GameObject enemyInstance;

    private const float canonicalSize = 10.0f;
    private const float SQRT3 = 1.7320508f;

    void Start()
    {
        cam = Camera.main;
        hexMat = GetComponent<MeshRenderer>().material;

        CalculateGridMetrics();
        SpawnPlayer();
        SpawnEnemyFromTransition();
    }

    void Update()
    {
        CalculateGridMetrics();
        HandleMouseInput();
        SetShaderParams();
        CheckEndCondition();
    }

    #region Initialization

    void CalculateGridMetrics()
    {
        hexSize = new Vector3(
            canonicalSize / hexScale,
            0.0f,
            canonicalSize / hexScale * SQRT3 / 2.0f
        );

        gridOrigin = GetComponent<Collider>().bounds.max
            - new Vector3(hexSize.x / 2.0f, 0.0f, hexSize.z);

        gridSize = gridDim;

        transform.localScale = new Vector3(
            (float)gridDim.x / hexScale + (hexSize.x / canonicalSize / 2.0f),
            transform.localScale.y,
            (float)((hexSize.z * gridDim.y + hexSize.z) / canonicalSize)
        );
    }

    void SpawnPlayer()
    {
        GameObject existingPlayer = GameObject.FindGameObjectWithTag("Player");

        if (existingPlayer != null)
        {
            playerInstance = existingPlayer;
        }
        else
        {
            playerInstance = Instantiate(playerPrefab, gridOrigin, Quaternion.identity);
        }
    }

    void SpawnEnemyFromTransition()
    {
        string enemyTag = CombatTransitionData.Instance.ennemyType;

        GameObject prefab = null;

        if (TagToPrefab.Instance != null)
        {
            prefab = TagToPrefab.Instance.GetPrefabForTag(enemyTag);
        }

        if (prefab == null)
        {
            prefab = fallbackEnemyPrefab;
            Debug.LogWarning($"Enemy prefab not found for tag: {enemyTag}");
        }

        Vector3 spawnGridPos = new Vector3(gridDim.x / 2, 0, gridDim.y / 2);
        Vector3 spawnWorldPos = GridToWorld(spawnGridPos);

        enemyInstance = Instantiate(prefab, spawnWorldPos, Quaternion.identity);
    }

    #endregion

    #region Input

    void HandleMouseInput()
    {
        mouseWorldPos = cam.ScreenToWorldPoint(
            new Vector3(
                Input.mousePosition.x,
                Input.mousePosition.y,
                cam.nearClipPlane + cam.transform.position.y
            )
        );

        activeHexPos = WorldToGrid(mouseWorldPos);

        if (Input.GetMouseButtonDown(0))
        {
            playerInstance.transform.position = GridToWorld(activeHexPos);
        }
    }

    #endregion

    #region Combat Check

    void CheckEndCondition()
    {
        if (playerInstance == null || enemyInstance == null)
            return;

        Vector3 playerGrid = WorldToGrid(playerInstance.transform.position);
        Vector3 enemyGrid = WorldToGrid(enemyInstance.transform.position);

        if (playerGrid == enemyGrid)
        {
            Debug.Log("Combat resolved — transitioning back to Exploration.");
            GameStateManager.Instance.TransitionToExploration();
        }
    }

    #endregion

    #region Grid Math

    public Vector3 WorldToGrid(Vector3 worldPos)
    {
        int y = (int)((gridOrigin.z + (hexSize.z / 2.0f) - worldPos.z) / hexSize.z);
        int x = (int)((gridOrigin.x + (hexSize.x / 2.0f)
                    - (y % 2 == 0 ? 0.0f : hexSize.x / 2.0f)
                    - worldPos.x) / hexSize.x);

        return new Vector3(x, 0.0f, y);
    }

    public Vector3 GridToWorld(Vector3 gridPos)
    {
        return gridOrigin - new Vector3(
            gridPos.x * hexSize.x + (gridPos.z % 2 == 0 ? 0.0f : hexSize.x / 2.0f),
            0.0f,
            gridPos.z * hexSize.z
        );
    }

    public Vector3 OffsetToAxial(Vector3 offsetPos)
    {
        float parity = offsetPos.z % 2;
        return new Vector3(
            offsetPos.x - (offsetPos.z - parity) / 2.0f,
            0.0f,
            offsetPos.z
        );
    }

    public float AxialDistance(Vector3 A, Vector3 B)
    {
        Vector3 d = A - B;
        return (Mathf.Abs(d.x) + Mathf.Abs(d.x + d.z) + Mathf.Abs(d.z)) / 2.0f;
    }

    #endregion

    #region Shader

    void SetShaderParams()
    {
        hexMat.SetFloat("_HexScale", hexScale);
        hexMat.SetVector("_GridScale", new Vector4(transform.localScale.x, transform.localScale.z, 0, 0));
        hexMat.SetVector("_GridDim", new Vector4(gridSize.x, gridSize.y, 0, 0));
        hexMat.SetFloat("_LineWeight", lineThickness);
        hexMat.SetColor("_BaseColor", baseColor);
        hexMat.SetColor("_HexColor", hexColor);
        hexMat.SetInt("_ClipEdges", clipEdges ? 1 : 0);
        hexMat.SetVector("_ActiveHex", new Vector4(activeHexPos.x, activeHexPos.z, 0, 0));
    }

    #endregion
}
