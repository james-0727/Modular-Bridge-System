using UnityEngine;
using UnityEngine.InputSystem;

public class BridgeSegment : MonoBehaviour
{
    [SerializeField] private SegmentType _segmentType;
    [SerializeField] private BoxCollider _boxCollider;

    private Vector3 _originalPosition;
    private bool _isDragging;
    private GameManager _gameManager => GameManager.Get();
    private BridgeSegment _activeDrag;
    private BridgeSegment _nearLeft;
    private BridgeSegment _nearRight;

    public void SetColor(Color color)
    {
        Renderer renderer = GetComponentInChildren<Renderer>();
        renderer.material.color = color;
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            return;
        }

        if (!_isDragging && mouse.leftButton.wasPressedThisFrame)
        {
            if (_activeDrag != null)
            {
                return;
            }

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
                {
                    p.z = _gameManager.LockedZ;
                }
                transform.position = p;
            }

            if (IsShiftHeld())
            {
                TryMagnetSnapToNearby();
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

    private void OnTriggerEnter(Collider other)
    {
        if (other == null)
        {
            return;
        }

        BridgeSegment segment = other.GetComponentInParent<BridgeSegment>();
        if (segment == null || segment == this)
        {
            return;
        }

        if (segment.transform.position.x < transform.position.x)
        {
            _nearLeft = ChooseCloserNeighbor(_nearLeft, segment);
        }
        else
        {
            _nearRight = ChooseCloserNeighbor(_nearRight, segment);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other == null)
        {
            return;
        }

        BridgeSegment segment = other.GetComponentInParent<BridgeSegment>();
        if (segment == null || segment == this)
        {
            return;
        }

        if (_nearLeft == segment)
        {
            _nearLeft = null;
        }
        if (_nearRight == segment)
        {
            _nearRight = null;
        }
    }

    public bool TryMagnetSnapToNearby()
    {
        if (_gameManager == null)
        {
            return false;
        }

        if (_nearLeft == null && _nearRight == null)
        {
            return false;
        }

        Vector3 pos = transform.position;

        if (_nearLeft != null && _nearLeft.gameObject == null) _nearLeft = null;
        if (_nearRight != null && _nearRight.gameObject == null) _nearRight = null;

        BridgeSegment nearest = null;
        if (_nearLeft != null && _nearRight != null)
        {
            float dxL = Mathf.Abs(_nearLeft.transform.position.x - pos.x);
            float dxR = Mathf.Abs(_nearRight.transform.position.x - pos.x);
            nearest = dxL <= dxR ? _nearLeft : _nearRight;
        }
        else
        {
            nearest = _nearLeft != null ? _nearLeft : _nearRight;
        }

        if (nearest == null)
        {
            return false;
        }

        float myWidth = GetWidthX();
        float otherWidth = nearest.GetWidthX();
        float gap = 0.02f;
        float half = (myWidth * 0.5f) + (otherWidth * 0.5f) + gap;

        float targetX = (pos.x <= nearest.transform.position.x)
            ? (nearest.transform.position.x - half)
            : (nearest.transform.position.x + half);

        Vector3 candidate = new Vector3(targetX, _gameManager.PlacementPlaneY, pos.z);
        if (_gameManager.ZLocked)
        {
            candidate.z = _gameManager.LockedZ;
        }

        Vector3 original = transform.position;
        transform.position = candidate;
        bool valid = _gameManager.IsValidPlacement(gameObject, _segmentType);
        if (!valid)
        {
            transform.position = original;
            return false;
        }

        return true;
    }

    private BridgeSegment ChooseCloserNeighbor(BridgeSegment current, BridgeSegment candidate)
    {
        if (current == null) return candidate;
        if (candidate == null) return current;

        float myX = transform.position.x;
        float dxCurrent = Mathf.Abs(current.transform.position.x - myX);
        float dxCandidate = Mathf.Abs(candidate.transform.position.x - myX);
        return dxCandidate < dxCurrent ? candidate : current;
    }

    private float GetWidthX()
    {
        if (_boxCollider != null)
        {
            float w = _boxCollider.bounds.size.x;
            return w > 0.0001f ? w : 1f;
        }

        Collider c = GetComponentInChildren<Collider>();
        if (c != null)
        {
            float w = c.bounds.size.x;
            return w > 0.0001f ? w : 1f;
        }

        return 1f;
    }

    private bool IsShiftHeld()
    {
        Keyboard kb = Keyboard.current;
        return kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed);
    }
}
