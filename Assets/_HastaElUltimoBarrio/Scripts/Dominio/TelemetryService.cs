using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace HastaElUltimoBarrio.Dominio
{
    [Serializable]
    public class PuntoChoqueDto
    {
        public float x;
        public float y;
        public float z;
        public float tiempoSeg;
    }

    [Serializable]
    public class RegistroNivelDto
    {
        public string escena;
        public float tiempoSeg;
        public float energiaInicial;
        public float energiaSobrante;
        public int puntajeFinal;
        public int choques;
        public bool completado;
        public string motivoFin;
        public int consultasSimulador;
        public int intentosPresurizacion;
        public float indiceJainJugador;
        public float indiceJainOptimo;
        public bool equidadLograda;
        public List<float> aperturasConfirmadas = new List<float>();
        public List<PuntoChoqueDto> puntosChoque = new List<PuntoChoqueDto>();
    }

    [Serializable]
    public class SesionTelemetriaDto
    {
        public string sessionId;
        public string fechaIsoUtc;
        public List<RegistroNivelDto> niveles = new List<RegistroNivelDto>();
    }

    // Capa de datos en JSON para registrar choques (mapa de calor), consumo de energia,
    // uso del simulador predictivo y metricas de equidad de Jain.
    public class TelemetryService
    {
        readonly bool guardarAutoEnDisco;
        readonly string rutaArchivo;
        SesionTelemetriaDto sesionActual;
        RegistroNivelDto nivelEnCurso;

        public SesionTelemetriaDto SesionActual => sesionActual;
        public RegistroNivelDto NivelEnCurso => nivelEnCurso;
        public string RutaArchivo => rutaArchivo;

        public TelemetryService(bool guardarAutoEnDisco = true, string rutaPersonalizada = null)
        {
            this.guardarAutoEnDisco = guardarAutoEnDisco;
            rutaArchivo = string.IsNullOrEmpty(rutaPersonalizada)
                ? Path.Combine(Application.persistentDataPath, "telemetria_sesiones.json")
                : rutaPersonalizada;
            IniciarSesion();
        }

        public void IniciarSesion(string sessionId = null)
        {
            sesionActual = new SesionTelemetriaDto
            {
                sessionId = string.IsNullOrEmpty(sessionId) ? Guid.NewGuid().ToString("N").Substring(0, 8) : sessionId,
                fechaIsoUtc = DateTime.UtcNow.ToString("o")
            };
            nivelEnCurso = null;
        }

        public void IniciarNivel(string escena, float energiaInicial)
        {
            if (sesionActual == null) IniciarSesion();
            nivelEnCurso = new RegistroNivelDto
            {
                escena = escena ?? string.Empty,
                energiaInicial = energiaInicial
            };
            sesionActual.niveles.Add(nivelEnCurso);
        }

        public void RegistrarChoque(Vector3 posicion, float tiempoSeg)
        {
            if (nivelEnCurso == null) return;
            nivelEnCurso.choques++;
            nivelEnCurso.puntosChoque.Add(new PuntoChoqueDto
            {
                x = posicion.x,
                y = posicion.y,
                z = posicion.z,
                tiempoSeg = Mathf.Max(0f, tiempoSeg)
            });
        }

        public void RegistrarConsultaSimulador()
        {
            if (nivelEnCurso == null) return;
            nivelEnCurso.consultasSimulador++;
        }

        public void RegistrarResultadoHidraulico(
            int intentos,
            float jainJugador,
            float jainOptimo,
            bool equidadLograda,
            IEnumerable<float> aperturas)
        {
            if (nivelEnCurso == null) return;
            nivelEnCurso.intentosPresurizacion = Mathf.Max(0, intentos);
            nivelEnCurso.indiceJainJugador = Mathf.Clamp01(jainJugador);
            nivelEnCurso.indiceJainOptimo = Mathf.Clamp01(jainOptimo);
            nivelEnCurso.equidadLograda = equidadLograda;
            nivelEnCurso.aperturasConfirmadas.Clear();
            if (aperturas != null)
            {
                foreach (float a in aperturas)
                    nivelEnCurso.aperturasConfirmadas.Add(Mathf.Clamp01(a));
            }
        }

        public void FinalizarNivel(
            bool completado,
            float tiempoSeg,
            float energiaSobrante,
            int puntajeFinal,
            int choques,
            string motivo = "")
        {
            if (nivelEnCurso == null) return;
            nivelEnCurso.completado = completado;
            nivelEnCurso.tiempoSeg = Mathf.Max(0f, tiempoSeg);
            nivelEnCurso.energiaSobrante = Mathf.Max(0f, energiaSobrante);
            nivelEnCurso.puntajeFinal = Mathf.Max(0, puntajeFinal);
            nivelEnCurso.choques = Mathf.Max(nivelEnCurso.choques, choques);
            nivelEnCurso.motivoFin = motivo ?? string.Empty;

            if (guardarAutoEnDisco)
                GuardarEnArchivo();
        }

        public string ExportarJson(bool prettyPrint = true)
        {
            if (sesionActual == null) IniciarSesion();
            return JsonUtility.ToJson(sesionActual, prettyPrint);
        }

        public string GuardarEnArchivo(string rutaExplicita = null)
        {
            string destino = string.IsNullOrEmpty(rutaExplicita) ? rutaArchivo : rutaExplicita;
            string dir = Path.GetDirectoryName(destino);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(destino, ExportarJson(true));
            return destino;
        }

        public static SesionTelemetriaDto DesdeJson(string json)
        {
            return string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<SesionTelemetriaDto>(json);
        }
    }
}
