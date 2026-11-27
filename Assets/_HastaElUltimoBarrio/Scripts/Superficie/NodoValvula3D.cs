using UnityEngine;

// Valvula física interactuable en la superficie 3D conectada a una arista de GrafoHidraulico.
public class NodoValvula3D : MonoBehaviour
{
    [Header("Vinculo Hidraulico")]
    public string aristaId;
    public string nombreVisible = "Válvula";
    public bool esBinaria = true;
    [Range(0f, 1f)] public float aperturaObjetivo = 0.65f;
    [Range(0.05f, 0.5f)] public float pasoEscalon = 0.20f;

    [Header("Referencias Visuales")]
    public Transform manijaRotable;
    public Light luzIndicadora;
    public GameObject objetoFugaAsociada;
    public Renderer[] tuberiasAsociadas;
    public Material matTuberiaSeca;
    public Material matTuberiaActivaCian;

    public float AperturaActual { get; private set; }
    public bool EsAristaRota { get; private set; }

    public string ObtenerEtiquetaAccion()
    {
        if (EsAristaRota)
            return AperturaActual > 0.01f
                ? $"[E] Cerrar válvula de rotura ({nombreVisible})"
                : $"[E] Reabrir fuga ({nombreVisible}) — CERRADA";

        if (esBinaria)
            return AperturaActual > 0.01f
                ? $"[E] Cerrar {nombreVisible}"
                : $"[E] Abrir {nombreVisible} ({Mathf.RoundToInt(aperturaObjetivo * 100f)}%)";

        return $"[E] Ajustar {nombreVisible} ({Mathf.RoundToInt(AperturaActual * 100f)}%)";
    }

    public float CalcularSiguienteApertura(float actual)
    {
        if (esBinaria)
            return actual > 0.01f ? 0f : Mathf.Clamp01(aperturaObjetivo);

        float siguiente = Mathf.Round((actual + pasoEscalon) * 20f) / 20f;
        if (siguiente > 1.001f) siguiente = 0f;
        return Mathf.Clamp01(siguiente);
    }

    public void AplicarEstadoVisual(float apertura, bool aristaActiva, bool esRota)
    {
        AperturaActual = Mathf.Clamp01(apertura);
        EsAristaRota = esRota;

        if (manijaRotable != null)
            manijaRotable.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(90f, 0f, AperturaActual));

        if (luzIndicadora != null)
        {
            if (esRota)
                luzIndicadora.color = AperturaActual > 0.01f ? new Color(1f, 0.25f, 0.15f) : new Color(0.18f, 0.85f, 0.45f);
            else
                luzIndicadora.color = aristaActiva ? new Color(0.15f, 0.85f, 0.95f) : new Color(1f, 0.62f, 0.1f);
        }

        if (objetoFugaAsociada != null)
            objetoFugaAsociada.SetActive(esRota && AperturaActual > 0.01f);

        if (tuberiasAsociadas != null && matTuberiaSeca != null && matTuberiaActivaCian != null)
        {
            Material mat = aristaActiva && !esRota ? matTuberiaActivaCian : matTuberiaSeca;
            foreach (var r in tuberiasAsociadas)
            {
                if (r != null) r.sharedMaterial = mat;
            }
        }
    }
}
