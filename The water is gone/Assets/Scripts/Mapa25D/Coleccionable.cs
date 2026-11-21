using UnityEngine;

// Objeto que gira y se recoge al acercarse el jugador.
public class Coleccionable : MonoBehaviour
{
    public float radio = 0.9f;
    public float giro = 90f;
    public static int recogidos;
    public static int total;

    Transform jugador;
    Vector3 basePos;

    void OnEnable() { total++; }

    void Start()
    {
        basePos = transform.position;
        var p = FindAnyObjectByType<Player25D>();
        if (p != null) jugador = p.transform;
    }

    void Update()
    {
        transform.Rotate(0f, giro * Time.deltaTime, 0f, Space.World);
        transform.position = basePos + Vector3.up * Mathf.Sin(Time.time * 2f) * 0.1f;
        if (jugador != null && Vector3.Distance(jugador.position + Vector3.up * 0.9f, transform.position + Vector3.up * 0.3f) < radio)
        {
            recogidos++;
            Destroy(gameObject);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() { recogidos = 0; total = 0; }
}
