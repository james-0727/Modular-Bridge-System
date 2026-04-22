using UnityEngine;
using System;
using Unity.VisualScripting;

public class GameManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float _placementPlaneY;

    private bool _startPlaced;
    private bool _endPlaced;
    private float _startX;
    private float _endX;
    private bool _zLocked;
    private float _lockedZ;
    private bool _orbitPivotLocked;
    private Vector3 _orbitPivot;

    public float PlacementPlaneY => _placementPlaneY;
    public bool ZLocked => _zLocked;
    public float LockedZ => _lockedZ;
    public bool OrbitPivotLocked => _orbitPivotLocked;
    public Vector3 OrbitPivot => _orbitPivot;

    public event Action BridgeStateChanged;

    public bool IsValidPlacement(GameObject candidate, SegmentType type)
    {
        if (candidate == null)
            return false;

        Vector3 pos = candidate.transform.position;

        if (_zLocked && Mathf.Abs(pos.z - _lockedZ) > 0.001f)
            return false;

        if (type == SegmentType.Start && _endPlaced && pos.x <= _endX)
            return false;

        if (type == SegmentType.End && _startPlaced && _startX <= pos.x)
            return false;

        if (type == SegmentType.Mid || type == SegmentType.MidExt)
        {
            if (!_startPlaced || !_endPlaced)
                return false;

            float min = Mathf.Min(_startX, _endX);
            float max = Mathf.Max(_startX, _endX);
            if (pos.x <= min || pos.x >= max)
                return false;
        }

        Collider col = candidate.GetComponentInChildren<Collider>();
        if (col == null)
            return true;

        Bounds b = col.bounds;
        Collider[] hits = Physics.OverlapBox(
            b.center,
            b.extents,
            candidate.transform.rotation,
            ~0,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider h = hits[i];
            if (h == null || h == col)
                continue;
            if (h.transform.IsChildOf(candidate.transform))
                continue;

            if (h.GetComponentInParent<BridgeSegment>() != null)
                return false;
        }

        return true;
    }

    public bool CanSpawnSegment(SegmentType type)
    {
        if (type == SegmentType.Start)
            return !_startPlaced;

        if (type == SegmentType.End)
            return !_endPlaced;

        if (type == SegmentType.Mid || type == SegmentType.MidExt)
            return _startPlaced && _endPlaced;

        return true;
    }

    public void RegisterSegmentPlaced(SegmentType type, Vector3 placedPosition)
    {
        bool beforeStart = _startPlaced;
        bool beforeEnd = _endPlaced;
        float beforeStartX = _startX;
        float beforeEndX = _endX;

        if (type == SegmentType.Start)
        {
            _startPlaced = true;
            _startX = placedPosition.x;
        }
        if (type == SegmentType.End)
        {
            _endPlaced = true;
            _endX = placedPosition.x;
        }

        if (!_zLocked && (type == SegmentType.Start || type == SegmentType.End))
        {
            _zLocked = true;
            _lockedZ = placedPosition.z;
        }
        
        if (_startPlaced && _endPlaced)
        {
            _orbitPivotLocked = true;
            _orbitPivot = new Vector3((_startX + _endX) / 2, _placementPlaneY, _lockedZ);
        }

        if (beforeStart != _startPlaced ||
            beforeEnd != _endPlaced ||
            beforeStartX != _startX ||
            beforeEndX != _endX)
        {
            BridgeStateChanged?.Invoke();
        }
    }

    private static GameManager _instance;
    private void Awake()
    {
        _instance = this;
    }

    public static GameManager Get()
    {
        if (_instance == null)
        {
            _instance = FindFirstObjectByType<GameManager>();
        }

        return _instance;
    }
}
