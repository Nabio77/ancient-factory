using UnityEngine;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;

namespace AncientFactory.Features.WorldMap
{
    public class WorldMapCamera : MonoBehaviour
    {
        [Title("Movement")]
        [SerializeField]
        private float panSpeed = 20f;

        [SerializeField]
        private float dragSpeed = 0.5f;

        [SerializeField]
        private float smoothTime = 0.1f;

        [Title("Zoom")]
        [SerializeField]
        private float zoomSpeed = 5f;

        [SerializeField]
        private float minZoom = 3f;

        [SerializeField]
        private float maxZoom = 30f;

        private Camera _camera;
        private Vector3 _targetPosition;
        private float _targetZoom;
        private Vector3 _velocity;
        private float _zoomVelocity;
        private Vector2 _lastMousePos;
        private bool _isDragging;

        // Saved position for reset
        private Vector3 _savedPosition;
        private float _savedZoom;

        /// <summary>
        /// When false, camera ignores all input. Used when UI overlays are active.
        /// </summary>
        public bool InputEnabled { get; set; } = true;

        void Awake()
        {
            _camera = GetComponent<Camera>();
            _targetPosition = transform.position;
            _targetZoom = _camera.orthographicSize;
        }

        void Update()
        {
            if (InputEnabled)
            {
                HandleKeyboardPan();
                HandleMouseDrag();
                HandleZoom();
            }
            ApplyMovement();
        }

        /// <summary>
        /// Save current position to restore later.
        /// </summary>
        public void SavePosition()
        {
            _savedPosition = _targetPosition;
            _savedZoom = _targetZoom;
        }

        /// <summary>
        /// Restore previously saved position.
        /// </summary>
        public void RestorePosition()
        {
            _targetPosition = _savedPosition;
            _targetZoom = _savedZoom;
        }

        private void HandleKeyboardPan()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            var input = Vector2.zero;

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1;

            if (input.sqrMagnitude > 0)
            {
                input.Normalize();
                float zoomFactor = _targetZoom / 10f;
                // 2D: Move in XY plane
                _targetPosition += new Vector3(input.x, input.y, 0) * (panSpeed * zoomFactor * Time.deltaTime);
            }
        }

        private void HandleMouseDrag()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            var mousePos = mouse.position.ReadValue();

            if (mouse.middleButton.wasPressedThisFrame)
            {
                _isDragging = true;
                _lastMousePos = mousePos;
            }

            if (mouse.middleButton.wasReleasedThisFrame)
            {
                _isDragging = false;
            }

            if (_isDragging)
            {
                var delta = mousePos - _lastMousePos;
                float zoomFactor = _targetZoom * 0.01f;
                // 2D: Drag in XY plane
                _targetPosition -= new Vector3(delta.x, delta.y, 0) * (dragSpeed * zoomFactor);
                _lastMousePos = mousePos;
            }
        }

        private void HandleZoom()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            var scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                _targetZoom -= scroll * zoomSpeed * 0.1f;
                _targetZoom = Mathf.Clamp(_targetZoom, minZoom, maxZoom);
            }
        }

        private void ApplyMovement()
        {
            // Keep Z position fixed for 2D camera
            var targetWithFixedZ = new Vector3(_targetPosition.x, _targetPosition.y, transform.position.z);
            transform.position = Vector3.SmoothDamp(transform.position, targetWithFixedZ, ref _velocity, smoothTime);
            _camera.orthographicSize = Mathf.SmoothDamp(_camera.orthographicSize, _targetZoom, ref _zoomVelocity, smoothTime);
        }

        [Button("Reset Position")]
        public void ResetPosition()
        {
            _targetPosition = new Vector3(0, 0, transform.position.z);
            _targetZoom = 10f;
        }
    }
}
