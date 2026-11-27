using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Menu principal construido por codigo: titulo institucional, guion corto,
// inicio de mision, ventana modal de creditos y salida segura.
public class MenuPrincipalUI : MonoBehaviour
{
    static readonly Color FondoPanel = new Color(0.05f, 0.08f, 0.11f, 0.86f);
    static readonly Color FondoModal = new Color(0.04f, 0.06f, 0.09f, 0.96f);
    static readonly Color Ambar = new Color(1f, 0.62f, 0.10f);
    static readonly Color Cian = new Color(0.15f, 0.78f, 0.85f);

    [Header("Navegacion")]
    public string escenaIniciarMision = "LVL_01_GaleriaCaptacion";

    [Header("Creditos")]
    public string tituloAcademico =
        "Modelado de Redes de Distribución Hídrica mediante Grafos y Optimización de Equidad en un Videojuego Serio para la Sensibilización Socioambiental en Arequipa";
    public string subtituloJuego = "HASTA EL ÚLTIMO BARRIO";
    public string asignatura = "Introducción al Desarrollo de Software de Entretenimiento (IDSE) — Semestre 2026-B";
    public string docenteAsesor = "M. Sc. Ing. Giovanni Cabrera Málaga";
    public string[] autores =
    {
        "Carlos Ccamaqque, Wilson Freddy",
        "Choquehuanca Bedoya, Brayan Denilson"
    };

    Font fuente;
    GameObject modalCreditos;

    public static void AsegurarEventSystem()
    {
        CargadorEscenasSeguro.AsegurarEventSystemInputSystem();
    }

    void Start()
    {
        AsegurarEventSystem();
        fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        ConstruirInterfaz();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (modalCreditos != null && modalCreditos.activeSelf)
        {
            if (kb.escapeKey.wasPressedThisFrame)
                AlternarCreditos(false);
            return;
        }

        if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
            IniciarMision();
    }

    void ConstruirInterfaz()
    {
        var canvasGo = new GameObject("Menu_Canvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // Tarjeta lateral izquierda para dejar visible la vinyeta 3D del lavadero andino.
        var panel = CrearCaja(canvasGo.transform, "PanelMenu", new Vector2(0f, 0.5f), new Vector2(70f, 0f),
            new Vector2(760f, 760f), FondoPanel, new Vector2(0f, 0.5f));

        CrearTexto(panel, "UNSA · EPIS · FERIA DE PROYECTOS 2026 (CATEGORÍA II)",
            new Vector2(44f, -38f), new Vector2(670f, 34f), 20, Cian, TextAnchor.MiddleLeft, FontStyle.Bold);

        CrearTexto(panel, subtituloJuego,
            new Vector2(44f, -82f), new Vector2(670f, 68f), 50, Ambar, TextAnchor.MiddleLeft, FontStyle.Bold);

        CrearTexto(panel, tituloAcademico,
            new Vector2(44f, -158f), new Vector2(670f, 110f), 22, new Color(0.92f, 0.95f, 0.98f), TextAnchor.UpperLeft, FontStyle.Normal);

        CrearTexto(panel,
            "Una rotura en la red principal ha dejado secos los grifos de las terrazas altas. " +
            "Inspecciona los túneles con el dron y equilibra las válvulas en superficie hasta abastecer " +
            "los cinco sectores comunitarios.",
            new Vector2(44f, -288f), new Vector2(670f, 128f), 23, new Color(0.8f, 0.85f, 0.9f), TextAnchor.UpperLeft, FontStyle.Normal);

        CrearBoton(panel, "BtnIniciar", "Iniciar Misión  [Enter]",
            new Vector2(44f, -450f), new Vector2(440f, 72f), Ambar, Color.black, IniciarMision);

        CrearBoton(panel, "BtnCreditos", "Créditos del Equipo",
            new Vector2(44f, -542f), new Vector2(440f, 64f), new Color(0.14f, 0.24f, 0.30f), Color.white, () => AlternarCreditos(true));

        CrearBoton(panel, "BtnSalir", "Salir",
            new Vector2(44f, -626f), new Vector2(440f, 58f), new Color(0.22f, 0.14f, 0.14f), Color.white, Salir);

        // Modal de creditos
        var modal = CrearCaja(canvasGo.transform, "ModalCreditos", new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(980f, 620f), FondoModal, new Vector2(0.5f, 0.5f));
        modalCreditos = modal.gameObject;

        var borde = modalCreditos.AddComponent<Outline>();
        borde.effectColor = Ambar;
        borde.effectDistance = new Vector2(2f, -2f);

        CrearTexto(modal, "CRÉDITOS Y AUTORÍA",
            new Vector2(48f, -34f), new Vector2(880f, 44f), 32, Ambar, TextAnchor.MiddleCenter, FontStyle.Bold);

        CrearTexto(modal, subtituloJuego + "\n" + tituloAcademico,
            new Vector2(48f, -92f), new Vector2(880f, 92f), 21, Cian, TextAnchor.MiddleCenter, FontStyle.Normal);

        string listaAutores = "";
        for (int i = 0; i < autores.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(autores[i])) continue;
            listaAutores += "•  " + autores[i] + "\n";
        }

        string cuerpo =
            "Universidad Nacional de San Agustín de Arequipa (UNSA)\n" +
            "Escuela Profesional de Ingeniería de Sistemas (EPIS)\n\n" +
            "Asignatura: " + asignatura + "\n" +
            "Docente Asesor: " + docenteAsesor + "\n\n" +
            "Autores:\n" + listaAutores;

        CrearTexto(modal, cuerpo,
            new Vector2(64f, -198f), new Vector2(850f, 300f), 23, Color.white, TextAnchor.UpperLeft, FontStyle.Normal);

        CrearBoton(modal, "BtnCerrarCreditos", "Cerrar  [Esc]",
            new Vector2(340f, -524f), new Vector2(300f, 60f), Ambar, Color.black, () => AlternarCreditos(false));

        modalCreditos.SetActive(false);
    }

