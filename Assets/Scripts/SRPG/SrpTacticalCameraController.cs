using UnityEngine;
using UnityEngine.EventSystems;

public sealed class SrpTacticalCameraController : MonoBehaviour
{
    public enum ViewMode
    {
        Perspective,
        TopOrthographic,
    }

    const float MinOrthoSize = 2.5f;
    const float MaxOrthoSize = 18f;
    const float MinPerspectiveDistance = 5f;
    const float MaxPerspectiveDistance = 24f;

    public float keyboardPanSpeed = 8f;
    public float mousePanSpeed = 0.025f;
    public float zoomSpeed = 1.4f;
    public KeyCode toggleViewKey = KeyCode.C;
    public KeyCode focusKey = KeyCode.F;

    Camera _camera;
    Vector3 _boardCenter;
    Vector3 _focusPoint;
    float _boardWidth = 8f;
    float _boardHeight = 8f;
    float _padding = 0.75f;
    float _cellSize = 1f;
    float _perspectiveDistance = 11f;
    float _topOrthographicSize = 8f;
    ViewMode _mode = ViewMode.Perspective;

    public ViewMode Mode => _mode;
    public bool IsTopOrthographic => _mode == ViewMode.TopOrthographic;

    public static SrpTacticalCameraController Ensure(Camera camera)
    {
        if (camera == null)
            return null;
        var controller = camera.GetComponent<SrpTacticalCameraController>();
        if (controller == null)
            controller = camera.gameObject.AddComponent<SrpTacticalCameraController>();
        controller._camera = camera;
        return controller;
    }

    public void ConfigureBoard(float width, float height, float cellSize, float padding)
    {
        _camera = _camera != null ? _camera : GetComponent<Camera>();
        _boardWidth = Mathf.Max(1f, width);
        _boardHeight = Mathf.Max(1f, height);
        _cellSize = Mathf.Max(0.01f, cellSize);
        _padding = Mathf.Max(0f, padding);
        _boardCenter = new Vector3((_boardWidth - 1f) * 0.5f * _cellSize, 0f, (_boardHeight - 1f) * 0.5f * _cellSize);
        _focusPoint = _boardCenter;
        _perspectiveDistance = Mathf.Clamp(Mathf.Max(_boardWidth, _boardHeight) * _cellSize * 1.45f, MinPerspectiveDistance, MaxPerspectiveDistance);
        _topOrthographicSize = CalculateBoardOrthographicSize();
        ApplyMode();
    }

    public void FocusBoard()
    {
        _focusPoint = _boardCenter;
        _topOrthographicSize = CalculateBoardOrthographicSize();
        ApplyMode();
    }

    public void ToggleViewMode()
    {
        _mode = _mode == ViewMode.Perspective ? ViewMode.TopOrthographic : ViewMode.Perspective;
        ApplyMode();
    }

    void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    void Update()
    {
        if (_camera == null)
            _camera = GetComponent<Camera>();
        if (_camera == null)
            return;

        if (Input.GetKeyDown(toggleViewKey))
            ToggleViewMode();
        if (Input.GetKeyDown(focusKey))
            FocusBoard();

        HandleZoom();
        HandleKeyboardPan();
        HandleMousePan();
    }

    void HandleZoom()
    {
        if (IsPointerOverUi())
            return;
        float wheel = Input.mouseScrollDelta.y;
        if (Mathf.Abs(wheel) < 0.001f)
            return;

        if (_mode == ViewMode.TopOrthographic)
        {
            _topOrthographicSize = Mathf.Clamp(
                _topOrthographicSize - wheel * zoomSpeed,
                MinOrthoSize,
                MaxOrthoSize);
            ApplyTopOrthographicTransform();
        }
        else
        {
            _perspectiveDistance = Mathf.Clamp(
                _perspectiveDistance - wheel * zoomSpeed,
                MinPerspectiveDistance,
                MaxPerspectiveDistance);
            ApplyPerspectiveTransform();
        }
    }

    void HandleKeyboardPan()
    {
        Vector3 input = Vector3.zero;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            input += Vector3.forward;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            input += Vector3.back;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            input += Vector3.right;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            input += Vector3.left;
        if (input.sqrMagnitude <= 0.0001f)
            return;

        PanFocus(input.normalized * keyboardPanSpeed * Time.deltaTime);
    }

    void HandleMousePan()
    {
        if (!Input.GetMouseButton(2) || IsPointerOverUi())
            return;
        Vector3 delta = new Vector3(-Input.GetAxis("Mouse X"), 0f, -Input.GetAxis("Mouse Y"));
        if (delta.sqrMagnitude <= 0.0001f)
            return;
        float scale = _mode == ViewMode.TopOrthographic ? _camera.orthographicSize : _perspectiveDistance;
        PanFocus(delta * scale * mousePanSpeed);
    }

