// Billboard.cs
using UnityEngine;

public class Billboard : MonoBehaviour
{
    Camera _cam;

    void Awake()
    {
        // Cache the main camera (or assign another camera if you have one)
        _cam = Camera.main;
    }

    void LateUpdate()
    {
        Vector3 camPos = _cam.transform.position;
        camPos.y = transform.position.y;                // ignore vertical offset
        transform.LookAt(camPos);                        // rotate only on Y
        // Flip the rotation so the label's forward vector faces the camera (prevents mirroring)
        transform.rotation = Quaternion.LookRotation(transform.position - camPos);
    }
}