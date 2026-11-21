using UnityEngine;

// Cámara lateral en perspectiva que sigue al jugador con suavizado.
public class CameraFollow25D : MonoBehaviour
{
    public Transform objetivo;
    public Vector3 offset = new Vector3(0f, 3f, -13f);
    public float suavizado = 6f;
    public float anticipacion = 1.5f;
    public float minX = -2f;
    public float maxX = 44f;

    float lastX;
    float look;

    void Start()
    {
        if (objetivo != null)
        {
            lastX = objetivo.position.x;
            transform.position = objetivo.position + offset;
        }
    }

    void LateUpdate()
    {
        if (objetivo == null) return;
        float dx = objetivo.position.x - lastX;
        lastX = objetivo.position.x;
        if (Mathf.Abs(dx) > 0.001f) look = Mathf.Lerp(look, Mathf.Sign(dx) * anticipacion, Time.deltaTime * 2f);

        Vector3 target = objetivo.position + offset + new Vector3(look, 0f, 0f);
        target.x = Mathf.Clamp(target.x, minX, maxX);
        transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * suavizado);
        transform.rotation = Quaternion.Euler(8f, 0f, 0f);
    }
}
