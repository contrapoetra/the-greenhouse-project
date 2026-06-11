using UnityEngine;
using System.Collections.Generic;
using System.IO;

[System.Serializable]
public class PlantSaveData
{
    public Vector3 position;
    public Quaternion rotation;
    public int currentStage;
    public float waterLevel;
    public bool isFertilized;
    public bool isPollinated;
    public bool isTiedUp;
    public float growthTracker;
    public int pollinationCycles;
    public bool isFruitVisible;
}

[System.Serializable]
public class DirtSaveData
{
    public Vector3 position;
    public bool isFertilized;
}

[System.Serializable]
public class GameSaveData
{
    public int currentDay;
    public int currentWeather;
    public Vector3 playerPosition;
    public Quaternion playerRotation;
    public string heldItemType; // "none", "seed", "fertilizer", "watering_can"
    public int heldItemUses;
    public bool hasPollen;
    public List<PlantSaveData> plants = new List<PlantSaveData>();
    public List<DirtSaveData> dirts = new List<DirtSaveData>();
}

public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance { get; private set; }
    private string savePath;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        savePath = Path.Combine(Application.persistentDataPath, "melon_save.json");
    }

    public bool SaveFileExists() => File.Exists(savePath);

    public void SaveGame()
    {
        try
        {
            GameSaveData data = new GameSaveData();

            if (DayProgressionManager.Instance != null)
                data.currentDay = DayProgressionManager.Instance.CurrentDay;
            
            if (TimeWeatherManager.Instance != null)
                data.currentWeather = (int)TimeWeatherManager.Instance.CurrentWeather;

            if (PlayerController.Instance != null)
            {
                data.playerPosition = PlayerController.Instance.transform.position;
                data.playerRotation = PlayerController.Instance.transform.rotation;
            }

            if (ClickEvent.Instance != null)
            {
                data.heldItemType = ClickEvent.Instance.GetHeldItemType();
                data.heldItemUses = ClickEvent.Instance.GetHeldItemUses();
            }

            if (PollinationManager.Instance != null)
                data.hasPollen = PollinationManager.Instance.HasPollen;

            PlantGrowth[] allPlants = Object.FindObjectsByType<PlantGrowth>(FindObjectsSortMode.None);
            foreach (var plant in allPlants)
            {
                data.plants.Add(new PlantSaveData
                {
                    position = plant.transform.position,
                    rotation = plant.transform.rotation,
                    currentStage = plant.currentStage,
                    waterLevel = plant.waterLevel,
                    isFertilized = plant.isFertilized,
                    isPollinated = plant.isPollinated,
                    isTiedUp = plant.isTiedUp,
                    growthTracker = plant.GetGrowthTracker(),
                    pollinationCycles = plant.GetPollinationCycles(),
                    isFruitVisible = plant.isFruitVisible
                });
            }

            CustomProperties[] allProps = Object.FindObjectsByType<CustomProperties>(FindObjectsSortMode.None);
            foreach (var prop in allProps)
            {
                if (System.Array.Exists(prop.properties, p => p == "dirt"))
                {
                    data.dirts.Add(new DirtSaveData
                    {
                        position = prop.transform.position,
                        isFertilized = System.Array.Exists(prop.properties, p => p == "fertilized")
                    });
                }
            }

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(savePath, json);
            Debug.Log($"[SaveSystem] Game Saved. Day: {data.currentDay}");
        }
        catch (System.Exception e) { Debug.LogError($"[SaveSystem] SAVE FAILED: {e.Message}"); }
    }

    public void LoadGame()
    {
        try
        {
            if (!SaveFileExists()) return;

            string json = File.ReadAllText(savePath);
            GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);

            if (TimeWeatherManager.Instance != null)
                TimeWeatherManager.Instance.ForceWeather((TimeWeatherManager.WeatherType)data.currentWeather);

            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.GetComponent<CharacterController>().enabled = false;
                PlayerController.Instance.transform.position = data.playerPosition;
                PlayerController.Instance.transform.rotation = data.playerRotation;
                PlayerController.Instance.GetComponent<CharacterController>().enabled = true;
            }

            if (ClickEvent.Instance != null)
                ClickEvent.Instance.RestoreHeldItem(data.heldItemType, data.heldItemUses);

            if (PollinationManager.Instance != null)
                PollinationManager.Instance.RestorePollenState(data.hasPollen);

            PlantGrowth[] currentPlants = Object.FindObjectsByType<PlantGrowth>(FindObjectsSortMode.None);
            foreach (var p in currentPlants) Destroy(p.gameObject);

            foreach (var pData in data.plants)
            {
                if (ClickEvent.Instance != null && ClickEvent.Instance.plantPrefab != null)
                {
                    GameObject newPlant = Instantiate(ClickEvent.Instance.plantPrefab, pData.position, pData.rotation);
                    PlantGrowth pg = newPlant.GetComponent<PlantGrowth>();
                    if (pg != null) pg.LoadFromData(pData);
                    
                    CustomProperties cp = newPlant.GetComponent<CustomProperties>() ?? newPlant.AddComponent<CustomProperties>();
                    cp.properties = new string[] { "planted" };
                    
                    foreach (Rigidbody rb in newPlant.GetComponentsInChildren<Rigidbody>())
                    {
                        rb.isKinematic = true;
                        rb.useGravity = false;
                    }
                }
            }
// 5. Restore Dirts
CustomProperties[] allProps = Object.FindObjectsByType<CustomProperties>(FindObjectsSortMode.None);
foreach (var dData in data.dirts)
{
    foreach (var prop in allProps)
    {
        if (Vector3.Distance(prop.transform.position, dData.position) < 0.1f)
        {
            List<string> pList = new List<string>(prop.properties);
            if (dData.isFertilized && !pList.Contains("fertilized")) pList.Add("fertilized");
            prop.properties = pList.ToArray();

            // Visual Indicator: Restore metallic/shiny look
            if (dData.isFertilized)
            {
                Renderer rend = prop.GetComponent<Renderer>() ?? prop.GetComponentInChildren<Renderer>();
                if (rend != null) rend.material.SetFloat("_Metallic", 1f);
            }
        }
    }
}
            Debug.Log("[SaveSystem] Load Complete.");
        }
        catch (System.Exception e) { Debug.LogError($"[SaveSystem] LOAD FAILED: {e.Message}"); }
    }

    public void DeleteSave()
    {
        if (SaveFileExists()) File.Delete(savePath);
    }
}
