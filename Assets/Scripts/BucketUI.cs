using UnityEngine;
using TMPro;

/// <summary>
/// Floating UI for the bucket that appears only when holding a melon.
/// Includes Inspector controls for easy fine-tuning.
/// </summary>
public class BucketUI : MonoBehaviour
{
    [Header("── UI Settings ──")]
    [Tooltip("How high above the bucket the text floats")]
    public float verticalOffset = 0.8f;
    
    [Tooltip("The size of the floating text object")]
    public Vector3 uiScale = new Vector3(0.005f, 0.005f, 0.005f);

    [Tooltip("Font size of the text")]
    public float fontSize = 24f;

    private GameObject _uiCanvas;
    private TextMeshProUGUI _promptText;
    private Camera _mainCamera;

    void Start()
    {
        _mainCamera = Camera.main;
        Debug.Log($"[BucketUI] Initialized on {gameObject.name}");

        // Create a world-space canvas for the text
        GameObject canvasGO = new GameObject("BucketCanvas");
        canvasGO.transform.SetParent(transform);
        
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        RectTransform rect = canvasGO.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(4, 2);

        // Add Text
        GameObject textGO = new GameObject("StoragePromptText");
        textGO.transform.SetParent(canvasGO.transform, false);
        
        _promptText = textGO.AddComponent<TextMeshProUGUI>();
        _promptText.alignment = TextAlignmentOptions.Center;
        _promptText.text = "Simpan\ndi Sini";
        _promptText.color = Color.white;

        _uiCanvas = canvasGO;
    }

    void Update()
    {
        if (_uiCanvas == null) return;

        // Visibility based on holding a melon
        bool showUI = ClickEvent.Instance != null && ClickEvent.Instance.IsHoldingMelon;
        _uiCanvas.SetActive(showUI);

        if (!showUI) return;

        // --- Apply Fine-tuning from Inspector ---
        _uiCanvas.transform.localPosition = new Vector3(0, verticalOffset, 0);
        _uiCanvas.transform.localScale = uiScale;
        if (_promptText != null) _promptText.fontSize = fontSize;

        // Billboard effect (face camera)
        if (_mainCamera != null)
        {
            _uiCanvas.transform.LookAt(_uiCanvas.transform.position + _mainCamera.transform.rotation * Vector3.forward,
                                      _mainCamera.transform.rotation * Vector3.up);
        }
    }
}
