using UnityEngine;

namespace Game.Combat.Grid
{
    public class BoardRenderer : MonoBehaviour
    {
        private MeshRenderer _board;
        private MeshRenderer _border;

        void Awake()
        {
            _board = GetComponent<MeshRenderer>();
            _border = transform.GetChild(0).GetComponent<MeshRenderer>();
        }

        public void SetBoard(Material _boardMat)
        {
            _board.material = _boardMat;
        }

        public void SetBorder(Material _borderMat)
        {
            _border.material = _borderMat;
        }
    }
}
