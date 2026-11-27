using System.Collections.Generic;
using System.Linq;
using HastaElUltimoBarrio.Dominio;
using NUnit.Framework;
using UnityEngine;

namespace HastaElUltimoBarrio.Tests.EditMode
{
    public class GrafoHidraulicoTests
    {
        [Test]
        public void BFS_ValvulaCerrada_DesconectaSectorYCaudalCero()
        {
            var red = GrafoHidraulico.CrearRedArequipaCincoSectores();
            red.FijarApertura("V5_Ladera", 0f);

            var res = red.Evaluar(100f);
            var ladera = res.Sectores.First(s => s.Id == "S5_Ladera");

            Assert.IsFalse(ladera.Conectado);
            Assert.AreEqual(0f, ladera.CaudalActual, 1e-4f);
            Assert.AreEqual(4, res.SectoresConectados);
            Assert.IsFalse(res.TodosAbastecidos);
        }

        [Test]
        public void BFS_FugaActiva_PenalizaCaudalYAlCerrarValvulaRecuperaTotal()
        {
            var red = GrafoHidraulico.CrearRedArequipaCincoSectores(incluirRamalFugaAislable: true);

            var conFuga = red.Evaluar(100f, proporcionPerdidaFuga: 0.45f);
            Assert.IsTrue(conFuga.FugaActivaDetectada);
            Assert.AreEqual(45f, conFuga.PerdidaPorFuga, 1e-3f);
            Assert.AreEqual(55f, conFuga.CaudalUtil, 1e-3f);
            Assert.IsFalse(conFuga.TodosAbastecidos);

            red.FijarApertura("V_AislamientoFuga", 0f);
            var aislada = red.Evaluar(100f, proporcionPerdidaFuga: 0.45f);
            Assert.IsFalse(aislada.FugaActivaDetectada);
            Assert.AreEqual(0f, aislada.PerdidaPorFuga, 1e-3f);
            Assert.AreEqual(100f, aislada.CaudalUtil, 1e-3f);
        }

        [Test]
        public void ConservacionDeMasa_SumaCaudalesMasFugaIgualACaudalFuente()
        {
            var red = GrafoHidraulico.CrearRedArequipaCincoSectores(incluirRamalFugaAislable: true);
            red.FijarApertura("V1_Mercado", 0.35f);
            red.FijarApertura("V3_Salud", 0.65f);

            var res = red.Evaluar(100f, proporcionPerdidaFuga: 0.40f);
            float sumaSectores = res.Sectores.Sum(s => s.CaudalActual);

            Assert.AreEqual(100f, sumaSectores + res.PerdidaPorFuga, 1e-3f);
        }

        [Test]
        public void DilemaAltitudinal_TodasAbiertasAlCien_DejaLaderaBajoElMinimo()
        {
            var red = GrafoHidraulico.CrearRedArequipaCincoSectores();
            var res = red.Evaluar(100f);

            var mercado = res.Sectores.First(s => s.Id == "S1_Mercado");
            var ladera = res.Sectores.First(s => s.Id == "S5_Ladera");

            Assert.Greater(mercado.CaudalActual, mercado.DemandaMinima);
            Assert.Less(ladera.CaudalActual, ladera.DemandaMinima);
            Assert.IsFalse(ladera.Abastecido);
            Assert.IsFalse(res.TodosAbastecidos);
        }

        [Test]
        public void OptimizadorJain_CincoSectores_CumpleTodosLosMinimosConAltaEquidad()
        {
            var red = GrafoHidraulico.CrearRedArequipaCincoSectores(incluirRamalFugaAislable: true);
            var antes = red.CapturarAperturas();

            var solucion = OptimizadorEquidadJain.CalcularOptimo(red, 100f, pasoValvula: 0.05f);
            var despues = red.CapturarAperturas();

            Assert.IsNotNull(solucion.ResultadoOptimo);
            Assert.IsFalse(solucion.ResultadoOptimo.FugaActivaDetectada);
            Assert.IsTrue(solucion.ResultadoOptimo.TodosAbastecidos);
            Assert.GreaterOrEqual(solucion.ResultadoOptimo.IndiceEquidadJain, 0.98f);
            Assert.AreEqual(0f, solucion.AperturasOptimas["V_AislamientoFuga"], 1e-4f);
            CollectionAssert.AreEquivalent(antes, despues);
        }

