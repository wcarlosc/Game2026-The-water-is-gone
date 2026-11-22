using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Uno por escena: energía, puntaje, choques, reinicio y fin del nivel.
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instancia { get; private set; }

    [Header("Nivel")]
    public string nombreNivel = "Nivel";
    [TextArea] public string objetivo = "Lleva el dron a la central.";
    [Tooltip("Escena que se carga al terminar. Vacío = siguiente en Build Profiles.")]
    public string siguienteEscena = "";

    [Header("Reglas")]
    public float energiaBase = 10f;
    public int puntajeInicial = 100;
    public int puntosPorChoque = 20;
    public int choquesParaReiniciar = 5;

    public float Energia { get; private set; }
    public float EnergiaInicial { get; private set; }
    public float EnergiaMaxima { get; private set; }
    public int Puntaje { get; private set; }
    public int Choques { get; private set; }
    public bool Terminado { get; private set; }

    public event System.Action<string, float> Mensaje;   // texto, duración
    public event System.Action<string> Resultado;        // tarjeta final

    void Awake()
    {
        Instancia = this;
        var gm = GameManager.Instancia;
        gm.NivelActual = SceneManager.GetActiveScene().name;
        EnergiaInicial = energiaBase + gm.EnergiaSobrante;
        Energia = EnergiaInicial;
        EnergiaMaxima = Mathf.Max(EnergiaInicial, energiaBase);
        Puntaje = puntajeInicial;
    }

    void Start()
    {
        if (Mensaje != null) Mensaje(objetivo, 4f);
    }

    public void ConsumirEnergia(float cantidad)
    {
        if (Terminado) return;
        Energia -= cantidad;
        if (Energia <= 0f)
        {
            Energia = 0f;
            Reiniciar("Sin combustible");
        }
    }

    public void SumarEnergia(float cantidad)
    {
        if (Terminado) return;
        Energia += cantidad;
        EnergiaMaxima = Mathf.Max(EnergiaMaxima, Energia);
        if (Mensaje != null) Mensaje("+" + cantidad.ToString("0.#") + " energía", 1.2f);
    }

    // Devuelve false si el choque reinicia el nivel.
    public bool RegistrarChoque()
    {
        if (Terminado) return false;
        Choques++;
        Puntaje = Mathf.Max(0, Puntaje - puntosPorChoque);
        if (Choques >= choquesParaReiniciar)
        {
            Reiniciar("Demasiados choques");
            return false;
        }
        if (Mensaje != null) Mensaje("¡Choque!  -" + puntosPorChoque, 1.2f);
        return true;
    }

    public void Reiniciar(string motivo)
    {
        if (Terminado) return;
        Terminado = true;
        StartCoroutine(RutinaReinicio(motivo));
    }

    IEnumerator RutinaReinicio(string motivo)
    {
        if (Mensaje != null) Mensaje(motivo + "\nReiniciando nivel...", 2.5f);
        yield return new WaitForSeconds(2.5f);
        // La energía vuelve a la inicial porque el sobrante del GameManager no cambió.
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex >= 0
            ? SceneManager.GetActiveScene().name
            : SceneManager.GetActiveScene().path);
    }

    public void Completar()
    {
        if (Terminado) return;
        Terminado = true;
        var gm = GameManager.Instancia;
        gm.EnergiaSobrante = Energia;
        gm.GuardarPuntaje(SceneManager.GetActiveScene().name, Puntaje);
        StartCoroutine(RutinaFin());
    }

    IEnumerator RutinaFin()
    {
        string txt = nombreNivel + " completado\n\nPuntaje: " + Puntaje + "\nChoques: " + Choques +
                     "\nEnergía sobrante: " + Energia.ToString("0.0");
        if (Resultado != null) Resultado(txt);
        yield return new WaitForSeconds(4f);

        string destino = siguienteEscena;
        if (string.IsNullOrEmpty(destino))
        {
            int sig = SceneManager.GetActiveScene().buildIndex + 1;
            if (sig > 0 && sig < SceneManager.sceneCountInBuildSettings)
            {
                SceneManager.LoadScene(sig);
                yield break;
            }
            if (Resultado != null) Resultado(txt + "\n\nFin de los túneles");
            yield break;
        }
        if (Application.CanStreamedLevelBeLoaded(destino)) SceneManager.LoadScene(destino);
        else if (Resultado != null) Resultado(txt + "\n\n(Falta la escena " + destino + ")");
    }
}
