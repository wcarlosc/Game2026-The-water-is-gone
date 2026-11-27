using System;
using System.Collections.Generic;
using UnityEngine;

namespace HastaElUltimoBarrio.Dominio
{
    public enum TipoNodoHidraulico
    {
        Fuente,
        Camara,
        SectorServicio
    }

    [Serializable]
    public class NodoHidraulico
    {
        public string Id;
        public string Nombre;
        public TipoNodoHidraulico Tipo;
        public float AltitudMetros;
        public float DemandaMinima;
        public string ImpactoSocialSinAgua;

        public NodoHidraulico(
            string id,
            string nombre,
            TipoNodoHidraulico tipo,
            float altitudMetros = 0f,
            float demandaMinima = 0f,
            string impactoSocialSinAgua = "")
        {
            Id = id;
            Nombre = nombre;
            Tipo = tipo;
            AltitudMetros = altitudMetros;
            DemandaMinima = Mathf.Max(0f, demandaMinima);
            ImpactoSocialSinAgua = impactoSocialSinAgua ?? string.Empty;
        }
    }

    [Serializable]
    public class AristaHidraulica
    {
        public string Id;
        public string OrigenId;
        public string DestinoId;
        public float ResistenciaBase;
        public bool TieneValvula;
        public float Apertura;
        public bool Rota;

        public AristaHidraulica(
            string id,
            string origenId,
            string destinoId,
            float resistenciaBase,
            bool tieneValvula = true,
            float aperturaInicial = 1f,
            bool rota = false)
        {
            Id = id;
            OrigenId = origenId;
            DestinoId = destinoId;
            ResistenciaBase = Mathf.Max(0.05f, resistenciaBase);
            TieneValvula = tieneValvula;
            Apertura = tieneValvula ? Mathf.Clamp01(aperturaInicial) : 1f;
            Rota = rota;
        }
    }

    [Serializable]
    public class EstadoSector
    {
        public string Id;
        public string Nombre;
        public float DemandaMinima;
        public float CaudalActual;
        public float CoberturaRelativa;
        public bool Conectado;
        public bool Abastecido;
        public string ImpactoSocialSinAgua;
    }

    [Serializable]
    public class ResultadoHidraulico
    {
        public float CaudalFuente;
        public float CaudalUtil;
        public float PerdidaPorFuga;
        public bool FugaActivaDetectada;
        public int SectoresConectados;
        public bool TodosAbastecidos;
        public float IndiceEquidadJain;
        public List<EstadoSector> Sectores = new List<EstadoSector>();
        public List<string> AristasActivasIds = new List<string>();
    }

    public static class CalculadorReglasCurso
    {
        public static float CalcularEnergiaInicial(float energiaBase, float energiaSobrante)
        {
            return Mathf.Max(0f, energiaBase) + Mathf.Max(0f, energiaSobrante);
        }

        public static int CalcularPuntajeTrasChoque(
            int puntajeActual,
            int puntosPorChoque,
            int choquesPrevios,
            int limiteChoques,
            out int nuevosChoques,
            out bool debeReiniciar)
        {
            nuevosChoques = Mathf.Max(0, choquesPrevios) + 1;
            int nuevoPuntaje = Mathf.Max(0, puntajeActual - Mathf.Max(0, puntosPorChoque));
            debeReiniciar = nuevosChoques >= Mathf.Max(1, limiteChoques);
            return nuevoPuntaje;
        }
    }

    // Grafo dirigido de distribucion hidraulica con BFS de conectividad,
    // deteccion de roturas y reparto proporcional por resistencia altitudinal.
    public class GrafoHidraulico
    {
        readonly Dictionary<string, NodoHidraulico> nodos = new Dictionary<string, NodoHidraulico>();
        readonly List<NodoHidraulico> ordenNodos = new List<NodoHidraulico>();
        readonly Dictionary<string, AristaHidraulica> aristasPorId = new Dictionary<string, AristaHidraulica>();
        readonly List<AristaHidraulica> ordenAristas = new List<AristaHidraulica>();
        readonly Dictionary<string, List<AristaHidraulica>> adyacencia = new Dictionary<string, List<AristaHidraulica>>();

        public IReadOnlyList<NodoHidraulico> Nodos => ordenNodos;
        public IReadOnlyList<AristaHidraulica> Aristas => ordenAristas;

        public void AgregarNodo(NodoHidraulico nodo)
        {
            if (nodo == null || string.IsNullOrEmpty(nodo.Id))
                throw new ArgumentException("El nodo debe tener un identificador valido.");
            if (nodos.ContainsKey(nodo.Id))
                throw new InvalidOperationException($"Nodo duplicado: {nodo.Id}");

            nodos[nodo.Id] = nodo;
            ordenNodos.Add(nodo);
            adyacencia[nodo.Id] = new List<AristaHidraulica>();
        }

