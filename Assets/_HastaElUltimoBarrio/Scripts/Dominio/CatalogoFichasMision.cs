using System;
using System.Collections.Generic;

namespace HastaElUltimoBarrio.Dominio
{
    [Serializable]
    public class FilaControlMision
    {
        public string Tecla;
        public string Accion;
        public string Detalle;

        public FilaControlMision(string tecla, string accion, string detalle)
        {
            Tecla = tecla;
            Accion = accion;
            Detalle = detalle;
        }
    }

    [Serializable]
    public class FichaMisionNivel
    {
        public string ClaveEscena;
        public string EtiquetaFase;
        public string Titulo;
        public string SituacionBarrio;
        public bool EsSuperficie3D;
        public FilaControlMision[] Controles;
        public string[] PasosGanar;
    }

    // Catalogo declarativo con el minitutorial, controles visuales y ruta secuencial de los 9 niveles.
    public static class CatalogoFichasMision
    {
        static readonly string[] OrdenEscenas =
        {
            "MenuPrincipal",
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

        static readonly FilaControlMision[] ControlesTunel25D =
        {
            new FilaControlMision("W / ↑ / ESPACIO", "Propulsor vertical", "Impulso de ascenso contra la gravedad del túnel."),
            new FilaControlMision("A / ←   D / →", "Inclinación lateral", "Desplaza el dron con inercia en el plano 2.5D."),
            new FilaControlMision("10 + SOBRANTE", "Batería acumulativa", "Inicias con 10.0 más la reserva guardada al ganar."),
            new FilaControlMision("MÁX. 5 CHOQUES", "Puntaje 100%", "Cada colisión en roca Peligro resta 20%; 5 reinician.")
        };

        static readonly FilaControlMision[] ControlesRoverTransicion =
        {
            new FilaControlMision("WASD / FLECHAS", "Conducir Rover SCADA", "Desplázate por la superficie en perspectiva isométrica 3D."),
            new FilaControlMision("PELIGROS FIJOS", "Reconocimiento sin móviles", "Rodea zanjas, escombros y fugas estáticas en capa Peligro."),
            new FilaControlMision("10 + SOBRANTE", "Batería acumulativa", "Conserva energía recogiendo baterías ámbar durante el recorrido."),
            new FilaControlMision("MÁX. 5 CHOQUES", "Puntaje 100%", "Cada choque resta 20%; al 5.º impacto se reinicia el nivel.")
        };

        static readonly FilaControlMision[] ControlesRoverObstaculos =
        {
            new FilaControlMision("WASD / FLECHAS", "Conducir Rover SCADA", "Cruza las rutas urbanas coordinando giros en perspectiva 3D."),
            new FilaControlMision("SINCRONÍA", "Obstáculos móviles", "Espera el pre-aviso ámbar de los géiseres y el ritmo de barreras."),
            new FilaControlMision("10 + SOBRANTE", "Batería acumulativa", "Inicias con 10.0 más el sobrante guardado del tramo previo."),
            new FilaControlMision("MÁX. 5 CHOQUES", "Puntaje 100%", "Cada choque en capa Peligro resta 20% y reaparece al inicio.")
        };

        public static string ObtenerSiguienteEscenaPorDefecto(string nombreEscenaActual)
        {
            if (string.IsNullOrEmpty(nombreEscenaActual)) return string.Empty;

            for (int i = 0; i < OrdenEscenas.Length; i++)
            {
                if (string.Equals(OrdenEscenas[i], nombreEscenaActual, StringComparison.OrdinalIgnoreCase))
                {
                    int siguiente = i + 1;
                    return siguiente < OrdenEscenas.Length ? OrdenEscenas[siguiente] : "MenuPrincipal";
                }
            }
            return string.Empty;
        }

        public static FichaMisionNivel ObtenerFicha(string nombreEscena)
        {
            string clave = string.IsNullOrEmpty(nombreEscena) ? "LVL_01_GaleriaCaptacion" : nombreEscena;

            switch (clave)
            {
                case "LVL_03_SifonMercado":
                    return new FichaMisionNivel
                    {
                        ClaveEscena = clave,
                        EtiquetaFase = "FASE 2 · TRANSICIÓN A SUPERFICIE 3D",
                        Titulo = "Nivel 3 · Ruta Exterior al Mercado",
                        SituacionBarrio =
                            "El diagnóstico subterráneo terminó. Ahora conduces el Rover SCADA en superficie 3D hacia el acceso " +
                            "del Mercado Popular sorteando derrumbes y fugas fijas (sin obstáculos móviles).",
                        EsSuperficie3D = true,
                        Controles = ControlesRoverTransicion,
                        PasosGanar = new[]
                        {
                            "Conduce con [WASD / Flechas] adaptándote al movimiento isométrico en superficie.",
                            "Esquiva escombros, zanjas y tuberías rotas estáticas marcadas en capa Peligro.",
                            "Recoge las baterías ámbar para acumular reserva hacia los niveles con movimiento.",
                            "Entra en la plataforma de salida (Finish) para avanzar al corredor de Salud."
                        }
                    };

                case "LVL_04_ConductoSalud":
                    return new FichaMisionNivel
                    {
                        ClaveEscena = clave,
                        EtiquetaFase = "FASE 2 · RUTA 3D CON OBSTÁCULOS MÓVILES",
                        Titulo = "Nivel 4 · Corredor al Centro de Salud",
                        SituacionBarrio =
                            "Atraviesas el corredor urbano rumbo al Centro de Salud frente a las primeras barreras de obra " +
                            "oscilantes y géiseres intermitentes a presión.",
                        EsSuperficie3D = true,
                        Controles = ControlesRoverObstaculos,
                        PasosGanar = new[]
                        {
                            "Observa el pre-aviso ámbar de los géiseres y el vaivén de las barreras antes de cruzar.",
                            "Conduce con [WASD / Flechas] evitando impactos en capa Peligro (máximo 5 choques).",
                            "Desvíate por los carriles laterales seguros para recoger baterías de respaldo.",
                            "Alcanza la plataforma PF_ZonaSalida (Finish) preservando el puntaje y energía."
                        }
                    };

                case "LVL_05_ImpulsionLadera":
                    return new FichaMisionNivel
                    {
                        ClaveEscena = clave,
                        EtiquetaFase = "FASE 2 · RUTA 3D CON OBSTÁCULOS MÓVILES",
                        Titulo = "Nivel 5 · Ascenso hacia Ladera",
                        SituacionBarrio =
                            "El trazado asciende hacia las terrazas altas del Barrio de la Ladera sorteando múltiples chorros " +
                            "de fuga y compuertas cinemáticas que exigen gestionar la batería SCADA.",
                        EsSuperficie3D = true,
                        Controles = ControlesRoverObstaculos,
                        PasosGanar = new[]
                        {
                            "Coordina cruces cortos entre ciclos de géiseres con [WASD / Flechas].",
                            "Recoge las baterías del recorrido antes de encarar el tramo final de barreras.",
                            "Evita roces contra objetos en capa Peligro para no perder 20% por choque.",
                            "Posa el Rover sobre PF_ZonaSalida (Finish) rumbo al cruce Refugio-Puente."
                        }
                    };

                case "LVL_06_CamaraRefugioPuente":
                    return new FichaMisionNivel
                    {
                        ClaveEscena = clave,
                        EtiquetaFase = "FASE 2 · RUTA 3D CON OBSTÁCULOS MÓVILES",
                        Titulo = "Nivel 6 · Cruce Refugio–Puente",
                        SituacionBarrio =
                            "Última ruta de aproximación entre el Refugio Comunitario y el Barrio del Puente para conservar " +
                            "la máxima batería antes de operar las válvulas del grafo hidráulico en Nivel 7.",
                        EsSuperficie3D = true,
                        Controles = ControlesRoverObstaculos,
                        PasosGanar = new[]
                        {
                            "Supera los obstáculos móviles combinados aprovechando las pausas seguras.",
                            "Asegura baterías extra para arrancar la fase de válvulas SCADA con ventaja.",
                            "Mantén al menos 1 vida libre (< 5 choques) hasta el tramo Sureste.",
                            "Ingresa en PF_ZonaSalida (Finish) para iniciar la operación de red en Nivel 7."
                        }
                    };

                case "LVL_07_AislamientoFuga":
                    return new FichaMisionNivel
                    {
                        ClaveEscena = clave,
                        EtiquetaFase = "FASE 3 · RED HIDRÁULICA 3D",
                        Titulo = "Nivel 7 · Calle de la Fuga",
                        SituacionBarrio =
                            "Ingresas al sector con consolas SCADA activas. Una rotura abierta al Norte desperdicia el 45% " +
                            "del caudal mientras el Centro de Salud permanece cerrado y sin esterilización.",
                        EsSuperficie3D = true,
                        Controles = new[]
                        {
                            new FilaControlMision("WASD / FLECHAS", "Conducir Rover SCADA", "Desplázate en perspectiva isométrica esquivando géiseres y barreras."),
                            new FilaControlMision("E", "Operar consola en campo", "Acércate a una válvula iluminada para cerrar la fuga o abrir pasos."),
                            new FilaControlMision("Q", "Simulador «¿Qué pasaría si…?»", "Previsualiza el Índice de Jain en el panel derecho sin alterar la red."),
                            new FilaControlMision("P", "Confirmar presurización", "Verifica 5/5 sectores en verde antes de dirigirte a la meta cian.")
                        },
                        PasosGanar = new[]
                        {
                            "Conduce hacia el sector Norte-Oeste y cierra la válvula de rotura con [E].",
                            "Esquiva los géiseres y abre con [E] la consola del Centro de Salud.",
                            "Comprueba en el panel derecho que los 5 sectores estén abastecidos y pulsa [P].",
                            "Avanza hasta la plataforma de salida iluminada en cian en la esquina Sureste."
                        }
                    };

                case "LVL_08_DepositoRegulacion":
                    return new FichaMisionNivel
                    {
                        ClaveEscena = clave,
                        EtiquetaFase = "FASE 3 · REGULACIÓN ALTITUDINAL",
                        Titulo = "Nivel 8 · Depósito de Regulación",
                        SituacionBarrio =
                            "El Mercado en zona baja (4 m, menor resistencia) está abierto al 100% y acapara presión, dejando a " +
                            "la Ladera alta (42 m, mayor resistencia) estrangulada en 40% y bajo su caudal mínimo vital.",
                        EsSuperficie3D = true,
                        Controles = new[]
                        {
                            new FilaControlMision("WASD / FLECHAS", "Conducir Rover SCADA", "Recorre los dos depósitos cruzando chorros intermitentes y barreras."),
                            new FilaControlMision("E  (SALTOS 20%)", "Regular apertura %", "Ajusta porcentualmente las válvulas físicas del Mercado y Ladera."),
                            new FilaControlMision("Q", "Simular equilibrio", "Evalúa qué porcentaje corrige el déficit altitudinal sin golpes de ariete."),
                            new FilaControlMision("P", "Confirmar presurización", "Valida los 5 sectores en equilibrio para habilitar la Plaza Central.")
                        },
                        PasosGanar = new[]
                        {
                            "Acércate al Ramal Bajo (Mercado) y regula su válvula con [E] hasta dejarla en 40%.",
                            "Cruza los obstáculos hacia el Noreste y abre el Ramal Alto (Ladera) con [E] al 100%.",
                            "Verifica que el panel muestre los 5 sectores abastecidos (Jain >= 0.98) y pulsa [P].",
                            "Entra al aro cian de salida en el Sureste para avanzar al nivel final."
                        }
                    };

                case "LVL_09_PlazaControlEquidad":
                    return new FichaMisionNivel
                    {
                        ClaveEscena = clave,
                        EtiquetaFase = "FASE FINAL · EQUIDAD DE JAIN",
                        Titulo = "Nivel 9 · Plaza de Control de Equidad",
                        SituacionBarrio =
                            "Plaza central con los cinco barrios de Arequipa y tarjetas izquierdas de impacto social. Debes aislar " +
                            "la fuga física en campo y equilibrar toda la red para reactivar la fuente y el lavadero comunitario.",
                        EsSuperficie3D = true,
                        Controles = new[]
                        {
                            new FilaControlMision("E  EN CAMPO", "Cerrar rotura física", "Ve primero a la válvula de rotura Noroeste para detener la fuga."),
                            new FilaControlMision("Q  +  [ - / + ]", "Simular paso fino (5%)", "Prueba combinaciones en el panel derecho para maximizar la Equidad de Jain."),
                            new FilaControlMision("APLICAR A RED", "Ejecutar ajuste simulado", "Vuelca las aperturas probadas del modo «¿Qué pasaría si…?» a la red real."),
                            new FilaControlMision("P", "Presurizar y activar lavadero", "Enciende el agua comunitaria y compara tu Índice de Jain frente al Óptimo.")
                        },
                        PasosGanar = new[]
                        {
                            "Conduce al Noroeste y cierra con [E] la Válvula de Aislamiento de Rotura.",
                            "Pulsa [Q] y ajusta - / + (ej. 30%, 40%, 60%, 60%, 100%) hasta poner en verde las 5 tarjetas sociales.",
                            "Pulsa «Aplicar a Red» (o ajusta en campo con [E]) y confirma la presurización con [P].",
                            "Observa encenderse el lavadero comunitario y posa el Rover en la plataforma final."
                        }
                    };

                default:
                    return CrearFichaTunel(clave);
            }
        }

        static FichaMisionNivel CrearFichaTunel(string clave)
        {
            var mapa = new Dictionary<string, (string titulo, string situacion, string extraPaso)>(StringComparer.OrdinalIgnoreCase)
            {
                {
                    "LVL_01_GaleriaCaptacion",
                    ("Nivel 1 · Galería de Captación",
                     "Una caída de presión amenaza las cámaras subterráneas de Arequipa. Pilota el dron de inspección desde el reservorio hacia la salida.",
                     "Aterriza con suavidad sobre la plataforma final (Finish) para conservar energía sobrante.")
                },
                {
                    "LVL_02_TunelPresion",
                    ("Nivel 2 · Túnel de Presión",
                     "El conducto presenta desniveles en zigzag con zonas estrechas. Aprovecha las plataformas seguras para guardar tu punto de reaparición.",
                     "Recoge las baterías ámbar del recorrido para iniciar el siguiente tramo con reserva adicional.")
                },
                {
                    "LVL_03_SifonMercado",
                    ("Nivel 3 · Sifón del Mercado",
                     "Cruzas la galería bajo el Mercado Popular para verificar las válvulas maestras que abastecen la zona comercial baja.",
                     "Controla los impulsos cortos en los giros antes de alcanzar la plataforma de salida.")
                },
                {
                    "LVL_04_ConductoSalud",
                    ("Nivel 4 · Conducto del Centro de Salud",
                     "Tramo con obstáculos móviles cinemáticos (compuertas y chorros activos). Observa su ritmo antes de acelerar con el dron.",
                     "Sincroniza el cruce de los chorros intermitentes sin tocar los bloques en rojo/peligro.")
                },
                {
                    "LVL_05_ImpulsionLadera",
                    ("Nivel 5 · Impulsión hacia Ladera",
                     "La tubería asciende hacia las terrazas altas enfrentando rejillas móviles y mayor exigencia de combustible entre plataformas.",
                     "Apóyate en cada plataforma segura para estabilizar el dron y ahorrar batería.")
                },
                {
                    "LVL_06_CamaraRefugioPuente",
                    ("Nivel 6 · Cámara Refugio-Puente",
                     "Último tramo subterráneo antes de salir a superficie. Supera los obstáculos sincronizados para conectar la consola SCADA exterior.",
                     "Completa la cámara final para tomar el control del Rover SCADA en Nivel 7.")
                }
            };

            if (!mapa.TryGetValue(clave, out var info))
            {
                info = ("Tramo Subterráneo 2.5D",
                    "Inspecciona la galería hidráulica con el dron manteniendo la reserva energética hasta la plataforma de salida.",
                    "Alcanza la plataforma de salida sin agotar la batería ni superar los 5 choques.");
            }

            return new FichaMisionNivel
            {
                ClaveEscena = clave,
                EtiquetaFase = "FASE 1 · SUBSUELO 2.5D",
                Titulo = info.titulo,
                SituacionBarrio = info.situacion,
                EsSuperficie3D = false,
                Controles = ControlesTunel25D,
                PasosGanar = new[]
                {
                    "Realiza impulsos cortos con [W / ↑ / Espacio] y corrige lateralmente con [A / D].",
                    "Pósate desde arriba en plataformas seguras para recargar estabilidad y fijar checkpoint.",
                    "Evita las paredes y obstáculos en capa Peligro (máximo 5 choques, -20% cada uno).",
                    info.extraPaso
                }
            };
        }
    }
}
