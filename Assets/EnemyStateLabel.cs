using UnityEngine;

/// <summary>
/// Shows a floating state label above an enemy using a world-space TextMesh child.
/// Billboards to the active camera each frame so it's always readable.
/// Works in Game view (URP/Built-in), Scene view, and builds — no Gizmos toggle needed.
/// </summary>
public class EnemyStateLabel : MonoBehaviour
{
    #region Serialized Fields
    [Header("Placement")]
    [SerializeField] private Vector3 _offset = new Vector3(0f, 2.5f, 0f);

    [Header("Appearance")]
    [SerializeField] private Color _color = Color.white;
    [SerializeField] private int _fontSize = 64;
    [SerializeField] private float _characterSize = 0.05f;
    [SerializeField] private FontStyle _fontStyle = FontStyle.Bold;
    #endregion

    #region Runtime
    private TextMesh _textMesh;
    private Transform _labelTransform;
    private string _state = "Idle";
    #endregion

    #region Public API
    public void SetState(string state)
    {
        _state = state;
        if (_textMesh != null) _textMesh.text = state;
    }
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        BuildLabel();
    }

    private void OnEnable()
    {
        if (_labelTransform == null) BuildLabel();
        if (_labelTransform != null) _labelTransform.gameObject.SetActive(true);
    }

    private void OnDisable()
    {
        if (_labelTransform != null) _labelTransform.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (_labelTransform == null) return;

        _labelTransform.position = transform.position + _offset;

        var cam = Camera.main;
        if (cam == null)
        {
            // Fall back to any active camera so Scene-view / editor still works.
            if (Camera.allCamerasCount > 0) cam = Camera.allCameras[0];
        }

        if (cam != null)
        {
            // Face the camera (flip 180 so text isn't mirrored).
            Vector3 forward = _labelTransform.position - cam.transform.position;
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.0001f)
            {
                _labelTransform.rotation = Quaternion.LookRotation(forward);
            }
        }
    }
    #endregion

    #region Internal
    private void BuildLabel()
    {
        if (_labelTransform != null) return;

        var go = new GameObject($"{name}_StateLabel");
        go.hideFlags = HideFlags.DontSave;
        _labelTransform = go.transform;
        _labelTransform.SetParent(null, worldPositionStays: true);

        _textMesh = go.AddComponent<TextMesh>();
        _textMesh.text = _state;
        _textMesh.anchor = TextAnchor.MiddleCenter;
        _textMesh.alignment = TextAlignment.Center;
        _textMesh.fontSize = _fontSize;
        _textMesh.characterSize = _characterSize;
        _textMesh.fontStyle = _fontStyle;
        _textMesh.color = _color;

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font != null)
        {
            _textMesh.font = font;
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial = font.material;
        }

        // Render on top of most geometry so it's visible through walls.
        var renderer = go.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = 5000;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    private void OnDestroy()
    {
        if (_labelTransform != null)
        {
            if (Application.isPlaying) Destroy(_labelTransform.gameObject);
            else DestroyImmediate(_labelTransform.gameObject);
        }
    }
    #endregion
}
