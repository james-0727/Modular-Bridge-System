using UnityEngine;
using UnityEngine.InputSystem;

public class BridgeSegment : MonoBehaviour
{
    private SegmentType _segmentType;
    private Vector3 _originalPosition;
    private bool _isDragging;
    private GameManager _gameManager => GameManager.Get();
    private BridgeSegment _activeDrag;

    public void SetSegmentType(SegmentType segmentType)
    {
        _segmentType = segmentType;
    }

    public void SetColor(Color color)
    {
        Renderer renderer = GetComponentInChildren<Renderer>();
        renderer.material.color = color;
    }

    void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
            return;

        if (_gameManager == null)
            return;

        Camera cam = Camera.main;
        if (cam == null)
            return;

        if (!_isDragging && mouse.leftButton.wasPressedThisFrame)
        {
            if (_activeDrag != null)
                return;

            Ray pickRay = cam.ScreenPointToRay(mouse.position.ReadValue());
            if (Physics.Raycast(pickRay, out RaycastHit hit, Mathf.Infinity, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider != null && hit.collider.transform.IsChildOf(transform))
                {
                    _activeDrag = this;
                    _isDragging = true;
                    _originalPosition = transform.position;
                }
            }
        }

        if (_isDragging && _activeDrag == this)
        {
            Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
            Plane plane = new Plane(Vector3.up, new Vector3(0f, _gameManager.PlacementPlaneY, 0f));
            if (plane.Raycast(ray, out float enter))
            {
                Vector3 p = ray.GetPoint(enter);
                if (_gameManager.ZLocked)
                    p.z = _gameManager.LockedZ;
                transform.position = p;
            }

            bool valid = _gameManager.IsValidPlacement(gameObject, _segmentType);

            SetColor(valid ? Color.green : Color.red);

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                if (!valid)
                {
                    transform.position = _originalPosition;
                }
                else
                {
                    _gameManager.RegisterSegmentPlaced(_segmentType, transform.position);
                }

                SetColor(Color.white);
                _isDragging = false;
                _activeDrag = null;
            }
        }
    }
}
