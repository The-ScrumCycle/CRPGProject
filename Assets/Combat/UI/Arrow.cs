using UnityEngine;

public class Arrow : MonoBehaviour
{
    [SerializeField] private GameObject head;
    [SerializeField] private GameObject shaft;

    private Material headMat;
    private Material shaftMat;

    void Awake()
    {
        if (head == null) head = transform.GetChild(0).gameObject;
        if (shaft == null) shaft = transform.GetChild(1).gameObject;

        headMat = head.GetComponent<MeshRenderer>().material;
        shaftMat = shaft.GetComponent<MeshRenderer>().material;
    }

    public void Render(Vector3 startPos, Vector3 endPos, Color color, Vector3 offset)
    {
        if (startPos == endPos) return;

        Vector3 direction = (endPos - startPos).normalized;
        float distance = Vector3.Distance(startPos, endPos);
        float shaftDistance = distance-0.1f;
        
        // Position
        head.transform.position = endPos;
        head.transform.position += offset;
        shaft.transform.position = startPos + direction*shaftDistance*0.5f;
        shaft.transform.position += offset;

        // Rotation
        if (direction != Vector3.zero)
        {
            head.transform.rotation = Quaternion.LookRotation(direction);
            shaft.transform.rotation = Quaternion.LookRotation(direction);
        }

        // Scale
        head.transform.localScale = new Vector3(
            head.transform.localScale.x,
            head.transform.localScale.y,
            head.transform.localScale.z * Mathf.Min(distance/2.0f, 1.0f)
        );
        shaft.transform.localScale = new Vector3(
            shaft.transform.localScale.x,
            shaft.transform.localScale.y,
            shaftDistance/(10f*transform.localScale.z)
        );

        headMat.SetColor("_Color", color);
        shaftMat.SetColor("_Color", color);
    }
}