    public void AlternarCreditos(bool visible)
    {
        if (modalCreditos != null)
            modalCreditos.SetActive(visible);
    }

    public void IniciarMision()
    {
        var gm = GameManager.Instancia;
        gm.EnergiaSobrante = 0f;
        gm.Puntajes.Clear();
        gm.Telemetria.IniciarSesion();
        AudioManager.Instancia.SonarLlegada();

        if (!CargadorEscenasSeguro.IntentarCargar(escenaIniciarMision))
            Debug.LogWarning("No se encontro la escena inicial: " + escenaIniciarMision);
    }

    public void Salir()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    RectTransform CrearCaja(Transform padre, string nombre, Vector2 ancla, Vector2 pos, Vector2 tam, Color color, Vector2 pivote)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(padre, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = ancla;
        rt.pivot = pivote;
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
        go.GetComponent<Image>().color = color;
        return rt;
    }

    Text CrearTexto(Transform padre, string contenido, Vector2 pos, Vector2 tam, int size, Color color, TextAnchor alineacion, FontStyle estilo)
    {
        var go = new GameObject("Texto", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(padre, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;

        var t = go.GetComponent<Text>();
        t.font = fuente;
        t.fontSize = size;
        t.fontStyle = estilo;
        t.color = color;
        t.alignment = alineacion;
        t.text = contenido;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    Button CrearBoton(Transform padre, string nombre, string etiqueta, Vector2 pos, Vector2 tam, Color fondo, Color textoColor, Action accion)
    {
        var rt = CrearCaja(padre, nombre, new Vector2(0f, 1f), pos, tam, fondo, new Vector2(0f, 1f));
        var btn = rt.gameObject.AddComponent<Button>();
        var colores = btn.colors;
        colores.highlightedColor = Color.Lerp(fondo, Color.white, 0.22f);
        colores.pressedColor = Color.Lerp(fondo, Color.black, 0.22f);
        btn.colors = colores;
        btn.onClick.AddListener(() => accion?.Invoke());

        var txt = CrearTexto(rt, etiqueta, Vector2.zero, tam, 26, textoColor, TextAnchor.MiddleCenter, FontStyle.Bold);
        var trt = txt.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.pivot = new Vector2(0.5f, 0.5f);
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        return btn;
    }
}
