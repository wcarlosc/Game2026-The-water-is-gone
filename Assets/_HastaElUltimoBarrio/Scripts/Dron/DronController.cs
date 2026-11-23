using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

// Dron de inspección (niveles 1-2). Impulsos con WASD, inercia, gravedad leve.
// Solo puede posarse en la layer "Seguro"; tocar "Peligro" es un choque.
[RequireComponent(typeof(Rigidbody))]
public class DronController : MonoBehaviour
{
    [Header("Vuelo")]
    public float fuerzaImpulso = 3f;
    public float gravedad = 2.5f;
    public float dampingLineal = 0.3f;
    public float velocidadMaxima = 7f;

    [Header("Combustible")]
    public float consumoVuelo = 0.25f;
    public float consumoPosado = 0.05f;
    public float umbralPocoCombustible = 3f;

    [Header("Referencias")]
    public Transform visual;
    public Animation animacion;
    public CinemachineImpulseSource sacudida;

    Rigidbody rb;
    int capaSeguro, capaPeligro;
    Vector3 puntoReaparicion;
    bool posado;
    bool reapareciendo;
    float invulnerable;
    float mirarX = 1f;
    bool animPocoCombustible;

    public bool Posado { get { return posado; } }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.linearDamping = dampingLineal;
        rb.angularDamping = 5f;
        rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        capaSeguro = LayerMask.NameToLayer("Seguro");
        capaPeligro = LayerMask.NameToLayer("Peligro");
        puntoReaparicion = transform.position;

