using UnityEngine;
using State;
using Game.Combat;

public class DiamondCreator : MonoBehaviour
{
    private GameObject diamond;
    public float rotateSpeed = 50f;
    public float bobSpeed = 3.0f;
    public float bobHeight = 0.3f;

    private GameState gamestate;
    private Vector3 startPos;

    public float pickupRadius = 3.0f;
    public string requiredFlag;  // required flag needed for setting 
    public string collectFlag;     // set in Inspector — GameState flag set when crystal is collected


    void Start()
    {
        Debug.Log("Creating Diamond");
        CreateDiamond();
        startPos = diamond.transform.position;
        gamestate = GameState.Instance;


    }

    void Update()
    {
        if (diamond == null) return;
        diamond.transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
        Vector3 pos = startPos;
        pos.y += Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        diamond.transform.position = pos;


        GameObject player = GameObject.FindWithTag("Player");
        float dist = Vector3.Distance(player.transform.position, diamond.transform.position);
        bool flag = gamestate.hasFlag(requiredFlag);
        //Debug.Log("Checking if has required flag");
        //Debug.Log($"Required Flag: {requiredFlag}");
        //Debug.Log($"Has required Flag: {flag}");
        //Debug.Log($"Distance: {dist}");
        //Debug.Log($"Should collect: {dist < pickupRadius && flag}");

        if (dist < pickupRadius && flag)
        {
            //Debug.Log("Destroying Diamond!");
            gamestate.setFlag(collectFlag);
            Destroy(diamond);
            //Debug.Log("Diamond collected!");
        }
    }

    /*PLEASE NOTE THIS FUNCTION IS AI GENERATED*/
    Material CreateCrystalMaterial()
    {
        // Try URP first, then Built-in
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        if (shader == null)
        {
            Debug.LogError("No valid shader found! Check your render pipeline.");
            return new Material(Shader.Find("Hidden/InternalErrorShader"));
        }
        
        Material mat = new Material(shader);

        if (mat.shader.name.Contains("Universal"))
        {
            // URP transparent setup
            mat.SetFloat("_Surface", 1); // 0=opaque, 1=transparent
            mat.SetFloat("_Blend", 0);   // alpha blend
            mat.SetFloat("_ZWrite", 0);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetColor("_BaseColor", new Color(0.15f, 0.5f, 0.95f, 0.55f));
            mat.SetFloat("_Smoothness", 0.95f);
            mat.SetFloat("_Metallic", 0.3f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;
        }
        else
        {
            // Built-in transparent setup
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            mat.color = new Color(0.15f, 0.5f, 0.95f, 0.55f);
            mat.SetFloat("_Glossiness", 0.95f);
            mat.SetFloat("_Metallic", 0.3f);
        }

        return mat;
    }

    Material CreateEdgeMaterial()
    {
        Material mat = CreateCrystalMaterial();
        mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Front);
        
        if (mat.shader.name.Contains("Universal"))
            mat.SetColor("_BaseColor", new Color(0.0f, 0.15f, 0.4f, 0.7f));
        else
            mat.color = new Color(0.0f, 0.15f, 0.4f, 0.7f);

        mat.renderQueue = 2999;
        return mat;
    }

    void CreateDiamond()
    {
        Mesh mesh = new Mesh();

        float topY = 0.6f;
        float midY = 0f;
        float botY = -1f;
        float w = 0.5f;

        Vector3 top = new Vector3(0, topY, 0);
        Vector3 bot = new Vector3(0, botY, 0);
        Vector3 m0 = new Vector3(-w, midY, -w);
        Vector3 m1 = new Vector3(w, midY, -w);
        Vector3 m2 = new Vector3(w, midY, w);
        Vector3 m3 = new Vector3(-w, midY, w);

        Vector3[] verts = new Vector3[]
        {
            top, m1, m0,
            top, m2, m1,
            top, m3, m2,
            top, m0, m3,
            bot, m0, m1,
            bot, m1, m2,
            bot, m2, m3,
            bot, m3, m0
        };

        int[] tris = new int[24];
        for (int i = 0; i < 24; i++) tris[i] = i;

        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();

        diamond = new GameObject("Diamond");
        diamond.transform.position = transform.position;

        MeshFilter mf = diamond.AddComponent<MeshFilter>();
        MeshRenderer mr = diamond.AddComponent<MeshRenderer>();
        mf.mesh = mesh;
        mr.material = CreateCrystalMaterial();

        GameObject outline = new GameObject("DiamondOutline");
        outline.transform.parent = diamond.transform;
        outline.transform.localPosition = Vector3.zero;
        outline.transform.localScale = Vector3.one * 1.02f;

        MeshFilter omf = outline.AddComponent<MeshFilter>();
        MeshRenderer omr = outline.AddComponent<MeshRenderer>();
        omf.mesh = mesh;
        omr.material = CreateEdgeMaterial();

        Debug.Log("Diamond created with shader: " + mr.material.shader.name);


    }

}