using UnityEngine;

/// <summary>
/// Detects when the bucket is filled with settled melons.
/// </summary>
public class BucketWinZone : MonoBehaviour
{
    public float minSettledTime = 2.0f;
    private bool _hasWon = false;

    void OnTriggerStay(Collider other)
    {
        if (_hasWon) return;

        // Find the root object of the melon
        GameObject melonRoot = other.transform.root.gameObject;
        CustomProperties props = melonRoot.GetComponentInChildren<CustomProperties>();

        if (props != null && System.Array.Exists(props.properties, p => p == "melon"))
        {
            // Check if melon is not being held and has settled for enough time
            if (!props.isBeingHeld && props.lastDropTime > 0)
            {
                float settledTime = Time.time - props.lastDropTime;
                if (settledTime >= minSettledTime)
                {
                    Win();
                }
            }
        }
    }

    void Win()
    {
        _hasWon = true;
        Debug.Log("🎉 YOU WIN! The bucket is full of melons!");
        
        if (DayProgressionManager.Instance != null)
        {
            DayProgressionManager.Instance.TriggerWin();
        }
    }
}