        if (animacion != null)
        {
            Capa("Idle", 0, WrapMode.Loop);
            Capa("Impulse", 1, WrapMode.Once);
            Capa("LowFuel", 2, WrapMode.Loop);
            Capa("Crash", 3, WrapMode.Once);
            animacion.Play("Idle");
        }
    }

    void Capa(string clip, int capa, WrapMode modo)
    {
        var st = animacion[clip];
        if (st == null) return;
        st.layer = capa;
        st.wrapMode = modo;
    }

    void Update()
    {
        var lm = LevelManager.Instancia;
        bool activo = lm == null || !lm.Terminado;
        if (invulnerable > 0f) invulnerable -= Time.deltaTime;

        if (activo && !reapareciendo) LeerImpulso();

        if (lm != null && activo && !reapareciendo)
            lm.ConsumirEnergia((posado ? consumoPosado : consumoVuelo) * Time.deltaTime);

        ActualizarVisual(lm);
    }

    void LeerImpulso()
    {
        var kb = Keyboard.current;
        var gp = Gamepad.current;
        bool pulso = false;
        Vector2 dir = Vector2.zero;

        if (kb != null)
        {
            pulso |= kb.wKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame;
            pulso |= kb.upArrowKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) dir.y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) dir.y -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) dir.x += 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) dir.x -= 1f;
        }
        if (gp != null)
        {
            var d = gp.dpad;
            pulso |= d.up.wasPressedThisFrame || d.down.wasPressedThisFrame || d.left.wasPressedThisFrame || d.right.wasPressedThisFrame;
            if (d.up.isPressed) dir.y += 1f;
            if (d.down.isPressed) dir.y -= 1f;
            if (d.right.isPressed) dir.x += 1f;
            if (d.left.isPressed) dir.x -= 1f;
        }

        if (!pulso || dir == Vector2.zero) return;

        // Estando posado, empujar hacia abajo no hace nada.
        if (posado && dir.y < 0f && Mathf.Approximately(dir.x, 0f)) return;

        Vector3 f = new Vector3(dir.x, dir.y, 0f).normalized * fuerzaImpulso;
        if (posado && f.y < 0.5f) f.y = 0.5f; // despegue mínimo para no arrastrarse por la plataforma
        posado = false;
        rb.AddForce(f, ForceMode.Impulse);
        if (Mathf.Abs(dir.x) > 0.01f) mirarX = Mathf.Sign(dir.x);
        if (animacion != null && animacion["Impulse"] != null)
        {
            animacion.Stop("Impulse");
            animacion.Play("Impulse");
        }
    }

    void FixedUpdate()
    {
        if (reapareciendo) return;
        if (!posado) rb.AddForce(Vector3.down * gravedad, ForceMode.Acceleration);
        Vector3 v = rb.linearVelocity;
        v.z = 0f;
        if (v.magnitude > velocidadMaxima) v = v.normalized * velocidadMaxima;
        rb.linearVelocity = v;
    }

    void ActualizarVisual(LevelManager lm)
    {
        if (visual != null)
        {
            Vector3 v = rb.linearVelocity;
            float roll = Mathf.Clamp(-v.x * 3f, -18f, 18f);
            float pitch = Mathf.Clamp(v.y * 2f, -10f, 10f);
            float yaw = 180f - 35f * mirarX;
            Quaternion obj = Quaternion.Euler(pitch, yaw, roll);
            visual.localRotation = Quaternion.Slerp(visual.localRotation, obj, Time.deltaTime * 6f);
        }

        if (animacion != null && lm != null)
        {
            bool poco = lm.Energia <= umbralPocoCombustible;
            if (poco != animPocoCombustible)
            {
                animPocoCombustible = poco;
                if (poco) animacion.Play("LowFuel"); else animacion.Stop("LowFuel");
            }
        }
    }

    // ---------------- Colisiones ----------------
    void OnCollisionEnter(Collision c) { Evaluar(c); }
    void OnCollisionStay(Collision c) { Evaluar(c); }

    void OnCollisionExit(Collision c)
    {
        if (c.gameObject.layer == capaSeguro) posado = false;
    }

    void Evaluar(Collision c)
    {
        if (reapareciendo) return;
        int capa = c.gameObject.layer;

        if (capa == capaPeligro)
        {
            Chocar();
            return;
        }

        // Caer sobre una plataforma segura siempre la cuenta como checkpoint, sin importar la velocidad.
        if (capa == capaSeguro && !posado && LlegaDesdeArriba(c))
            Posar(c.collider);
    }

    // Desde arriba = alguna normal de contacto apunta hacia arriba, o el contacto está en la cara
    // superior de la plataforma (al caer sobre el borde, la base redonda de la cápsula da normales
    // diagonales que antes no contaban, y el dron resbalaba hasta el piso de Peligro).
    bool LlegaDesdeArriba(Collision c)
    {
        float tope = c.collider.bounds.max.y;
        for (int i = 0; i < c.contactCount; i++)
        {
            var p = c.GetContact(i);
            if (p.normal.y > 0.5f || p.point.y >= tope - 0.05f) return true;
        }
        return false;
    }

    void Posar(Collider plataforma)
    {
        posado = true;
        rb.linearVelocity = Vector3.zero;
        Bounds b = plataforma.bounds;
        puntoReaparicion = new Vector3(b.center.x, b.max.y + 0.05f, 0f);

        if (plataforma.CompareTag("Finish") && LevelManager.Instancia != null)
            LevelManager.Instancia.Completar();
    }

    void Chocar()
    {
        if (invulnerable > 0f || reapareciendo) return;
        invulnerable = 1f;
        if (sacudida != null) sacudida.GenerateImpulse();
        if (animacion != null && animacion["Crash"] != null) { animacion.Stop("Crash"); animacion.Play("Crash"); }

        bool sigue = LevelManager.Instancia == null || LevelManager.Instancia.RegistrarChoque();
        StartCoroutine(Reaparecer(sigue));
    }

    IEnumerator Reaparecer(bool mover)
    {
        reapareciendo = true;
        posado = false;
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
        yield return new WaitForSeconds(0.6f);
        if (mover)
        {
            rb.position = puntoReaparicion;
            transform.position = puntoReaparicion;
        }
        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        reapareciendo = false;
    }

    public void FijarPuntoReaparicion(Vector3 p) { puntoReaparicion = p; }
}
