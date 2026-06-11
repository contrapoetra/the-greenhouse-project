using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Greenhouse Project - Time & Weather Manager
/// Updated to use in-game time synced with DayProgressionManager.
/// </summary>
public class TimeWeatherManager : MonoBehaviour
{
    public static TimeWeatherManager Instance { get; private set; }

    public enum WeatherType { Sunny, Cloudy, Rainy }

    [Header("── UI Text ──")]
    public TextMeshProUGUI dayTimeText;
    public TextMeshProUGUI weatherText;

    [Header("── UI Images ──")]
    public Image dayIcon;
    public Image weatherIcon;

    [Header("── Sprite Assets ──")]
    public Sprite sunIcon;
    public Sprite moonIcon;
    public Sprite sunnySprite;
    public Sprite cloudySprite;
    public Sprite rainySprite;

    [Header("── Time Settings ──")]
    [Tooltip("How many in-game minutes pass per real second?")]
    public float timeSpeed = 1f; 
    public float startHour = 8f; // Start at 8 AM

    [Header("── Weather Config ──")]
    public float weatherChangeDuration = 120f;
    public float sunnyChance = 0.5f;
    public float cloudyChance = 0.3f;

    public event Action<WeatherType> OnWeatherChanged;
    public event Action<int> OnDayChanged;

    private WeatherType _currentWeather;
    private float _inGameHour;
    private float _weatherTimer;

    public WeatherType CurrentWeather => _currentWeather;
    public int CurrentDay => DayProgressionManager.Instance != null ? DayProgressionManager.Instance.CurrentDay : 0;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log($"[TimeWeatherManager] Awake on {gameObject.name}");
    }

    void Start()
    {
        _inGameHour = startHour;
        _weatherTimer = weatherChangeDuration;
        SetWeather(RollWeather());
        UpdateTimeUI();
    }

    void Update()
    {
        UpdateClock();
        UpdateWeatherTimer();
        UpdateTimeUI();
    }

    void UpdateClock()
    {
        // Advance time
        _inGameHour += (timeSpeed / 60f) * Time.deltaTime;
        if (_inGameHour >= 24f) _inGameHour = 0f;
    }

    public void ResetTime()
    {
        _inGameHour = startHour;
        Debug.Log("[TimeWeatherManager] Clock reset to morning.");
    }

    void UpdateTimeUI()
    {
        // Format Time
        int hours = Mathf.FloorToInt(_inGameHour);
        int minutes = Mathf.FloorToInt((_inGameHour - hours) * 60);
        
        string period = hours >= 12 ? "PM" : "AM";
        int displayHour = hours % 12;
        if (displayHour == 0) displayHour = 12;

        string timeString = $"{displayHour:D2}:{minutes:D2} {period}";
        int day = CurrentDay;

        // Update Text
        if (dayTimeText != null)
            dayTimeText.text = $"Day {day} - {timeString}";

        // Update Icon (Daytime is 6 AM to 6 PM)
        if (dayIcon != null)
        {
            bool isDaytime = hours >= 6 && hours < 18;
            dayIcon.sprite = isDaytime ? sunIcon : moonIcon;
        }
    }

    void UpdateWeatherTimer()
    {
        _weatherTimer -= Time.deltaTime;
        if (_weatherTimer <= 0f)
        {
            _weatherTimer = weatherChangeDuration;
            SetWeather(RollWeather());
        }
    }

    public void SetWeather(WeatherType newWeather)
    {
        _currentWeather = newWeather;
        UpdateWeatherUI();
        OnWeatherChanged?.Invoke(_currentWeather);
    }

    void UpdateWeatherUI()
    {
        if (weatherText == null || weatherIcon == null) return;
        switch (_currentWeather)
        {
            case WeatherType.Sunny:
                weatherIcon.sprite = sunnySprite;
                weatherText.text = "Cuaca: Cerah";
                break;
            case WeatherType.Cloudy:
                weatherIcon.sprite = cloudySprite;
                weatherText.text = "Cuaca: Mendung";
                break;
            case WeatherType.Rainy:
                weatherIcon.sprite = rainySprite;
                weatherText.text = "Cuaca: Hujan";
                break;
        }
    }

    WeatherType RollWeather()
    {
        float roll = UnityEngine.Random.value;
        if (roll < sunnyChance) return WeatherType.Sunny;
        else if (roll < sunnyChance + cloudyChance) return WeatherType.Cloudy;
        else return WeatherType.Rainy;
    }

    public void AdvanceDay(int amount = 1)
    {
        // In this system, DayProgressionManager handles the day number.
        // We just reset the clock and roll new weather.
        ResetTime();
        SetWeather(RollWeather());
    }

    public void ForceWeather(WeatherType weather)
    {
        _weatherTimer = weatherChangeDuration;
        SetWeather(weather);
    }
}
