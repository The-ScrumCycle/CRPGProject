using UnityEngine;
using System.Collections.Generic;

public class GridEntity
{
    public HexGrid grid; // The grid that this entity is a member of
    public GameObject obj; // The game object that represents this grid object
    public Vector2Int pos; // Grid position of this object

    public GridEntity(HexGrid pGrid, GameObject pObj, Vector2Int pPos)
    {
        grid = pGrid;
        obj = pObj;
        pos = pPos;
    }

    // Get Rid of it Entirely
    public void DeleteEntity()
    {
        Object.Destroy(obj);
    }

    // Move from current position to new position
    public void Move(Vector2Int newPos)
    {
        if (grid.Move(pos, newPos))
        {
            obj.transform.position = grid.GridToWorld(newPos);
            pos = newPos;
        }
    }
}

// A single hexagon in the grid
public class HexCell
{
    public Vector2Int pos; // Position within the grid
    public GridEntity occupant; // the entity currently in this cell

    public HexCell(Vector2Int pPos)
    {
        pos = pPos;
        occupant = null;
    }

    public HexCell(Vector2Int pPos, GridEntity pOccupant)
    {
        pos = pPos;
        occupant = pOccupant;
    }
}

public class HexGrid : MonoBehaviour
{
    [Header("Hex Rendering Options")]
    [SerializeField] Color baseColor; // Background colour
    [SerializeField] Color hexColor; // Hexagon colour
    [SerializeField] [Range(0.0f, 1.0f)] float lineThickness; // Thickness of hexagon lines
    [SerializeField] [Min(0)] int hexScale; // Size of hexagons (bigger is smaller)
    [SerializeField] [Min(0)] Vector2Int gridDim; // dimensions of the grid
    [SerializeField] bool clipEdges; // Whether partial hexagons outside of the grid should be drawn

    const float canonicalSize = 10.0f; // The world space size of the plane mesh at default scale
    const float s = 1.7320508f; // Sqrt(3), length of hexagon side 

    List<HexCell> cells; // All hexagons in the grid 

    Vector3 gridOrigin; // The position of the center of the (0, 0) hex in world space
    Vector3 hexSize; // The world-space dimensions of a single hexagon
    Vector3 mouseWorldPos; // World-space position of mouse 
    Vector2Int activeHexPos; // The Grid-space position of currently selected hex
    Material hexMat; // The material which renders the hex grid
    Camera cam;

    void Awake()
    {
        cam = Camera.main;
        hexMat = GetComponent<MeshRenderer>().material;

        hexSize = new Vector3(canonicalSize/hexScale, 0.0f, canonicalSize/hexScale * s / 2.0f);

        // Adjust transform to fit grid
        transform.localScale = new Vector3(
            (float)gridDim.x/hexScale + (hexSize.x/canonicalSize/2.0f), 
            transform.localScale.y, 
            (float)((hexSize.z*gridDim.y + hexSize.z)/canonicalSize));
        Physics.SyncTransforms(); // Force collider to update

        gridOrigin = GetComponent<Collider>().bounds.max - new Vector3(hexSize.x/2.0f, 0.0f, hexSize.z);

        InitGrid();
    }

