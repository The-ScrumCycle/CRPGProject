using UnityEngine;

public class PedestalVisual : MonoBehaviour
{
    [SerializeField] private Color _tint = Color.white;

    private Material _pedestalMat;
    private Color _baseColor;

    void Start()
    {
        _pedestalMat = GetComponent<MeshRenderer>().material;
        _baseColor = _pedestalMat.color;
    }

    void Update()
    {
        _pedestalMat.color = _baseColor * _tint;
    }
}
