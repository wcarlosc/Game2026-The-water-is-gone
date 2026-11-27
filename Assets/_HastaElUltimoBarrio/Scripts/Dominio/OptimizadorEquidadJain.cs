using System;
using System.Collections.Generic;
using UnityEngine;

namespace HastaElUltimoBarrio.Dominio
{
    public class SolucionOptimaHidraulica
    {
        public Dictionary<string, float> AperturasOptimas = new Dictionary<string, float>();
        public ResultadoHidraulico ResultadoOptimo;
    }

    // Calcula una configuracion discreta de valvulas que cierra fugas rotas
    // y maximiza el Indice de Equidad de Jain cumpliendo los minimos vitales.
    public static class OptimizadorEquidadJain
    {
        public static SolucionOptimaHidraulica CalcularOptimo(
            GrafoHidraulico grafo,
            float caudalFuente,
            float pasoValvula = 0.05f)
        {
            if (grafo == null) throw new ArgumentNullException(nameof(grafo));
            float paso = Mathf.Clamp(pasoValvula, 0.01f, 0.25f);
            var respaldo = grafo.CapturarAperturas();

            try
            {
                var regulables = new List<AristaHidraulica>();
                var destinoSector = new Dictionary<string, NodoHidraulico>();
                foreach (var n in grafo.Nodos)
                {
                    if (n.Tipo == TipoNodoHidraulico.SectorServicio)
                        destinoSector[n.Id] = n;
                }

                // 1. Cerrar cualquier arista rota y abrir troncales intermedias sanas.
                var productosDeseados = new Dictionary<string, float>();
                float maxProducto = 0f;

                foreach (var a in grafo.Aristas)
                {
                    if (!a.TieneValvula) continue;
                    if (a.Rota)
                    {
                        a.Apertura = 0f;
                        continue;
                    }

                    regulables.Add(a);
                    if (destinoSector.TryGetValue(a.DestinoId, out var sec) && sec.DemandaMinima > 0f)
                    {
                        float prod = sec.DemandaMinima * a.ResistenciaBase;
                        productosDeseados[a.Id] = prod;
                        if (prod > maxProducto) maxProducto = prod;
                    }
                    else
                    {
                        a.Apertura = 1f;
                    }
                }

                // 2. Inicializacion analitica proporcional a (demanda_minima * resistencia), cuantizada al paso.
                foreach (var kv in productosDeseados)
                {
                    float crudo = maxProducto > 1e-5f ? kv.Value / maxProducto : 1f;
                    float cuantizado = Mathf.Clamp(Mathf.Round(crudo / paso) * paso, paso, 1f);
                    grafo.FijarApertura(kv.Key, cuantizado);
                }

                ResultadoHidraulico mejor = grafo.Evaluar(caudalFuente);

                // 3. Refinamiento por ascenso de coordenadas discreto.
                for (int iter = 0; iter < 40; iter++)
                {
                    bool huboMejora = false;
                    foreach (var a in regulables)
                    {
                        if (!productosDeseados.ContainsKey(a.Id)) continue;
                        float original = a.Apertura;

                        foreach (float delta in new[] { -paso, paso })
                        {
                            float candidato = Mathf.Clamp(Mathf.Round((original + delta) / paso) * paso, paso, 1f);
                            if (Mathf.Approximately(candidato, original)) continue;

                            a.Apertura = candidato;
                            var eval = grafo.Evaluar(caudalFuente);
                            if (EsMejor(eval, mejor))
                            {
                                mejor = eval;
                                original = candidato;
                                huboMejora = true;
                            }
                            else
                            {
                                a.Apertura = original;
                            }
                        }
                    }
                    if (!huboMejora) break;
                }

                return new SolucionOptimaHidraulica
                {
                    AperturasOptimas = grafo.CapturarAperturas(),
                    ResultadoOptimo = mejor
                };
            }
            finally
            {
                grafo.RestaurarAperturas(respaldo);
            }
        }

        static bool EsMejor(ResultadoHidraulico candidato, ResultadoHidraulico actual)
        {
            if (candidato.TodosAbastecidos != actual.TodosAbastecidos)
                return candidato.TodosAbastecidos;

            int abastCand = ContarAbastecidos(candidato);
            int abastAct = ContarAbastecidos(actual);
            if (abastCand != abastAct)
                return abastCand > abastAct;

            return candidato.IndiceEquidadJain > actual.IndiceEquidadJain + 1e-5f;
        }

        static int ContarAbastecidos(ResultadoHidraulico r)
        {
            int c = 0;
            foreach (var s in r.Sectores)
                if (s.Abastecido) c++;
            return c;
        }
    }
}
