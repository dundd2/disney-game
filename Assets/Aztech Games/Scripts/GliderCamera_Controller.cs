using UnityEngine;

namespace AztechGames
{ 
    public class GliderCamera_Controller : MonoBehaviour
    { 
        [Tooltip("The mode of the camera (First Person or Third Person).")]
        public enum CameraMode
        {
            FPS,
            TPS
        }
        public CameraMode cameraMode;
        
        [Space(10)]
        [Tooltip("The transform of the First Person Camera.")]
        public Transform FPSCamera;
        
        [Space(5)]
        [Tooltip("The allowed range for horizontal rotation of the camera.")]
        public Vector2 minMaxXAngle = new Vector2(-70.0f, 70.0f);
        [Tooltip("The allowed range for vertical rotation of the camera.")]
        public Vector2 minMaxYAngle = new Vector2(-180.0f, 180.0f);
        [Tooltip("The allowed range for zoom distance.")]
        public Vector2 minMaxZoomDistance = new Vector2(5.0f, 50.0f);
        [Tooltip("The offset to look at when in Third Person mode.")]
        public Vector3 lookAtOffset;
        
        [Space(5)]
        [Tooltip("The speed of camera rotation.")]
        public float rotationSpeed = 5.0f;
        [Tooltip("The speed of camera zooming.")]
        public float zoomSpeed = 25.0f;

        private Transform Target;
        private float currentX = 0.0f; 
        private float currentY = 0.0f;
        
        [Tooltip("The speed of glider rotation.")]
        public float gliderRotationSpeed = 50.0f;

        [Tooltip("The speed of glider forward movement.")]
        public float forwardSpeed = 20.0f;

        void Update()
        {
            // Aircraft Yaw Left: A.
            // Aircraft Yaw Right: D.
            float horizontalInput = Input.GetKey(KeyCode.D) ? 1 : (Input.GetKey(KeyCode.A) ? -1 : 0);
            transform.Rotate(Vector3.up, horizontalInput * gliderRotationSpeed * Time.deltaTime);

            // Aircraft Pitch Forward: Num8.
            // Aircraft Pitch Back: Num5.
            float verticalInput = Input.GetKey(KeyCode.Keypad8) ? 1 : (Input.GetKey(KeyCode.Keypad5) ? -1 : 0);
            transform.Rotate(Vector3.right, verticalInput * gliderRotationSpeed * Time.deltaTime);

            // Aircraft Roll Left: Num4.
            // Aircraft Roll Right: Num6.
            float rollInput = Input.GetKey(KeyCode.Keypad4) ? 1 : (Input.GetKey(KeyCode.Keypad6) ? -1 : 0);
            transform.Rotate(Vector3.forward, rollInput * gliderRotationSpeed * Time.deltaTime);

            // Aircraft Throttle Up: W.
            // Aircraft Throttle Down: S.
            if (Input.GetKey(KeyCode.W))
            {
                forwardSpeed += 1 * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.S))
            {
                forwardSpeed -= 1 * Time.deltaTime;
            }

            transform.Translate(Vector3.forward * forwardSpeed * Time.deltaTime);
        }

        void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.tag == "Ground")
            {
                Debug.Log("Game Over!");
                // Implement game over logic here (e.g., display game over UI, restart button)
                // For now, I'll just disable the script
                this.enabled = false;
            }
        }
    }
}
