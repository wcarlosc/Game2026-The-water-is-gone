using System;
using System.Collections.Generic;
using HastaElUltimoBarrio.Dominio;
using UnityEngine;

public enum ModoRedSuperficie
{
    Nivel07_AislamientoFuga,
    Nivel08_DepositoRegulacion,
    Nivel09_PlazaControlEquidad
}

// Orquesta el grafo hidraulico, la optimizacion de Jain, el simulador predictivo
// "¿Que pasaria si...?" y las visualizaciones 3D en los niveles de superficie.
public class GestorRedSuperficie : MonoBehaviour
{
    public static GestorRedSuperficie Instancia { get; private set; }

    [Header("Configuracion Hidraulica")]
    public ModoRedSuperficie modoEscenario = ModoRedSuperficie.Nivel07_AislamientoFuga;
    public float caudalFuente = 100f;
    public bool permitirRegulacionRemotaEnHUD = false;

    [Header("Escena")]
    public NodoValvula3D[] valvulasEscena;
    public Renderer indicadorZonaSalida;
    public Material matSalidaPendiente;
    public Material matSalidaLista;

    public GrafoHidraulico Grafo { get; private set; }
    public ResultadoHidraulico ResultadoReal { get; private set; }
    public ResultadoHidraulico ResultadoMostrado { get; private set; }
    public SolucionOptimaHidraulica SolucionOptima { get; private set; }
    public bool ModoSimulacionActivo { get; private set; }
    public bool RedEquilibradaConfirmada { get; private set; }
    public int IntentosPresurizacion { get; private set; }

    public event Action EstadoActualizado;

    readonly Dictionary<string, float> aperturasSimuladas = new Dictionary<string, float>();
    readonly Dictionary<string, NodoValvula3D> mapaValvulas = new Dictionary<string, NodoValvula3D>();

    void Awake()
    {
        Instancia = this;
        ConstruirGrafoInicial();
    }

    void Start()
    {
        if (valvulasEscena == null || valvulasEscena.Length == 0)
            valvulasEscena = FindObjectsByType<NodoValvula3D>(FindObjectsSortMode.None);

        foreach (var v in valvulasEscena)
        {
            if (v != null && !string.IsNullOrEmpty(v.aristaId))
                mapaValvulas[v.aristaId] = v;
        }

        EvaluarYNotificar(real: true);
    }

    void ConstruirGrafoInicial()
    {
        bool conFuga = modoEscenario == ModoRedSuperficie.Nivel07_AislamientoFuga
                    || modoEscenario == ModoRedSuperficie.Nivel09_PlazaControlEquidad;
        Grafo = GrafoHidraulico.CrearRedArequipaCincoSectores(incluirRamalFugaAislable: conFuga);

        if (modoEscenario == ModoRedSuperficie.Nivel07_AislamientoFuga)
        {
            // Ramales de barrios pre-calibrados; la rotura inicia abierta (1.0) y Salud cerrado (0.0).
            Grafo.FijarApertura("V1_Mercado", 0.30f);
            Grafo.FijarApertura("V2_Puente", 0.40f);
            Grafo.FijarApertura("V3_Salud", 0.00f);
            Grafo.FijarApertura("V4_Refugio", 0.60f);
            Grafo.FijarApertura("V5_Ladera", 1.00f);
            Grafo.FijarApertura("V_AislamientoFuga", 1.00f);
        }
        else if (modoEscenario == ModoRedSuperficie.Nivel08_DepositoRegulacion)
        {
            Grafo.FijarApertura("V1_Mercado", 1.00f);
            Grafo.FijarApertura("V2_Puente", 0.40f);
            Grafo.FijarApertura("V3_Salud", 0.65f);
            Grafo.FijarApertura("V4_Refugio", 0.60f);
            Grafo.FijarApertura("V5_Ladera", 1.00f);
        }
        else
        {
            Grafo.FijarApertura("V1_Mercado", 1.00f);
            Grafo.FijarApertura("V2_Puente", 1.00f);
            Grafo.FijarApertura("V3_Salud", 1.00f);
            Grafo.FijarApertura("V4_Refugio", 1.00f);
            Grafo.FijarApertura("V5_Ladera", 1.00f);
            if (Grafo.ObtenerArista("V_AislamientoFuga") != null)
                Grafo.FijarApertura("V_AislamientoFuga", 1.00f);
        }

        SolucionOptima = OptimizadorEquidadJain.CalcularOptimo(Grafo, caudalFuente, 0.05f);
        SincronizarDiccionarioSimulacion();
        ActualizarIndicadorSalida(false);
    }

    void SincronizarDiccionarioSimulacion()
    {
        aperturasSimuladas.Clear();
        foreach (var kv in Grafo.CapturarAperturas())
            aperturasSimuladas[kv.Key] = kv.Value;
    }

    public void CambiarValvulaCampo(NodoValvula3D nodo)
    {
        if (nodo == null || Grafo == null) return;
        var arista = Grafo.ObtenerArista(nodo.aristaId);
        if (arista == null) return;

        float nuevaApertura = nodo.CalcularSiguienteApertura(arista.Apertura);
        Grafo.FijarApertura(nodo.aristaId, nuevaApertura);
        RedEquilibradaConfirmada = false;
        ActualizarIndicadorSalida(false);

        if (!ModoSimulacionActivo)
            SincronizarDiccionarioSimulacion();

        AudioManager.Instancia.SonarAgua();
        EvaluarYNotificar(!ModoSimulacionActivo);
    }

