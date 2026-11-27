using System.Collections.Generic;
using System.Linq;
using HastaElUltimoBarrio.Dominio;
using NUnit.Framework;
using UnityEngine;

namespace HastaElUltimoBarrio.Tests.EditMode
{
    public class ReglasPartidaTests
    {
        [Test]
        public void FlujoMultinivel_EnergiaAcumuladaYReinicioPreservaInicial()
        {
            // Nivel 1 inicia en 10.0 y sobran 2.4 unidades
            float energiaN1 = CalculadorReglasCurso.CalcularEnergiaInicial(10f, 0f);
            Assert.AreEqual(10f, energiaN1, 1e-4f);

            // Nivel 2 arranca con 10 + 2.4 = 12.4
            float energiaN2Inicial = CalculadorReglasCurso.CalcularEnergiaInicial(10f, 2.4f);
            Assert.AreEqual(12.4f, energiaN2Inicial, 1e-4f);

            // Si falla en N2, al reiniciar no se modifica el sobrante previo -> vuelve a 12.4
            float energiaN2TrasReinicio = CalculadorReglasCurso.CalcularEnergiaInicial(10f, 2.4f);
            Assert.AreEqual(energiaN2Inicial, energiaN2TrasReinicio, 1e-4f);
        }

        [Test]
        public void EscenarioNivel07_EstadoInicialFallaYAlCerrarFugaYAbrirSaludCumple()
        {
            var red = GrafoHidraulico.CrearRedArequipaCincoSectores(incluirRamalFugaAislable: true);
            red.FijarApertura("V1_Mercado", 0.30f);
            red.FijarApertura("V2_Puente", 0.40f);
            red.FijarApertura("V3_Salud", 0.00f);
            red.FijarApertura("V4_Refugio", 0.60f);
            red.FijarApertura("V5_Ladera", 1.00f);
            red.FijarApertura("V_AislamientoFuga", 1.00f);

            var inicial = red.Evaluar(100f);
            Assert.IsTrue(inicial.FugaActivaDetectada);
            Assert.IsFalse(inicial.TodosAbastecidos);
            Assert.IsFalse(inicial.Sectores.First(s => s.Id == "S3_Salud").Conectado);

            // Jugador cierra rotura y abre el paso calibrado al Centro de Salud
            red.FijarApertura("V_AislamientoFuga", 0f);
            red.FijarApertura("V3_Salud", 0.65f);

            var resuelto = red.Evaluar(100f);
            Assert.IsFalse(resuelto.FugaActivaDetectada);
            Assert.IsTrue(resuelto.TodosAbastecidos);
            Assert.GreaterOrEqual(resuelto.IndiceEquidadJain, 0.99f);
        }

        [Test]
        public void TelemetryService_RegistraSecuenciaMultinivelTunelYSuperficie()
        {
            var t = new TelemetryService(guardarAutoEnDisco: false);
            t.IniciarSesion("S_TEST_07");

            t.IniciarNivel("LVL_01_GaleriaCaptacion", 10f);
            t.RegistrarChoque(new Vector3(54f, 2.5f, 0f), 14.2f);
            t.FinalizarNivel(true, 41.0f, 2.8f, 80, 1, "Completado");

            t.IniciarNivel("LVL_07_AislamientoFuga", 12.8f);
            t.RegistrarConsultaSimulador();
            t.RegistrarResultadoHidraulico(1, 0.994f, 0.995f, true, new[] { 0.30f, 0.40f, 0.65f, 0.60f, 1.00f, 0f });
            t.FinalizarNivel(true, 55.3f, 3.1f, 100, 0, "Completado");

            var dto = TelemetryService.DesdeJson(t.ExportarJson(false));
            Assert.AreEqual(2, dto.niveles.Count);
            Assert.AreEqual("LVL_01_GaleriaCaptacion", dto.niveles[0].escena);
            Assert.AreEqual("LVL_07_AislamientoFuga", dto.niveles[1].escena);
            Assert.AreEqual(12.8f, dto.niveles[1].energiaInicial, 1e-3f);
            Assert.IsTrue(dto.niveles[1].equidadLograda);
        }

