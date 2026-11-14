using UnityEngine;

/// <summary>Câmera ortográfica que segue o alvo com SmoothDamp no LateUpdate.</summary>
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothTime = 0.12f;

    Vector3 velocity;

    void LateUpdate()
    {
        if (target == null) return;
        Vector3 p = target.position;
        Vector3 goal = new Vector3(p.x, p.y, transform.position.z);
        transform.position = Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime);
    }
}
