using UnityEngine;

// Meta del nivel y HUD mínimo.
public class Meta25D : MonoBehaviour
{
    public float radio = 1.3f;
    Transform jugador;
    bool completado;
    GUIStyle estilo;

    void Start()
    {
        var p = FindAnyObjectByType<Player25D>();
        if (p != null) jugador = p.transform;
    }

    void Update()
    {
        if (completado || jugador == null) return;
        Vector3 d = jugador.position - transform.position;
        d.y = 0f;
        if (d.magnitude < radio && jugador.position.y > transform.position.y - 0.2f) completado = true;
    }

    void OnGUI()
    {
        if (estilo == null)
        {
            estilo = new GUIStyle(GUI.skin.label);
            estilo.fontSize = 22;
            estilo.normal.textColor = Color.white;
        }
        GUI.Label(new Rect(20, 15, 600, 40), "Baterías: " + Coleccionable.recogidos + " / " + Coleccionable.total, estilo);
        GUI.Label(new Rect(20, 45, 600, 40), "A/D o flechas: mover   Espacio: saltar", estilo);
        if (completado)
        {
            var big = new GUIStyle(estilo);
            big.fontSize = 40;
            big.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(0, Screen.height * 0.35f, Screen.width, 60), "¡Nivel completado!", big);
        }
    }
}
