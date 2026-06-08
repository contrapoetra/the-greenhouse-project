using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System.Collections;

/// <summary>
/// Greenhouse Project - Weather Manager (HDRP)
/// Mengontrol visual cuaca luar greenhouse secara runtime:
///   • Directional Light (intensitas + warna)
///   • HDRP Volume (fog density, sky exposure)
///   • Rain Particle System (dibuat otomatis dari kode)
///   • Ambient Sound (via SoundManager)
///
/// Setup:
/// 1. Attach script ini ke GameObject baru bernama "WeatherManager"
/// 2. Assign referensi di Inspector (lihat komentar tiap field)
/// 3. Pastikan scene punya Global Volume dengan PhysicallyBasedSky + Fog
/// </summary>
public class WeatherManager : MonoBehaviour
{
    public static WeatherManager Instance { get; private set; }

    // ─────────────────────────────────────────────
    // REFERENCES
    // ─────────────────────────────────────────────

    [Header("── Lighting ──")]
    [Tooltip("Drag Directional Light (Sun) dari Hierarchy")]
    public Light directionalLight;

    [Tooltip("HDAdditionalLightData pada Directional Light — drag komponen yang sama")]
    public HDAdditionalLightData hdLightData;

    [Header("── HDRP Volume ──")]
    [Tooltip("Drag Global Volume yang ada di scene (berisi PhysicallyBasedSky & Fog)")]
    public Volume globalVolume;

    [Header("── Rain Particle ──")]
    [Tooltip("Kosongkan — script ini akan buat otomatis. " +
             "Atau drag prefab rain particle-mu kalau sudah punya.")]
    public ParticleSystem rainParticleSystem;

    [Tooltip("Di bawah kamera / player — tempat spawn partikel hujan")]
    public Transform rainSpawnPoint;

    [Header("── Sound ──")]
    [Tooltip("AudioSource khusus ambient cuaca (pisah dari BGM)")]
    public AudioSource ambientAudioSource;

    [Tooltip("Clip untuk cuaca Cerah (suara burung, angin sepoi)")]
    public AudioClip soundSunny;

    [Tooltip("Clip untuk cuaca Mendung (angin, suasana sepi)")]
    public AudioClip soundCloudy;

    [Tooltip("Clip untuk cuaca Hujan")]
    public AudioClip soundRainy;

    // ─────────────────────────────────────────────
    // CONFIG PER CUACA
    // ─────────────────────────────────────────────

    [Header("── Cerah ──")]
    public float sunnyLightIntensity   = 130000f; // Lux (HDRP physical unit)
    public Color sunnyLightColor       = new Color(1f, 0.95f, 0.8f);
    public float sunnyFogAttenuation   = 2000f;   // Makin besar = fog makin tipis
    public float sunnyExposure         = 0f;      // Sky exposure offset

    [Header("── Mendung ──")]
    public float cloudyLightIntensity  = 60000f;
    public Color cloudyLightColor      = new Color(0.8f, 0.85f, 0.9f);
    public float cloudyFogAttenuation  = 600f;    // Fog lebih tebal
    public float cloudyExposure        = -0.5f;

    [Header("── Hujan ──")]
    public float rainyLightIntensity   = 35000f;
    public Color rainyLightColor       = new Color(0.6f, 0.65f, 0.75f);
    public float rainyFogAttenuation   = 250f;    // Fog tebal
    public float rainyExposure         = -1f;

    [Header("── Transisi ──")]
    [Range(0.5f, 10f)]
    [Tooltip("Durasi transisi saat cuaca berganti (detik)")]
    public float transitionDuration    = 3f;

    [Range(0f, 1f)]
    [Tooltip("Volume ambient sound cuaca")]
    public float ambientVolume         = 0.5f;

    // ─────────────────────────────────────────────
    // PRIVATE STATE
    // ─────────────────────────────────────────────

    private Fog            _fog;
    private PhysicallyBasedSky _sky;
    private Coroutine      _transitionCoroutine;

    // Target values (hasil transisi)
    private float _targetLightIntensity;
    private Color _targetLightColor;
    private float _targetFogAttenuation;
    private float _targetExposure;

    // ─────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        // Ambil override dari Global Volume
        if (globalVolume != null)
        {
            globalVolume.profile.TryGet(out _fog);
            globalVolume.profile.TryGet(out _sky);
        }
        else
        {
            Debug.LogWarning("[WeatherManager] Global Volume belum di-assign!");
        }

        // Buat rain particle system kalau belum ada
        if (rainParticleSystem == null)
            CreateRainParticleSystem();

