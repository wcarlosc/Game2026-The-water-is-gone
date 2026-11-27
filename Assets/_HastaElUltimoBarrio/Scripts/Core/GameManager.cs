using System.Collections.Generic;
using HastaElUltimoBarrio.Dominio;
using UnityEngine;

// Persistente entre escenas: energía sobrante, puntaje de cada nivel y telemetría.
public class GameManager : MonoBehaviour
{
    static GameManager instancia;

    public static GameManager Instancia
    {
        get
        {
            if (instancia == null)
            {
                var go = new GameObject("GameManager");
                instancia = go.AddComponent<GameManager>();
            }
            return instancia;
        }
    }

    [Tooltip("Energía que sobró del nivel anterior y se suma al siguiente.")]
    public float EnergiaSobrante;
    public string NivelActual;
    public readonly Dictionary<string, int> Puntajes = new Dictionary<string, int>();

    TelemetryService telemetria;
    public TelemetryService Telemetria
    {
        get
        {
            if (telemetria == null)
                telemetria = new TelemetryService();
            return telemetria;
        }
    }

    void Awake()
    {
        if (instancia != null && instancia != this) { Destroy(gameObject); return; }
        instancia = this;
        DontDestroyOnLoad(gameObject);
        if (telemetria == null)
            telemetria = new TelemetryService();
    }

    public void GuardarPuntaje(string nivel, int puntaje)
    {
        Puntajes[nivel] = puntaje;
    }

    public float ObtenerPromedioPuntajes()
    {
        if (Puntajes.Count == 0) return 0f;
        float suma = 0f;
        foreach (var kv in Puntajes) suma += kv.Value;
        return suma / Puntajes.Count;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ConfigurarMatrizColisiones()
    {
        int jugador = LayerMask.NameToLayer("Jugador");
        int seguro = LayerMask.NameToLayer("Seguro");
        int peligro = LayerMask.NameToLayer("Peligro");
        int recolectable = LayerMask.NameToLayer("Recolectable");
        int interactuable = LayerMask.NameToLayer("Interactuable");

        if (seguro >= 0) Physics.IgnoreLayerCollision(seguro, seguro, true);
        if (peligro >= 0) Physics.IgnoreLayerCollision(peligro, peligro, true);
        if (seguro >= 0 && peligro >= 0) Physics.IgnoreLayerCollision(seguro, peligro, true);

        if (recolectable >= 0)
        {
            Physics.IgnoreLayerCollision(recolectable, recolectable, true);
            if (seguro >= 0) Physics.IgnoreLayerCollision(recolectable, seguro, true);
            if (peligro >= 0) Physics.IgnoreLayerCollision(recolectable, peligro, true);
        }

        if (interactuable >= 0 && peligro >= 0)
            Physics.IgnoreLayerCollision(interactuable, peligro, true);

        // Asegura que Jugador mantenga colision con Seguro, Peligro, Recolectable e Interactuable.
        if (jugador >= 0)
        {
            if (seguro >= 0) Physics.IgnoreLayerCollision(jugador, seguro, false);
            if (peligro >= 0) Physics.IgnoreLayerCollision(jugador, peligro, false);
            if (recolectable >= 0) Physics.IgnoreLayerCollision(jugador, recolectable, false);
            if (interactuable >= 0) Physics.IgnoreLayerCollision(jugador, interactuable, false);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ReiniciarEstatico() { instancia = null; }
}
