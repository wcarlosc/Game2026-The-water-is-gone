using System.Collections.Generic;
using HastaElUltimoBarrio.Dominio;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// HUD para los niveles en superficie 3D: bateria SCADA, puntaje %, fallos X/5,
// panel de los 5 sectores con Indice de Jain, tarjetas de impacto social y simulador "¿Que pasaria si...?".
public class HUDRed3D : MonoBehaviour
{
    public static HUDRed3D Instancia { get; private set; }

    [Header("Opciones de Nivel")]
    public bool mostrarTarjetasImpacto = false;

    static readonly Color PanelOscuro = new Color(0.05f, 0.08f, 0.11f, 0.78f);
    static readonly Color TarjetaFondo = new Color(0.08f, 0.12f, 0.17f, 0.90f);
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

    class TarjetaImpactoUI
    {
        public Outline Borde;
        public Text Texto;
    }

    static readonly Dictionary<string, string> SectorAArista = new Dictionary<string, string>
    {
        { "S1_Mercado", "V1_Mercado" },
        { "S2_Puente", "V2_Puente" },
        { "S3_Salud", "V3_Salud" },
        { "S4_Refugio", "V4_Refugio" },
        { "S5_Ladera", "V5_Ladera" }
    };

    static readonly Dictionary<string, string> ImpactoRestablecidoPorSector = new Dictionary<string, string>
    {
        { "S1_Mercado", "Lavado higiénico de alimentos restablecido." },
        { "S2_Puente", "Presión domiciliaria continua en viviendas." },
        { "S3_Salud", "Esterilización e instrumental operativos." },
        { "S4_Refugio", "Comedor comunitario con reserva activa." },
        { "S5_Ladera", "Agua en pilones altos sin acarreo en baldes." }
    };

    Font fuente;
    LevelManager lm;
    GestorRedSuperficie gestor;

    RectTransform barraBateria;
    Image barraBateriaImg;
    Text txtBateria, txtPuntaje, txtChoques, txtJain, txtEstadoRed, txtAvisoAccion, txtMensaje, txtResultado;
    Image panelDerechoImg;
    Outline bordeSimulacion;
    Button btnSimular, btnAplicarSim, btnRestaurar, btnConfirmar;
    Text txtBtnSimular;
    GameObject bannerMision;
    Image bannerMisionImg;
    GameObject panelResultado;
    readonly List<FilaSectorUI> filas = new List<FilaSectorUI>();
    readonly List<TarjetaImpactoUI> tarjetasImpacto = new List<TarjetaImpactoUI>();

