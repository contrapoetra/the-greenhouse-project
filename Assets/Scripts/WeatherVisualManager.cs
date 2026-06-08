using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Greenhouse Project - Weather Visual Manager
/// Menerapkan efek visual nyata saat cuaca berubah:
/// - Sunny  : cahaya terang, langit biru, tidak hujan
/// - Cloudy : cahaya redup, langit abu-abu, tidak hujan
/// - Rainy  : cahaya sangat redup, langit gelap, partikel hujan aktif
///
/// Setup: Lihat arahan di bagian bawah file ini atau di README.
/// </summary>
public class WeatherVisualManager : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // REFERENCES — Drag dari Inspector
    // ─────────────────────────────────────────────

    [Header("── Lighting ──")]
    [Tooltip("Drag 'Sun' Directional Light dari Hierarchy")]
    public Light sunLight;

    [Tooltip("Warna cahaya saat Cerah (kuning hangat)")]
    public Color sunnyLightColor = new Color(1f, 0.96f, 0.84f);

    [Tooltip("Warna cahaya saat Mendung (putih keabu-abuan)")]
    public Color cloudyLightColor = new Color(0.78f, 0.82f, 0.88f);

    [Tooltip("Warna cahaya saat Hujan (abu-abu gelap kebiruan)")]
    public Color rainyLightColor = new Color(0.55f, 0.62f, 0.72f);

    [Header("── Intensity ──")]
    [Tooltip("Intensitas cahaya saat Cerah")]
    [Range(0f, 3f)] public float sunnyIntensity = 1.5f;

    [Tooltip("Intensitas cahaya saat Mendung")]
    [Range(0f, 3f)] public float cloudyIntensity = 0.6f;

    [Tooltip("Intensitas cahaya saat Hujan")]
    [Range(0f, 3f)] public float rainyIntensity = 0.3f;

    [Header("── Fog ──")]
    [Tooltip("Aktifkan perubahan fog saat cuaca berubah")]
    public bool useFog = true;

    public Color sunnyFogColor  = new Color(0.80f, 0.88f, 0.95f);
    public Color cloudyFogColor = new Color(0.60f, 0.63f, 0.67f);
    public Color rainyFogColor  = new Color(0.40f, 0.44f, 0.50f);

    [Range(0f, 0.1f)] public float sunnyFogDensity  = 0.005f;
    [Range(0f, 0.1f)] public float cloudyFogDensity = 0.015f;
    [Range(0f, 0.1f)] public float rainyFogDensity  = 0.030f;

    [Header("── Rain Particle ──")]
    [Tooltip("Drag Rain Particle System dari Hierarchy")]
    public ParticleSystem rainParticles;

    [Header("── Skybox Material ──")]
    [Tooltip("(Opsional) Material skybox yang dipakai scene ini")]
    public Material skyboxMaterial;

    [Tooltip("Nama property Exposure/Tint di skybox material kamu")]
    public string skyboxExposureProperty = "_Exposure";

    [Range(0f, 2f)] public float sunnySkyExposure  = 1.3f;
    [Range(0f, 2f)] public float cloudySkyExposure = 0.6f;
    [Range(0f, 2f)] public float rainySkyExposure  = 0.4f;

    [Header("── Transition ──")]
    [Tooltip("Durasi transisi antar cuaca (detik)")]
    [Range(0.5f, 10f)] public float transitionDuration = 3f;

    // ─────────────────────────────────────────────
    // STATE
    // ─────────────────────────────────────────────

    private Coroutine _transitionCoroutine;
    private TimeWeatherManager.WeatherType _currentWeather;

    // ─────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────

    void Start()
    {
        if (useFog) RenderSettings.fog = true;
        StartCoroutine(InitAfterFrame());
    }

    // Tunggu 1 frame agar semua Awake/Start lain selesai,
    // termasuk TimeWeatherManager yang set Instance-nya di Awake.
    IEnumerator InitAfterFrame()
    {
        yield return null; // tunggu 1 frame

        if (TimeWeatherManager.Instance != null)
        {
            // Subscribe event (jika belum dari OnEnable)
            TimeWeatherManager.Instance.OnWeatherChanged -= OnWeatherChanged;
            TimeWeatherManager.Instance.OnWeatherChanged += OnWeatherChanged;

            // Terapkan cuaca saat ini langsung tanpa transisi
            ApplyWeatherImmediate(TimeWeatherManager.Instance.CurrentWeather);
        }
        else
        {
            Debug.LogWarning("[WeatherVisual] TimeWeatherManager.Instance masih null setelah 1 frame. Pastikan ada di scene yang sama.");
        }
    }

    void OnEnable()
    {
        if (TimeWeatherManager.Instance != null)
            TimeWeatherManager.Instance.OnWeatherChanged += OnWeatherChanged;
    }

    void OnDisable()
    {
        if (TimeWeatherManager.Instance != null)
            TimeWeatherManager.Instance.OnWeatherChanged -= OnWeatherChanged;
    }

    // ─────────────────────────────────────────────
    // EVENT CALLBACK
    // ─────────────────────────────────────────────

    void OnWeatherChanged(TimeWeatherManager.WeatherType newWeather)
    {
        if (_currentWeather == newWeather) return;
        _currentWeather = newWeather;

        // Stop transisi sebelumnya jika masih berjalan
        if (_transitionCoroutine != null)
            StopCoroutine(_transitionCoroutine);

        _transitionCoroutine = StartCoroutine(TransitionWeather(newWeather));
    }

    // ─────────────────────────────────────────────
    // TRANSITION COROUTINE
    // ─────────────────────────────────────────────

    IEnumerator TransitionWeather(TimeWeatherManager.WeatherType weather)
    {
        // ── Ambil nilai TARGET berdasarkan cuaca baru ──
        float   targetIntensity  = GetTargetIntensity(weather);
        Color   targetLightColor = GetTargetLightColor(weather);
        Color   targetFogColor   = GetTargetFogColor(weather);
        float   targetFogDensity = GetTargetFogDensity(weather);
        float   targetSkyExposure = GetTargetSkyExposure(weather);

        // ── Ambil nilai AWAL dari kondisi sekarang ──
        float   startIntensity   = sunLight  != null ? sunLight.intensity : 1f;
        Color   startLightColor  = sunLight  != null ? sunLight.color     : Color.white;
        Color   startFogColor    = RenderSettings.fogColor;
        float   startFogDensity  = RenderSettings.fogDensity;
        float   startSkyExposure = skyboxMaterial != null && skyboxMaterial.HasProperty(skyboxExposureProperty)
                                   ? skyboxMaterial.GetFloat(skyboxExposureProperty)
                                   : 1f;

        // ── Handle Rain ──
        bool shouldRain = (weather == TimeWeatherManager.WeatherType.Rainy);
        if (shouldRain && rainParticles != null && !rainParticles.isPlaying)
            rainParticles.Play();

        // ── Lerp selama transitionDuration ──
        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration);

            // Lighting
            if (sunLight != null)
            {
                sunLight.intensity = Mathf.Lerp(startIntensity,  targetIntensity,  t);
                sunLight.color     = Color.Lerp(startLightColor, targetLightColor, t);
            }

            // Fog
            if (useFog)
            {
                RenderSettings.fogColor   = Color.Lerp(startFogColor,   targetFogColor,   t);
                RenderSettings.fogDensity = Mathf.Lerp(startFogDensity, targetFogDensity, t);
            }

            // Skybox exposure
            if (skyboxMaterial != null && skyboxMaterial.HasProperty(skyboxExposureProperty))
                skyboxMaterial.SetFloat(skyboxExposureProperty,
                    Mathf.Lerp(startSkyExposure, targetSkyExposure, t));

            yield return null;
        }

        // ── Stop rain setelah transisi selesai (jika tidak hujan) ──
        if (!shouldRain && rainParticles != null && rainParticles.isPlaying)
            rainParticles.Stop();

        Debug.Log($"[WeatherVisual] Transisi ke {weather} selesai.");
    }

    // ─────────────────────────────────────────────
    // IMMEDIATE (tanpa transisi — untuk inisialisasi)
    // ─────────────────────────────────────────────

    void ApplyWeatherImmediate(TimeWeatherManager.WeatherType weather)
    {
        _currentWeather = weather;

        if (sunLight != null)
        {
            sunLight.intensity = GetTargetIntensity(weather);
            sunLight.color     = GetTargetLightColor(weather);
        }

        if (useFog)
        {
            RenderSettings.fogColor   = GetTargetFogColor(weather);
            RenderSettings.fogDensity = GetTargetFogDensity(weather);
        }

        if (skyboxMaterial != null && skyboxMaterial.HasProperty(skyboxExposureProperty))
            skyboxMaterial.SetFloat(skyboxExposureProperty, GetTargetSkyExposure(weather));

        if (rainParticles != null)
        {
            if (weather == TimeWeatherManager.WeatherType.Rainy)
                rainParticles.Play();
            else
                rainParticles.Stop();
        }
    }

    // ─────────────────────────────────────────────
    // HELPERS — Getter per cuaca
    // ─────────────────────────────────────────────

    float GetTargetIntensity(TimeWeatherManager.WeatherType w) => w switch
    {
        TimeWeatherManager.WeatherType.Sunny  => sunnyIntensity,
        TimeWeatherManager.WeatherType.Cloudy => cloudyIntensity,
        TimeWeatherManager.WeatherType.Rainy  => rainyIntensity,
        _ => 1f
    };

    Color GetTargetLightColor(TimeWeatherManager.WeatherType w) => w switch
    {
        TimeWeatherManager.WeatherType.Sunny  => sunnyLightColor,
        TimeWeatherManager.WeatherType.Cloudy => cloudyLightColor,
        TimeWeatherManager.WeatherType.Rainy  => rainyLightColor,
        _ => Color.white
    };

    Color GetTargetFogColor(TimeWeatherManager.WeatherType w) => w switch
    {
        TimeWeatherManager.WeatherType.Sunny  => sunnyFogColor,
        TimeWeatherManager.WeatherType.Cloudy => cloudyFogColor,
        TimeWeatherManager.WeatherType.Rainy  => rainyFogColor,
        _ => Color.gray
    };

    float GetTargetFogDensity(TimeWeatherManager.WeatherType w) => w switch
    {
        TimeWeatherManager.WeatherType.Sunny  => sunnyFogDensity,
        TimeWeatherManager.WeatherType.Cloudy => cloudyFogDensity,
        TimeWeatherManager.WeatherType.Rainy  => rainyFogDensity,
        _ => 0.01f
    };

    float GetTargetSkyExposure(TimeWeatherManager.WeatherType w) => w switch
    {
        TimeWeatherManager.WeatherType.Sunny  => sunnySkyExposure,
        TimeWeatherManager.WeatherType.Cloudy => cloudySkyExposure,
        TimeWeatherManager.WeatherType.Rainy  => rainySkyExposure,
        _ => 1f
    };
}