        [Test]
        public void SimularQuePasariaSi_NoMutaEstadoConfirmadoDelGrafo()
        {
            var red = GrafoHidraulico.CrearRedArequipaCincoSectores();
            var propuesta = new Dictionary<string, float>
            {
                { "V1_Mercado", 0.30f },
                { "V5_Ladera", 1.00f }
            };

            var simulado = red.SimularQuePasariaSi(100f, propuesta);
            var original = red.Evaluar(100f);

            Assert.AreEqual(1f, red.ObtenerArista("V1_Mercado").Apertura, 1e-4f);
            Assert.AreNotEqual(
                simulado.Sectores.First(s => s.Id == "S5_Ladera").CaudalActual,
                original.Sectores.First(s => s.Id == "S5_Ladera").CaudalActual);
        }

        [Test]
        public void ReglaEnergia_AcumulaSobranteSobreBaseSinNegativos()
        {
            Assert.AreEqual(13.5f, CalculadorReglasCurso.CalcularEnergiaInicial(10f, 3.5f), 1e-4f);
            Assert.AreEqual(10f, CalculadorReglasCurso.CalcularEnergiaInicial(10f, -2f), 1e-4f);
        }

        [Test]
        public void ReglaPuntajeYChoques_DescuentaVeinteYReiniciaEnQuintoChoque()
        {
            int puntaje = 100;
            int choques = 0;

            for (int i = 1; i <= 4; i++)
            {
                puntaje = CalculadorReglasCurso.CalcularPuntajeTrasChoque(
                    puntaje, 20, choques, 5, out choques, out bool reiniciar);
                Assert.AreEqual(100 - i * 20, puntaje);
                Assert.IsFalse(reiniciar);
            }

            puntaje = CalculadorReglasCurso.CalcularPuntajeTrasChoque(
                puntaje, 20, choques, 5, out choques, out bool reiniciarQuinto);

            Assert.AreEqual(5, choques);
            Assert.AreEqual(0, puntaje);
            Assert.IsTrue(reiniciarQuinto);
        }

        [Test]
        public void TelemetryService_SerializaYDeserializaSesionCompletaJson()
        {
            var telemetria = new TelemetryService(guardarAutoEnDisco: false);
            telemetria.IniciarSesion("P01");
            telemetria.IniciarNivel("LVL_09_PlazaControlEquidad", 12.4f);
            telemetria.RegistrarChoque(new Vector3(14.5f, 2.2f, 0f), 18.7f);
            telemetria.RegistrarConsultaSimulador();
            telemetria.RegistrarConsultaSimulador();
            telemetria.RegistrarResultadoHidraulico(2, 0.96f, 0.99f, true, new[] { 0.3f, 0.45f, 0.65f, 0.8f, 1.0f });
            telemetria.FinalizarNivel(true, 64.2f, 4.1f, 80, 1, "Completado");

            string json = telemetria.ExportarJson(false);
            var restaurado = TelemetryService.DesdeJson(json);

            Assert.IsNotNull(restaurado);
            Assert.AreEqual("P01", restaurado.sessionId);
            Assert.AreEqual(1, restaurado.niveles.Count);
            Assert.AreEqual("LVL_09_PlazaControlEquidad", restaurado.niveles[0].escena);
            Assert.AreEqual(2, restaurado.niveles[0].consultasSimulador);
            Assert.AreEqual(1, restaurado.niveles[0].puntosChoque.Count);
            Assert.AreEqual(14.5f, restaurado.niveles[0].puntosChoque[0].x, 1e-3f);
            Assert.AreEqual(0.96f, restaurado.niveles[0].indiceJainJugador, 1e-3f);
            Assert.IsTrue(restaurado.niveles[0].equidadLograda);
        }

        [Test]
        public void OptimizadorJain_NoFallaConCaudalNulo()
        {
            var red = GrafoHidraulico.CrearRedArequipaCincoSectores();
            var sol = OptimizadorEquidadJain.CalcularOptimo(red, 0f);

            Assert.IsNotNull(sol.ResultadoOptimo);
            Assert.IsFalse(sol.ResultadoOptimo.TodosAbastecidos);
            Assert.AreEqual(0f, sol.ResultadoOptimo.IndiceEquidadJain, 1e-4f);
        }
    }
}