        // Subscribe ke event cuaca
        if (TimeWeatherManager.Instance != null)
            TimeWeatherManager.Instance.OnWeatherChanged += OnWeatherChanged;
        else
            Debug.LogWarning("[WeatherManager] TimeWeatherManager tidak ditemukan!");

        // Terapkan cuaca awal
        if (TimeWeatherManager.Instance != null)
            ApplyWeatherImmediate(TimeWeatherManager.Instance.CurrentWeather);
    }

    void OnDestroy()
    {
        if (TimeWeatherManager.Instance != null)
            TimeWeatherManager.Instance.OnWeatherChanged -= OnWeatherChanged;
    }

    // ─────────────────────────────────────────────
    // EVENT HANDLER
    // ─────────────────────────────────────────────

    void OnWeatherChanged(TimeWeatherManager.WeatherType weather)
    {
        // Hentikan transisi sebelumnya kalau masih berjalan
        if (_transitionCoroutine != null)
            StopCoroutine(_transitionCoroutine);

        _transitionCoroutine = StartCoroutine(TransitionWeather(weather));
    }

    // ─────────────────────────────────────────────
    // TRANSISI
    // ─────────────────────────────────────────────

    IEnumerator TransitionWeather(TimeWeatherManager.WeatherType weather)
    {
        // Tentukan target nilai berdasarkan cuaca
        SetTargetValues(weather);

        // Simpan nilai awal
        float startIntensity    = directionalLight != null ? directionalLight.intensity : 0f;
        Color startColor        = directionalLight != null ? directionalLight.color     : Color.white;
        float startFogAtten     = _fog != null && _fog.meanFreePath.overrideState
                                  ? _fog.meanFreePath.value : sunnyFogAttenuation;
        float startExposure     = _sky != null && _sky.exposure.overrideState
                                  ? _sky.exposure.value : 0f;

        float elapsed = 0f;

        // Ganti sound segera (tidak perlu ditransisi)
        PlayAmbientSound(weather);

        // Aktif/nonaktifkan rain particle
        SetRainActive(weather == TimeWeatherManager.WeatherType.Rainy);

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration);

            // Interpolasi lighting
            if (directionalLight != null)
            {
                directionalLight.intensity = Mathf.Lerp(startIntensity, _targetLightIntensity, t);
                directionalLight.color     = Color.Lerp(startColor,     _targetLightColor,     t);
            }

            if (hdLightData != null)
                hdLightData.SetIntensity(Mathf.Lerp(startIntensity, _targetLightIntensity, t));

            // Interpolasi fog
            if (_fog != null)
            {
                _fog.meanFreePath.Override(Mathf.Lerp(startFogAtten, _targetFogAttenuation, t));
            }

            // Interpolasi sky exposure
            if (_sky != null)
            {
                _sky.exposure.Override(Mathf.Lerp(startExposure, _targetExposure, t));
            }

            yield return null;
        }

        // Pastikan tepat di target
        ApplyWeatherImmediate(weather);
    }

    // ─────────────────────────────────────────────
    // HELPER
    // ─────────────────────────────────────────────

    void SetTargetValues(TimeWeatherManager.WeatherType weather)
    {
        switch (weather)
        {
            case TimeWeatherManager.WeatherType.Sunny:
                _targetLightIntensity  = sunnyLightIntensity;
                _targetLightColor      = sunnyLightColor;
                _targetFogAttenuation  = sunnyFogAttenuation;
                _targetExposure        = sunnyExposure;
                break;

            case TimeWeatherManager.WeatherType.Cloudy:
                _targetLightIntensity  = cloudyLightIntensity;
                _targetLightColor      = cloudyLightColor;
                _targetFogAttenuation  = cloudyFogAttenuation;
                _targetExposure        = cloudyExposure;
                break;

            case TimeWeatherManager.WeatherType.Rainy:
                _targetLightIntensity  = rainyLightIntensity;
                _targetLightColor      = rainyLightColor;
                _targetFogAttenuation  = rainyFogAttenuation;
                _targetExposure        = rainyExposure;
                break;
        }
    }

    /// <summary>
    /// Terapkan cuaca langsung tanpa transisi (untuk inisialisasi awal).
    /// </summary>
    void ApplyWeatherImmediate(TimeWeatherManager.WeatherType weather)
    {
        SetTargetValues(weather);

        if (directionalLight != null)
        {
            directionalLight.intensity = _targetLightIntensity;
            directionalLight.color     = _targetLightColor;
        }

        if (hdLightData != null)
            hdLightData.SetIntensity(_targetLightIntensity);

        if (_fog != null)
            _fog.meanFreePath.Override(_targetFogAttenuation);

        if (_sky != null)
            _sky.exposure.Override(_targetExposure);

        PlayAmbientSound(weather);
        SetRainActive(weather == TimeWeatherManager.WeatherType.Rainy);
    }

    void PlayAmbientSound(TimeWeatherManager.WeatherType weather)
    {
        if (ambientAudioSource == null) return;

        AudioClip clip = weather switch
        {
            TimeWeatherManager.WeatherType.Sunny  => soundSunny,
            TimeWeatherManager.WeatherType.Cloudy => soundCloudy,
            TimeWeatherManager.WeatherType.Rainy  => soundRainy,
            _                                     => null
        };

        if (clip == null) return;
        if (ambientAudioSource.clip == clip && ambientAudioSource.isPlaying) return;

        ambientAudioSource.clip   = clip;
        ambientAudioSource.loop   = true;
        ambientAudioSource.volume = ambientVolume;
        ambientAudioSource.Play();
    }

    void SetRainActive(bool active)
    {
        if (rainParticleSystem == null) return;

        if (active && !rainParticleSystem.isPlaying)
            rainParticleSystem.Play();
        else if (!active && rainParticleSystem.isPlaying)
            rainParticleSystem.Stop();
    }

    // ─────────────────────────────────────────────
    // AUTO-CREATE RAIN PARTICLE
    // ─────────────────────────────────────────────

    /// <summary>
    /// Membuat Rain Particle System secara programatik.
    /// Attach ke rainSpawnPoint (ikut kamera), atau ke WeatherManager jika tidak ada.
    /// </summary>
    void CreateRainParticleSystem()
    {
        Transform parent = rainSpawnPoint != null ? rainSpawnPoint : transform;

        GameObject rainGO = new GameObject("RainParticleSystem");
        rainGO.transform.SetParent(parent);
        // Tidak ada rotasi pada GO — rotasi diatur di dalam shape module
        rainGO.transform.localPosition = Vector3.zero;
        rainGO.transform.localRotation = Quaternion.identity;

        ParticleSystem ps = rainGO.AddComponent<ParticleSystem>();

        // ── Main Module ──
        var main               = ps.main;
        main.startLifetime     = new ParticleSystem.MinMaxCurve(1.2f, 1.8f);
        main.startSpeed        = new ParticleSystem.MinMaxCurve(18f, 25f);
        main.startSize         = new ParticleSystem.MinMaxCurve(0.04f, 0.08f);
        main.startColor        = new Color(0.7f, 0.85f, 1f, 0.55f);
        main.gravityModifier   = 2.5f;
        main.simulationSpace   = ParticleSystemSimulationSpace.World;
        main.maxParticles      = 5000;

        // ── Emission ──
        var emission           = ps.emission;
        emission.rateOverTime  = 800f;

        // ── Shape: Box horizontal lebar di atas player ──
        // scale X=lebar kiri-kanan, Y=lebar depan-belakang, Z=tipis (ketebalan box)
        // position.y = 15 → box mengambang 15m di atas posisi player
        // rotation X = 90 → putar SHAPE saja agar emit ke bawah, tanpa putar GO
        var shape              = ps.shape;
        shape.enabled          = true;
        shape.shapeType        = ParticleSystemShapeType.Box;
        shape.scale            = new Vector3(80f, 80f, 0.5f);
        shape.position         = new Vector3(0f, 15f, 0f);
        shape.rotation         = new Vector3(90f, 0f, 0f);

        // ── Velocity over Lifetime: angin tipis horizontal ──
        var vel                = ps.velocityOverLifetime;
        vel.enabled            = true;
        vel.space              = ParticleSystemSimulationSpace.World;
        vel.x                  = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);
        vel.y                  = new ParticleSystem.MinMaxCurve(0f, 0f);
        vel.z                  = new ParticleSystem.MinMaxCurve(0f, 0f);

        // ── Renderer: stretch vertikal agar terlihat seperti tetes jatuh ──
        var renderer           = rainGO.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode    = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.05f;
        renderer.lengthScale   = 2f;

        // Gunakan material default HDRP particle
        renderer.material      = new Material(Shader.Find("HDRP/Unlit"))
        {
            color = new Color(0.7f, 0.85f, 1f, 0.5f)
        };

        rainParticleSystem = ps;
        ps.Stop(); // Mulai dalam kondisi mati
    }
}