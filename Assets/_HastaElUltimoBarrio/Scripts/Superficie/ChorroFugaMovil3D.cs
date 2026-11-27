using UnityEngine;

public enum ModoObstaculoSuperficie
{
    PulsoGeiser,
    BarreraOscilante
}

// Obstaculo movil en superficie 3D (capa Peligro) con Rigidbody cinematico compatible con PhysX.
[RequireComponent(typeof(Rigidbody))]
public class ChorroFugaMovil3D : MonoBehaviour
{
    [Header("Modo")]
    public ModoObstaculoSuperficie modo = ModoObstaculoSuperficie.PulsoGeiser;
    public float desfaseInicial = 0f;

    [Header("Pulso Geiser")]
    public float duracionPreAviso = 0.8f;
    public float duracionChorro = 1.5f;
    public float duracionPausa = 1.5f;
    public float radioChorro = 0.35f;
    public float alturaMaximaChorro = 2.2f;
    public Transform visualChorro;
    public Renderer rendererChorro;
    public Material matPreAvisoAmbar;
    public Material matChorroCian;

    [Header("Barrera Oscilante")]
    public Vector3 desplazamiento = new Vector3(3.5f, 0f, 0f);
    public float velocidad = 1.6f;

    Rigidbody rb;
    CapsuleCollider colGeiser;
    BoxCollider colBarrera;
    Vector3 posInicial;

    void Awake()
    {
        LimpiarStaticRecursivo(gameObject);

        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (modo == ModoObstaculoSuperficie.PulsoGeiser)
        {
            var boxAntiguo = GetComponent<BoxCollider>();
            if (boxAntiguo != null) boxAntiguo.enabled = false;

            colGeiser = GetComponent<CapsuleCollider>();
            if (colGeiser == null) colGeiser = gameObject.AddComponent<CapsuleCollider>();
            colGeiser.isTrigger = false;
            colGeiser.direction = 1;
            colGeiser.radius = radioChorro;
            colGeiser.enabled = false;
        }
        else
        {
            colBarrera = GetComponent<BoxCollider>();
            if (colBarrera == null) colBarrera = gameObject.AddComponent<BoxCollider>();
            colBarrera.isTrigger = false;
            colBarrera.enabled = true;
        }

        posInicial = transform.position;
    }

    static void LimpiarStaticRecursivo(GameObject raiz)
    {
        if (raiz == null) return;
        raiz.isStatic = false;
        foreach (Transform t in raiz.transform)
            LimpiarStaticRecursivo(t.gameObject);
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
            if (colGeiser != null && colGeiser.enabled) colGeiser.enabled = false;
            ActualizarVisualGeiser(0.28f, true, matPreAvisoAmbar);
        }
        else if (fase < duracionPreAviso + duracionChorro)
        {
            // Chorro activo letal en capa Peligro: collider sincronizado 100% con el cilindro visible
            ActualizarVisualGeiser(alturaMaximaChorro, true, matChorroCian);
            if (colGeiser != null)
            {
                colGeiser.radius = radioChorro;
                colGeiser.height = Mathf.Max(radioChorro * 2f, alturaMaximaChorro);
                colGeiser.center = new Vector3(0f, alturaMaximaChorro * 0.5f, 0f);
                if (!colGeiser.enabled) colGeiser.enabled = true;
            }
        }
        else
        {
            // Pausa apagada: oculta el chorro y desactiva su collider
            if (colGeiser != null && colGeiser.enabled) colGeiser.enabled = false;
            ActualizarVisualGeiser(0f, false, matPreAvisoAmbar);
        }
    }

    void ActualizarVisualGeiser(float altura, bool visible, Material mat)
    {
        if (visualChorro != null)
        {
            if (visualChorro.gameObject.activeSelf != visible)
                visualChorro.gameObject.SetActive(visible);

            if (visible)
            {
                // PrimitiveType.Cylinder mide 2u de alto y 1u de diametro por defecto
                float diametro = radioChorro * 2f;
                visualChorro.localScale = new Vector3(diametro, altura * 0.5f, diametro);
                visualChorro.localPosition = new Vector3(0f, altura * 0.5f, 0f);
            }
        }
        if (visible && rendererChorro != null && mat != null && rendererChorro.sharedMaterial != mat)
            rendererChorro.sharedMaterial = mat;
    }
}
