using UnityEngine;

// Gestor persistente de efectos sonoros del proyecto.
// Carga los clips desde Resources/Audio si no fueron asignados en el Inspector.
public class AudioManager : MonoBehaviour
{
    static AudioManager instancia;

    public static AudioManager Instancia
    {
        get
        {
            if (instancia == null)
            {
                var go = new GameObject("AudioManager");
                instancia = go.AddComponent<AudioManager>();
            }
            return instancia;
        }
    }

    [Header("Clips obligatorios")]
    public AudioClip clipBateria;
    public AudioClip clipChoque;
    public AudioClip clipLlegada;
    public AudioClip clipPropulsor;
    public AudioClip clipAgua;

    [Header("Ajustes")]
    [Range(0f, 1f)] public float volumenGeneral = 0.85f;
    [Range(0f, 1f)] public float volumenPropulsor = 0.45f;
    public float intervaloMinPropulsor = 0.06f;

    AudioSource sfxSource;
    AudioSource propulsorSource;
    float ultimoPropulsor = -10f;

    void Awake()
    {
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        instancia = this;
        DontDestroyOnLoad(gameObject);

        sfxSource = CrearFuente("SFX_General", volumenGeneral);
        propulsorSource = CrearFuente("SFX_Propulsor", volumenPropulsor);
        CargarRecursosPorDefecto();
    }

    AudioSource CrearFuente(string nombre, float volumen)
    {
        var hijo = new GameObject(nombre);
        hijo.transform.SetParent(transform, false);
        var src = hijo.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = false;
        src.spatialBlend = 0f;
        src.volume = volumen;
        return src;
    }

    void CargarRecursosPorDefecto()
    {
        if (clipBateria == null) clipBateria = Resources.Load<AudioClip>("Audio/sfx_bateria");
        if (clipChoque == null) clipChoque = Resources.Load<AudioClip>("Audio/sfx_choque");
        if (clipLlegada == null) clipLlegada = Resources.Load<AudioClip>("Audio/sfx_llegada");
        if (clipPropulsor == null) clipPropulsor = Resources.Load<AudioClip>("Audio/sfx_propulsor");
        if (clipAgua == null) clipAgua = Resources.Load<AudioClip>("Audio/sfx_agua");
    }

    public void SonarBateria() => ReproducirGeneral(clipBateria, 0.9f);
    public void SonarChoque() => ReproducirGeneral(clipChoque, 1.0f);
    public void SonarLlegada() => ReproducirGeneral(clipLlegada, 1.0f);
    public void SonarAgua() => ReproducirGeneral(clipAgua, 0.75f);

    public void SonarPropulsor()
    {
        if (clipPropulsor == null || propulsorSource == null) return;
        if (Time.unscaledTime - ultimoPropulsor < intervaloMinPropulsor) return;
        ultimoPropulsor = Time.unscaledTime;
        propulsorSource.pitch = Random.Range(0.95f, 1.05f);
        propulsorSource.PlayOneShot(clipPropulsor, volumenPropulsor);
    }

    void ReproducirGeneral(AudioClip clip, float escala)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.pitch = 1f;
        sfxSource.PlayOneShot(clip, volumenGeneral * escala);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ReiniciarEstatico() { instancia = null; }
}
