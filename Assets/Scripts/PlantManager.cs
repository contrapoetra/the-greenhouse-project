using UnityEngine;

public class PlantManager : MonoBehaviour
{
    public static PlantManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void GrowAllPlants(bool force = false)
    {
        PlantGrowth[] plants = Object.FindObjectsByType<PlantGrowth>(FindObjectsSortMode.None);
        foreach (PlantGrowth plant in plants)
        {
            plant.ProgressStage(force);
        }
        Debug.Log($"Progressed {plants.Length} plants (Forced: {force}).");
    }
}
