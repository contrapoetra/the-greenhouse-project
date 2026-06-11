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

    [Header("── Fog Settings (HDRP Volume) ──")]
    public UnityEngine.Rendering.Volume globalVolume;
    public float sunnyFogDistance = 400f;
    public float cloudyFogDistance = 100f;
    public float rainyFogDistance = 15f;

    private HDAdditionalLightData _hdLight;
    private Fog _fog;

    void Start()
    {
        if (sunLight != null)
            _hdLight = sunLight.GetComponent<HDAdditionalLightData>();

        if (globalVolume != null && globalVolume.profile.TryGet<Fog>(out var fogComponent))
        {
            _fog = fogComponent;
        }

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
        float targetFog = sunnyFogDistance;

        switch (weather)
        {
            case TimeWeatherManager.WeatherType.Sunny:
                SetIntensity(sunnyIntensity);
                if (sunLight != null) sunLight.colorTemperature = 6500f; 
                targetFog = sunnyFogDistance;
                break;

            case TimeWeatherManager.WeatherType.Cloudy:
                SetIntensity(cloudyIntensity);
                if (sunLight != null) sunLight.colorTemperature = 13000f;
                targetFog = cloudyFogDistance;
                break;

            case TimeWeatherManager.WeatherType.Rainy:
                SetIntensity(rainyIntensity);
                if (sunLight != null) sunLight.colorTemperature = 20000f;
                targetFog = rainyFogDistance;
                break;
        }

        if (_fog != null)
        {
            _fog.meanFreePath.value = targetFog;
        }

        Debug.Log($"[WeatherVisual] Cuaca: {weather} | Fog: {targetFog}m");
    }

    void SetIntensity(float lux)
    {
        if (_hdLight != null)
            _hdLight.SetIntensity(lux, UnityEngine.Rendering.LightUnit.Lux);
        else if (sunLight != null)
            sunLight.intensity = lux;
    }
}
