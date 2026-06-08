using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class WeatherVisualSimple : MonoBehaviour
{
    [Header("── Sun Light ──")]
    public Light sunLight;

    [Header("── Intensity (Lux, HDRP) ──")]
    public float sunnyIntensity  = 130000f;
    public float cloudyIntensity = 50000f;
    public float rainyIntensity  = 20000f;

    private HDAdditionalLightData _hdLight;

    void Start()
    {
        if (sunLight != null)
            _hdLight = sunLight.GetComponent<HDAdditionalLightData>();

        StartCoroutine(InitAfterFrame());
    }

    IEnumerator InitAfterFrame()
    {
        yield return null;
        if (TimeWeatherManager.Instance != null)
        {
            TimeWeatherManager.Instance.OnWeatherChanged += ApplyWeather;
            ApplyWeather(TimeWeatherManager.Instance.CurrentWeather);
        }
    }

    void OnDisable()
    {
        if (TimeWeatherManager.Instance != null)
            TimeWeatherManager.Instance.OnWeatherChanged -= ApplyWeather;
    }

    void ApplyWeather(TimeWeatherManager.WeatherType weather)
    {
        switch (weather)
        {
            case TimeWeatherManager.WeatherType.Sunny:
                SetIntensity(sunnyIntensity);
                if (sunLight != null) sunLight.colorTemperature = 6500f; // kuning hangat
                break;

            case TimeWeatherManager.WeatherType.Cloudy:
                SetIntensity(cloudyIntensity);
                if (sunLight != null) sunLight.colorTemperature = 13000f; // sedikit lebih dingin
                break;

            case TimeWeatherManager.WeatherType.Rainy:
                SetIntensity(rainyIntensity);
                if (sunLight != null) sunLight.colorTemperature = 20000f; // abu-abu suram
                break;
        }

        Debug.Log($"[WeatherVisual] Cuaca: {weather}");
    }

    void SetIntensity(float lux)
    {
        if (_hdLight != null)
            _hdLight.SetIntensity(lux, UnityEngine.Rendering.LightUnit.Lux);
        else if (sunLight != null)
            sunLight.intensity = lux;
    }
}