using UnityEngine;

public class HexGrid : MonoBehaviour
{
    [Header("Hex Rendering Options")]
    [SerializeField] Color baseColor; // Background colour
    [SerializeField] Color hexColor; // Hexagon colour
    [SerializeField] [Range(0.0f, 1.0f)] float lineThickness; // Thickness of hexagon lines
    [SerializeField] [Min(0)] int hexScale;
    [SerializeField] [Min(0)] Vector2Int gridDim; // Desired dimensions of the grid
    [SerializeField] bool useFixedDim; // Whether or not the grid dimensions are set manually
    [SerializeField] bool clipEdges; // Whether partial hexagons outside of the grid should be drawn

    [Header("Entity Options")]
    [SerializeField] GameObject characterObj;

    Vector2Int gridSize; // The dimensions of the grid
    Vector3 gridOrigin; // The position of the center of the (0, 0) hex in world space
    Vector3 mouseWorldPos;
    Vector3 activeHexPos;
    Material hexMat; // The material which renders the hex grid
    GameObject character;
    Camera cam;

    void Start()
    {
        cam = Camera.main;
        hexMat = GetComponent<MeshRenderer>().material;
        gridOrigin = GetComponent<Collider>().bounds.max - new Vector3(1.0f, 0.0f, 1.5f);

        character = Instantiate(characterObj, gridOrigin, Quaternion.identity);
    }

    void Update()
    {
        gridOrigin = GetComponent<Collider>().bounds.max - new Vector3(1.0f, 0.0f, 1.5f);
        mouseWorldPos = cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, cam.nearClipPlane + cam.transform.position.y));
        activeHexPos = WorldToGrid(mouseWorldPos);

        if (Input.GetMouseButtonDown(0))
        {
            character.transform.position = GridToWorld(activeHexPos);
        }

        SetShaderParams();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(mouseWorldPos, 0.2f);
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.position, 0.2f); // Debug, draw point at center of grid

        // Debug, draw point at every sphere center
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                Gizmos.DrawSphere(GridToWorld(new Vector3(x, 0.0f, y)), 0.2f);
            }
        }
    }

    // Takes world space pos and returns grid space pos
    public Vector3 WorldToGrid(Vector3 worldPos)
    {
        float s = 1.7320508f;
        int YCoord = (int)((gridOrigin.z+(s/2.0f) - worldPos.z)/s);
        int XCoord = (int)((gridOrigin.x+1.0f - (YCoord%2==0 ? 0.0f : 1.0f) - worldPos.x)/2.0f);

        return new Vector3(XCoord, 0.0f, YCoord);
    }

    // Takes grid space pos and return world space pos (center of the hex)
    public Vector3 GridToWorld(Vector3 gridPos)
    {
        return gridOrigin - new Vector3(gridPos.x*2.0f + (gridPos.z%2==0 ? 0.0f : 1.0f), 0.0f, gridPos.z*1.7320508f);
    }

    void SetShaderParams()
    {
        if (useFixedDim)
        {
            gridSize = gridDim;
            hexMat.SetFloat("_HexScale", 1.0f);
            hexMat.SetVector("_GridScale", new Vector4(gridDim.x, gridDim.y, 0.0f, 0.0f));
            hexMat.SetVector("_GridDim", new Vector4(gridSize.x, gridSize.y, 0.0f, 0.0f));
        }
        else
        {
            gridSize = new Vector2Int((int)transform.localScale.x, (int)transform.localScale.z) * hexScale;
            hexMat.SetFloat("_HexScale", hexScale);
            hexMat.SetVector("_GridScale", new Vector4(transform.localScale.x, transform.localScale.z, 0.0f, 0.0f));
            hexMat.SetVector("_GridDim", new Vector4(gridSize.x, gridSize.y, 0.0f, 0.0f));
        }

        hexMat.SetFloat("_LineWeight", lineThickness);
        hexMat.SetColor("_BaseColor", baseColor);
        hexMat.SetColor("_HexColor", hexColor);
        hexMat.SetInt("_ClipEdges", clipEdges ? 1:0);

        hexMat.SetVector("_ActiveHex", new Vector4(activeHexPos.x, activeHexPos.z, 0.0f, 0.0f));   
    }
}
