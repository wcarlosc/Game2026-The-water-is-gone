using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

// Helper centralizado de carga de escenas para Play Mode en Editor (sin tocar EditorBuildSettings.asset)
// y ejecucion final mediante SceneManager, asegurando siempre Time.timeScale = 1 y EventSystem InputSystem.
public static class CargadorEscenasSeguro
{
    public static void PrepararCambioEscena()
    {
        Time.timeScale = 1f;
        MisionTutorialUI.LimpiarEstadoPausaEstatico();
    }

    public static bool IntentarCargar(string nombreORuta)
    {
        if (string.IsNullOrEmpty(nombreORuta)) return false;

        PrepararCambioEscena();

        if (Application.CanStreamedLevelBeLoaded(nombreORuta))
        {
            SceneManager.LoadScene(nombreORuta);
            return true;
        }

#if UNITY_EDITOR
        string rutaEditor = ResolverRutaEscenaEditor(nombreORuta);
        if (!string.IsNullOrEmpty(rutaEditor) && Application.isPlaying)
        {
            EditorSceneManager.LoadSceneInPlayMode(rutaEditor, new LoadSceneParameters(LoadSceneMode.Single));
            return true;
        }
#endif
        return false;
    }

    public static void RecargarEscenaActual()
    {
        PrepararCambioEscena();
        Scene activa = SceneManager.GetActiveScene();

        if (activa.buildIndex >= 0)
        {
            SceneManager.LoadScene(activa.buildIndex);
            return;
        }

#if UNITY_EDITOR
        if (Application.isPlaying && !string.IsNullOrEmpty(activa.path) && File.Exists(activa.path))
        {
            EditorSceneManager.LoadSceneInPlayMode(activa.path, new LoadSceneParameters(LoadSceneMode.Single));
            return;
        }
#endif
        IntentarCargar(activa.name);
    }

    public static void AsegurarEventSystemInputSystem()
    {
        var es = Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include);
        if (es == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            return;
        }

        if (!es.gameObject.activeSelf)
            es.gameObject.SetActive(true);

        var legado = es.GetComponent<StandaloneInputModule>();
        if (legado != null)
        {
            legado.enabled = false;
            Object.Destroy(legado);
        }

        var modulo = es.GetComponent<InputSystemUIInputModule>();
        if (modulo == null)
            modulo = es.gameObject.AddComponent<InputSystemUIInputModule>();
        modulo.enabled = true;
    }

#if UNITY_EDITOR
    static string ResolverRutaEscenaEditor(string nombreORuta)
    {
        if (nombreORuta.EndsWith(".unity") && File.Exists(nombreORuta))
            return nombreORuta;

        string limpio = Path.GetFileNameWithoutExtension(nombreORuta);
        string[] candidatas =
        {
            $"Assets/_HastaElUltimoBarrio/Scenes/{limpio}.unity",
            $"Assets/_HastaElUltimoBarrio/Scenes/Niveles/{limpio}.unity",
            $"Assets/Scenes/{limpio}.unity"
        };

        foreach (string c in candidatas)
        {
            if (File.Exists(c)) return c;
        }

        string[] guids = AssetDatabase.FindAssets(limpio + " t:Scene", new[] { "Assets/_HastaElUltimoBarrio/Scenes" });
        foreach (string g in guids)
        {
            string ruta = AssetDatabase.GUIDToAssetPath(g);
            if (string.Equals(Path.GetFileNameWithoutExtension(ruta), limpio, System.StringComparison.OrdinalIgnoreCase))
                return ruta;
        }

        return string.Empty;
    }
#endif
}