    void ApplyMode()
    {
        if (_camera == null)
            return;
        _camera.nearClipPlane = 0.01f;
        _camera.farClipPlane = 80f;
        if (_mode == ViewMode.TopOrthographic)
            ApplyTopOrthographicTransform();
        else
            ApplyPerspectiveTransform();
    }

    void PanFocus(Vector3 worldDelta)
    {
        _focusPoint = ClampFocusToBoard(_focusPoint + new Vector3(worldDelta.x, 0f, worldDelta.z));
        if (_mode == ViewMode.TopOrthographic)
            ApplyTopOrthographicTransform();
        else
            ApplyPerspectiveTransform();
    }

    void ApplyTopOrthographicTransform()
    {
        _camera.orthographic = true;
        _camera.orthographicSize = _topOrthographicSize;
        _camera.transform.position = _focusPoint + Vector3.up * 12f;
        _camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    void ApplyPerspectiveTransform()
    {
        _camera.orthographic = false;
        Vector3 target = new Vector3(_focusPoint.x, _boardCenter.y, _focusPoint.z);
        Quaternion rotation = Quaternion.Euler(55f, 0f, 0f);
        _camera.transform.rotation = rotation;
        _camera.transform.position = target - (rotation * Vector3.forward) * _perspectiveDistance;
    }

    float CalculateBoardOrthographicSize()
    {
        float aspect = _camera != null ? Mathf.Max(_camera.aspect, 0.01f) : 1f;
        float halfNeededV = _boardHeight * _cellSize * 0.5f;
        float halfNeededH = _boardWidth * _cellSize / (2f * aspect);
        return Mathf.Clamp(Mathf.Max(halfNeededV, halfNeededH) + _padding, MinOrthoSize, MaxOrthoSize);
    }

    Vector3 ClampFocusToBoard(Vector3 focus)
    {
        float maxX = Mathf.Max(0f, (_boardWidth - 1f) * _cellSize);
        float maxZ = Mathf.Max(0f, (_boardHeight - 1f) * _cellSize);
        focus.x = Mathf.Clamp(focus.x, 0f, maxX);
        focus.y = _boardCenter.y;
        focus.z = Mathf.Clamp(focus.z, 0f, maxZ);
        return focus;
    }

    static bool IsPointerOverUi()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

#if UNITY_INCLUDE_TESTS
    public KeyCode TestToggleViewKey => toggleViewKey;

    public bool TestPanZoomFocusReturnsToBoardCenter()
    {
        _camera = _camera != null ? _camera : GetComponent<Camera>();
        if (_camera == null)
            return false;

        _mode = ViewMode.Perspective;
        FocusBoard();
        PanFocus(new Vector3(2.25f, 0f, -1.5f));
        _perspectiveDistance = Mathf.Clamp(_perspectiveDistance - zoomSpeed, MinPerspectiveDistance, MaxPerspectiveDistance);
        ApplyPerspectiveTransform();
        FocusBoard();

        var rotation = Quaternion.Euler(55f, 0f, 0f);
        Vector3 expected = _boardCenter - (rotation * Vector3.forward) * _perspectiveDistance;
        return Vector3.Distance(_camera.transform.position, expected) < 0.01f
            && Quaternion.Angle(_camera.transform.rotation, rotation) < 0.01f;
    }

    public bool TestPerspectiveZoomChangesFocusDistance()
    {
        _camera = _camera != null ? _camera : GetComponent<Camera>();
        if (_camera == null)
            return false;

        _mode = ViewMode.Perspective;
        FocusBoard();
        float before = Vector3.Distance(_camera.transform.position, _focusPoint);
        _perspectiveDistance = Mathf.Clamp(_perspectiveDistance - zoomSpeed, MinPerspectiveDistance, MaxPerspectiveDistance);
        ApplyPerspectiveTransform();
        float after = Vector3.Distance(_camera.transform.position, _focusPoint);
        return Mathf.Abs(after - before) > 0.1f && Mathf.Abs(after - _perspectiveDistance) < 0.01f;
    }

    public bool TestPanThenZoomKeepsFocusPoint()
    {
        _camera = _camera != null ? _camera : GetComponent<Camera>();
        if (_camera == null)
            return false;

        _mode = ViewMode.Perspective;
        FocusBoard();
        PanFocus(new Vector3(_cellSize, 0f, _cellSize));
        Vector3 focusBeforeZoom = _focusPoint;
        _perspectiveDistance = Mathf.Clamp(_perspectiveDistance + zoomSpeed, MinPerspectiveDistance, MaxPerspectiveDistance);
        ApplyPerspectiveTransform();
        return Vector3.Distance(_focusPoint, focusBeforeZoom) < 0.001f
            && Vector3.Distance(_focusPoint, _camera.transform.position) > 0.1f;
    }
#endif
}
