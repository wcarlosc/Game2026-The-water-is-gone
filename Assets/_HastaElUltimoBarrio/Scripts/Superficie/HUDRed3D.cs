using System.Collections.Generic;
using HastaElUltimoBarrio.Dominio;
using UnityEngine;
using UnityEngine.UI;

// HUD para los niveles en superficie 3D: bateria SCADA, puntaje %, fallos X/5,
// panel de los 5 sectores con Indice de Jain y simulador "¿Que pasaria si...?".
public class HUDRed3D : MonoBehaviour
{
    public static HUDRed3D Instancia { get; private set; }

    static readonly Color PanelOscuro = new Color(0.05f, 0.08f, 0.11f, 0.78f);
    static readonly Color Ambar = new Color(1f, 0.62f, 0.10f);
    static readonly Color Cian = new Color(0.15f, 0.80f, 0.88f);
    static readonly Color VerdeOk = new Color(0.22f, 0.82f, 0.48f);
    static readonly Color RojoAlerta = new Color(0.95f, 0.32f, 0.25f);

    class FilaSectorUI
    {
        public string AristaId;
        public Text Titulo;
        public Text Caudal;
        public RectTransform Barra;
        public Image BarraImg;
        public Button BtnMenos;
        public Button BtnMas;
    }

    static readonly Dictionary<string, string> SectorAArista = new Dictionary<string, string>
    {
        { "S1_Mercado", "V1_Mercado" },
        { "S2_Puente", "V2_Puente" },
        { "S3_Salud", "V3_Salud" },
        { "S4_Refugio", "V4_Refugio" },
        { "S5_Ladera", "V5_Ladera" }
    };

    Font fuente;
    LevelManager lm;
    GestorRedSuperficie gestor;

    RectTransform barraBateria;
    Image barraBateriaImg;
    Text txtBateria, txtPuntaje, txtChoques, txtJain, txtEstadoRed, txtAvisoAccion, txtMensaje, txtResultado;
    Image panelDerechoImg;
    Outline bordeSimulacion;
    Button btnSimular, btnRestaurar, btnConfirmar;
    Text txtBtnSimular;
    GameObject panelResultado;
    readonly List<FilaSectorUI> filas = new List<FilaSectorUI>();

    float mensajeHasta;
    int ultDecimas = -1, ultPuntaje = -1, ultChoques = -1, ultMaxChoques = -1;

    void Awake()
    {
        Instancia = this;
    }

    void Start()
    {
        MenuPrincipalUI.AsegurarEventSystem();
        lm = LevelManager.Instancia;
        gestor = GestorRedSuperficie.Instancia;
        fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        ConstruirHUD();

        if (lm != null)
        {
            lm.Mensaje += MostrarAlertaTemporal;
            lm.Resultado += MostrarResultado;
            MostrarAlertaTemporal(lm.objetivo, 4.5f);
        }

        if (gestor != null)
        {
            gestor.EstadoActualizado += RefrescarPanelHidraulico;
            RefrescarPanelHidraulico();
        }
    }

    void OnDestroy()
    {
        if (lm != null)
        {
            lm.Mensaje -= MostrarAlertaTemporal;
            lm.Resultado -= MostrarResultado;
        }
        if (gestor != null)
            gestor.EstadoActualizado -= RefrescarPanelHidraulico;
    }

