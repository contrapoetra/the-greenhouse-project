using UnityEngine;

/// <summary>
/// Greenhouse Project - Environment Manager
/// Menerima event cuaca dari TimeWeatherManager dan menerapkan efek
/// ke kondisi lingkungan greenhouse (suhu, kelembaban, cahaya).
/// 
/// Attach ke GameObject "greenhouse" atau "EnvironmentSystem".
/// </summary>
public class EnvironmentManager : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // SINGLETON
    // ─────────────────────────────────────────────
    public static EnvironmentManager Instance { get; private set; }

    // ─────────────────────────────────────────────
    // CONFIG
    // ─────────────────────────────────────────────
    [Header("── Kondisi Normal Greenhouse ──")]
    [Range(20f, 35f)] public float baseTemperature  = 26f;  // °C
    [Range(40f, 80f)] public float baseHumidity     = 60f;  // %
    [Range(0f, 1f)]   public float baseLightLevel   = 0.8f; // 0-1

    [Header("── Pengaruh Cuaca ──")]
    public float sunnyTempBonus    = +5f;   // Cerah → suhu naik
    public float sunnyHumidPenalty = -10f;  // Cerah → kelembaban turun

    public float cloudyLightPenalty = -0.3f; // Mendung → cahaya berkurang

    public float rainyHumidBonus   = +20f;  // Hujan → kelembaban naik
    public float rainyTempPenalty  = -3f;   // Hujan → suhu turun sedikit

    // ─────────────────────────────────────────────
    // STATE (Read-only dari luar)
    // ─────────────────────────────────────────────
    public float Temperature  { get; private set; }
    public float Humidity     { get; private set; }
    public float LightLevel   { get; private set; }

    // ─────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Set nilai awal
        Temperature = baseTemperature;
        Humidity    = baseHumidity;
        LightLevel  = baseLightLevel;
    }

    void OnEnable()
    {
        // Subscribe ke event cuaca
        if (TimeWeatherManager.Instance != null)
            TimeWeatherManager.Instance.OnWeatherChanged += ApplyWeatherEffect;
    }

    void OnDisable()
    {
        // Unsubscribe saat disabled
        if (TimeWeatherManager.Instance != null)
            TimeWeatherManager.Instance.OnWeatherChanged -= ApplyWeatherEffect;
    }

    // ─────────────────────────────────────────────
    // WEATHER EFFECT
    // ─────────────────────────────────────────────

    /// <summary>
    /// Dipanggil otomatis saat cuaca berubah dari TimeWeatherManager.
    /// Mengubah nilai lingkungan sesuai jenis cuaca.
    /// </summary>
    void ApplyWeatherEffect(TimeWeatherManager.WeatherType weather)
    {
        // Reset ke baseline dulu
        Temperature = baseTemperature;
        Humidity    = baseHumidity;
        LightLevel  = baseLightLevel;

        switch (weather)
        {
            case TimeWeatherManager.WeatherType.Sunny:
                Temperature += sunnyTempBonus;
                Humidity    += sunnyHumidPenalty;
                // LightLevel tidak berubah (tetap penuh)
                break;

            case TimeWeatherManager.WeatherType.Cloudy:
                LightLevel  += cloudyLightPenalty;
                // Suhu & kelembaban tidak berubah signifikan
                break;

            case TimeWeatherManager.WeatherType.Rainy:
                Humidity    += rainyHumidBonus;
                Temperature += rainyTempPenalty;
                LightLevel  += cloudyLightPenalty; // Hujan juga mengurangi cahaya
                break;
        }

        // Clamp agar tidak out of range
        Temperature = Mathf.Clamp(Temperature, 15f, 45f);
        Humidity    = Mathf.Clamp(Humidity,     0f, 100f);
        LightLevel  = Mathf.Clamp01(LightLevel);

        Debug.Log($"[Environment] Cuaca: {weather} | Suhu: {Temperature:F1}°C | " +
                  $"Kelembaban: {Humidity:F1}% | Cahaya: {LightLevel:F2}");
    }

    // ─────────────────────────────────────────────
    // PUBLIC HELPERS
    // ─────────────────────────────────────────────

    /// <summary>
    /// Cek apakah kondisi greenhouse optimal untuk pertumbuhan tanaman.
    /// </summary>
    public bool IsOptimalForGrowth()
    {
        bool tempOk  = Temperature >= 22f && Temperature <= 32f;
        bool humidOk = Humidity    >= 50f && Humidity    <= 80f;
        bool lightOk = LightLevel  >= 0.5f;

        return tempOk && humidOk && lightOk;
    }

    /// <summary>
    /// Ambil ringkasan kondisi lingkungan sebagai string (untuk UI debug).
    /// </summary>
    public string GetConditionSummary()
    {
        return $"Suhu: {Temperature:F1}°C | Kelembaban: {Humidity:F1}% | Cahaya: {LightLevel * 100:F0}%";
    }
}