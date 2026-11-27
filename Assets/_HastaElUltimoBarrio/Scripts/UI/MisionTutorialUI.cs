using HastaElUltimoBarrio.Dominio;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Minitutorial y tarjeta visual de mision por nivel: muestra situacion del barrio,
// teclas graficas y pasos exactos para ganar, pausando tiempo y peligros hasta confirmar.
public class MisionTutorialUI : MonoBehaviour
{
    public static MisionTutorialUI Instancia { get; private set; }
    public static bool PausaActiva { get; private set; }
    public static float BloqueoInputHastaTiempoReal { get; private set; }
    public static bool JuegoBloqueadoPorTutorial => PausaActiva || Time.unscaledTime < BloqueoInputHastaTiempoReal;

    static readonly Color VeloOscuro = new Color(0.02f, 0.04f, 0.06f, 0.80f);
    static readonly Color FondoModal = new Color(0.06f, 0.09f, 0.13f, 0.96f);
    static readonly Color BloqueFondo = new Color(0.09f, 0.14f, 0.19f, 0.92f);
    static readonly Color FondoTecla = new Color(0.13f, 0.19f, 0.26f, 1f);
    static readonly Color Ambar = new Color(1f, 0.62f, 0.10f);
    static readonly Color Cian = new Color(0.15f, 0.80f, 0.88f);

    Font fuente;
    GameObject modalRaiz;
    GameObject botonGuiaObj;
    Text txtBotonPrincipal;
    FichaMisionNivel ficha;
    bool esReapertura;
    float tiempoAperturaReal;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ReiniciarEstatico()
    {
        LimpiarEstadoPausaEstatico();
    }

    public static void LimpiarEstadoPausaEstatico()
    {
        Instancia = null;
        PausaActiva = false;
        BloqueoInputHastaTiempoReal = 0f;
        Time.timeScale = 1f;
    }

    public static MisionTutorialUI AsegurarEnEscena()
    {
        string escena = SceneManager.GetActiveScene().name;
        if (escena == "MenuPrincipal") return null;

        CargadorEscenasSeguro.AsegurarEventSystemInputSystem();
        if (Instancia != null) return Instancia;

        var existente = FindAnyObjectByType<MisionTutorialUI>();
        if (existente != null) return existente;

        var go = new GameObject("MisionTutorialUI");
        return go.AddComponent<MisionTutorialUI>();
    }

