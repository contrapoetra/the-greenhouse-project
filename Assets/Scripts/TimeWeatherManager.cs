using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Greenhouse Project - Time & Weather Manager
/// Mengelola tampilan waktu real-time dan sistem cuaca simulasi.
/// 
/// Setup di Inspector:
/// 1. Attach script ini ke GameObject "WeatherTimePanel"
/// 2. Drag referensi UI ke slot yang tersedia
/// 3. Assign sprite icon dari UI Assets
/// </summary>
public class TimeWeatherManager : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // SINGLETON
    // ─────────────────────────────────────────────
    public static TimeWeatherManager Instance { get; private set; }

    // ─────────────────────────────────────────────
    // ENUMS
    // ─────────────────────────────────────────────
    public enum WeatherType
    {
        Sunny,   // Cerah  → suhu naik
        Cloudy,  // Mendung → cahaya berkurang
        Rainy    // Hujan   → kelembaban naik
    }

    // ─────────────────────────────────────────────
    // UI REFERENCES
    // ─────────────────────────────────────────────
    [Header("── UI Text ──")]
    [Tooltip("Text yang menampilkan 'Day X - HH:MM AM/PM'")]
    public TextMeshProUGUI dayTimeText;

    [Tooltip("Text yang menampilkan 'Cuaca: ...'")]
    public TextMeshProUGUI weatherText;

    [Header("── UI Images ──")]
    [Tooltip("Image icon siang/malam (sun atau moon)")]
    public Image dayIcon;

    [Tooltip("Image icon cuaca")]
    public Image weatherIcon;

    // ─────────────────────────────────────────────
    // SPRITE ASSETS
    // ─────────────────────────────────────────────
    [Header("── Day/Night Icons ──")]
    public Sprite sunIcon;    // Siang  (06:00 - 18:00)
    public Sprite moonIcon;   // Malam  (18:00 - 06:00)

    [Header("── Weather Icons ──")]
    public Sprite sunnySprite;   // ☀️ Cerah
    public Sprite cloudySprite;  // ☁️ Mendung
    public Sprite rainySprite;   // 🌧️ Hujan

    // ─────────────────────────────────────────────
    // GAME DAY CONFIG
    // ─────────────────────────────────────────────
    [Header("── Day System ──")]
    [Tooltip("Hari awal game dimulai")]
    public int startDay = 1;

    [Tooltip("Durasi satu hari game (dalam menit real). 0 = ikut jam sistem penuh.")]
    [Range(0, 60)]
    public int gameDayDurationMinutes = 0; // 0 berarti ikut jam real

    // ─────────────────────────────────────────────
    // WEATHER CONFIG
    // ─────────────────────────────────────────────
    [Header("── Weather System ──")]
    [Tooltip("Interval pergantian cuaca (detik)")]
    [Range(30, 600)]
    public float weatherChangeDuration = 120f;

    [Tooltip("Kemungkinan cuaca Cerah (0-1)")]
    [Range(0f, 1f)]
    public float sunnyChanc = 0.5f;

    [Tooltip("Kemungkinan cuaca Mendung (0-1)")]
    [Range(0f, 1f)]
    public float cloudyChance = 0.3f;

    // Rainy chance = 1 - sunny - cloudy (otomatis)

    // ─────────────────────────────────────────────
    // EVENTS (untuk sistem lain subscribe)
    // ─────────────────────────────────────────────
    /// <summary>Dipanggil setiap cuaca berubah. Subscribe dari PlantManager, EnvironmentManager, dll.</summary>
    public event Action<WeatherType> OnWeatherChanged;

    /// <summary>Dipanggil setiap pergantian hari.</summary>
    public event Action<int> OnDayChanged;

    // ─────────────────────────────────────────────
    // STATE
    // ─────────────────────────────────────────────
    private WeatherType _currentWeather;
    private int _currentDay;
    private DateTime _gameStartTime;
    private float _weatherTimer;

    // ─────────────────────────────────────────────
    // PUBLIC PROPERTIES
    // ─────────────────────────────────────────────
    public WeatherType CurrentWeather => _currentWeather;
    public int CurrentDay => _currentDay;

    // ─────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────
    void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        _currentDay = startDay;
        _gameStartTime = DateTime.Now;
        _weatherTimer = weatherChangeDuration;

        // Cuaca awal: random sesuai probabilitas
        SetWeather(RollWeather());

        // Update UI pertama kali
        UpdateTimeUI();
    }

    void Update()
    {
        UpdateTimeUI();
        UpdateWeatherTimer();
    }

    // ─────────────────────────────────────────────
    // TIME SYSTEM
    // ─────────────────────────────────────────────

    /// <summary>
    /// Update tampilan waktu dan hari setiap frame.
    /// Jam mengikuti DateTime.Now (jam sistem / jam riil).
    /// </summary>
    void UpdateTimeUI()
    {
        DateTime now = DateTime.Now;

        // ── Format Jam ──
        int hour   = now.Hour;
        int minute = now.Minute;

        string period    = hour >= 12 ? "PM" : "AM";
        int displayHour  = hour % 12;
        if (displayHour == 0) displayHour = 12;

        string timeString = $"{displayHour:D2}:{minute:D2} {period}";

        // ── Hitung Day ──
        // Jika gameDayDurationMinutes > 0, hari berganti berdasarkan durasi game
        // Jika 0, hari tidak berganti otomatis (berdasarkan event/manual)
        if (gameDayDurationMinutes > 0)
        {
            double minutesElapsed = (DateTime.Now - _gameStartTime).TotalMinutes;
            int calculatedDay     = startDay + (int)(minutesElapsed / gameDayDurationMinutes);

            if (calculatedDay != _currentDay)
            {
                _currentDay = calculatedDay;
                OnDayChanged?.Invoke(_currentDay);
                // Ganti cuaca tiap pergantian hari juga
                SetWeather(RollWeather());
            }
        }

        // ── Update Text ──
        if (dayTimeText != null)
            dayTimeText.text = $"Day {_currentDay} - {timeString}";

        // ── Icon Siang/Malam ──
        if (dayIcon != null)
        {
            bool isDaytime = hour >= 6 && hour < 18;
            dayIcon.sprite = isDaytime ? sunIcon : moonIcon;
        }
    }

    // ─────────────────────────────────────────────
    // WEATHER SYSTEM
    // ─────────────────────────────────────────────

    /// <summary>
    /// Timer untuk pergantian cuaca otomatis.
    /// </summary>
    void UpdateWeatherTimer()
    {
        _weatherTimer -= Time.deltaTime;

        if (_weatherTimer <= 0f)
        {
            _weatherTimer = weatherChangeDuration;
            SetWeather(RollWeather()); // Ganti cuaca
        }
    }

    /// <summary>
    /// Set cuaca baru dan update semua UI.
    /// Bisa dipanggil dari luar (misal: EventSystem, cutscene, debug).
    /// </summary>
    public void SetWeather(WeatherType newWeather)
    {
        _currentWeather = newWeather;

        UpdateWeatherUI();

        // Broadcast ke sistem lain
        OnWeatherChanged?.Invoke(_currentWeather);
    }

    /// <summary>
    /// Update tampilan icon dan teks cuaca.
    /// </summary>
    void UpdateWeatherUI()
    {
        if (weatherText == null || weatherIcon == null) return;

        switch (_currentWeather)
        {
            case WeatherType.Sunny:
                weatherIcon.sprite = sunnySprite;
                weatherText.text   = "Cuaca: Cerah";
                break;

            case WeatherType.Cloudy:
                weatherIcon.sprite = cloudySprite;
                weatherText.text   = "Cuaca: Mendung";
                break;

            case WeatherType.Rainy:
                weatherIcon.sprite = rainySprite;
                weatherText.text   = "Cuaca: Hujan";
                break;
        }
    }

    /// <summary>
    /// Menentukan cuaca secara random berdasarkan probabilitas yang bisa diatur di Inspector.
    /// </summary>
    WeatherType RollWeather()
    {
        // Normalisasi agar total tidak melebihi 1
        float totalChance = Mathf.Clamp01(sunnyChanc + cloudyChance);
        float rainyChance = Mathf.Max(0f, 1f - totalChance);

        float roll = UnityEngine.Random.value; // 0.0 - 1.0

        if (roll < sunnyChanc)
            return WeatherType.Sunny;
        else if (roll < sunnyChanc + cloudyChance)
            return WeatherType.Cloudy;
        else
            return WeatherType.Rainy;
    }

    // ─────────────────────────────────────────────
    // PUBLIC HELPERS
    // ─────────────────────────────────────────────

    /// <summary>
    /// Tambah hari secara manual. Gunakan dari sistem lain (misal: tidur/istirahat).
    /// </summary>
    public void AdvanceDay(int amount = 1)
    {
        _currentDay += amount;
        OnDayChanged?.Invoke(_currentDay);
        SetWeather(RollWeather());
    }

    /// <summary>
    /// Force set cuaca spesifik (untuk testing atau cutscene).
    /// </summary>
    public void ForceWeather(WeatherType weather)
    {
        _weatherTimer = weatherChangeDuration; // Reset timer
        SetWeather(weather);
    }

    /// <summary>
    /// Kembalikan deskripsi cuaca sebagai string (untuk sistem lain).
    /// </summary>
    public string GetWeatherDescription()
    {
        return _currentWeather switch
        {
            WeatherType.Sunny  => "Cerah",
            WeatherType.Cloudy => "Mendung",
            WeatherType.Rainy  => "Hujan",
            _                  => "Tidak Diketahui"
        };
    }
}