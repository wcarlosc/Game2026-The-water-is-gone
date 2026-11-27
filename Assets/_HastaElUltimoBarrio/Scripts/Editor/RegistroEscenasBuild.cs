using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Utilidad de Editor para registrar las 10 escenas oficiales en Build Profiles cuando
// esten integradas en main (NO modifica EditorBuildSettings.asset en ramas de desarrollo).
public static class RegistroEscenasBuild
{
    public static readonly string[] RutasDiezEscenas =
    {
        "Assets/_HastaElUltimoBarrio/Scenes/MenuPrincipal.unity",
        "Assets/_HastaElUltimoBarrio/Scenes/Niveles/LVL_01_GaleriaCaptacion.unity",
        "Assets/_HastaElUltimoBarrio/Scenes/Niveles/LVL_02_TunelPresion.unity",
        "Assets/_HastaElUltimoBarrio/Scenes/Niveles/LVL_03_SifonMercado.unity",
        "Assets/_HastaElUltimoBarrio/Scenes/Niveles/LVL_04_ConductoSalud.unity",
        "Assets/_HastaElUltimoBarrio/Scenes/Niveles/LVL_05_ImpulsionLadera.unity",
        "Assets/_HastaElUltimoBarrio/Scenes/Niveles/LVL_06_CamaraRefugioPuente.unity",
        "Assets/_HastaElUltimoBarrio/Scenes/Niveles/LVL_07_AislamientoFuga.unity",
        "Assets/_HastaElUltimoBarrio/Scenes/Niveles/LVL_08_DepositoRegulacion.unity",
        "Assets/_HastaElUltimoBarrio/Scenes/Niveles/LVL_09_PlazaControlEquidad.unity"
    };

    [MenuItem("Tools/Hasta el último barrio/Registrar las 10 escenas en Build Profiles")]
    public static void RegistrarInteractivo()
    {
        var presentes = new List<EditorBuildSettingsScene>();
        var faltantes = new List<string>();

        foreach (string ruta in RutasDiezEscenas)
        {
            if (File.Exists(ruta))
                presentes.Add(new EditorBuildSettingsScene(ruta, true));
            else
                faltantes.Add(Path.GetFileNameWithoutExtension(ruta));
        }

        if (faltantes.Count > 0)
        {
            bool confirmar = EditorUtility.DisplayDialog(
                "Escenas incompletas en esta rama",
                $"Faltan {faltantes.Count} escenas (esperadas tras fusionar los PRs en main):\n- "
                + string.Join("\n- ", faltantes)
                + $"\n\n¿Deseas registrar únicamente las {presentes.Count} escenas disponibles o cancelar para no modificar EditorBuildSettings.asset?",
                "Cancelar (recomendado)",
                "Registrar disponibles");

            // Boton izquierdo (true) es Cancelar por seguridad en ramas de feature.
            if (confirmar)
            {
                Debug.Log("[RegistroEscenasBuild] Operación cancelada. EditorBuildSettings.asset no fue modificado.");
                return;
            }
        }

        EditorBuildSettings.scenes = presentes.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log($"[RegistroEscenasBuild] Se registraron {presentes.Count} de {RutasDiezEscenas.Length} escenas en Build Profiles.");
    }

    // Punto de entrada para automatizacion tras fusionar los PRs en main (-executeMethod RegistroEscenasBuild.RegistrarDiezEscenasBatch).
    public static void RegistrarDiezEscenasBatch()
    {
        var escenas = new List<EditorBuildSettingsScene>();
        foreach (string ruta in RutasDiezEscenas)
        {
            if (!File.Exists(ruta))
            {
                Debug.LogError($"[RegistroEscenasBuild] Falta escena requerida: {ruta}");
                continue;
            }
            escenas.Add(new EditorBuildSettingsScene(ruta, true));
        }

        EditorBuildSettings.scenes = escenas.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log($"[RegistroEscenasBuild] Registro batch completado ({escenas.Count}/{RutasDiezEscenas.Length} escenas).");
    }
}
