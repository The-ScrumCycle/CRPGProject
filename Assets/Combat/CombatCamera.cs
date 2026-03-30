using UnityEngine;

namespace Game.Combat
{
    public class CombatCamera : MonoBehaviour
    {
        [SerializeField] private float sensitivity = 500.0f;
        [SerializeField] private float scrollSpeed = 20.0f;
        [SerializeField] private float deceleration = 10.0f;
        [SerializeField] private float minDistance = 5.0f;
        [SerializeField] private float maxDistance = 20.0f;
        [SerializeField] private Transform target;

        private Camera _mainCamera;
        private Vector3 _startPos;
        private Quaternion _startRotation;
        private float distanceFromCenter;
        private float pitch;
        private float yaw;
        private Vector2 mouseVelocity;

        void Start()
        {
            if (target == null) target = GameObject.FindWithTag("HexGrid").transform;

            _mainCamera = Camera.main;
            distanceFromCenter = Vector3.Distance(transform.position, target.position);

            _startPos = transform.position;
            _startRotation = transform.rotation;

            pitch = _startRotation.eulerAngles.x;
            yaw = _startRotation.eulerAngles.y;
        }

        void Update()
        {
            distanceFromCenter -= Input.GetAxis("Mouse ScrollWheel") * scrollSpeed;
            distanceFromCenter = Mathf.Clamp(distanceFromCenter, minDistance, maxDistance);

            if (Input.GetMouseButton(2)){
                mouseVelocity = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            }
            else if (Input.GetKeyDown(KeyCode.F)){
                mouseVelocity = Vector2.zero;

                pitch = _startRotation.eulerAngles.x;
                yaw = _startRotation.eulerAngles.y;

                distanceFromCenter = Vector3.Distance(_startPos, target.position);
            }
            else{
                mouseVelocity = Vector2.Lerp(mouseVelocity, Vector2.zero, deceleration * Time.deltaTime);
            }

            yaw += mouseVelocity.x * sensitivity * Time.deltaTime;
            pitch -= mouseVelocity.y * sensitivity * Time.deltaTime;

            Quaternion nextRotation = Quaternion.Euler(pitch, yaw, 0.0f);
            _mainCamera.transform.position = target.position + nextRotation * Vector3.forward * -distanceFromCenter;
            _mainCamera.transform.rotation = nextRotation;
        }
    }
}
