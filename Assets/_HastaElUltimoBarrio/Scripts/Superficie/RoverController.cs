using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

// Controlador del Rover SCADA en superficie isometrica 3D (plano XZ).
// Mantiene las reglas del curso: bateria acumulada (10 + sobrante), choques (-20%, max 5) y llegada.
[RequireComponent(typeof(Rigidbody))]
public class RoverController : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidadMaxima = 5.8f;
    public float aceleracion = 26f;
    public float radioInteraccion = 2.6f;

    [Header("Bateria SCADA")]
    public float consumoMarcha = 0.20f;
    public float consumoEspera = 0.04f;

    [Header("Referencias")]
    public Transform visual;
    public CinemachineImpulseSource sacudida;

    Rigidbody rb;
    int capaPeligro;
    Vector3 puntoReaparicion;
    Vector2 entradaMovimiento;
    bool reapareciendo;
    float invulnerable;
    float sigAudioMotor;
    float sigAvisoSalida;
    NodoValvula3D valvulaCercana;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        capaPeligro = LayerMask.NameToLayer("Peligro");
        puntoReaparicion = transform.position;
    }

    void Update()
    {
        var lm = LevelManager.Instancia;
        bool activo = lm == null || !lm.Terminado;
        if (invulnerable > 0f) invulnerable -= Time.deltaTime;

        if (!activo || reapareciendo)
        {
            entradaMovimiento = Vector2.zero;
            return;
        }

        LeerEntradas();
        ActualizarValvulaCercana();

        bool enMarcha = entradaMovimiento.sqrMagnitude > 0.02f;
        if (lm != null)
            lm.ConsumirEnergia((enMarcha ? consumoMarcha : consumoEspera) * Time.deltaTime);

        if (enMarcha && Time.time >= sigAudioMotor)
        {
            sigAudioMotor = Time.time + 0.28f;
            AudioManager.Instancia.SonarPropulsor();
        }
    }

    void LeerEntradas()
    {
        Vector2 dir = Vector2.zero;
        bool interactuar = false;
        bool presurizar = false;
        bool simular = false;

        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) dir.y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) dir.y -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) dir.x += 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) dir.x -= 1f;

            interactuar |= kb.eKey.wasPressedThisFrame;
            presurizar |= kb.pKey.wasPressedThisFrame;
            simular |= kb.qKey.wasPressedThisFrame;
        }

        var gp = Gamepad.current;
        if (gp != null)
        {
            Vector2 stick = gp.leftStick.ReadValue();
            if (stick.magnitude > 0.2f) dir = stick;
            interactuar |= gp.buttonSouth.wasPressedThisFrame;
            presurizar |= gp.buttonWest.wasPressedThisFrame;
            simular |= gp.buttonNorth.wasPressedThisFrame;
        }

        entradaMovimiento = dir.sqrMagnitude > 1f ? dir.normalized : dir;

        var gestor = GestorRedSuperficie.Instancia;
        if (gestor == null) return;

        if (interactuar && valvulaCercana != null)
            gestor.CambiarValvulaCampo(valvulaCercana);
        if (simular)
            gestor.AlternarModoSimulacion();
        if (presurizar)
            gestor.ConfirmarPresurizacion();
    }

    void FixedUpdate()
    {
        if (reapareciendo) return;

        Vector3 dirMundo = CalcularDireccionIsometrica(entradaMovimiento);
        Vector3 velObj = dirMundo * velocidadMaxima;
        Vector3 velActual = rb.linearVelocity;
        Vector3 nuevaHoriz = Vector3.MoveTowards(
            new Vector3(velActual.x, 0f, velActual.z),
            velObj,
            aceleracion * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector3(nuevaHoriz.x, velActual.y, nuevaHoriz.z);

        if (visual != null && nuevaHoriz.sqrMagnitude > 0.08f)
        {
            Quaternion rotObj = Quaternion.LookRotation(nuevaHoriz.normalized, Vector3.up);
            visual.rotation = Quaternion.Slerp(visual.rotation, rotObj, Time.fixedDeltaTime * 10f);
        }
    }

    Vector3 CalcularDireccionIsometrica(Vector2 input)
    {
        if (input.sqrMagnitude < 0.001f) return Vector3.zero;
        var cam = Camera.main;
        if (cam == null) return new Vector3(input.x, 0f, input.y).normalized;

        Vector3 fwd = cam.transform.forward;
        fwd.y = 0f;
        fwd.Normalize();
        Vector3 right = cam.transform.right;
        right.y = 0f;
        right.Normalize();
        return (fwd * input.y + right * input.x).normalized;
    }

    void ActualizarValvulaCercana()
    {
        var gestor = GestorRedSuperficie.Instancia;
        if (gestor == null || gestor.valvulasEscena == null)
        {
            valvulaCercana = null;
            return;
        }

        NodoValvula3D mejor = null;
        float mejorDist = radioInteraccion * radioInteraccion;
        foreach (var v in gestor.valvulasEscena)
        {
            if (v == null) continue;
            float d2 = (v.transform.position - transform.position).sqrMagnitude;
            if (d2 <= mejorDist)
            {
                mejorDist = d2;
                mejor = v;
            }
        }

        valvulaCercana = mejor;
        if (HUDRed3D.Instancia != null)
            HUDRed3D.Instancia.ActualizarAvisoInteraccion(valvulaCercana != null ? valvulaCercana.ObtenerEtiquetaAccion() : "");
    }

    void OnCollisionEnter(Collision c) => EvaluarContacto(c.gameObject);
    void OnCollisionStay(Collision c) => EvaluarContacto(c.gameObject);
    void OnTriggerEnter(Collider other) => EvaluarContacto(other.gameObject);

    void EvaluarContacto(GameObject otro)
    {
        if (reapareciendo) return;

        if (otro.layer == capaPeligro)
        {
            Chocar();
            return;
        }

        if (otro.CompareTag("Finish"))
        {
            var gestor = GestorRedSuperficie.Instancia;
            if (gestor == null || gestor.RedEquilibradaConfirmada)
            {
                if (LevelManager.Instancia != null)
                    LevelManager.Instancia.Completar();
            }
            else if (Time.time >= sigAvisoSalida)
            {
                sigAvisoSalida = Time.time + 2f;
                if (HUDRed3D.Instancia != null)
                    HUDRed3D.Instancia.MostrarAlertaTemporal(
                        "Aísla la fuga, abastece los 5 sectores y confirma presurización [P] antes de salir.", 2.6f);
            }
        }
    }

    void Chocar()
    {
        if (invulnerable > 0f || reapareciendo) return;
        invulnerable = 1.0f;
        AudioManager.Instancia.SonarChoque();
        if (sacudida != null) sacudida.GenerateImpulse();

        bool sigue = LevelManager.Instancia == null || LevelManager.Instancia.RegistrarChoque(transform.position);
        StartCoroutine(Reaparecer(sigue));
    }

    IEnumerator Reaparecer(bool mover)
    {
        reapareciendo = true;
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
        yield return new WaitForSeconds(0.55f);
        if (mover)
        {
            rb.position = puntoReaparicion;
            transform.position = puntoReaparicion;
        }
        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        reapareciendo = false;
    }
}
