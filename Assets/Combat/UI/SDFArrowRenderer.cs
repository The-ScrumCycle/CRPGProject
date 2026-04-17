using UnityEngine;

namespace Game.Combat.UI
{
   
    public class SDFArrowRenderer : ArrowRenderer
    {
        [Header("Arrow Settings")]
        [SerializeField][Min(0)] private float bodyWidth;
        [SerializeField][Min(0)] private float bodyHeight;
        [SerializeField][Min(0)] private float headWidth;
        [SerializeField][Min(0)] private float headHeight;
        [SerializeField][Range(0.0f, 1.0f)] private float outlineWeight;
        [SerializeField] private Color color = Color.red;
        [SerializeField] private Color outlineColor = Color.black;

        [Header("Debug Points")]
        [SerializeField] Transform startPoint;
        [SerializeField] Transform endPoint;

        private const float CANONICAL_SIZE = 10.0f; // The default width/height of the plane mesh

        private Material arrowMat;
        private Vector4[] verts;
        private Vector3 tipPos;

        void Awake()
        {
            verts = new Vector4[7];
            arrowMat = GetComponent<MeshRenderer>().material;
        }

        void Update()
        {
            if (startPoint != null && endPoint != null)
            {
                Render(startPoint.position, endPoint.position, Color.red, 0.0f);
            }
        }

        /// <summary>
        /// We set the positions of each vertex of the arrow polygon in object-space
        /// Each vertex must be adjacent (i.e. there must be an edge between them) to the next one in the array
        /// This is to ensure that the polygon is constructed correctly
        /// </summary>
        private void BuildArrow(float bodyWidth, float bodyHeight, float headWidth, float headHeight)
        {
            verts = new Vector4[7];

            // Build Body
            Vector2 extents = new Vector2(bodyWidth/2.0f, bodyHeight/2.0f);
            verts[0] = new Vector4(-extents.x, -extents.y, 0.0f, 0.0f);
            verts[1] = new Vector4(extents.x, -extents.y, 0.0f, 0.0f);
            verts[2] = new Vector4(extents.x, extents.y, 0.0f, 0.0f);
            verts[6] = new Vector4(-extents.x, extents.y, 0.0f, 0.0f);

            // Build Head
            extents = new Vector2(headWidth/2.0f, headHeight/2.0f);
            verts[3] = new Vector4(verts[2].x + extents.x, verts[2].y, 0.0f, 0.0f);
            verts[4] = new Vector4(0.0f, verts[2].y + headHeight, 0.0f, 0.0f);
            verts[5] = new Vector4(verts[6].x - extents.x, verts[2].y, 0.0f, 0.0f);

            for (int i = 0; i < 7; i++)
            {
                verts[i].y -= headHeight/2.0f;
            }

            tipPos = new Vector3(verts[4].x, 0.0f, verts[4].y);
        }

        // We position the plane object such that the tip of the arrow is placed directly on endPos
        private void PositionArrow(Vector3 startPos, Vector3 endPos)
        {
            Vector3 direction = (endPos - startPos).normalized;

            transform.position = endPos - direction * tipPos.magnitude;
            transform.rotation = Quaternion.Euler(0, 180, 0) * Quaternion.LookRotation(direction);
        }

        /// <summary>
        /// The shader handles the actual rendering of the arrow polygon
        /// The shader will render the arrow in object-space
        /// This is to ensure that the shape of the arrow is unaffected by the scale of the plane object
        /// </summary>
        private void RefreshShaderParams()
        {
            if (arrowMat == null) return;

            arrowMat.SetColor("_BaseColor", color);
            arrowMat.SetColor("_OutlineColor", outlineColor);
            arrowMat.SetFloat("_OutlineWeight", outlineWeight);

            arrowMat.SetVector("_PlaneSize", new Vector4(
                CANONICAL_SIZE*transform.localScale.x, 
                CANONICAL_SIZE*transform.localScale.z, 
                0.0f, 
                0.0f
            ));

            arrowMat.SetVectorArray("_Verts", verts);
        }

        public override void SetColor(Color pColor)
        {
            color = pColor;
        }

        public void SetOutlineColor(Color pColor)
        {
            outlineColor = pColor;
        }

        public override void Render(Vector3 startPos, Vector3 endPos, Color color, float offset, float bodyWidth, float headWidth, float headHeight)
        {
            float distance = (endPos - startPos).magnitude;

            BuildArrow(bodyWidth, distance-headHeight, headWidth, headHeight);
            PositionArrow(startPos, endPos);
            SetColor(color);

            // We want the plane to always be slightly larger than the width/height of the arrow
            // Must be big enough to contain both the arrow and its outline
            transform.localScale = new Vector3
            (
                1.5f*(bodyWidth + headWidth)/CANONICAL_SIZE,
                transform.localScale.y,
                1.5f*distance/CANONICAL_SIZE
            );

            transform.position = new Vector3(
                transform.position.x,
                offset,
                transform.position.z
            );

            RefreshShaderParams();
        }

        public override void Render(Vector3 startPos, Vector3 endPos, Color color, float offset)
        {
            Render(startPos, endPos, color, offset, bodyWidth, headWidth, headHeight);
        }
    }

}