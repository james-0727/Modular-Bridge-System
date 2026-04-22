using UnityEngine;
using UnityEngine.InputSystem;

public class OrbitZoomCamera : MonoBehaviour
{
    [SerializeField] private Transform _orbitCenter;
    [SerializeField] private Vector3 _orbitCenterWorld = Vector3.zero;

    [Header("Smoothing")]
    [SerializeField] private float _pivotSmoothTime = 0.12f;
    [SerializeField] private float _minDistance = 2f;
    [SerializeField] private float _maxDistance = 80f;
    [SerializeField] private float _zoomSensitivity = 2f;
    [SerializeField] private float _orbitSensitivity = 0.25f;
    [SerializeField] private float _minPitch = -89f;
    [SerializeField] private float _maxPitch = 89f;

    private float _distance;
    private float _yaw;
    private float _pitch;

    private Vector3 _smoothedPivot;
    private Vector3 _pivotVelocity;
    private bool _pivotInitialized;

    private GameManager _gameManager => GameManager.Get();
    private System.Action _onBridgeStateChanged;

    public Vector3 Pivot
    {
        get
        {
            if (_gameManager.OrbitPivotLocked)
            {
                return _gameManager.OrbitPivot;
            }

            if (_orbitCenter != null)
            {
                return _orbitCenter.position;
            }

            return _orbitCenterWorld;
        }
    }

    private void Start()
    {
        _smoothedPivot = Pivot;
        _pivotInitialized = true;
        RecalculateOrbitFromCurrentTransform(_smoothedPivot);
    }

    private void OnEnable()
    {
        if (_onBridgeStateChanged == null)
        {
            _onBridgeStateChanged = () => RecalculateOrbitFromCurrentTransform(_smoothedPivot);
        }

        if (_gameManager != null)
        {
            _gameManager.BridgeStateChanged += _onBridgeStateChanged;
        }
    }

    private void OnDisable()
    {
        if (_gameManager != null && _onBridgeStateChanged != null)
        {
            _gameManager.BridgeStateChanged -= _onBridgeStateChanged;
        }
    }

    private void LateUpdate()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        if (mouse.rightButton.isPressed)
        {
            Vector2 delta = mouse.delta.ReadValue();
            _yaw += delta.x * _orbitSensitivity;
            _pitch -= delta.y * _orbitSensitivity;
            _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);
        }

        float scroll = mouse.scroll.ReadValue().y;

        if (Mathf.Abs(scroll) > 0.01f)
        {
            _distance -= scroll * _zoomSensitivity;
            _distance = Mathf.Clamp(_distance, _minDistance, _maxDistance);
        }

        Vector3 targetPivot = Pivot;
        if (!_pivotInitialized)
        {
            _smoothedPivot = targetPivot;
            _pivotInitialized = true;
        }

        if (_pivotSmoothTime <= 0f)
        {
            _smoothedPivot = targetPivot;
            _pivotVelocity = Vector3.zero;
        }
        else
        {
            _smoothedPivot = Vector3.SmoothDamp(
                _smoothedPivot,
                targetPivot,
                ref _pivotVelocity,
                _pivotSmoothTime
            );
        }

        Vector3 pivot = _smoothedPivot;
        Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3 offset = rot * new Vector3(0f, 0f, -_distance);
        transform.position = pivot + offset;
        transform.LookAt(pivot);
    }

    private void RecalculateOrbitFromCurrentTransform(Vector3 pivot)
    {
        Vector3 toCamera = transform.position - pivot;
        _distance = Mathf.Clamp(toCamera.magnitude, _minDistance, _maxDistance);

        if (toCamera.sqrMagnitude > 1e-6f)
        {
            Quaternion look = Quaternion.LookRotation(-toCamera.normalized, Vector3.up);
            Vector3 e = look.eulerAngles;
            _yaw = e.y;
            _pitch = e.x;

            if (_pitch > 180f)
            {
                _pitch -= 360f;
            }

            _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);
        }
        else
        {
            _yaw = 0f;
            _pitch = 25f;
        }
    }
}
