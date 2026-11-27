using UnityEngine;

public enum ModoObstaculoSuperficie
{
    PulsoGeiser,
    BarreraOscilante
}

// Obstaculo movil en superficie 3D (capa Peligro) con Rigidbody cinematico compatible con PhysX.
[RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
public class ChorroFugaMovil3D : MonoBehaviour
{
    [Header("Modo")]
    public ModoObstaculoSuperficie modo = ModoObstaculoSuperficie.PulsoGeiser;
    public float desfaseInicial = 0f;

    [Header("Pulso Geiser")]
    public float duracionPreAviso = 0.8f;
    public float duracionChorro = 1.5f;
    public float duracionPausa = 1.5f;
    public Transform visualChorro;
    public Renderer rendererChorro;
    public Material matPreAvisoAmbar;
    public Material matChorroCian;

    [Header("Barrera Oscilante")]
    public Vector3 desplazamiento = new Vector3(3.5f, 0f, 0f);
    public float velocidad = 1.6f;

    Rigidbody rb;
    BoxCollider col;
    Vector3 posInicial;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        col = GetComponent<BoxCollider>();
        col.isTrigger = false;
        posInicial = transform.position;
    }

    void FixedUpdate()
    {
        if (modo == ModoObstaculoSuperficie.BarreraOscilante)
        {
            float t = Mathf.PingPong((Time.time + desfaseInicial) * velocidad, 1f);
            float suave = t * t * (3f - 2f * t);
            rb.MovePosition(posInicial + desplazamiento * suave);
            return;
        }

        float ciclo = duracionPreAviso + duracionChorro + duracionPausa;
        if (ciclo <= 0.05f) return;
        float fase = Mathf.Repeat(Time.time + desfaseInicial, ciclo);

        if (fase < duracionPreAviso)
        {
            // Pre-aviso visual sin colision
            if (col.enabled) col.enabled = false;
            ActualizarVisualGeiser(0.18f, matPreAvisoAmbar);
        }
        else if (fase < duracionPreAviso + duracionChorro)
        {
            // Chorro activo letal en capa Peligro
            if (!col.enabled) col.enabled = true;
            ActualizarVisualGeiser(1.0f, matChorroCian);
        }
        else
        {
            // Pausa apagada
            if (col.enabled) col.enabled = false;
            ActualizarVisualGeiser(0.03f, matPreAvisoAmbar);
        }
    }

    void ActualizarVisualGeiser(float escalaY, Material mat)
    {
        if (visualChorro != null)
        {
            var s = visualChorro.localScale;
            s.y = escalaY;
            visualChorro.localScale = s;
            visualChorro.localPosition = new Vector3(0f, escalaY * 0.9f, 0f);
        }
        if (rendererChorro != null && mat != null && rendererChorro.sharedMaterial != mat)
            rendererChorro.sharedMaterial = mat;
    }
}
