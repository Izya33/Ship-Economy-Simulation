using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 5f;
    public Vector3 offset = new Vector3(0, 0, -10f); // -10 z is important for 2D!

    [SerializeField] private Player ship;
    private float leftThrust;
    private float rightThrust;
    private float currentZoom = 5f;
    private float targetZoom = 5f;
    void Start()
    {
        leftThrust = ship.leftThrust;
        rightThrust = ship.rightThrust;
    }

    void LateUpdate()
{
    leftThrust = ship.leftThrust;
    rightThrust = ship.rightThrust;

    if (leftThrust == 0 && rightThrust == 0)
    {
        float scroll = Mouse.current.scroll.ReadValue().y;
        targetZoom -= scroll * 0.4f;
        targetZoom = Mathf.Clamp(targetZoom, 2f, 20f);
    }
    else
    {
        float zoomFactor = 1f + (leftThrust + rightThrust) / (2f * ship.maxThrust);
        targetZoom = 5f * zoomFactor;
    }

    // Smooth zoom every frame
    currentZoom = Mathf.Lerp(currentZoom, targetZoom, Time.deltaTime * smoothSpeed);
    Camera.main.orthographicSize = currentZoom;

    Vector3 desiredPos = target.position + offset;
    transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
}
}
