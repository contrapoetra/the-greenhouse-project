using UnityEngine;
using TMPro;

/// <summary>
/// Floating UI for plants to show water level.
/// </summary>
public class PlantUI : MonoBehaviour
{
    private PlantGrowth _plant;
    private GameObject _uiCanvas;
    private TextMeshProUGUI _waterText;
    private Camera _mainCamera;

    void Start()
    {
        _plant = GetComponentInParent<PlantGrowth>();
        _mainCamera = Camera.main;

        // Create a world-space canvas for the text
        GameObject canvasGO = new GameObject("PlantCanvas");
        canvasGO.transform.SetParent(transform);
        canvasGO.transform.localPosition = new Vector3(0, 1.2f, 0); // Position above plant

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        RectTransform rect = canvasGO.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(2, 1);
        rect.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        // Add Text
        GameObject textGO = new GameObject("WaterLevelText");
        textGO.transform.SetParent(canvasGO.transform, false);
        
        _waterText = textGO.AddComponent<TextMeshProUGUI>();
        _waterText.alignment = TextAlignmentOptions.Center;
        _waterText.fontSize = 20;
        _waterText.text = "0%";
        _waterText.color = Color.white;

        _uiCanvas = canvasGO;
    }

    void Update()
    {
        if (_plant == null || _uiCanvas == null) return;

        // Visibility based on holding watering can
        bool showUI = ClickEvent.Instance != null && ClickEvent.Instance.IsHoldingWateringCan;
        _uiCanvas.SetActive(showUI);

        if (!showUI) return;

        // Update percentage
        int percent = Mathf.RoundToInt(_plant.waterLevel * 100);
        _waterText.text = $"{percent}%";

        // Color based on status
        if (_plant.isWatered) _waterText.color = Color.cyan;
        else _waterText.color = Color.white;

        // Billboard effect (face camera)
        if (_mainCamera != null)
        {
            _uiCanvas.transform.LookAt(_uiCanvas.transform.position + _mainCamera.transform.rotation * Vector3.forward,
                                      _mainCamera.transform.rotation * Vector3.up);
        }
    }
}