    void ConstruirHUD()
    {
        var canvasGo = new GameObject("HUD_Red3D_Canvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 15;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // 1. Panel superior izquierdo (mismas reglas del curso: bateria, puntaje %, fallos 0/5)
        var pIzq = Caja(canvasGo.transform, "PanelEstado", new Vector2(0f, 1f), new Vector2(26f, -26f),
            new Vector2(460f, 152f), PanelOscuro, new Vector2(0f, 1f));
        Texto(pIzq, "Batería SCADA", new Vector2(20f, -14f), new Vector2(240f, 34f), 25, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
        txtBateria = Texto(pIzq, "10.0", new Vector2(320f, -14f), new Vector2(120f, 34f), 26, Ambar, TextAnchor.MiddleRight, FontStyle.Bold);
        var fondoBar = Caja(pIzq, "FondoBat", new Vector2(0f, 1f), new Vector2(20f, -54f), new Vector2(420f, 22f), new Color(0f, 0f, 0f, 0.55f), new Vector2(0f, 1f));
        barraBateria = Caja(fondoBar, "RellenoBat", new Vector2(0f, 1f), Vector2.zero, new Vector2(420f, 22f), Ambar, new Vector2(0f, 1f));
        barraBateriaImg = barraBateria.GetComponent<Image>();

        txtPuntaje = Texto(pIzq, "Puntaje 100%", new Vector2(20f, -92f), new Vector2(230f, 40f), 27, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
        txtChoques = Texto(pIzq, "Fallos 0/5", new Vector2(250f, -92f), new Vector2(190f, 40f), 27, Color.white, TextAnchor.MiddleRight, FontStyle.Bold);

        // 2. Panel SCADA derecho con 5 sectores + Jain + "¿Que pasaria si...?"
        var pDer = Caja(canvasGo.transform, "PanelSCADA", new Vector2(1f, 1f), new Vector2(-24f, -24f),
            new Vector2(530f, 680f), PanelOscuro, new Vector2(1f, 1f));
        panelDerechoImg = pDer.GetComponent<Image>();
        bordeSimulacion = pDer.gameObject.AddComponent<Outline>();
        bordeSimulacion.effectColor = Cian;
        bordeSimulacion.effectDistance = new Vector2(2f, -2f);

        Texto(pDer, "RED HIDRÁULICA (5 SECTORES)", new Vector2(20f, -14f), new Vector2(490f, 30f), 22, Cian, TextAnchor.MiddleLeft, FontStyle.Bold);
        txtJain = Texto(pDer, "Equidad Jain: 0.000", new Vector2(20f, -46f), new Vector2(490f, 30f), 21, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
        txtEstadoRed = Texto(pDer, "Estado: Evaluando...", new Vector2(20f, -78f), new Vector2(490f, 30f), 20, Ambar, TextAnchor.MiddleLeft, FontStyle.Normal);

        for (int i = 0; i < 5; i++)
        {
            float y = -122f - i * 82f;
            var fila = new FilaSectorUI();
            fila.Titulo = Texto(pDer, "Sector", new Vector2(20f, y), new Vector2(280f, 28f), 20, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            fila.Caudal = Texto(pDer, "0.0 / 0 L/s", new Vector2(300f, y), new Vector2(210f, 28f), 19, Color.white, TextAnchor.MiddleRight, FontStyle.Normal);

            var fBar = Caja(pDer, "FBar_" + i, new Vector2(0f, 1f), new Vector2(20f, y - 34f), new Vector2(370f, 20f), new Color(0f, 0f, 0f, 0.5f), new Vector2(0f, 1f));
            fila.Barra = Caja(fBar, "Bar_" + i, new Vector2(0f, 1f), Vector2.zero, new Vector2(180f, 20f), Cian, new Vector2(0f, 1f));
            fila.BarraImg = fila.Barra.GetComponent<Image>();

            int idx = i;
            fila.BtnMenos = Boton(pDer, "-", new Vector2(402f, y - 30f), new Vector2(48f, 30f), new Color(0.18f, 0.24f, 0.30f), Color.white, () => AjustarFila(idx, -0.10f));
            fila.BtnMas = Boton(pDer, "+", new Vector2(458f, y - 30f), new Vector2(48f, 30f), new Color(0.18f, 0.24f, 0.30f), Color.white, () => AjustarFila(idx, 0.10f));
            filas.Add(fila);
        }

        btnSimular = Boton(pDer, "¿Qué pasaría si…? [Q]", new Vector2(20f, -544f), new Vector2(310f, 48f),
            new Color(0.15f, 0.36f, 0.44f), Color.white, () => gestor?.AlternarModoSimulacion());
        txtBtnSimular = btnSimular.GetComponentInChildren<Text>();

        btnRestaurar = Boton(pDer, "Vista Real", new Vector2(342f, -544f), new Vector2(168f, 48f),
            new Color(0.26f, 0.26f, 0.30f), Color.white, () => gestor?.RestaurarVistaReal());

        btnConfirmar = Boton(pDer, "Confirmar Presurización [P]", new Vector2(20f, -606f), new Vector2(490f, 54f),
            Ambar, Color.black, () => gestor?.ConfirmarPresurizacion());

        // 3. Aviso de interaccion inferior y mensajes centrales
        txtAvisoAccion = Texto(canvasGo.transform, "", Vector2.zero, new Vector2(780f, 54f), 26, Ambar, TextAnchor.MiddleCenter, FontStyle.Bold);
        var rtAv = txtAvisoAccion.rectTransform;
        rtAv.anchorMin = rtAv.anchorMax = new Vector2(0.5f, 0.08f);
        rtAv.pivot = new Vector2(0.5f, 0.5f);
        rtAv.anchoredPosition = Vector2.zero;
        txtAvisoAccion.gameObject.AddComponent<Outline>().effectColor = Color.black;

        txtMensaje = Texto(canvasGo.transform, "", Vector2.zero, new Vector2(1180f, 120f), 32, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
        var rtMsg = txtMensaje.rectTransform;
        rtMsg.anchorMin = rtMsg.anchorMax = new Vector2(0.36f, 0.86f);
        rtMsg.pivot = new Vector2(0.5f, 0.5f);
        rtMsg.anchoredPosition = Vector2.zero;
        txtMensaje.gameObject.AddComponent<Outline>().effectColor = Color.black;

        // 4. Panel resultado fin de nivel
        var res = Caja(canvasGo.transform, "Resultado", new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(720f, 400f), new Color(0.05f, 0.08f, 0.1f, 0.90f), new Vector2(0.5f, 0.5f));
        panelResultado = res.gameObject;
        txtResultado = Texto(res, "", Vector2.zero, new Vector2(680f, 360f), 34, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
        var rtR = txtResultado.rectTransform;
        rtR.anchorMin = rtR.anchorMax = rtR.pivot = new Vector2(0.5f, 0.5f);
        rtR.anchoredPosition = Vector2.zero;
        panelResultado.SetActive(false);
    }

    void AjustarFila(int idx, float delta)
    {
        if (gestor == null || idx < 0 || idx >= filas.Count) return;
        gestor.AjustarValvulaDesdeHUD(filas[idx].AristaId, delta);
    }

    void Update()
    {
        if (lm != null)
        {
            float frac = lm.EnergiaMaxima > 0f ? Mathf.Clamp01(lm.Energia / lm.EnergiaMaxima) : 0f;
            barraBateria.sizeDelta = new Vector2(420f * frac, 22f);
            bool poco = lm.Energia <= 3f;
            barraBateriaImg.color = poco ? Color.Lerp(Ambar, RojoAlerta, Mathf.PingPong(Time.time * 3f, 1f)) : Ambar;

            int dec = Mathf.RoundToInt(lm.Energia * 10f);
            if (dec != ultDecimas) { ultDecimas = dec; txtBateria.text = (dec * 0.1f).ToString("0.0"); }

            if (lm.Puntaje != ultPuntaje) { ultPuntaje = lm.Puntaje; txtPuntaje.text = "Puntaje " + ultPuntaje + "%"; }

            if (lm.Choques != ultChoques || lm.choquesParaReiniciar != ultMaxChoques)
            {
                ultChoques = lm.Choques;
                ultMaxChoques = lm.choquesParaReiniciar;
                txtChoques.text = "Fallos " + ultChoques + "/" + ultMaxChoques;
                txtChoques.color = ultChoques >= ultMaxChoques - 1 ? RojoAlerta : Color.white;
            }
        }

        if (txtMensaje != null && txtMensaje.text.Length > 0)
        {
            float resto = mensajeHasta - Time.time;
            var c = txtMensaje.color;
            c.a = Mathf.Clamp01(resto / 0.6f);
            txtMensaje.color = c;
            if (resto <= 0f) txtMensaje.text = "";
        }
    }

    void RefrescarPanelHidraulico()
    {
        if (gestor == null || gestor.ResultadoMostrado == null) return;

        var res = gestor.ResultadoMostrado;
        float optJain = gestor.SolucionOptima != null && gestor.SolucionOptima.ResultadoOptimo != null
            ? gestor.SolucionOptima.ResultadoOptimo.IndiceEquidadJain
            : 1f;

        string prefijoSim = gestor.ModoSimulacionActivo ? "[SIMULACIÓN] " : "";
        txtJain.text = $"{prefijoSim}Equidad Jain: {res.IndiceEquidadJain:0.000}   (Óptimo: {optJain:0.000})";
        bordeSimulacion.effectColor = gestor.ModoSimulacionActivo ? Ambar : Cian;
        txtBtnSimular.text = gestor.ModoSimulacionActivo ? "Salir Simulación [Q]" : "¿Qué pasaría si…? [Q]";
        btnRestaurar.gameObject.SetActive(gestor.ModoSimulacionActivo);

        if (res.FugaActivaDetectada)
        {
            txtEstadoRed.text = $"ALERTA: Fuga abierta (-{res.PerdidaPorFuga:0.#} L/s). Cierra rotura en campo.";
            txtEstadoRed.color = RojoAlerta;
        }
        else if (res.TodosAbastecidos)
        {
            txtEstadoRed.text = gestor.RedEquilibradaConfirmada
                ? "CONFIRMACIÓN OK: Dirígete a la plataforma de salida."
                : "5/5 sectores cubiertos. Pulsa Confirmar Presurización [P].";
            txtEstadoRed.color = VerdeOk;
        }
        else
        {
            txtEstadoRed.text = "Déficit en sectores altos: revisa válvulas o simula.";
            txtEstadoRed.color = Ambar;
        }

        bool permitirBotones = gestor.ModoSimulacionActivo || gestor.permitirRegulacionRemotaEnHUD;
        for (int i = 0; i < res.Sectores.Count && i < filas.Count; i++)
        {
            var s = res.Sectores[i];
            var f = filas[i];
            f.AristaId = SectorAArista.TryGetValue(s.Id, out var aId) ? aId : "";

            float ap = !string.IsNullOrEmpty(f.AristaId) ? gestor.ObtenerAperturaMostrada(f.AristaId) : 0f;
            f.Titulo.text = $"{s.Nombre} ({Mathf.RoundToInt(ap * 100f)}%)";
            f.Caudal.text = $"{s.CaudalActual:0.0} / Min {s.DemandaMinima:0}";
            f.Caudal.color = s.Abastecido ? VerdeOk : RojoAlerta;

            float ratio = s.DemandaMinima > 0f ? Mathf.Clamp01(s.CaudalActual / (s.DemandaMinima * 1.5f)) : 0f;
            f.Barra.sizeDelta = new Vector2(370f * ratio, 20f);
            f.BarraImg.color = s.Abastecido ? Cian : RojoAlerta;

            f.BtnMenos.interactable = permitirBotones && !string.IsNullOrEmpty(f.AristaId);
            f.BtnMas.interactable = permitirBotones && !string.IsNullOrEmpty(f.AristaId);
        }
    }

    public void ActualizarAvisoInteraccion(string texto)
    {
        if (txtAvisoAccion != null) txtAvisoAccion.text = texto;
    }

    public void MostrarAlertaTemporal(string texto, float duracion)
    {
        if (txtMensaje == null) return;
        txtMensaje.text = texto;
        txtMensaje.color = Color.white;
        mensajeHasta = Time.time + duracion;
    }

    void MostrarResultado(string texto)
    {
        panelResultado.SetActive(true);
        txtResultado.text = texto;
        txtMensaje.text = "";
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

    Text Texto(Transform padre, string txt, Vector2 pos, Vector2 tam, int tamFuente, Color color, TextAnchor al, FontStyle estilo)
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
        t.text = txt;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    Button Boton(Transform padre, string etiqueta, Vector2 pos, Vector2 tam, Color fondo, Color textoColor, System.Action accion)
    {
        var rt = Caja(padre, "Btn_" + etiqueta, new Vector2(0f, 1f), pos, tam, fondo, new Vector2(0f, 1f));
        var btn = rt.gameObject.AddComponent<Button>();
        btn.onClick.AddListener(() => accion?.Invoke());
        var t = Texto(rt, etiqueta, Vector2.zero, tam, 20, textoColor, TextAnchor.MiddleCenter, FontStyle.Bold);
        t.rectTransform.anchorMin = Vector2.zero;
        t.rectTransform.anchorMax = Vector2.one;
        t.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        t.rectTransform.offsetMin = Vector2.zero;
        t.rectTransform.offsetMax = Vector2.zero;
        return btn;
    }
}
