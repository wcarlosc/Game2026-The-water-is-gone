using System.Collections.Generic;
using UnityEngine;

// Persistente entre escenas: energía sobrante y puntaje de cada nivel.
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

    void Awake()
    {
        if (instancia != null && instancia != this) { Destroy(gameObject); return; }
        instancia = this;
        DontDestroyOnLoad(gameObject);
    }

    public void GuardarPuntaje(string nivel, int puntaje)
    {
        Puntajes[nivel] = puntaje;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ReiniciarEstatico() { instancia = null; }
}
