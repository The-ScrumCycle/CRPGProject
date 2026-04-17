using UnityEngine;

namespace Game.Combat.UI
{

    // This class is more or less obsolete, use SDFArrowRenderer
    public class MeshArrowRenderer : ArrowRenderer
    {
        [SerializeField][Min(0)] private float bodyWidth;
        [SerializeField][Min(0)] private float headWidth;
        [SerializeField][Min(0)] private float headHeight;
        [SerializeField][Min(0)] private float outlineWeight;
        [SerializeField] private Color color = Color.red;
        [SerializeField] private Color outlineColor = Color.black;

        private Mesh arrowMesh;
        private Material arrowMat;
        private Vector3 tipPos;

        void Awake()
        {
            arrowMesh = GetComponent<MeshFilter>().mesh;
            if (arrowMesh == null) arrowMesh = new Mesh();

            arrowMat = GetComponent<MeshRenderer>().material;
        }

        private void BuildArrow(float bodyWidth, float bodyHeight, float headWidth, float headHeight, Mesh arrowMesh)
        {
            Vector3[] verts = new Vector3[7];
            int[] tris = new int[15];

            // Build Body
            Vector2 extents = new Vector2(bodyWidth/2.0f, bodyHeight/2.0f);
            verts[0] = new Vector3(-extents.x, 0.0f, -extents.y);
            verts[1] = new Vector3(extents.x, 0.0f, -extents.y);
            verts[2] = new Vector3(-extents.x, 0.0f, extents.y);
            verts[3] = new Vector3(extents.x, 0.0f, extents.y);

            // Build Head
            extents = new Vector2(headWidth/2.0f, headHeight/2.0f);
            verts[4] = new Vector3(verts[2].x - extents.x, 0.0f, verts[2].z);
            verts[5] = new Vector3(verts[3].x + extents.x, 0.0f, verts[2].z);
            verts[6] = new Vector3(0.0f, 0.0f, verts[2].z + headHeight);

            tipPos = verts[6];

            // Build Triangles
            tris[0] = 0;
            tris[1] = 1;
            tris[2] = 2;

            tris[3] = 3;
            tris[4] = 2;
            tris[5] = 1;

            tris[6] = 2;
            tris[7] = 3;
            tris[8] = 6;

            tris[9] = 4;
            tris[10] = 2;
            tris[11] = 6;

            tris[12] = 5;
            tris[13] = 6;
            tris[14] = 3;

            if (arrowMesh == null) arrowMesh = new Mesh();

            // Apply to mesh
            arrowMesh.Clear();
            arrowMesh.vertices = verts;
            arrowMesh.triangles = tris;
            arrowMesh.RecalculateNormals();
        }

        private void PositionArrow(Vector3 startPos, Vector3 endPos)
        {
            Vector3 direction = (endPos - startPos).normalized;

            transform.position = endPos - direction * tipPos.magnitude;
            transform.rotation = Quaternion.LookRotation(direction);
        }

        public override void SetColor(Color color)
        {
            arrowMat.color = color;
        }

        public override void SetOutlineColor(Color pColor)
        {
            outlineColor = pColor;
        }

        public override void Render(Vector3 startPos, Vector3 endPos, Color color, Color outlineColor, float offset, float bodyWidth, float headWidth, float headHeight)
        {
            float distance = (endPos - startPos).magnitude;
            
            BuildArrow(bodyWidth, distance-headHeight, headWidth, headHeight, arrowMesh);
            PositionArrow(startPos, endPos);
            SetColor(color);
            SetOutlineColor(outlineColor);

            transform.position = new Vector3(
                transform.position.x,
                offset,
                transform.position.z
            );
        }

        public override void Render(Vector3 startPos, Vector3 endPos, Color color, Color outlineColor, float offset)
        {
            Render(startPos, endPos, color, outlineColor, offset, bodyWidth, headWidth, headHeight);
        }
    }

}