        [Test]
        public void EscenarioNivel08_RegulacionDosRamalesAltitudinales_EquilibraCincoSectores()
        {
            var red = GrafoHidraulico.CrearRedArequipaCincoSectores(incluirRamalFugaAislable: false);
            red.FijarApertura("V1_Mercado", 1.00f);
            red.FijarApertura("V2_Puente", 0.40f);
            red.FijarApertura("V3_Salud", 0.65f);
            red.FijarApertura("V4_Refugio", 0.60f);
            red.FijarApertura("V5_Ladera", 0.40f);

            var inicial = red.Evaluar(100f);
            Assert.IsFalse(inicial.TodosAbastecidos);
            Assert.IsFalse(inicial.Sectores.First(s => s.Id == "S5_Ladera").Abastecido);

            // Regular un solo ramal (solo Ladera al 100% sin estrangular Mercado) sigue en deficit
            red.FijarApertura("V5_Ladera", 1.00f);
            Assert.IsFalse(red.Evaluar(100f).TodosAbastecidos);

            // Al regular ambos ramales (Mercado 40% y Ladera 100%) los 5 sectores cumplen minimos
            red.FijarApertura("V1_Mercado", 0.40f);
            var equilibrado = red.Evaluar(100f);
            Assert.IsTrue(equilibrado.TodosAbastecidos);
            Assert.GreaterOrEqual(equilibrado.IndiceEquidadJain, 0.98f);
        }

        [Test]
        public void EscenarioNivel09_SimuladorQuePasariaSiYComparacionOptimoJain()
        {
            var red = GrafoHidraulico.CrearRedArequipaCincoSectores(incluirRamalFugaAislable: true);
            var inicial = red.Evaluar(100f);
            Assert.IsTrue(inicial.FugaActivaDetectada);
            Assert.IsTrue(inicial.Sectores.All(s => !string.IsNullOrEmpty(s.ImpactoSocialSinAgua)));

            var propuesta = new Dictionary<string, float>
            {
                { "V_AislamientoFuga", 0f },
                { "V1_Mercado", 0.30f },
                { "V2_Puente", 0.40f },
                { "V3_Salud", 0.60f },
                { "V4_Refugio", 0.60f },
                { "V5_Ladera", 1.00f }
            };

            var simulado = red.SimularQuePasariaSi(100f, propuesta);
            Assert.IsFalse(simulado.FugaActivaDetectada);
            Assert.IsTrue(simulado.TodosAbastecidos);

            // Simular no debe mutar el estado real del grafo
            Assert.IsTrue(red.Evaluar(100f).FugaActivaDetectada);

            var optimo = OptimizadorEquidadJain.CalcularOptimo(red, 100f, 0.05f);
            Assert.IsNotNull(optimo.ResultadoOptimo);
            Assert.LessOrEqual(Mathf.Abs(optimo.ResultadoOptimo.IndiceEquidadJain - simulado.IndiceEquidadJain), 0.02f);
        }

        [Test]
        public void CatalogoFichasMision_NueveNivelesYRutaSecuencialEstanCompletos()
        {
            string[] niveles =
            {
                "LVL_01_GaleriaCaptacion",
                "LVL_02_TunelPresion",
                "LVL_03_SifonMercado",
                "LVL_04_ConductoSalud",
                "LVL_05_ImpulsionLadera",
                "LVL_06_CamaraRefugioPuente",
                "LVL_07_AislamientoFuga",
                "LVL_08_DepositoRegulacion",
                "LVL_09_PlazaControlEquidad"
            };

            for (int i = 0; i < niveles.Length; i++)
            {
                var ficha = CatalogoFichasMision.ObtenerFicha(niveles[i]);
                Assert.IsNotNull(ficha);
                Assert.IsFalse(string.IsNullOrEmpty(ficha.Titulo), niveles[i]);
                Assert.IsFalse(string.IsNullOrEmpty(ficha.SituacionBarrio), niveles[i]);
                Assert.GreaterOrEqual(ficha.Controles.Length, 4, niveles[i]);
                foreach (var c in ficha.Controles)
                {
                    Assert.IsFalse(string.IsNullOrEmpty(c.Tecla));
                    Assert.IsFalse(string.IsNullOrEmpty(c.Accion));
                }
                Assert.GreaterOrEqual(ficha.PasosGanar.Length, 3, niveles[i]);
                Assert.AreEqual(i >= 6, ficha.EsSuperficie3D, niveles[i]);
            }

            Assert.AreEqual("LVL_08_DepositoRegulacion", CatalogoFichasMision.ObtenerSiguienteEscenaPorDefecto("LVL_07_AislamientoFuga"));
            Assert.AreEqual("LVL_09_PlazaControlEquidad", CatalogoFichasMision.ObtenerSiguienteEscenaPorDefecto("LVL_08_DepositoRegulacion"));
            Assert.AreEqual("MenuPrincipal", CatalogoFichasMision.ObtenerSiguienteEscenaPorDefecto("LVL_09_PlazaControlEquidad"));
        }
    }
}