    float mensajeHasta;
    int ultDecimas = -1, ultPuntaje = -1, ultChoques = -1, ultMaxChoques = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ReiniciarEstatico()
    {
        Instancia = null;
    }

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
            fila.BtnMenos = Boton(pDer, "-", new Vector2(402f, y - 30f), new Vector2(48f, 30f), new Color(0.18f, 0.24f, 0.30f), Color.white, () => AjustarFila(idx, -0.05f));
            fila.BtnMas = Boton(pDer, "+", new Vector2(458f, y - 30f), new Vector2(48f, 30f), new Color(0.18f, 0.24f, 0.30f), Color.white, () => AjustarFila(idx, 0.05f));
            filas.Add(fila);
        }

        btnSimular = Boton(pDer, "¿Qué pasaría si…? [Q]", new Vector2(20f, -544f), new Vector2(310f, 48f),
            new Color(0.15f, 0.36f, 0.44f), Color.white, () => gestor?.AlternarModoSimulacion());
        txtBtnSimular = btnSimular.GetComponentInChildren<Text>();

        btnAplicarSim = Boton(pDer, "Aplicar a Red", new Vector2(266f, -544f), new Vector2(128f, 48f),
            new Color(0.16f, 0.56f, 0.42f), Color.white, () => gestor?.AplicarSimulacionARealSiPermitido());
        btnAplicarSim.gameObject.SetActive(false);

        btnRestaurar = Boton(pDer, "Vista Real", new Vector2(342f, -544f), new Vector2(168f, 48f),
            new Color(0.26f, 0.26f, 0.30f), Color.white, () => gestor?.RestaurarVistaReal());

        btnConfirmar = Boton(pDer, "Confirmar Presurización [P]", new Vector2(20f, -606f), new Vector2(490f, 54f),
            Ambar, Color.black, () => gestor?.ConfirmarPresurizacion());

        // 2b. Tarjetas de impacto social (solo en LVL_09, bajo PanelEstado en margen izquierdo libre)
        if (mostrarTarjetasImpacto)
        {
            var pImp = Caja(canvasGo.transform, "PanelImpactoSocial", new Vector2(0f, 1f), new Vector2(26f, -194f),
                new Vector2(460f, 446f), PanelOscuro, new Vector2(0f, 1f));
            Texto(pImp, "IMPACTO SOCIAL EN BARRIOS", new Vector2(18f, -12f), new Vector2(424f, 28f), 20, Cian, TextAnchor.MiddleLeft, FontStyle.Bold);

            for (int i = 0; i < 5; i++)
            {
                float y = -48f - i * 78f;
                var card = Caja(pImp, "Impacto_" + i, new Vector2(0f, 1f), new Vector2(18f, y),
                    new Vector2(424f, 70f), TarjetaFondo, new Vector2(0f, 1f));
                var borde = card.gameObject.AddComponent<Outline>();
                borde.effectColor = RojoAlerta;
                borde.effectDistance = new Vector2(1.5f, -1.5f);

                var txtCard = Texto(card, "", new Vector2(12f, -6f), new Vector2(400f, 58f), 17, Color.white, TextAnchor.MiddleLeft, FontStyle.Normal);
                tarjetasImpacto.Add(new TarjetaImpactoUI { Borde = borde, Texto = txtCard });
            }
        }

        // 3. Banner de mision centrado arriba (entre PanelEstado y PanelSCADA) y aviso inferior de interaccion
        var rtBanner = Caja(canvasGo.transform, "BannerMision", new Vector2(0.482f, 1f), new Vector2(0f, -26f),
            new Vector2(780f, 92f), PanelOscuro, new Vector2(0.5f, 1f));
        bannerMision = rtBanner.gameObject;
        bannerMisionImg = rtBanner.GetComponent<Image>();
        var bordeBanner = bannerMision.AddComponent<Outline>();
        bordeBanner.effectColor = Cian;
        bordeBanner.effectDistance = new Vector2(1.5f, -1.5f);

        txtMensaje = Texto(rtBanner, "", Vector2.zero, new Vector2(740f, 78f), 25, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
        var rtMsg = txtMensaje.rectTransform;
        rtMsg.anchorMin = rtMsg.anchorMax = rtMsg.pivot = new Vector2(0.5f, 0.5f);
        rtMsg.anchoredPosition = Vector2.zero;
        txtMensaje.gameObject.AddComponent<Outline>().effectColor = Color.black;
        bannerMision.SetActive(false);

        txtAvisoAccion = Texto(canvasGo.transform, "", Vector2.zero, new Vector2(760f, 52f), 26, Ambar, TextAnchor.MiddleCenter, FontStyle.Bold);
        var rtAv = txtAvisoAccion.rectTransform;
        rtAv.anchorMin = rtAv.anchorMax = new Vector2(0.482f, 0f);
        rtAv.pivot = new Vector2(0.5f, 0f);
        rtAv.anchoredPosition = new Vector2(0f, 28f);
        txtAvisoAccion.gameObject.AddComponent<Outline>().effectColor = Color.black;

        // 4. Panel resultado fin de nivel (incluye comparativa Jugador vs Optimo de Jain)
        var res = Caja(canvasGo.transform, "Resultado", new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(860f, 500f), new Color(0.05f, 0.08f, 0.1f, 0.92f), new Vector2(0.5f, 0.5f));
        panelResultado = res.gameObject;
        var bordeRes = panelResultado.AddComponent<Outline>();
        bordeRes.effectColor = Cian;
        bordeRes.effectDistance = new Vector2(2f, -2f);

        txtResultado = Texto(res, "", Vector2.zero, new Vector2(800f, 380f), 25, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
        var rtR = txtResultado.rectTransform;
        rtR.anchorMin = rtR.anchorMax = rtR.pivot = new Vector2(0.5f, 0.5f);
        rtR.anchoredPosition = new Vector2(0f, 30f);

        var btnMenu = Boton(res, "Volver al Menú Principal", Vector2.zero, new Vector2(320f, 48f),
            Ambar, Color.black, VolverMenuPrincipal);
        var rtBtnMenu = (RectTransform)btnMenu.transform;
        rtBtnMenu.anchorMin = rtBtnMenu.anchorMax = new Vector2(0.5f, 0f);
        rtBtnMenu.pivot = new Vector2(0.5f, 0f);
        rtBtnMenu.anchoredPosition = new Vector2(0f, 22f);

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
            float alfa = Mathf.Clamp01(resto / 0.6f);
            var c = txtMensaje.color;
            c.a = alfa;
            txtMensaje.color = c;
            if (bannerMisionImg != null)
            {
                var cb = PanelOscuro;
                cb.a = PanelOscuro.a * alfa;
                bannerMisionImg.color = cb;
            }
            if (resto <= 0f)
            {
                txtMensaje.text = "";
                if (bannerMision != null) bannerMision.SetActive(false);
            }
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

        bool puedeAplicar = gestor.ModoSimulacionActivo && gestor.permitirRegulacionRemotaEnHUD;
        if (btnAplicarSim != null) btnAplicarSim.gameObject.SetActive(puedeAplicar);
        btnRestaurar.gameObject.SetActive(gestor.ModoSimulacionActivo);

        var rtSim = (RectTransform)btnSimular.transform;
        var rtRest = (RectTransform)btnRestaurar.transform;
        if (puedeAplicar)
        {
            rtSim.sizeDelta = new Vector2(236f, 48f);
            rtRest.anchoredPosition = new Vector2(402f, -544f);
            rtRest.sizeDelta = new Vector2(108f, 48f);
        }
        else
        {
            rtSim.sizeDelta = new Vector2(310f, 48f);
            rtRest.anchoredPosition = new Vector2(342f, -544f);
            rtRest.sizeDelta = new Vector2(168f, 48f);
        }

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

            if (i < tarjetasImpacto.Count)
            {
                var card = tarjetasImpacto[i];
                bool ok = s.Abastecido && !res.FugaActivaDetectada;
                card.Borde.effectColor = ok ? VerdeOk : RojoAlerta;
                string msgOk = ImpactoRestablecidoPorSector.TryGetValue(s.Id, out var txtOk)
                    ? txtOk
                    : "Caudal mínimo vital cubierto.";
                card.Texto.text = ok
                    ? $"{s.Nombre}: {msgOk}"
                    : $"{s.Nombre}: «{s.ImpactoSocialSinAgua}»";
                card.Texto.color = ok ? new Color(0.84f, 0.98f, 0.88f) : new Color(1f, 0.86f, 0.84f);
            }
        }
    }

    public void ActualizarAvisoInteraccion(string texto)
    {
        if (txtAvisoAccion != null) txtAvisoAccion.text = texto;
    }

    public void MostrarAlertaTemporal(string texto, float duracion)
    {
        if (txtMensaje == null) return;
        bool hayTexto = !string.IsNullOrEmpty(texto);
        if (bannerMision != null) bannerMision.SetActive(hayTexto);
        if (bannerMisionImg != null) bannerMisionImg.color = PanelOscuro;
        txtMensaje.text = texto;
        txtMensaje.color = Color.white;
        mensajeHasta = Time.time + duracion;
    }

    void MostrarResultado(string texto)
    {
        panelResultado.SetActive(true);
        string completo = texto;
        if (gestor != null && gestor.ResultadoReal != null)
        {
            string comp = gestor.ObtenerResumenComparativoJain();
            if (!string.IsNullOrEmpty(comp))
                completo += "\n\n" + comp;
            if (gestor.modoEscenario == ModoRedSuperficie.Nivel09_PlazaControlEquidad && gestor.RedEquilibradaConfirmada)
                completo += "\n\n¡Lavadero comunitario activo! Agua equitativa hasta el último barrio.";
        }
        txtResultado.text = completo;
        txtMensaje.text = "";
        if (bannerMision != null) bannerMision.SetActive(false);
    }

    void VolverMenuPrincipal()
    {
        if (Application.CanStreamedLevelBeLoaded("MenuPrincipal"))
            SceneManager.LoadScene("MenuPrincipal");
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