        public void AgregarArista(AristaHidraulica arista)
        {
            if (arista == null || string.IsNullOrEmpty(arista.Id))
                throw new ArgumentException("La arista debe tener un identificador valido.");
            if (!nodos.ContainsKey(arista.OrigenId) || !nodos.ContainsKey(arista.DestinoId))
                throw new InvalidOperationException($"Arista {arista.Id} referencia nodos inexistentes.");
            if (aristasPorId.ContainsKey(arista.Id))
                throw new InvalidOperationException($"Arista duplicada: {arista.Id}");

            aristasPorId[arista.Id] = arista;
            ordenAristas.Add(arista);
            adyacencia[arista.OrigenId].Add(arista);
        }

        public AristaHidraulica ObtenerArista(string aristaId)
        {
            return aristasPorId.TryGetValue(aristaId, out var a) ? a : null;
        }

        public void FijarApertura(string aristaId, float apertura)
        {
            if (!aristasPorId.TryGetValue(aristaId, out var arista))
                throw new KeyNotFoundException($"No existe la arista {aristaId}");
            arista.Apertura = arista.TieneValvula ? Mathf.Clamp01(apertura) : 1f;
        }

        public Dictionary<string, float> CapturarAperturas()
        {
            var copia = new Dictionary<string, float>(ordenAristas.Count);
            foreach (var a in ordenAristas)
                copia[a.Id] = a.Apertura;
            return copia;
        }

        public void RestaurarAperturas(IReadOnlyDictionary<string, float> snapshot)
        {
            if (snapshot == null) return;
            foreach (var kv in snapshot)
            {
                if (aristasPorId.TryGetValue(kv.Key, out var a) && a.TieneValvula)
                    a.Apertura = Mathf.Clamp01(kv.Value);
            }
        }

        public ResultadoHidraulico SimularQuePasariaSi(
            float caudalFuente,
            IReadOnlyDictionary<string, float> aperturasPropuestas,
            float proporcionPerdidaFuga = 0.45f)
        {
            var respaldo = CapturarAperturas();
            try
            {
                if (aperturasPropuestas != null)
                    RestaurarAperturas(aperturasPropuestas);
                return Evaluar(caudalFuente, proporcionPerdidaFuga);
            }
            finally
            {
                RestaurarAperturas(respaldo);
            }
        }

        public ResultadoHidraulico Evaluar(float caudalFuente, float proporcionPerdidaFuga = 0.45f)
        {
            float qFuente = Mathf.Max(0f, caudalFuente);
            var resistenciaAcumulada = new Dictionary<string, float>(ordenNodos.Count);
            var aristasActivas = new List<string>();
            var cola = new Queue<string>();
            bool fugaActiva = false;

            // 1. BFS desde fuentes para conectividad, deteccion de roturas y resistencia de ruta minima.
            foreach (var n in ordenNodos)
            {
                if (n.Tipo == TipoNodoHidraulico.Fuente)
                {
                    resistenciaAcumulada[n.Id] = 0f;
                    cola.Enqueue(n.Id);
                }
            }

            while (cola.Count > 0)
            {
                string actual = cola.Dequeue();
                float rActual = resistenciaAcumulada[actual];

                foreach (var arista in adyacencia[actual])
                {
                    float aperturaEfectiva = arista.TieneValvula ? Mathf.Clamp01(arista.Apertura) : 1f;
                    if (aperturaEfectiva <= 0.001f) continue;

                    aristasActivas.Add(arista.Id);

                    if (arista.Rota)
                    {
                        fugaActiva = true;
                        continue; // Una tuberia rota abierta pierde presion y no entrega flujo util aguas abajo.
                    }

                    float rTramo = Mathf.Max(0.05f, arista.ResistenciaBase) / aperturaEfectiva;
                    float rCandidata = rActual + rTramo;

                    if (!resistenciaAcumulada.TryGetValue(arista.DestinoId, out float rPrevia) || rCandidata < rPrevia)
                    {
                        resistenciaAcumulada[arista.DestinoId] = rCandidata;
                        cola.Enqueue(arista.DestinoId);
                    }
                }
            }

            float perdidaFuga = fugaActiva ? qFuente * Mathf.Clamp01(proporcionPerdidaFuga) : 0f;
            float qUtil = Mathf.Max(0f, qFuente - perdidaFuga);

            // 2. Conductancias efectivas hacia los sectores de servicio alcanzados.
            var sectores = new List<NodoHidraulico>();
            var conductancias = new Dictionary<string, float>();
            float sumaConductancias = 0f;
            int conectados = 0;

            foreach (var n in ordenNodos)
            {
                if (n.Tipo != TipoNodoHidraulico.SectorServicio) continue;
                sectores.Add(n);

                if (resistenciaAcumulada.TryGetValue(n.Id, out float rTotal) && rTotal > 1e-5f)
                {
                    float g = 1f / rTotal;
                    conductancias[n.Id] = g;
                    sumaConductancias += g;
                    conectados++;
                }
                else
                {
                    conductancias[n.Id] = 0f;
                }
            }

            // 3. Reparto proporcional e Indice de Equidad de Jain sobre cobertura (q_i / min_i).
            var resultado = new ResultadoHidraulico
            {
                CaudalFuente = qFuente,
                CaudalUtil = qUtil,
                PerdidaPorFuga = perdidaFuga,
                FugaActivaDetectada = fugaActiva,
                SectoresConectados = conectados,
                AristasActivasIds = aristasActivas
            };

            double sumX = 0.0;
            double sumX2 = 0.0;
            bool todosCumplen = !fugaActiva && sectores.Count > 0;

            foreach (var s in sectores)
            {
                float g = conductancias[s.Id];
                bool conectado = g > 0f;
                float caudal = (conectado && sumaConductancias > 1e-5f)
                    ? qUtil * (g / sumaConductancias)
                    : 0f;

                float cobertura = s.DemandaMinima > 1e-5f ? caudal / s.DemandaMinima : (conectado ? 1f : 0f);
                bool abastecido = conectado && (caudal + 1e-3f >= s.DemandaMinima);
                if (!abastecido) todosCumplen = false;

                sumX += cobertura;
                sumX2 += (double)cobertura * cobertura;

                resultado.Sectores.Add(new EstadoSector
                {
                    Id = s.Id,
                    Nombre = s.Nombre,
                    DemandaMinima = s.DemandaMinima,
                    CaudalActual = caudal,
                    CoberturaRelativa = cobertura,
                    Conectado = conectado,
                    Abastecido = abastecido,
                    ImpactoSocialSinAgua = s.ImpactoSocialSinAgua
                });
            }

            resultado.TodosAbastecidos = todosCumplen;
            resultado.IndiceEquidadJain = (sectores.Count > 0 && sumX2 > 1e-9)
                ? (float)((sumX * sumX) / (sectores.Count * sumX2))
                : 0f;

            return resultado;
        }

