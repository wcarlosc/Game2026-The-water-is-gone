using UnityEngine;
using UnityEngine.UI;

// HUD de los túneles: energía, puntaje, choques, objetivo y tarjeta de resultado.
// Se construye solo al iniciar; basta con tenerlo en la escena.
public class HUDTunel : MonoBehaviour
{
    static readonly Color Ambar = new Color(1f, 0.62f, 0.1f);

    RectTransform barraRelleno;
    Image barraImagen;
    Text txtEnergia, txtPuntaje, txtChoques, txtMensaje, txtResultado;
    GameObject panelResultado;
    float mensajeHasta;
    Font fuente;
    LevelManager lm;

    void Start()
    {
        lm = LevelManager.Instancia;
        fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Construir();
        if (lm != null)
        {
            lm.Mensaje += MostrarMensaje;
            lm.Resultado += MostrarResultado;
            MostrarMensaje(lm.objetivo, 4f);
        }
    }

    void OnDestroy()
    {
        if (lm != null)
        {
            lm.Mensaje -= MostrarMensaje;
            lm.Resultado -= MostrarResultado;
        }
    }

    void Construir()
    {
        var canvasGo = new GameObject("HUD_Canvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var panel = Caja(canvasGo.transform, "Panel", new Vector2(0, 1), new Vector2(30, -30), new Vector2(460, 150), new Color(0.05f, 0.08f, 0.1f, 0.6f));
        Texto(panel, "Energía", new Vector2(20, -14), new Vector2(200, 34), 26, Color.white, TextAnchor.MiddleLeft);
        txtEnergia = Texto(panel, "10.0", new Vector2(330, -14), new Vector2(110, 34), 26, Ambar, TextAnchor.MiddleRight);

        var fondo = Caja(panel, "BarraFondo", new Vector2(0, 1), new Vector2(20, -54), new Vector2(420, 22), new Color(0f, 0f, 0f, 0.5f));
        barraRelleno = Caja(fondo, "BarraRelleno", new Vector2(0, 1), Vector2.zero, new Vector2(420, 22), Ambar);
        barraImagen = barraRelleno.GetComponent<Image>();

        txtPuntaje = Texto(panel, "Puntaje 100", new Vector2(20, -92), new Vector2(220, 40), 28, Color.white, TextAnchor.MiddleLeft);
        txtChoques = Texto(panel, "Choques 0/5", new Vector2(240, -92), new Vector2(200, 40), 28, Color.white, TextAnchor.MiddleRight);

        txtMensaje = Texto(canvasGo.transform, "", Vector2.zero, new Vector2(1400, 160), 40, Color.white, TextAnchor.MiddleCenter);
        var rtM = txtMensaje.rectTransform;
        rtM.anchorMin = rtM.anchorMax = new Vector2(0.5f, 0.8f);
        rtM.pivot = new Vector2(0.5f, 0.5f);
        rtM.anchoredPosition = Vector2.zero;
        var sombra = txtMensaje.gameObject.AddComponent<Outline>();
        sombra.effectColor = new Color(0, 0, 0, 0.8f);
        sombra.effectDistance = new Vector2(2, -2);

        var res = Caja(canvasGo.transform, "Resultado", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720, 420), new Color(0.05f, 0.08f, 0.1f, 0.88f));
        res.pivot = new Vector2(0.5f, 0.5f);
        res.anchoredPosition = Vector2.zero;
        panelResultado = res.gameObject;
        txtResultado = Texto(res, "", Vector2.zero, new Vector2(680, 400), 36, Color.white, TextAnchor.MiddleCenter);
        var rtR = txtResultado.rectTransform;
        rtR.anchorMin = rtR.anchorMax = rtR.pivot = new Vector2(0.5f, 0.5f);
        rtR.anchoredPosition = Vector2.zero;
        panelResultado.SetActive(false);
    }

    RectTransform Caja(Transform padre, string nombre, Vector2 ancla, Vector2 pos, Vector2 tam, Color color)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(padre, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = ancla;
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
        go.GetComponent<Image>().color = color;
        return rt;
    }

    Text Texto(Transform padre, string txt, Vector2 pos, Vector2 tam, int tamFuente, Color color, TextAnchor al)
    {
        var go = new GameObject("Texto", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(padre, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
        var t = go.GetComponent<Text>();
        t.font = fuente;
        t.fontSize = tamFuente;
        t.color = color;
        t.alignment = al;
        t.text = txt;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    void Update()
    {
        if (lm == null) return;
        float frac = lm.EnergiaMaxima > 0f ? Mathf.Clamp01(lm.Energia / lm.EnergiaMaxima) : 0f;
        barraRelleno.sizeDelta = new Vector2(420f * frac, 22f);
        bool poco = lm.Energia <= 3f;
        barraImagen.color = poco ? Color.Lerp(Ambar, Color.red, Mathf.PingPong(Time.time * 3f, 1f)) : Ambar;
        txtEnergia.text = lm.Energia.ToString("0.0");
        txtPuntaje.text = "Puntaje " + lm.Puntaje;
        txtChoques.text = "Choques " + lm.Choques + "/" + lm.choquesParaReiniciar;
        txtChoques.color = lm.Choques >= lm.choquesParaReiniciar - 1 ? new Color(1f, 0.4f, 0.3f) : Color.white;

        if (txtMensaje.text.Length > 0)
        {
            float resto = mensajeHasta - Time.time;
            var c = txtMensaje.color;
            c.a = Mathf.Clamp01(resto / 0.6f);
            txtMensaje.color = c;
            if (resto <= 0f) txtMensaje.text = "";
        }
    }

    void MostrarMensaje(string texto, float duracion)
    {
        txtMensaje.text = texto;
        txtMensaje.color = Color.white;
        mensajeHasta = Time.time + duracion;
    }

    void MostrarResultado(string texto)
    {
        panelResultado.SetActive(true);
        txtResultado.text = texto;
        txtResultado.color = Color.white;
        txtMensaje.text = "";
    }
}
