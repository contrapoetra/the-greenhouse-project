using UnityEngine;
using System.Linq;

enum Phases
{
    PlantSeed,
    WaterPlant
}

public class ClickEvent : MonoBehaviour
{
    public static ClickEvent Instance { get; private set; }

    public Transform cameraTransform;
    public Transform holdPoint;
    public float range = 100f;

    public GameObject seedPrefab;
    public GameObject fertilizerPrefab;
    public GameObject plantPrefab;
    public GameObject wateringCanPrefab;

    GameObject heldObject;
    int heldUses = 0;

    public bool IsHoldingWateringCan { get; private set; }

    // --- Save System Helpers ---
    public string GetHeldItemType()
    {
        if (heldObject == null) return "none";
        CustomProperties props = heldObject.GetComponentInChildren<CustomProperties>();
        if (props == null) return "none";
        if (System.Array.Exists(props.properties, p => p == "seed")) return "seed";
        if (System.Array.Exists(props.properties, p => p == "fertilizer")) return "fertilizer";
        if (System.Array.Exists(props.properties, p => p == "watering_can") || heldObject.name.Contains("watering_can")) return "watering_can";
        return "none";
    }

    public int GetHeldItemUses() => heldUses;

    public void RestoreHeldItem(string type, int uses)
    {
        if (type == "none") return;
        if (type == "seed") SpawnItem(seedPrefab, uses, "seed");
        else if (type == "fertilizer") SpawnItem(fertilizerPrefab, uses, "fertilizer");
        else if (type == "watering_can") PickupObject(Instantiate(wateringCanPrefab));
    }

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        IsHoldingWateringCan = false;
        if (heldObject != null)
        {
            CustomProperties heldProps = heldObject.GetComponentInChildren<CustomProperties>();
            if (heldProps != null && (System.Array.Exists(heldProps.properties, p => p == "watering_can") || heldObject.name.Contains("watering_can")))
            {
                IsHoldingWateringCan = true;
            }
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            RaycastHit hit;
            if (GetPrioritizedHit(out hit))
            {
                PlantGrowth plant = hit.collider.GetComponentInParent<PlantGrowth>();
                if (plant != null) plant.TieUp();
            }
        }

        if (heldObject != null)
        {
            CustomProperties heldProps = heldObject.GetComponentInChildren<CustomProperties>();
            bool isWateringCan = heldProps != null && (System.Array.Exists(heldProps.properties, p => p == "watering_can") || heldObject.name.Contains("watering_can"));

            if (isWateringCan)
            {
                ParticleSystem ps = heldObject.GetComponentInChildren<ParticleSystem>();
                if (Input.GetMouseButton(0))
                {
                    Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
                    RaycastHit[] hits = Physics.SphereCastAll(ray, 0.5f, range, Physics.AllLayers, QueryTriggerInteraction.Collide);
                    
                    if (hits.Length > 0)
                    {
                        if (ps != null && !ps.isPlaying) ps.Play();
                        foreach (var hitInfo in hits)
                        {
                            PlantGrowth plant = hitInfo.collider.GetComponentInParent<PlantGrowth>() ?? 
                                               hitInfo.collider.GetComponent<PlantGrowth>() ?? 
                                               hitInfo.collider.GetComponentInChildren<PlantGrowth>();
                            if (plant != null) plant.Water(0.5f * Time.deltaTime);
                        }
                    }
                    else if (ps != null && ps.isPlaying) ps.Stop();
                }
                else if (ps != null && ps.isPlaying) ps.Stop();

                if (Input.GetMouseButton(0)) return; 
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            bool didHit = GetPrioritizedHit(out hit);

            if (heldObject != null)
            {
                CustomProperties heldProps = heldObject.GetComponentInChildren<CustomProperties>();
                bool isSeed = heldProps != null && System.Array.Exists(heldProps.properties, p => p == "seed");
                bool isFertilizer = heldProps != null && System.Array.Exists(heldProps.properties, p => p == "fertilizer");

                if (didHit)
                {
                    CustomProperties hitProps = hit.collider.GetComponentInParent<CustomProperties>();
                    bool isDirt = hitProps != null && System.Array.Exists(hitProps.properties, p => p == "dirt");

                    if (isFertilizer && isDirt)
                    {
                        if (!System.Array.Exists(hitProps.properties, p => p == "fertilized"))
                        {
                            System.Collections.Generic.List<string> pList = new System.Collections.Generic.List<string>(hitProps.properties);
                            pList.Add("fertilized");
                            hitProps.properties = pList.ToArray();
                            heldUses--;
                            ItemData data = heldObject.GetComponentInChildren<ItemData>();
                            if (data != null) data.uses = heldUses;
                            if (heldUses <= 0) { Destroy(heldObject); heldObject = null; }
                            return;
                        }
                    }

                    if (isSeed && isDirt)
                    {
                        Plant(hit.point, hitProps);
                        return;
                    }
                }
                return;
            }

            if (didHit)
            {
                if (hit.collider.gameObject.name == "ComputerCollider" && DayProgressionManager.Instance != null)
                {
                    DayProgressionManager.Instance.OnComputerClicked();
                    return;
                }

                CustomProperties props = hit.collider.GetComponentInParent<CustomProperties>();
                if (props != null && props.properties != null)
                {
                    if (System.Array.Exists(props.properties, p => p == "male_flower"))
                    {
                        PollinationManager.Instance.OnFlowerClicked(hit.collider.gameObject, "male_flower");
                        return;
                    }
                    if (System.Array.Exists(props.properties, p => p == "female_flower"))
                    {
                        PollinationManager.Instance.OnFlowerClicked(hit.collider.gameObject, "female_flower");
                        return;
                    }

                    if (System.Array.Exists(props.properties, p => p == "seed_storage")) SpawnItem(seedPrefab, 30, "seed");
                    else if (System.Array.Exists(props.properties, p => p == "fertilizer_storage")) SpawnItem(fertilizerPrefab, 10, "fertilizer");
                    else if (System.Array.Exists(props.properties, p => p == "pickup")) PickupObject(hit.collider.transform.root.gameObject);
                }
            }
        }

        if (Input.GetMouseButtonDown(1) && heldObject != null) DropObject();
    }