        // Red canonica de 5 sectores andinos en Arequipa (caudal nominal Q = 100 L/s):
        // Con todas las valvulas abiertas al 100%, el ramal bajo acapara caudal y la ladera queda bajo el minimo.
        public static GrafoHidraulico CrearRedArequipaCincoSectores(bool incluirRamalFugaAislable = false)
        {
            var g = new GrafoHidraulico();

            g.AgregarNodo(new NodoHidraulico("F_Planta", "Reservorio Principal", TipoNodoHidraulico.Fuente, 0f));
            g.AgregarNodo(new NodoHidraulico("C_Dist", "Camara de Distribucion", TipoNodoHidraulico.Camara, 2f));

            g.AgregarNodo(new NodoHidraulico("S1_Mercado", "Mercado Popular", TipoNodoHidraulico.SectorServicio, 4f, 18f,
                "Los puestos de alimentos operan sin lavado higienico."));
            g.AgregarNodo(new NodoHidraulico("S2_Puente", "Barrio del Puente", TipoNodoHidraulico.SectorServicio, 10f, 16f,
                "Las viviendas de la parte baja reciben agua intermitente."));
            g.AgregarNodo(new NodoHidraulico("S3_Salud", "Centro de Salud", TipoNodoHidraulico.SectorServicio, 18f, 20f,
                "Sin agua no podemos esterilizar el instrumental."));
            g.AgregarNodo(new NodoHidraulico("S4_Refugio", "Refugio Comunitario", TipoNodoHidraulico.SectorServicio, 26f, 15f,
                "El comedor y los albergados no tienen reserva para cocinar."));
            g.AgregarNodo(new NodoHidraulico("S5_Ladera", "Barrio de la Ladera", TipoNodoHidraulico.SectorServicio, 42f, 17f,
                "Llevo dos dias bajando a la fuente de la plaza con baldes."));

            g.AgregarArista(new AristaHidraulica("E_Troncal", "F_Planta", "C_Dist", 0.05f, tieneValvula: false, aperturaInicial: 1f));
            g.AgregarArista(new AristaHidraulica("V1_Mercado", "C_Dist", "S1_Mercado", 1.00f, tieneValvula: true, aperturaInicial: 1f));
            g.AgregarArista(new AristaHidraulica("V2_Puente", "C_Dist", "S2_Puente", 1.35f, tieneValvula: true, aperturaInicial: 1f));
            g.AgregarArista(new AristaHidraulica("V3_Salud", "C_Dist", "S3_Salud", 1.80f, tieneValvula: true, aperturaInicial: 1f));
            g.AgregarArista(new AristaHidraulica("V4_Refugio", "C_Dist", "S4_Refugio", 2.30f, tieneValvula: true, aperturaInicial: 1f));
            g.AgregarArista(new AristaHidraulica("V5_Ladera", "C_Dist", "S5_Ladera", 3.40f, tieneValvula: true, aperturaInicial: 1f));

            if (incluirRamalFugaAislable)
            {
                g.AgregarNodo(new NodoHidraulico("C_DesvioAveria", "Derivacion Antigua", TipoNodoHidraulico.Camara, 5f));
                g.AgregarArista(new AristaHidraulica("V_AislamientoFuga", "C_Dist", "C_DesvioAveria", 1.20f, tieneValvula: true, aperturaInicial: 1f, rota: true));
            }

            return g;
        }
    }
}