    void Update()
    {
        hexSize = new Vector3(canonicalSize/hexScale, 0.0f, canonicalSize/hexScale * s / 2.0f);

        // Adjust transform to fit grid
        transform.localScale = new Vector3(
            (float)gridDim.x/hexScale + (hexSize.x/canonicalSize/2.0f), 
            transform.localScale.y, 
            (float)((hexSize.z*gridDim.y + hexSize.z)/canonicalSize));
        Physics.SyncTransforms(); // Force collider to update

        gridOrigin = GetComponent<Collider>().bounds.max - new Vector3(hexSize.x/2.0f, 0.0f, hexSize.z);

        mouseWorldPos = cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, cam.nearClipPlane + cam.transform.position.y));
        activeHexPos = WorldToGrid(mouseWorldPos);

        SetShaderParams();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(mouseWorldPos, 0.2f); // Draw point at mouse pos

        Gizmos.color = Color.green;

        // Draw point at every hex center
        for (int x = 0; x < gridDim.x; x++)
        {
            for (int y = 0; y < gridDim.y; y++)
            {
                Gizmos.DrawSphere(GridToWorld(new Vector2Int(x, y)), 1.0f/hexScale);
            }
        }

        // Draw point at all neighbours of active hex
        Gizmos.color = Color.red;
        foreach (Vector2Int gridPos in GetNeighbours(activeHexPos))
        {
            Gizmos.DrawSphere(GridToWorld(gridPos), 1.0f/hexScale);
        }

        // Draw point at grid origin
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(gridOrigin, 1.0f/hexScale);
    }

    public Vector2Int GetActiveHexPos()
    {
        return activeHexPos;
    }

    // Initialize list of cell objects
    void InitGrid()
    {
        cells = new List<HexCell>();
        for (int x = 0; x < gridDim.x; x++)
        {
            for (int y = 0; y < gridDim.y; y++)
            {
                cells.Add(new HexCell(new Vector2Int(x, y)));
            }
        }
    }

    // Return HexCell object at given position
    HexCell GetHex(Vector2Int gridPos)
    {
        return cells[gridPos.y + gridDim.y*gridPos.x];
    }

    // Return true if given position is within grid bounds
    public bool IsInBounds(Vector2Int gridPos)
    {
        return 
            gridPos.x >= 0 &&
            gridPos.y >= 0 &&
            gridPos.x < gridDim.x &&
            gridPos.y < gridDim.y;
    }

    // Add an entity to the grid
    public bool AddOccupant(GridEntity newOccupant)
    {
        if (!IsInBounds(newOccupant.pos)) return false;

        HexCell cellAtPos = GetHex(newOccupant.pos);
        if (cellAtPos.occupant == null)
        {
            cellAtPos.occupant = newOccupant;
            return true;
        }
        else
        {
            return false;
        }
    }

    // Remove an entity from the grid
    public bool RemoveOccupant(GridEntity oldOccupant)
    {
        if (!IsInBounds(oldOccupant.pos)) return false;

        HexCell cellAtPos = GetHex(oldOccupant.pos);
        if (cellAtPos.occupant == oldOccupant)
        {
            cellAtPos.occupant.DeleteEntity();
            cellAtPos.occupant = null;
            return true;
        }
        else
        {
            return false;
        }
    }

    // Remove entity from the grid at given position
    public bool RemoveOccupant(Vector2Int gridPos)
    {
        if (!IsInBounds(gridPos)) return false;
    
        HexCell cellAtPos = GetHex(gridPos);
        if (cellAtPos.occupant == null)
        {
            return false;
        }
        else
        {
            cellAtPos.occupant.DeleteEntity();
            cellAtPos.occupant = null;
            return true;
        }
    }

    // Move entity from startPos to endPos
    public bool Move(Vector2Int startPos, Vector2Int endPos)
    {
        if (!IsInBounds(startPos) || !IsInBounds(endPos)) return false;

        HexCell startCell = GetHex(startPos);
        HexCell endCell = GetHex(endPos);
        if (startCell.occupant != null && endCell.occupant == null)
        {
            endCell.occupant = startCell.occupant;
            startCell.occupant = null;
            return true;
        }
        else
        {
            return false;
        }
    }

    // Takes world space pos and returns grid space pos
    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        int YCoord = (int)((gridOrigin.z+(hexSize.z/2.0f) - worldPos.z)/hexSize.z);
        int XCoord = (int)((gridOrigin.x+(hexSize.x/2.0f) - (YCoord%2==0 ? 0.0f : hexSize.x/2.0f) - worldPos.x)/hexSize.x);

        return new Vector2Int(XCoord, YCoord);
    }

    // Takes grid space pos and return world space pos (center of the hex)
    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        return gridOrigin - new Vector3(gridPos.x*hexSize.x + (gridPos.y%2==0 ? 0.0f : hexSize.x/2.0f), 0.0f, gridPos.y*hexSize.z);
    }

    // Takes a position in offset coordinates (typical grid position) and converts to axial coordinates
    // This makes some algorithms easier
    public static Vector2Int OffsetToAxial(Vector2Int offsetPos)
    {
        float parity = offsetPos.y%2;
        return new Vector2Int(
            (int)(offsetPos.x - (offsetPos.y - parity) / 2.0f),
            offsetPos.y);
    }

    // Takes a position in axial coordinates and converts to offset coordinates
    public static Vector2Int AxialToOffset(Vector2Int axialPos)
    {
        float parity = axialPos.y%2;
        return new Vector2Int(
            (int)(axialPos.x - (axialPos.y - parity) / 2.0f), 
            axialPos.y);
    }

    // Takes two positions in axial coordinates and calculates distance between them
    public static float AxialDistance(Vector2Int A, Vector2Int B)
    {
        Vector2Int inter = A - B;
        return (Mathf.Abs(inter.x) + Mathf.Abs(inter.x + inter.y) + Mathf.Abs(inter.y)) / 2.0f;
    }
    
    // Get all adjacent grid positions to a given grid position
    public Vector2Int[] GetNeighbours(Vector2Int gridPos)
    {
        Vector2Int[] neighbours = new Vector2Int[6];

        Vector2Int[] evenOffsets = {new Vector2Int(1, 0), new Vector2Int(-1, 0),
                                new Vector2Int(0, 1), new Vector2Int(0, -1),
                                new Vector2Int(-1, 1), new Vector2Int(-1, -1)};
        Vector2Int[] oddOffsets = {new Vector2Int(1, 0), new Vector2Int(-1, 0),
                                new Vector2Int(0, 1), new Vector2Int(0, -1),
                                new Vector2Int(1, -1), new Vector2Int(1, 1)};

        for (int i = 0; i < 6; i++)
        {
            neighbours[i] = gridPos + (gridPos.y%2==0 ? evenOffsets[i] : oddOffsets[i]);
        }

        return neighbours;
    }

    // Passes shader all necessary information
    void SetShaderParams()
    {
        hexMat.SetFloat("_HexScale", hexScale);
        hexMat.SetVector("_GridScale", new Vector4(transform.localScale.x, transform.localScale.z, 0.0f, 0.0f));
        hexMat.SetVector("_GridDim", new Vector4(gridDim.x, gridDim.y, 0.0f, 0.0f));

        hexMat.SetFloat("_LineWeight", lineThickness);
        hexMat.SetColor("_BaseColor", baseColor);
        hexMat.SetColor("_HexColor", hexColor);
        hexMat.SetInt("_ClipEdges", clipEdges ? 1:0);

        hexMat.SetVector("_ActiveHex", new Vector4(activeHexPos.x, activeHexPos.y, 0.0f, 0.0f));   
    }
}
