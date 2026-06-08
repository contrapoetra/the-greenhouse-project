using UnityEngine;

public class CustomProperties : MonoBehaviour
{
    [Header("Original State (Auto-filled)")]
    public Vector3 originalScale = Vector3.one;

    public string[] properties;

    [Header("Held Transformation Offsets")]
    public Vector3 heldPosition = Vector3.zero;
    public Vector3 heldRotation = Vector3.zero;
    public Vector3 heldScale = Vector3.one;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
