using UnityEngine;

/// <summary>
/// Automatically destroys the object if it has been dropped for more than a certain duration.
/// </summary>
public class ItemCleanup : MonoBehaviour
{
    public float cleanupTime = 5.0f;
    private CustomProperties _props;

    void Start()
    {
        _props = GetComponentInChildren<CustomProperties>();
        if (_props == null) _props = gameObject.AddComponent<CustomProperties>();
    }

    void Update()
    {
        if (_props == null) return;

        // Only cleanup if not being held and time since drop exceeds threshold
        if (!_props.isBeingHeld && _props.lastDropTime > 0)
        {
            if (Time.time - _props.lastDropTime > cleanupTime)
            {
                Debug.Log($"[Cleanup] Destroying {gameObject.name} (Dropped too long)");
                Destroy(gameObject);
            }
        }
    }
}
