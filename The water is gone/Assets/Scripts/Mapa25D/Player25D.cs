using UnityEngine;
using UnityEngine.InputSystem;

// Controlador simple para 2.5D: se mueve en X, salta en Y y queda fijo en un carril Z.
[RequireComponent(typeof(CharacterController))]
public class Player25D : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidad = 6f;
    public float fuerzaSalto = 7.5f;
    public float gravedad = -20f;
    public float carrilZ = 0f;

    [Header("Respawn")]
    public float alturaMuerte = -6f;
    public Transform visual;

    CharacterController cc;
    Vector3 vel;
    Vector3 puntoInicio;
    float coyote;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        puntoInicio = transform.position;
    }

    void Update()
    {
        float dir = 0f;
        bool saltar = false;
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) dir -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) dir += 1f;
            saltar |= kb.spaceKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame;
        }
        var gp = Gamepad.current;
        if (gp != null)
        {
            float x = gp.leftStick.x.ReadValue();
            if (Mathf.Abs(x) > 0.2f) dir = Mathf.Sign(x);
            saltar |= gp.buttonSouth.wasPressedThisFrame;
        }

        if (cc.isGrounded) { coyote = 0.12f; if (vel.y < 0f) vel.y = -2f; }
        else coyote -= Time.deltaTime;

        if (saltar && coyote > 0f) { vel.y = fuerzaSalto; coyote = 0f; }
        vel.y += gravedad * Time.deltaTime;
        vel.x = dir * velocidad;

        // Mantener el carril Z
        float corrZ = (carrilZ - transform.position.z) * 10f;
        cc.Move(new Vector3(vel.x, vel.y, corrZ) * Time.deltaTime);

        if (visual != null && Mathf.Abs(dir) > 0.01f)
            visual.localRotation = Quaternion.Euler(0f, dir > 0 ? 90f : -90f, 0f);

        if (transform.position.y < alturaMuerte) Respawn();
    }

    public void SetCheckpoint(Vector3 p) { puntoInicio = p; }

    public void Respawn()
    {
        cc.enabled = false;
        transform.position = puntoInicio;
        vel = Vector3.zero;
        cc.enabled = true;
    }
}
