using UnityEngine;

enum Phases
{
    PlantSeed,
    WaterPlant
}

public class ClickEvent : MonoBehaviour
{
    public Transform cameraTransform;
    public Transform holdPoint;
    public float range = 100f;

    public GameObject seedPrefab;
    public GameObject plantPrefab;

    GameObject heldObject;
    int heldUses = 0;

    void Update()
    {
        // 🖱️ LEFT CLICK = interact
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
            RaycastHit hit;

            bool didHit = Physics.Raycast(ray, out hit, range);

            if (didHit)
            {
                Debug.Log("Hit: " + hit.collider.name);
            }
            else
            {
                Debug.Log("Hit nothing");
            }

            // If holding something → only try planting
            if (heldObject != null)
            {
                if (didHit)
                {
                    CustomProperties props = hit.collider.GetComponentInParent<CustomProperties>();

                    if (props != null && props.properties != null &&
                        System.Array.Exists(props.properties, p => p == "dirt"))
                    {
                        Debug.Log("Planting on dirt");
                        Plant(hit.point);
                        return;
                    }
                }

                // ❌ NO MORE dropping here
                return;
            }

            // Not holding anything → normal interaction
            if (didHit)
            {
                CustomProperties props = hit.collider.GetComponentInParent<CustomProperties>();

                if (props != null && props.properties != null)
                {
                    if (System.Array.Exists(props.properties, p => p == "seed_storage"))
                    {
                        Debug.Log("Took seeds");
                        SpawnSeeds(seedPrefab, 30);
                    }

                    if (System.Array.Exists(props.properties, p => p == "pickup"))
                    {
                        Debug.Log("Picked up object");
                        PickupObject(hit.collider.transform.root.gameObject);
                    }
                }
            }
        }

        // 🖱️ RIGHT CLICK = drop
        if (Input.GetMouseButtonDown(1))
        {
            if (heldObject != null)
            {
                Debug.Log("Dropped with right click");
                DropObject();
            }
        }
    }

    void SpawnSeeds(GameObject prefab, int amount)
    {
        if (prefab == null) return;

        if (heldObject != null)
        {
            Destroy(heldObject);
        }

        heldObject = Instantiate(prefab);
        heldUses = amount;

        EnsurePickupProperty(heldObject);

        ItemData data = heldObject.GetComponent<ItemData>();
        if (data == null) data = heldObject.AddComponent<ItemData>();
        data.uses = amount;

        PrepareHeldObject(heldObject);
    }

    void Plant(Vector3 position)
    {
        if (heldUses <= 0) return;

        Quaternion randomRot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        GameObject plant = Instantiate(plantPrefab, position + Vector3.up * 0.1f, randomRot);

        // ❌ remove pickup
        foreach (CustomProperties p in plant.GetComponentsInChildren<CustomProperties>())
        {
            p.properties = new string[] { "planted" };
        }

        // freeze physics
        foreach (Rigidbody rb in plant.GetComponentsInChildren<Rigidbody>())
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        heldUses--;

        ItemData data = heldObject.GetComponent<ItemData>();
        if (data != null) data.uses = heldUses;

        Debug.Log("Planted. Remaining: " + heldUses);

        if (heldUses <= 0)
        {
            Destroy(heldObject);
            heldObject = null;
        }
    }

    void PickupObject(GameObject obj)
    {
        if (heldObject != null)
        {
            DropObject();
        }

        heldObject = obj;

        ItemData data = heldObject.GetComponent<ItemData>();
        heldUses = (data != null) ? data.uses : 0;
    }

    void EnsurePickupProperty(GameObject obj)
    {
        CustomProperties props = obj.GetComponent<CustomProperties>();

        if (props == null)
        {
            props = obj.AddComponent<CustomProperties>();
        }

        props.properties = new string[] { "pickup" };
    }

    void PrepareHeldObject(GameObject obj)
    {
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        Collider col = obj.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        obj.transform.SetParent(holdPoint);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;
    }

    void DropObject()
    {
        if (heldObject == null) return;

        heldObject.transform.SetParent(null);

        Rigidbody rb = heldObject.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = false;

        Collider col = heldObject.GetComponent<Collider>();
        if (col != null) col.enabled = true;

        heldObject = null;
        heldUses = 0;
    }
}
