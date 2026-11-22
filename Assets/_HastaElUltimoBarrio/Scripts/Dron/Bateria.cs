using UnityEngine;

// Batería recolectable (layer Recolectable, collider trigger).
public class Bateria : MonoBehaviour
{
    public float energia = 3f;
    public float giro = 90f;
    Vector3 basePos;

    void Start() { basePos = transform.position; }

    void Update()
    {
        transform.Rotate(0f, giro * Time.deltaTime, 0f, Space.World);
        transform.position = basePos + Vector3.up * Mathf.Sin(Time.time * 2.5f) * 0.08f;
    }

    void OnTriggerEnter(Collider otro)
    {
        if (otro.GetComponentInParent<DronController>() == null) return;
        if (LevelManager.Instancia != null) LevelManager.Instancia.SumarEnergia(energia);
        Destroy(gameObject);
    }
}
