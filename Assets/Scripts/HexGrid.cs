using UnityEngine;

public class HexGrid : MonoBehaviour
{
    [SerializeField] Color baseColor; // Background colour
    [SerializeField] Color hexColor; // Hexagon colour
    [SerializeField] [Range(0.0f, 1.0f)] float lineThickness; // Thickness of hexagon lines
    [SerializeField] [Min(0)] int hexScale;
    [SerializeField] [Min(0)] Vector2Int gridDim;
    [SerializeField] bool useFixedDim; // Whether or not the grid dimensions are set manually
    [SerializeField] bool clipEdges;

    Vector2Int gridSize;
    Material hexMat;

    void Start()
    {
        hexMat = GetComponent<MeshRenderer>().material;
    }

    void Update()
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
    }
}