    void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        Instancia = this;
        ficha = CatalogoFichasMision.ObtenerFicha(SceneManager.GetActiveScene().name);
    }

    void Start()
    {
        CargadorEscenasSeguro.AsegurarEventSystemInputSystem();
        fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        ConstruirInterfaz();
        AbrirTarjeta(reapertura: false);
    }

    void OnDestroy()
    {
        if (Instancia == this)
        {
            Instancia = null;
            PausaActiva = false;
            BloqueoInputHastaTiempoReal = 0f;
            Time.timeScale = 1f;
        }
    }

    void Update()
    {
        var lm = LevelManager.Instancia;
        if (lm != null && lm.Terminado)
        {
            if (botonGuiaObj != null && botonGuiaObj.activeSelf) botonGuiaObj.SetActive(false);
            if (PausaActiva) CerrarTarjeta();
            return;
        }

        var kb = Keyboard.current;
        if (kb == null) return;

        if (PausaActiva)
        {
            if (Time.unscaledTime - tiempoAperturaReal < 0.15f) return;

            bool cerrar = kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame;
            if (esReapertura)
                cerrar |= kb.escapeKey.wasPressedThisFrame || kb.hKey.wasPressedThisFrame;

            if (cerrar)
                CerrarTarjeta();
            return;
        }

        if (kb.hKey.wasPressedThisFrame)
            AbrirTarjeta(reapertura: true);
    }

    void ConstruirInterfaz()
    {
        Color acento = ficha.EsSuperficie3D ? Cian : Ambar;

        var canvasGo = new GameObject("Tutorial_Canvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 45;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // Boton persistente en esquina inferior izquierda para reabrir la guia cuando se desee
        var rtGuia = Caja(canvasGo.transform, "BtnGuiaMision", new Vector2(0f, 0f), new Vector2(26f, 24f),
            new Vector2(178f, 46f), new Color(0.06f, 0.10f, 0.15f, 0.90f), new Vector2(0f, 0f));
        botonGuiaObj = rtGuia.gameObject;
        var bordeGuia = botonGuiaObj.AddComponent<Outline>();
        bordeGuia.effectColor = acento;
        bordeGuia.effectDistance = new Vector2(1.5f, -1.5f);
        var btnGuia = botonGuiaObj.AddComponent<Button>();
        btnGuia.onClick.AddListener(() => AbrirTarjeta(reapertura: true));
        var txtG = Texto(rtGuia, "? Guía  [H]", Vector2.zero, new Vector2(178f, 46f), 20, acento, TextAnchor.MiddleCenter, FontStyle.Bold);
        Estirar(txtG.rectTransform);

        // Velo de fondo + Modal centrado
        var velo = Caja(canvasGo.transform, "VeloTutorial", new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(1920f, 1080f), VeloOscuro, new Vector2(0.5f, 0.5f));
        velo.anchorMin = Vector2.zero;
        velo.anchorMax = Vector2.one;
        velo.offsetMin = Vector2.zero;
        velo.offsetMax = Vector2.zero;
        modalRaiz = velo.gameObject;

        var modal = Caja(velo, "TarjetaMision", new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(1180f, 720f), FondoModal, new Vector2(0.5f, 0.5f));
        var bordeModal = modal.gameObject.AddComponent<Outline>();
        bordeModal.effectColor = acento;
        bordeModal.effectDistance = new Vector2(2f, -2f);

        // Encabezado: chip de fase + titulo del nivel
        var chip = Caja(modal, "ChipFase", new Vector2(0f, 1f), new Vector2(40f, -28f),
            new Vector2(400f, 34f), new Color(acento.r * 0.25f, acento.g * 0.25f, acento.b * 0.25f, 1f), new Vector2(0f, 1f));
        chip.gameObject.AddComponent<Outline>().effectColor = acento;
        var txtChip = Texto(chip, ficha.EtiquetaFase, Vector2.zero, new Vector2(400f, 34f), 17, acento, TextAnchor.MiddleCenter, FontStyle.Bold);
        Estirar(txtChip.rectTransform);

        Texto(modal, ficha.Titulo, new Vector2(40f, -70f), new Vector2(1100f, 48f), 34, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);

        // Bloque (a): Situacion del barrio
        var blkSit = Caja(modal, "BloqueSituacion", new Vector2(0f, 1f), new Vector2(40f, -126f),
            new Vector2(1100f, 114f), BloqueFondo, new Vector2(0f, 1f));
        blkSit.gameObject.AddComponent<Outline>().effectColor = new Color(acento.r, acento.g, acento.b, 0.35f);
        Texto(blkSit, "A) SITUACIÓN DEL BARRIO", new Vector2(18f, -10f), new Vector2(1060f, 26f), 18, acento, TextAnchor.MiddleLeft, FontStyle.Bold);
        Texto(blkSit, ficha.SituacionBarrio, new Vector2(18f, -38f), new Vector2(1060f, 68f), 20, new Color(0.90f, 0.94f, 0.98f), TextAnchor.UpperLeft, FontStyle.Normal);

        // Bloque (b): Controles visuales con keycaps
        var blkCtrl = Caja(modal, "BloqueControles", new Vector2(0f, 1f), new Vector2(40f, -256f),
            new Vector2(540f, 366f), BloqueFondo, new Vector2(0f, 1f));
        blkCtrl.gameObject.AddComponent<Outline>().effectColor = new Color(acento.r, acento.g, acento.b, 0.35f);
        Texto(blkCtrl, "B) CONTROLES DEL NIVEL", new Vector2(18f, -12f), new Vector2(504f, 28f), 18, acento, TextAnchor.MiddleLeft, FontStyle.Bold);

        for (int i = 0; i < ficha.Controles.Length && i < 4; i++)
        {
            var ctrl = ficha.Controles[i];
            float y = -48f - i * 76f;

            var keycap = Caja(blkCtrl, "Tecla_" + i, new Vector2(0f, 1f), new Vector2(18f, y - 4f),
                new Vector2(174f, 54f), FondoTecla, new Vector2(0f, 1f));
            var bordeKey = keycap.gameObject.AddComponent<Outline>();
            bordeKey.effectColor = acento;
            bordeKey.effectDistance = new Vector2(1.5f, -1.5f);
            var txtKey = Texto(keycap, ctrl.Tecla, Vector2.zero, new Vector2(174f, 54f), 16, acento, TextAnchor.MiddleCenter, FontStyle.Bold);
            Estirar(txtKey.rectTransform);

            Texto(blkCtrl, ctrl.Accion, new Vector2(204f, y), new Vector2(320f, 24f), 19, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            Texto(blkCtrl, ctrl.Detalle, new Vector2(204f, y - 24f), new Vector2(320f, 40f), 16, new Color(0.78f, 0.85f, 0.91f), TextAnchor.UpperLeft, FontStyle.Normal);
        }

        // Bloque (c): Pasos exactos para ganar
        var blkPasos = Caja(modal, "BloquePasos", new Vector2(0f, 1f), new Vector2(600f, -256f),
            new Vector2(540f, 366f), BloqueFondo, new Vector2(0f, 1f));
        blkPasos.gameObject.AddComponent<Outline>().effectColor = new Color(acento.r, acento.g, acento.b, 0.35f);
        Texto(blkPasos, "C) PASOS EXACTOS PARA GANAR", new Vector2(18f, -12f), new Vector2(504f, 28f), 18, acento, TextAnchor.MiddleLeft, FontStyle.Bold);

        for (int i = 0; i < ficha.PasosGanar.Length && i < 4; i++)
        {
            float y = -48f - i * 76f;
            var badge = Caja(blkPasos, "Paso_" + i, new Vector2(0f, 1f), new Vector2(18f, y - 8f),
                new Vector2(38f, 38f), acento, new Vector2(0f, 1f));
            var txtNum = Texto(badge, (i + 1).ToString(), Vector2.zero, new Vector2(38f, 38f), 20, Color.black, TextAnchor.MiddleCenter, FontStyle.Bold);
            Estirar(txtNum.rectTransform);

            Texto(blkPasos, ficha.PasosGanar[i], new Vector2(68f, y - 2f), new Vector2(454f, 66f), 18, new Color(0.92f, 0.95f, 0.98f), TextAnchor.MiddleLeft, FontStyle.Normal);
        }

        // Botones de pie de tarjeta
        Boton(modal, "BtnMenuMision", "Volver al Menú Principal", new Vector2(40f, -640f), new Vector2(330f, 54f),
            new Color(0.20f, 0.24f, 0.30f), Color.white, () => CargadorEscenasSeguro.IntentarCargar("MenuPrincipal"));

        var btnComenzar = Boton(modal, "BtnComenzarMision", "Comenzar Misión  [Enter / Espacio]", new Vector2(740f, -640f), new Vector2(400f, 54f),
            acento, Color.black, CerrarTarjeta);
        txtBotonPrincipal = btnComenzar.GetComponentInChildren<Text>();
    }

    public void AbrirTarjeta(bool reapertura)
    {
        if (modalRaiz == null) return;
        esReapertura = reapertura;
        PausaActiva = true;
        tiempoAperturaReal = Time.unscaledTime;
        Time.timeScale = 0f;

        if (txtBotonPrincipal != null)
            txtBotonPrincipal.text = reapertura ? "Continuar Misión  [Enter / Espacio]" : "Comenzar Misión  [Enter / Espacio]";

        modalRaiz.SetActive(true);
        if (botonGuiaObj != null) botonGuiaObj.SetActive(false);
    }

    public void CerrarTarjeta()
    {
        if (!PausaActiva && modalRaiz != null && !modalRaiz.activeSelf) return;

        PausaActiva = false;
        BloqueoInputHastaTiempoReal = Time.unscaledTime + 0.18f;
        Time.timeScale = 1f;

        if (modalRaiz != null) modalRaiz.SetActive(false);
        if (botonGuiaObj != null) botonGuiaObj.SetActive(true);
    }

    RectTransform Caja(Transform padre, string nombre, Vector2 ancla, Vector2 pos, Vector2 tam, Color color, Vector2 pivote)
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

    Text Texto(Transform padre, string contenido, Vector2 pos, Vector2 tam, int tamFuente, Color color, TextAnchor al, FontStyle estilo)
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
        t.fontSize = tamFuente;
        t.fontStyle = estilo;
        t.color = color;
        t.alignment = al;
        t.text = contenido;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    Button Boton(Transform padre, string nombre, string etiqueta, Vector2 pos, Vector2 tam, Color fondo, Color textoColor, System.Action accion)
    {
        var rt = Caja(padre, nombre, new Vector2(0f, 1f), pos, tam, fondo, new Vector2(0f, 1f));
        var btn = rt.gameObject.AddComponent<Button>();
        var colores = btn.colors;
        colores.highlightedColor = Color.Lerp(fondo, Color.white, 0.22f);
        colores.pressedColor = Color.Lerp(fondo, Color.black, 0.22f);
        btn.colors = colores;
        btn.onClick.AddListener(() => accion?.Invoke());

        var t = Texto(rt, etiqueta, Vector2.zero, tam, 21, textoColor, TextAnchor.MiddleCenter, FontStyle.Bold);
        Estirar(t.rectTransform);
        return btn;
    }

    static void Estirar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
