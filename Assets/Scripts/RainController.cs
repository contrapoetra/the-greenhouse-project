using System.Collections;
using UnityEngine;

public class RainController : MonoBehaviour
{
    [Tooltip("Drag RainParticle dari Hierarchy")]
    public ParticleSystem rainParticles;

    [Tooltip("Drag Player agar hujan ikut bergerak")]
    public Transform followTarget;

    [Tooltip("Ketinggian spawn hujan di atas player")]
    public float heightOffset = 15f;

    void Start()
    {
        if (rainParticles != null) rainParticles.Stop();
        StartCoroutine(InitAfterFrame());
    }

    IEnumerator InitAfterFrame()
    {
        yield return null;
        if (TimeWeatherManager.Instance != null)
        {
            TimeWeatherManager.Instance.OnWeatherChanged += OnWeatherChanged;
            OnWeatherChanged(TimeWeatherManager.Instance.CurrentWeather);
        }
    }

    void OnDisable()
    {
        if (TimeWeatherManager.Instance != null)
            TimeWeatherManager.Instance.OnWeatherChanged -= OnWeatherChanged;
    }

    void OnWeatherChanged(TimeWeatherManager.WeatherType weather)
    {
        if (rainParticles == null) return;

        if (weather == TimeWeatherManager.WeatherType.Rainy)
            rainParticles.Play();
        else
            rainParticles.Stop();
    }

    void Update()
    {
        if (followTarget == null || rainParticles == null) return;
        Vector3 pos = followTarget.position;
        pos.y += heightOffset;
        rainParticles.transform.position = pos;
    }
}