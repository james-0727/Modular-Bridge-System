using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class BridgeSegmentUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Image _thumbnail;
    [SerializeField] private TextMeshProUGUI _description;
    [SerializeField] private BridgeSegment _segmentPrefab;
    [SerializeField] private SegmentType _segmentType = SegmentType.Mid;

    [Header("UI State")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField, Range(0f, 1f)] private float _disabledAlpha = 0.35f;

    private BridgeSegment _clonedSegment;
    private GameManager _gameManager => GameManager.Get();

    private void Awake()
    {
        if (_canvasGroup == null)
        {
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void OnEnable()
    {
        RefreshAvailabilityUI();
        _gameManager.BridgeStateChanged += RefreshAvailabilityUI;
    }

    private void OnDisable()
    {
        if (_gameManager != null)
        {
            _gameManager.BridgeStateChanged -= RefreshAvailabilityUI;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_segmentPrefab == null)
        {
            return;
        }

        if (_gameManager == null)
        {
            return;
        }

        if (!_gameManager.CanSpawnSegment(_segmentType))
        {
            return;
        }

        _clonedSegment = Instantiate(_segmentPrefab, Vector3.zero, Quaternion.identity);
        UpdateClonedPosition(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        UpdateClonedPosition(eventData);
        if (_clonedSegment != null)
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed))
            {
                _clonedSegment.TryMagnetSnapToNearby();
            }

            if (_gameManager.IsValidPlacement(_clonedSegment.gameObject, _segmentType))
            {
                _clonedSegment.SetColor(Color.green);
            }
            else
            {
                _clonedSegment.SetColor(Color.red);
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_clonedSegment == null)
        {
            return;
        }

        if (_gameManager == null || !_gameManager.IsValidPlacement(_clonedSegment.gameObject, _segmentType))
        {
            Destroy(_clonedSegment.gameObject);
            _clonedSegment = null;
            return;
        }
        else
        {
            _clonedSegment.SetColor(Color.white);
        }

        _gameManager.RegisterSegmentPlaced(_segmentType, _clonedSegment.transform.position);

        _clonedSegment = null;
    }

    private void UpdateClonedPosition(PointerEventData eventData)
    {
        if (_clonedSegment == null)
        {
            return;
        }

        Camera cam = eventData.pressEventCamera != null ? eventData.pressEventCamera : Camera.main;
        if (cam == null)
        {
            return;
        }

        Ray ray = cam.ScreenPointToRay(eventData.position);
        if (_gameManager == null)
        {
            return;
        }

        Plane plane = new Plane(Vector3.up, new Vector3(0f, _gameManager.PlacementPlaneY, 0f));
        if (plane.Raycast(ray, out float enter))
        {
            Vector3 p = ray.GetPoint(enter);
            if (_gameManager.ZLocked)
            {
                p.z = _gameManager.LockedZ;
            }
            _clonedSegment.transform.position = p;
        }
    }

    private void RefreshAvailabilityUI()
    {
        if (_canvasGroup == null)
        {
            return;
        }

        bool available = _gameManager.CanSpawnSegment(_segmentType);
        _canvasGroup.alpha = available ? 1f : _disabledAlpha;
        _canvasGroup.blocksRaycasts = available;
        _canvasGroup.interactable = available;
    }

}
