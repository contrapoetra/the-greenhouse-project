using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Greenhouse Project - Environment HUD
/// Menampilkan kondisi greenhouse (suhu, kelembaban, cahaya) secara real-time di layar.
///
/// Setup:
/// 1. Attach script ini ke GameObject "EnvironmentHUD" di dalam Canvas
/// 2. Assign semua referensi UI di Inspector
/// 3. Pastikan EnvironmentManager sudah ada di scene
/// </summary>
public class EnvironmentHUD : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // REFERENCES - Drag dari Inspector
    // ─────────────────────────────────────────────

    [Header("── Progress Bars ──")]
    public Image barTemperature;
    public Image barHumidity;
    public Image barLight;

    [Header("── Value Texts ──")]
    public TextMeshProUGUI textTemperature;
    public TextMeshProUGUI textHumidity;
    public TextMeshProUGUI textLight;

    [Header("── Status ──")]
    public TextMeshProUGUI textStatus;
    public Image statusDot;

    [Header("── Panel ──")]
    [Tooltip("Root panel HUD, untuk toggle show/hide dengan tombol H")]
    public GameObject hudPanel;

    // ─────────────────────────────────────────────
    // COLOR CONFIG
    // ─────────────────────────────────────────────

    [Header("── Warna Bar ──")]
    public Color colorOptimal  = new Color(0.59f, 0.77f, 0.35f); // Hijau
    public Color colorWarning  = new Color(0.94f, 0.62f, 0.15f); // Kuning
    public Color colorDanger   = new Color(0.89f, 0.29f, 0.29f); // Merah

    // ─────────────────────────────────────────────
    // RANGE CONFIG (untuk normalisasi bar)
    // ─────────────────────────────────────────────

    [Header("── Range Suhu (°C) ──")]
    public float tempMin = 15f;
    public float tempMax = 45f;
    [Tooltip("Batas bawah suhu optimal")]
    public float tempOptimalMin = 22f;
    [Tooltip("Batas atas suhu optimal")]
    public float tempOptimalMax = 32f;

    [Header("── Range Kelembaban (%) ──")]
    public float humMin = 0f;
    public float humMax = 100f;
    public float humOptimalMin = 50f;
    public float humOptimalMax = 80f;

    [Header("── Range Cahaya (0-1) ──")]
    public float lightOptimalMin = 0.5f;

    // ─────────────────────────────────────────────
    // STATE
    // ─────────────────────────────────────────────

    private bool _hudVisible = true;
    private float _updateInterval = 0.2f; // Update 5x per detik, hemat performa
    private float _updateTimer;

    // ─────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────

    void Start()
    {
        if (hudPanel != null)
            hudPanel.SetActive(true);

        // Langsung update sekali di awal
        UpdateHUD();
    }

    void Update()
    {
        // Toggle HUD dengan tombol H
        if (Input.GetKeyDown(KeyCode.H))
            ToggleHUD();

        // Progress growth with G key (Debug)
        if (Input.GetKeyDown(KeyCode.G))
        {
            if (PlantManager.Instance != null)
                PlantManager.Instance.GrowAllPlants();
        }

        // Update berkala (tidak perlu tiap frame)
        _updateTimer -= Time.deltaTime;
        if (_updateTimer <= 0f)
        {
            _updateTimer = _updateInterval;
            UpdateHUD();
        }
    }

    // ─────────────────────────────────────────────
    // CORE UPDATE
    // ─────────────────────────────────────────────

    public void OnGrowButtonPressed()
    {
        if (PlantManager.Instance != null)
            PlantManager.Instance.GrowAllPlants();
    }

    void UpdateHUD()
    {
        if (EnvironmentManager.Instance == null) return;

        float temp  = EnvironmentManager.Instance.Temperature;
        float hum   = EnvironmentManager.Instance.Humidity;
        float light = EnvironmentManager.Instance.LightLevel;

        // ── Update Bar & Teks ──
        UpdateBar(barTemperature, textTemperature,
            Normalize(temp, tempMin, tempMax),
            GetTempColor(temp),
            temp.ToString("F1") + "°C");

        UpdateBar(barHumidity, textHumidity,
            Normalize(hum, humMin, humMax),
            GetHumColor(hum),
            hum.ToString("F1") + "%");

        UpdateBar(barLight, textLight,
            light,
            GetLightColor(light),
            (light * 100f).ToString("F0") + "%");

        // ── Update Status ──
        UpdateStatus(temp, hum, light);
    }

    // ─────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────

    void UpdateBar(Image bar, TextMeshProUGUI label, float fillAmount, Color color, string displayText)
    {
        if (bar   != null) { bar.fillAmount = Mathf.Clamp01(fillAmount); bar.color = color; }
        if (label != null) { label.text = displayText; label.color = color; }
    }

    void UpdateStatus(float temp, float hum, float light)
    {
        bool tempOk  = temp  >= tempOptimalMin  && temp  <= tempOptimalMax;
        bool humOk   = hum   >= humOptimalMin   && hum   <= humOptimalMax;
        bool lightOk = light >= lightOptimalMin;

        string statusMsg;
        Color  dotColor;

        if (tempOk && humOk && lightOk)
        {
            statusMsg = "Kondisi optimal";
            dotColor  = colorOptimal;
        }
        else if (!tempOk && temp > tempOptimalMax)
        {
            statusMsg = "Suhu terlalu tinggi!";
            dotColor  = colorDanger;
        }
        else if (!tempOk && temp < tempOptimalMin)
        {
            statusMsg = "Suhu terlalu rendah!";
            dotColor  = colorDanger;
        }
        else if (!humOk && hum > humOptimalMax)
        {
            statusMsg = "Kelembaban terlalu tinggi!";
            dotColor  = colorDanger;
        }
        else if (!humOk && hum < humOptimalMin)
        {
            statusMsg = "Kelembaban terlalu rendah!";
            dotColor  = colorDanger;
        }
        else if (!lightOk)
        {
            statusMsg = "Cahaya kurang";
            dotColor  = colorWarning;
        }
        else
        {
            statusMsg = "Perhatikan kondisi";
            dotColor  = colorWarning;
        }

        if (textStatus != null) { textStatus.text = statusMsg; textStatus.color = dotColor; }
        if (statusDot  != null)   statusDot.color  = dotColor;
    }

    // Normalisasi nilai ke 0-1 untuk fillAmount bar
    float Normalize(float value, float min, float max)
        => Mathf.Clamp01((value - min) / (max - min));

    // ─────────────────────────────────────────────
    // COLOR LOGIC
    // ─────────────────────────────────────────────

    Color GetTempColor(float temp)
    {
        if (temp >= tempOptimalMin && temp <= tempOptimalMax) return colorOptimal;
        float dist = temp < tempOptimalMin
            ? tempOptimalMin - temp
            : temp - tempOptimalMax;
        return dist > 5f ? colorDanger : colorWarning;
    }

    Color GetHumColor(float hum)
    {
        if (hum >= humOptimalMin && hum <= humOptimalMax) return colorOptimal;
        float dist = hum < humOptimalMin
            ? humOptimalMin - hum
            : hum - humOptimalMax;
        return dist > 15f ? colorDanger : colorWarning;
    }

    Color GetLightColor(float light)
    {
        if (light >= lightOptimalMin) return colorOptimal;
        return light < 0.3f ? colorDanger : colorWarning;
    }

    // ─────────────────────────────────────────────
    // PUBLIC
    // ─────────────────────────────────────────────

    public void ToggleHUD()
    {
        _hudVisible = !_hudVisible;
        if (hudPanel != null)
            hudPanel.SetActive(_hudVisible);
    }
}