    public void AjustarValvulaDesdeHUD(string aristaId, float delta)
    {
        if (Grafo == null) return;
        var arista = Grafo.ObtenerArista(aristaId);
        if (arista == null || !arista.TieneValvula) return;

        if (ModoSimulacionActivo)
        {
            float baseVal = aperturasSimuladas.TryGetValue(aristaId, out float v) ? v : arista.Apertura;
            aperturasSimuladas[aristaId] = Mathf.Clamp01(Mathf.Round((baseVal + delta) * 20f) / 20f);
            GameManager.Instancia.Telemetria.RegistrarConsultaSimulador();
            EvaluarYNotificar(false);
            return;
        }

        if (!permitirRegulacionRemotaEnHUD) return;
        Grafo.FijarApertura(aristaId, Mathf.Clamp01(Mathf.Round((arista.Apertura + delta) * 20f) / 20f));
        RedEquilibradaConfirmada = false;
        ActualizarIndicadorSalida(false);
        SincronizarDiccionarioSimulacion();
        EvaluarYNotificar(true);
    }

    public void AlternarModoSimulacion()
    {
        ModoSimulacionActivo = !ModoSimulacionActivo;
        if (ModoSimulacionActivo)
        {
            SincronizarDiccionarioSimulacion();
            GameManager.Instancia.Telemetria.RegistrarConsultaSimulador();
        }
        EvaluarYNotificar(!ModoSimulacionActivo);
    }

    public void RestaurarVistaReal()
    {
        ModoSimulacionActivo = false;
        SincronizarDiccionarioSimulacion();
        EvaluarYNotificar(true);
    }

    public void AplicarSimulacionARealSiPermitido()
    {
        if (!permitirRegulacionRemotaEnHUD) return;
        Grafo.RestaurarAperturas(aperturasSimuladas);
        ModoSimulacionActivo = false;
        RedEquilibradaConfirmada = false;
        ActualizarIndicadorSalida(false);
        EvaluarYNotificar(true);
    }

    public void ConfirmarPresurizacion()
    {
        var lm = LevelManager.Instancia;
        if (lm != null && lm.Terminado) return;

        IntentosPresurizacion++;
        if (ModoSimulacionActivo)
            RestaurarVistaReal();
        else
            EvaluarYNotificar(true);

        var res = ResultadoReal;
        if (res.FugaActivaDetectada || !res.TodosAbastecidos)
        {
            AudioManager.Instancia.SonarChoque();
            Vector3 pos = Vector3.zero;
            var rover = FindAnyObjectByType<RoverController>();
            if (rover != null) pos = rover.transform.position;

            string detalle = res.FugaActivaDetectada
                ? "Fuga activa en calle: cierra la válvula de rotura antes de presurizar."
                : ObtenerPrimerImpactoSocial(res);

            if (lm != null) lm.RegistrarChoque(pos);
            if (HUDRed3D.Instancia != null) HUDRed3D.Instancia.MostrarAlertaTemporal("Golpe de ariete: " + detalle, 3.5f);
            return;
        }

        RedEquilibradaConfirmada = true;
        AudioManager.Instancia.SonarAgua();
        ActualizarIndicadorSalida(true);

        var valoresApertura = new List<float>();
        foreach (var a in Grafo.Aristas)
        {
            if (a.TieneValvula) valoresApertura.Add(a.Apertura);
        }

        float jainOpt = SolucionOptima != null && SolucionOptima.ResultadoOptimo != null
            ? SolucionOptima.ResultadoOptimo.IndiceEquidadJain
            : 1f;

        GameManager.Instancia.Telemetria.RegistrarResultadoHidraulico(
            IntentosPresurizacion,
            res.IndiceEquidadJain,
            jainOpt,
            true,
            valoresApertura);

        if (HUDRed3D.Instancia != null)
            HUDRed3D.Instancia.MostrarAlertaTemporal("¡Red equilibrada! Lleva el Rover a la plataforma de salida.", 4.5f);

        EstadoActualizado?.Invoke();
    }

    string ObtenerPrimerImpactoSocial(ResultadoHidraulico res)
    {
        foreach (var s in res.Sectores)
        {
            if (!s.Abastecido && !string.IsNullOrEmpty(s.ImpactoSocialSinAgua))
                return $"{s.Nombre}: {s.ImpactoSocialSinAgua}";
        }
        return "Al menos un sector permanece bajo su caudal mínimo vital.";
    }

    void EvaluarYNotificar(bool real)
    {
        ResultadoReal = Grafo.Evaluar(caudalFuente);
        ResultadoMostrado = real
            ? ResultadoReal
            : Grafo.SimularQuePasariaSi(caudalFuente, aperturasSimuladas);

        var activas = new HashSet<string>(ResultadoReal.AristasActivasIds);
        foreach (var a in Grafo.Aristas)
        {
            if (mapaValvulas.TryGetValue(a.Id, out var nodo))
                nodo.AplicarEstadoVisual(a.Apertura, activas.Contains(a.Id), a.Rota);
        }

        EstadoActualizado?.Invoke();
    }

    void ActualizarIndicadorSalida(bool lista)
    {
        if (indicadorZonaSalida == null) return;
        Material target = lista ? matSalidaLista : matSalidaPendiente;
        if (target != null) indicadorZonaSalida.sharedMaterial = target;
    }

    public float ObtenerAperturaMostrada(string aristaId)
    {
        if (ModoSimulacionActivo && aperturasSimuladas.TryGetValue(aristaId, out float sim))
            return sim;
        var a = Grafo != null ? Grafo.ObtenerArista(aristaId) : null;
        return a != null ? a.Apertura : 0f;
    }
}