    /// <summary>
    /// Uses RaycastAll to find specific interactables (like flowers) even if blocked by general plant colliders.
    /// </summary>
    public bool GetPrioritizedHit(out RaycastHit bestHit)
    {
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, range);
        bestHit = new RaycastHit();

        if (hits.Length == 0) return false;

        // 1. Sort by distance
        var sortedHits = hits.OrderBy(h => h.distance).ToList();

        // 2. Try to find HIGH PRIORITY targets (flowers) first, anywhere in the line
        foreach (var hit in sortedHits)
        {
            CustomProperties props = hit.collider.GetComponentInParent<CustomProperties>();
            if (props != null && props.properties != null)
            {
                if (System.Array.Exists(props.properties, p => p == "male_flower") || 
                    System.Array.Exists(props.properties, p => p == "female_flower"))
                {
                    bestHit = hit;
                    return true;
                }
            }
        }

        // 3. Fallback to the very first thing we hit
        bestHit = sortedHits[0];
        return true;
    }

    void LateUpdate()
    {
        if (heldObject != null)
        {
            CustomProperties props = heldObject.GetComponentInChildren<CustomProperties>();
            if (props != null)
            {
                heldObject.transform.localPosition = props.heldPosition;
                heldObject.transform.localRotation = Quaternion.Euler(props.heldRotation);
                heldObject.transform.localScale = props.heldScale;
            }
        }
    }

    void SpawnItem(GameObject prefab, int amount, string tag)
    {
        if (prefab == null) return;
        if (heldObject != null) Destroy(heldObject);

        heldObject = Instantiate(prefab);
        heldUses = amount;
        
        CustomProperties props = heldObject.GetComponentInChildren<CustomProperties>();
        if (props == null) props = heldObject.AddComponent<CustomProperties>();
        
        System.Collections.Generic.List<string> pList = new System.Collections.Generic.List<string>();
        if (props.properties != null) pList.AddRange(props.properties);
        if (!pList.Contains("pickup")) pList.Add("pickup");
        if (!pList.Contains(tag)) pList.Add(tag);
        props.properties = pList.ToArray();

        ItemData data = heldObject.GetComponentInChildren<ItemData>();
        if (data == null) data = heldObject.AddComponent<ItemData>();
        data.uses = amount;

        PrepareHeldObject(heldObject);
    }

    void Plant(Vector3 position, CustomProperties dirtProps)
    {
        if (heldUses <= 0) return;
        float checkRadius = 0.25f;
        Collider[] hitColliders = Physics.OverlapSphere(position, checkRadius);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.GetComponentInParent<PlantGrowth>() != null) return;
        }

        Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        GameObject plant = Instantiate(plantPrefab, position, rotation);
        PlantGrowth pg = plant.GetComponent<PlantGrowth>();
        if (pg != null) pg.isFertilized = System.Array.Exists(dirtProps.properties, p => p == "fertilized");

        CustomProperties rootProps = plant.GetComponent<CustomProperties>() ?? plant.AddComponent<CustomProperties>();
        rootProps.properties = new string[] { "planted" };

        foreach (Rigidbody rb in plant.GetComponentsInChildren<Rigidbody>())
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        heldUses--;
        ItemData data = heldObject.GetComponentInChildren<ItemData>();
        if (data != null) data.uses = heldUses;
        if (heldUses <= 0) { Destroy(heldObject); heldObject = null; }
    }

    public void PickupObject(GameObject obj)
    {
        if (heldObject != null) DropObject();
        heldObject = obj;
        PrepareHeldObject(heldObject);
        ItemData data = heldObject.GetComponentInChildren<ItemData>();
        heldUses = (data != null) ? data.uses : 0;
    }

    void PrepareHeldObject(GameObject obj)
    {
        foreach (Rigidbody rb in obj.GetComponentsInChildren<Rigidbody>())
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.transform.localPosition = Vector3.zero;
            rb.transform.localRotation = Quaternion.identity;
        }

        foreach (Collider col in obj.GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }

        CustomProperties props = obj.GetComponentInChildren<CustomProperties>();
        if (props == null) props = obj.AddComponent<CustomProperties>();
        
        if (props.originalScale == Vector3.one || props.originalScale == Vector3.zero)
        {
            props.originalScale = obj.transform.localScale;
        }

        obj.transform.SetParent(holdPoint);
        obj.transform.localPosition = props.heldPosition;
        obj.transform.localRotation = Quaternion.Euler(props.heldRotation);
        obj.transform.localScale = props.heldScale;
    }

    void DropObject()
    {
        if (heldObject == null) return;

        heldObject.transform.SetParent(null);
        CustomProperties props = heldObject.GetComponentInChildren<CustomProperties>();
        if (props != null) heldObject.transform.localScale = props.originalScale;

        foreach (Rigidbody rb in heldObject.GetComponentsInChildren<Rigidbody>())
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        foreach (Collider col in heldObject.GetComponentsInChildren<Collider>())
        {
            col.enabled = true;
        }

        heldObject = null;
        heldUses = 0;
    }
}
