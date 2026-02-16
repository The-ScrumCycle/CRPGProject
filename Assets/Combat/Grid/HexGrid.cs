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
    [SerializeField] GameObject player;

    [Header("Ennemy Options")]
    [SerializeField] GameObject ennemyObj;
    [SerializeField] GameObject ennemy;


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

        // when loads, place player -- Carlos Change
        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            character = player;
        }
        else
        {
            character = Instantiate(characterObj, gridOrigin, Quaternion.identity);
        }


        // set ennemy position -- Carlos Change
        string tag = CombatTransitionData.Instance.ennemyType;
        ennemy     = TagToPrefab.Instance.GetPrefabForTag(tag);

        if (ennemy != null)
        {
            ennemy = Instantiate(ennemy, GridToWorld(new Vector3(3.0f, 0.0f, 3.0f)), Quaternion.identity);
        }
        else
        {
            ennemy = Instantiate(ennemyObj, GridToWorld(new Vector3(3.0f, 0.0f, 3.0f)), Quaternion.identity);
            Debug.Log("Ennemy prefab not found for tag: " + tag);
        }


    }

    void Update()
    {
        gridOrigin = GetComponent<Collider>().bounds.max - new Vector3(1.0f, 0.0f, 1.5f);
        mouseWorldPos = cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, cam.nearClipPlane + cam.transform.position.y));
        activeHexPos = WorldToGrid(mouseWorldPos);

        if (Input.GetMouseButtonDown(0))
        {
            character.transform.position = GridToWorld(activeHexPos);
            //Debug.Log(activeHexPos);
        }

        Debug.Log(AxialDistance(OffsetToAxial(activeHexPos), OffsetToAxial(WorldToGrid(character.transform.position))));

        SetShaderParams();


        TestTransition();


    }

    void TestTransition()
    {
        // if both player and ennemy in same position, transition to exploration - Carlos Change 

        if (ennemy == null) return;
        if (character == null) return;

        if (ennemy.transform.position == character.transform.position)
        {
            Debug.Log("Transitioning to Exploration");
            GameStateManager.Instance.TransitionToExploration();
        }
    }

void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(mouseWorldPos, 0.2f); // Debug, draw point at mouse pos
        Gizmos.color = Color.green;

        // Debug, draw point at every hex center
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                Gizmos.DrawSphere(GridToWorld(new Vector3(x, 0.0f, y)), 0.2f);
            }
        }

        if (Application.isPlaying) Gizmos.DrawLine(character.transform.position, GridToWorld(activeHexPos));

        Gizmos.color = Color.red;
        foreach (Vector3 gridPos in GetNeighbours(activeHexPos))
        {
            Gizmos.DrawSphere(GridToWorld(gridPos), 0.2f);
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

    // Takes a position in offset coordinates (typical grid position) and converts to axial coordinates
    // This makes some algorithms easier
    public Vector3 OffsetToAxial(Vector3 offsetPos)
    {
        float parity = offsetPos.z%2;
        return new Vector3(
            offsetPos.x - (offsetPos.z - parity) / 2.0f, 
            0.0f, 
            offsetPos.z);
    }

    // Takes a position in axial coordinates and converts to offset coordinates
    public Vector3 AxialToOffset(Vector3 axialPos)
    {
        float parity = axialPos.z%2;
        return new Vector3(
            axialPos.x - (axialPos.z - parity) / 2.0f, 
            0.0f, 
            axialPos.z);
    }

    // Takes two positions in axial coordinates and calculates distance between them
    public float AxialDistance(Vector3 A, Vector3 B)
    {
        Vector3 inter = A - B;
        return (Mathf.Abs(inter.x) + Mathf.Abs(inter.x + inter.z) + Mathf.Abs(inter.z)) / 2.0f;
    }
    
    // Get all adjacent grid positions to a given grid position
    public Vector3[] GetNeighbours(Vector3 gridPos)
    {
        Vector3[] neighbours = new Vector3[6];

        Vector3[] evenOffsets = {new Vector3(1.0f, 0.0f, 0.0f), new Vector3(-1.0f, 0.0f, 0.0f),
                                new Vector3(0.0f, 0.0f, 1.0f), new Vector3(0.0f, 0.0f, -1.0f),
                                new Vector3(-1.0f, 0.0f, 1.0f), new Vector3(-1.0f, 0.0f, -1.0f)};
        Vector3[] oddOffsets = {new Vector3(1.0f, 0.0f, 0.0f), new Vector3(-1.0f, 0.0f, 0.0f),
                                new Vector3(0.0f, 0.0f, 1.0f), new Vector3(0.0f, 0.0f, -1.0f),
                                new Vector3(1.0f, 0.0f, -1.0f), new Vector3(1.0f, 0.0f, 1.0f)};

        for (int i = 0; i < 6; i++)
        {
            neighbours[i] = gridPos + (gridPos.z%2==0 ? evenOffsets[i] : oddOffsets[i]);
        }

        return neighbours;
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
