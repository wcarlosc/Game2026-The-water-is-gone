using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Construye por codigo MenuPrincipal y los niveles 3D isometricos de superficie (Menu + LVL_07..09)
// usando prefabs reales de Kit_Pueblo, sin tocar EditorBuildSettings ni las escenas 2.5D de main.
public static class ConstructorRed3D
{
    const string R = "Assets/_HastaElUltimoBarrio/";
    const string KIT = R + "Prefabs/Kit_Pueblo/";

    static int capaJugador, capaSeguro, capaPeligro, capaRecolectable, capaInteractuable;

    [MenuItem("Tools/Hasta el último barrio/Construir menú y niveles 7-9 (Red 3D)")]
    public static void ConstruirRed3DCompletaInteractivo()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EjecutarConstruccionCompleta();
    }

    // Punto de entrada sin dialogos para batchmode (-executeMethod ConstructorRed3D.ConstruirRed3DCompletaBatch).
    public static void ConstruirRed3DCompletaBatch()
    {
        EjecutarConstruccionCompleta();
    }

    [MenuItem("Tools/Hasta el último barrio/Construir menú y nivel 7 (Red 3D)")]
    public static void ConstruirMenuYNivel7Interactivo()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EjecutarConstruccionCompleta();
    }

    // Alias de compatibilidad para automatizaciones previas.
    public static void ConstruirMenuYNivel7Batch()
    {
        EjecutarConstruccionCompleta();
    }

    static void EjecutarConstruccionCompleta()
    {
        InicializarCapasYMateriales();
        AsegurarPrefabRover();
        ConstruirMenuPrincipal();
        ConstruirNivel7();
        ConstruirNivel8();
        ConstruirNivel9();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ConstructorRed3D] MenuPrincipal y LVL_07..LVL_09 generados correctamente.");
    }

    static void InicializarCapasYMateriales()
    {
        capaJugador = LayerMask.NameToLayer("Jugador");
        capaSeguro = LayerMask.NameToLayer("Seguro");
        capaPeligro = LayerMask.NameToLayer("Peligro");
        capaRecolectable = LayerMask.NameToLayer("Recolectable");
        capaInteractuable = LayerMask.NameToLayer("Interactuable");

        AsegurarMaterialSkyboxAmanecer();
        AsegurarMaterialLit("M_Tuberia_Seca", new Color(0.32f, 0.36f, 0.40f), 0.35f, 0.45f, false, Color.black);
        AsegurarMaterialLit("M_Chorro_Cian", new Color(0.25f, 0.88f, 0.96f), 0.8f, 0.1f, true, new Color(0.18f, 0.82f, 0.95f) * 2.4f);
        AsegurarMaterialLit("M_Alerta_Roja", new Color(0.95f, 0.28f, 0.20f), 0.4f, 0.2f, true, new Color(0.95f, 0.22f, 0.15f) * 2.0f);
        AsegurarMaterialLit("M_Salida_Lista", new Color(0.16f, 0.86f, 0.72f), 0.5f, 0.2f, true, new Color(0.12f, 0.85f, 0.75f) * 2.2f);
    }

    static Material AsegurarMaterialSkyboxAmanecer()
    {
        string p = R + "Art/Materials/M_Skybox_Superficie_Amanecer.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (m != null) return m;

        var sh = Shader.Find("Skybox/Procedural");
        m = new Material(sh != null ? sh : Shader.Find("Universal Render Pipeline/Lit"));
        AssetDatabase.CreateAsset(m, p);
        if (m.HasProperty("_SunSize")) m.SetFloat("_SunSize", 0.045f);
        if (m.HasProperty("_AtmosphereThickness")) m.SetFloat("_AtmosphereThickness", 1.30f);
        if (m.HasProperty("_SkyTint")) m.SetColor("_SkyTint", new Color(0.54f, 0.66f, 0.82f));
        if (m.HasProperty("_GroundColor")) m.SetColor("_GroundColor", new Color(0.36f, 0.31f, 0.28f));
        if (m.HasProperty("_Exposure")) m.SetFloat("_Exposure", 1.1f);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material AsegurarMaterialLit(string nombre, Color baseCol, float smoothness, float metallic, bool emisivo, Color emision)
    {
        string p = R + "Art/Materials/" + nombre + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (m != null) return m;

        m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        AssetDatabase.CreateAsset(m, p);
        m.SetColor("_BaseColor", baseCol);
        m.SetFloat("_Smoothness", smoothness);
        m.SetFloat("_Metallic", metallic);
        if (emisivo)
        {
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.SetColor("_EmissionColor", emision);
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material CargarMaterial(string nombre) => AssetDatabase.LoadAssetAtPath<Material>(R + "Art/Materials/" + nombre + ".mat");

    static GameObject CargarKit(string ruta)
    {
        var go = AssetDatabase.LoadAssetAtPath<GameObject>(KIT + ruta + ".prefab");
        if (go == null) Debug.LogError("Falta prefab del kit: " + ruta);
        return go;
    }

    static GameObject Poner(string ruta, Vector3 pos, Vector3 euler, Vector3 escala, Transform padre)
    {
        var pf = CargarKit(ruta);
        if (pf == null) return null;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, padre);
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(euler));
        go.transform.localScale = escala;
        QuitarColliders(go);
        return go;
    }

    static GameObject PonerConBoxAjustado(string ruta, Vector3 pos, Vector3 euler, Vector3 escala, Transform padre, int capa = -1)
    {
        var go = Poner(ruta, pos, euler, escala, padre);
        if (go == null) return null;
        AgregarBoxAjustado(go, go, capa >= 0 ? capa : capaSeguro);
        return go;
    }

    static void AgregarBoxAjustado(GameObject destinoCollider, GameObject raizMallas, int capa)
    {
        if (destinoCollider == null || raizMallas == null) return;
        if (!TryCalcularBoundsLocales(destinoCollider.transform, raizMallas, out Bounds b)) return;

        var bc = destinoCollider.AddComponent<BoxCollider>();
        bc.center = b.center;
        bc.size = new Vector3(
            Mathf.Max(0.08f, b.size.x),
            Mathf.Max(0.08f, b.size.y),
            Mathf.Max(0.08f, b.size.z));
        if (capa >= 0) CapaRecursiva(destinoCollider, capa);
    }

    static bool TryCalcularBoundsLocales(Transform espacioDestino, GameObject raizMallas, out Bounds resultado)
    {
        resultado = default;
        bool iniciado = false;
        var filtros = raizMallas.GetComponentsInChildren<MeshFilter>(true);
        foreach (var mf in filtros)
        {
            if (mf.sharedMesh == null) continue;
            Bounds mb = mf.sharedMesh.bounds;
            Vector3 c = mb.center;
            Vector3 e = mb.extents;

            for (int sx = -1; sx <= 1; sx += 2)
            for (int sy = -1; sy <= 1; sy += 2)
            for (int sz = -1; sz <= 1; sz += 2)
            {
                Vector3 esquinaMalla = c + new Vector3(e.x * sx, e.y * sy, e.z * sz);
                Vector3 mundo = mf.transform.TransformPoint(esquinaMalla);
                Vector3 local = espacioDestino.InverseTransformPoint(mundo);
                if (!iniciado)
                {
                    resultado = new Bounds(local, Vector3.zero);
                    iniciado = true;
                }
                else
                {
                    resultado.Encapsulate(local);
                }
            }
        }
        return iniciado;
    }

    static void QuitarColliders(GameObject go)
    {
        foreach (var c in go.GetComponentsInChildren<Collider>(true))
            Object.DestroyImmediate(c, true);
    }

    static void DesactivarEstaticoRecursivo(GameObject go)
    {
        if (go == null) return;
        foreach (var t in go.GetComponentsInChildren<Transform>(true))
        {
            t.gameObject.isStatic = false;
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
        }
    }

    static void CapaRecursiva(GameObject go, int capa)
    {
        if (capa < 0) return;
        foreach (var t in go.GetComponentsInChildren<Transform>(true))
            t.gameObject.layer = capa;
    }

    static void AsegurarPrefabRover()
    {
        string p = R + "Prefabs/Personajes/PF_RoverSCADA.prefab";
        var raiz = new GameObject("PF_RoverSCADA");
        raiz.tag = "Player";

        var visual = new GameObject("Visual");
        visual.transform.SetParent(raiz.transform, false);

        var chasis = Poner("Obra/carretilla", Vector3.zero, Vector3.zero, new Vector3(0.95f, 0.85f, 0.95f), visual.transform);
        if (chasis != null) chasis.name = "Chasis";

        var caja = Poner("Obra/caja_herramientas", new Vector3(0f, 0.56f, -0.05f), Vector3.zero, Vector3.one * 0.9f, visual.transform);
        if (caja != null) caja.name = "ModuloSCADA";

        var tablet = Poner("Equipos/tablet", new Vector3(0f, 0.84f, 0.14f), new Vector3(35f, 0f, 0f), Vector3.one * 1.15f, visual.transform);
        if (tablet != null) tablet.name = "AntenaTablet";

        var baliza = Poner("Obra/baliza", new Vector3(0f, 0.80f, -0.22f), Vector3.zero, Vector3.one * 0.82f, visual.transform);
        if (baliza != null) baliza.name = "Baliza";

        var luz = new GameObject("LuzRover").AddComponent<Light>();
        luz.transform.SetParent(visual.transform, false);
        luz.transform.localPosition = new Vector3(0f, 1.25f, 0.1f);
        luz.type = LightType.Point;
        luz.color = new Color(1f, 0.65f, 0.18f);
        luz.range = 4.5f;
        luz.intensity = 1.6f;
        luz.shadows = LightShadows.None;

        var rb = raiz.AddComponent<Rigidbody>();
        rb.mass = 1.5f;
        rb.useGravity = true;
        rb.linearDamping = 0.5f;
        rb.angularDamping = 6f;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        var cap = raiz.AddComponent<CapsuleCollider>();
        cap.direction = 1;
        cap.center = new Vector3(0f, 0.58f, 0f);
        cap.height = 1.12f;
        cap.radius = 0.44f;

        string pmPath = R + "Art/Materials/PM_Rover.asset";
        var pm = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(pmPath);
        if (pm == null)
        {
            pm = new PhysicsMaterial("PM_Rover") { dynamicFriction = 0f, staticFriction = 0f, bounciness = 0f, frictionCombine = PhysicsMaterialCombine.Minimum };
            AssetDatabase.CreateAsset(pm, pmPath);
        }
        cap.sharedMaterial = pm;

        var imp = raiz.AddComponent<CinemachineImpulseSource>();
        imp.ImpulseDefinition.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump;
        imp.ImpulseDefinition.ImpulseDuration = 0.25f;
        imp.DefaultVelocity = new Vector3(0.25f, 0.25f, 0.25f);

        var rc = raiz.AddComponent<RoverController>();
        rc.visual = visual.transform;
        rc.sacudida = imp;

        // Quita BatchingStatic/StaticEditorFlags heredados de Kit_Pueblo para que la malla siga al Rigidbody
        DesactivarEstaticoRecursivo(raiz);
        CapaRecursiva(raiz, capaJugador);
        PrefabUtility.SaveAsPrefabAsset(raiz, p);
        Object.DestroyImmediate(raiz);
    }

    static void ConfigurarIluminacionSuperficie()
    {
        var sky = CargarMaterial("M_Skybox_Superficie_Amanecer");
        if (sky != null) RenderSettings.skybox = sky;

        var sol = Object.FindAnyObjectByType<Light>();
        if (sol != null && sol.type == LightType.Directional)
        {
            sol.name = "Luz_Sol_Andino";
            sol.transform.rotation = Quaternion.Euler(46f, -32f, 0f);
            sol.color = new Color(1f, 0.93f, 0.84f);
            sol.intensity = 1.35f;
            sol.shadows = LightShadows.Soft;
            sol.shadowStrength = 0.55f;
        }

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.56f, 0.64f, 0.74f);
        RenderSettings.ambientEquatorColor = new Color(0.45f, 0.42f, 0.39f);
        RenderSettings.ambientGroundColor = new Color(0.22f, 0.20f, 0.19f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.62f, 0.67f, 0.72f);
        RenderSettings.fogStartDistance = 42f;
        RenderSettings.fogEndDistance = 125f;

        var perfil = AssetDatabase.LoadAssetAtPath<VolumeProfile>(R + "Settings/Volumes/Settings_Volumen_Tunel.asset");
        if (perfil != null)
        {
            var vol = new GameObject("Volumen_Global").AddComponent<Volume>();
            vol.isGlobal = true;
            vol.sharedProfile = perfil;
        }
    }

    // ================================================================== MENU PRINCIPAL
    public static void ConstruirMenuPrincipal()
    {
        var escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var esc = new GameObject("VinyetaLavadero").transform;

        // Piso 16x16 m
        for (int x = -8; x < 8; x += 2)
        {
            for (int z = -8; z < 8; z += 2)
                Poner("Suelos/suelo_losas_piedra", new Vector3(x + 1f, 0f, z + 1f), Vector3.zero, Vector3.one, esc);
        }

        Poner("Equipos/lavadero", new Vector3(2.8f, 0f, -0.8f), new Vector3(0f, -155f, 0f), Vector3.one * 1.15f, esc);
        Poner("Edificios/casa_ladrillo", new Vector3(5.2f, 0f, 3.8f), new Vector3(0f, 180f, 0f), Vector3.one, esc);
        Poner("Edificios/clinica", new Vector3(-3.2f, 0f, 5.2f), new Vector3(0f, 15f, 0f), Vector3.one, esc);
        Poner("Edificios/fuente", new Vector3(0.4f, -0.4f, 6.8f), Vector3.zero, Vector3.one, esc);
        Poner("Naturaleza/eucalipto", new Vector3(6.6f, 0f, -2.5f), Vector3.zero, Vector3.one * 1.2f, esc);
        Poner("Naturaleza/montana", new Vector3(6f, -2f, 44f), new Vector3(0f, 25f, 0f), new Vector3(5.5f, 4.2f, 5.5f), esc);

        var cam = Camera.main;
        cam.transform.SetPositionAndRotation(new Vector3(-4.2f, 4.0f, -8.5f), Quaternion.Euler(17f, 24f, 0f));
        cam.fieldOfView = 34f;
        cam.farClipPlane = 180f;
        cam.clearFlags = CameraClearFlags.Skybox;

        ConfigurarIluminacionSuperficie();

        var ui = new GameObject("UI_MenuPrincipal").AddComponent<MenuPrincipalUI>();
        ui.escenaIniciarMision = "LVL_01_GaleriaCaptacion";

        EditorSceneManager.SaveScene(escena, R + "Scenes/MenuPrincipal.unity");
    }

    // ================================================================== NIVEL 7 (AISLAMIENTO DE FUGA)
    public static void ConstruirNivel7()
    {
        var escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var raizPiso = new GameObject("Piso").transform;
        var raizEntorno = new GameObject("Entorno").transform;
        var raizTuberias = new GameObject("Tuberias").transform;
        var raizValvulas = new GameObject("Valvulas").transform;
        var raizObstaculos = new GameObject("ObstaculosMoviles").transform;
        var raizBaterias = new GameObject("Baterias").transform;

        // Piso continuo 28x24 m (losas decorativas + un BoxCollider uniforme sin juntas)
        for (int x = -14; x < 14; x += 2)
        {
            for (int z = -12; z < 12; z += 2)
            {
                string losa = (Mathf.Abs(x) <= 6 && Mathf.Abs(z) <= 6) ? "Suelos/suelo_losas_ocre" : "Suelos/suelo_losas_piedra";
                Poner(losa, new Vector3(x + 1f, 0f, z + 1f), Vector3.zero, Vector3.one, raizPiso);
            }
        }
        var colPiso = new GameObject("ColliderPiso");
        colPiso.transform.SetParent(raizPiso, false);
        colPiso.layer = capaSeguro >= 0 ? capaSeguro : 0;
        var bcPiso = colPiso.AddComponent<BoxCollider>();
        bcPiso.center = new Vector3(0f, -0.25f, 0f);
        bcPiso.size = new Vector3(28f, 0.5f, 24f);

        // Muros perimetrales (colliders estrictos al ancho/alto real del muro)
        AgregarMuroPerimetral(raizEntorno, new Vector3(0f, 0f, 12.2f), new Vector3(28.4f, 1.2f, 0.5f), horizontal: true, largo: 28);
        AgregarMuroPerimetral(raizEntorno, new Vector3(0f, 0f, -12.2f), new Vector3(28.4f, 1.2f, 0.5f), horizontal: true, largo: 28);
        AgregarMuroPerimetral(raizEntorno, new Vector3(-14.2f, 0f, 0f), new Vector3(0.5f, 1.2f, 24.4f), horizontal: false, largo: 24);
        AgregarMuroPerimetral(raizEntorno, new Vector3(14.2f, 0f, 0f), new Vector3(0.5f, 1.2f, 24.4f), horizontal: false, largo: 24);

        // Edificios e infraestructura del pueblo (BoxCollider ajustado estrictamente a su volumen visible)
        PonerConBoxAjustado("Equipos/tanque", new Vector3(-10.5f, 0f, 8.2f), Vector3.zero, Vector3.one * 1.15f, raizEntorno, capaSeguro);
        PonerConBoxAjustado("Equipos/bomba_agua", new Vector3(-8.0f, 0f, 8.2f), new Vector3(0f, 90f, 0f), Vector3.one * 1.25f, raizEntorno, capaSeguro);
        PonerConBoxAjustado("Edificios/clinica", new Vector3(9.0f, 0f, 6.2f), new Vector3(0f, -90f, 0f), Vector3.one * 0.95f, raizEntorno, capaSeguro);
        PonerConBoxAjustado("Edificios/casa_ladrillo", new Vector3(-9.5f, 0f, -8.5f), new Vector3(0f, 90f, 0f), Vector3.one * 0.9f, raizEntorno, capaSeguro);
        PonerConBoxAjustado("Edificios/fuente", new Vector3(0.5f, 0f, -7.8f), Vector3.zero, Vector3.one, raizEntorno, capaSeguro);
        PonerConBoxAjustado("Equipos/lavadero", new Vector3(4.5f, 0f, -8.5f), new Vector3(0f, 180f, 0f), Vector3.one * 1.15f, raizEntorno, capaSeguro);
        PonerConBoxAjustado("Mercado/puesto", new Vector3(-1.5f, 0f, 8.5f), Vector3.zero, Vector3.one, raizEntorno, capaSeguro);

        foreach (var posEuc in new[] { new Vector3(-12.2f, 0f, 2.5f), new Vector3(12.2f, 0f, -2.5f) })
        {
            var euc = Poner("Naturaleza/eucalipto", posEuc, Vector3.zero, Vector3.one * 1.1f, raizEntorno);
            if (euc != null)
            {
                var bcTronco = euc.AddComponent<BoxCollider>();
                bcTronco.center = new Vector3(0f, 1.1f, 0f);
                bcTronco.size = new Vector3(0.5f, 2.2f, 0.5f);
                CapaRecursiva(euc, capaSeguro);
            }
        }
        Poner("Naturaleza/montana", new Vector3(18f, -3f, 48f), new Vector3(0f, 35f, 0f), new Vector3(6f, 4.5f, 6f), raizEntorno);

        Material matSeca = CargarMaterial("M_Tuberia_Seca");
        Material matCian = CargarMaterial("M_Tuberia_Cian");
        Material matChorro = CargarMaterial("M_Chorro_Cian");
        Material matAmbar = CargarMaterial("M_Ambar_Emisivo");
        Material matRoja = CargarMaterial("M_Alerta_Roja");
        Material matSalida = CargarMaterial("M_Salida_Lista");

        // Tuberias troncales desde tanque (-7, 0.1, 8.2) hasta camara central (-5.5, 0.1, 2.5)
        var troncal = CrearTramoTuberias(raizTuberias, "Troncal", new[]
        {
            new Vector3(-6.8f, 0.05f, 8.2f),
            new Vector3(-5.8f, 0.05f, 6.2f),
            new Vector3(-5.8f, 0.05f, 4.2f)
        }, matCian);

        // Tramo de fuga rota hacia (-3.0, 0.05, 1.5)
        PonerConBoxAjustado("Ruinas_e_industria/tubo_roto_base", new Vector3(-3.0f, 0f, 1.6f), new Vector3(0f, 90f, 0f), Vector3.one * 1.25f, raizTuberias, capaSeguro);
        var visualFuga = CrearVisualFugaActiva(new Vector3(-3.0f, 0.35f, 1.6f), raizTuberias, matChorro);

        // Ramal hacia Centro de Salud (de (1.5, 0.05, 4.5) a (6.0, 0.05, 6.0))
        var tubSalud = CrearTramoTuberias(raizTuberias, "RamalSalud", new[]
        {
            new Vector3(1.8f, 0.05f, 4.5f),
            new Vector3(3.8f, 0.05f, 5.2f),
            new Vector3(5.8f, 0.05f, 5.8f)
        }, matSeca);

        // Ramal hacia Fuente y Lavadero (Sur)
        var tubLadera = CrearTramoTuberias(raizTuberias, "RamalSur", new[]
        {
            new Vector3(-2.5f, 0.05f, -3.5f),
            new Vector3(-0.5f, 0.05f, -5.2f),
            new Vector3(2.5f, 0.05f, -6.5f)
        }, matCian);

        // Valvula 1 (Campo): Aislamiento de Fuga (-5.5, 0, 2.2)
        var vFuga = CrearConsolaValvula(
            "Valvula_AislamientoFuga",
            "V_AislamientoFuga",
            "Aislamiento Rotura",
            new Vector3(-5.4f, 0f, 2.0f),
            esBinaria: true,
            aperturaObjetivo: 1f,
            raizValvulas,
            matSeca,
            matCian,
            visualFuga,
            null);

        // Valvula 2 (Campo): Paso Centro de Salud (0.8, 0, 4.2)
        var vSalud = CrearConsolaValvula(
            "Valvula_CentroSalud",
            "V3_Salud",
            "Ramal Centro de Salud",
            new Vector3(0.6f, 0f, 4.2f),
            esBinaria: true,
            aperturaObjetivo: 0.65f,
            raizValvulas,
            matSeca,
            matCian,
            null,
            tubSalud);

        // Valvula 3 (Campo): Derivacion Ladera / Refugio (-2.2, 0, -2.5)
        var vRefugio = CrearConsolaValvula(
            "Valvula_Refugio",
            "V4_Refugio",
            "Ramal Refugio",
            new Vector3(-2.6f, 0f, -2.4f),
            esBinaria: false,
            aperturaObjetivo: 0.60f,
            raizValvulas,
            matSeca,
            matCian,
            null,
            tubLadera);

        // Obstaculos moviles en superficie (Regla 5)
        CrearGeiserMovil(raizObstaculos, "Geiser_Cruce_1", new Vector3(-0.8f, 0f, 0.2f), 0.0f, matAmbar, matChorro);
        CrearGeiserMovil(raizObstaculos, "Geiser_Cruce_2", new Vector3(3.4f, 0f, 0.8f), 1.7f, matAmbar, matChorro);
        CrearBarreraOscilante(raizObstaculos, "Barrera_Obra_Salud", new Vector3(5.5f, 0f, -1.8f), new Vector3(0f, 0f, 4.2f), 1.4f, raizObstaculos);

        // Baterias de refuerzo en desvios laterales
        var pfBat = AssetDatabase.LoadAssetAtPath<GameObject>(R + "Prefabs/Kit_Tunel/PF_Bateria.prefab");
        if (pfBat != null)
        {
            foreach (var pos in new[] { new Vector3(-11.0f, 0.65f, 4.5f), new Vector3(1.5f, 0.65f, 8.8f), new Vector3(9.8f, 0.65f, -0.8f) })
            {
                var b = (GameObject)PrefabUtility.InstantiatePrefab(pfBat, raizBaterias);
                b.transform.position = pos;
            }
        }

        // Zona de salida en esquina Sureste
        var salidaGo = Poner("Juego/plataforma_diana", new Vector3(9.5f, 0.02f, -7.8f), Vector3.zero, Vector3.one * 1.35f, raizEntorno);
        salidaGo.name = "PF_ZonaSalida";
        salidaGo.tag = "Finish";
        CapaRecursiva(salidaGo, capaSeguro);
        var bcSal = salidaGo.AddComponent<BoxCollider>();
        bcSal.isTrigger = true;
        bcSal.center = new Vector3(0f, 0.6f, 0f);
        bcSal.size = new Vector3(2.6f, 1.2f, 2.6f);

        var anillo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        anillo.name = "IndicadorSalida";
        anillo.transform.SetParent(salidaGo.transform, false);
        anillo.transform.localPosition = new Vector3(0f, 0.25f, 0f);
        anillo.transform.localScale = new Vector3(1.8f, 0.05f, 1.8f);
        Object.DestroyImmediate(anillo.GetComponent<Collider>());
        var renSal = anillo.GetComponent<Renderer>();
        renSal.sharedMaterial = matRoja;

        // Rover SCADA + PlayerStart
        Vector3 inicioRover = new Vector3(-10.2f, 0.1f, -4.5f);
        var pfRover = AssetDatabase.LoadAssetAtPath<GameObject>(R + "Prefabs/Personajes/PF_RoverSCADA.prefab");
        var rover = (GameObject)PrefabUtility.InstantiatePrefab(pfRover);
        rover.name = "RoverSCADA";
        rover.transform.position = inicioRover;

        var start = new GameObject("PlayerStart");
        start.tag = "Respawn";
        start.transform.position = inicioRover;

        // Camara isometrica Cinemachine
        var cams = new GameObject("Camaras").transform;
        var main = Camera.main;
        main.transform.SetParent(cams, true);
        main.transform.SetPositionAndRotation(inicioRover + new Vector3(-11f, 16f, -15f), Quaternion.Euler(42f, 35f, 0f));
        main.fieldOfView = 28f;
        main.farClipPlane = 200f;
        main.clearFlags = CameraClearFlags.Skybox;
        if (main.GetComponent<CinemachineBrain>() == null) main.gameObject.AddComponent<CinemachineBrain>();

        var cmGo = new GameObject("CM_Isometrica");
        cmGo.transform.SetParent(cams, false);
        cmGo.transform.SetPositionAndRotation(main.transform.position, Quaternion.Euler(42f, 35f, 0f));
        var cm = cmGo.AddComponent<CinemachineCamera>();
        cm.Follow = rover.transform;
        var lens = cm.Lens;
        lens.FieldOfView = 28f;
        lens.NearClipPlane = 0.3f;
        lens.FarClipPlane = 200f;
        cm.Lens = lens;

        var pc = cmGo.AddComponent<CinemachinePositionComposer>();
        pc.CameraDistance = 23f;
        pc.Damping = new Vector3(0.45f, 0.45f, 0.45f);
        cmGo.AddComponent<CinemachineImpulseListener>();

        // Sistemas de escena
        var sis = new GameObject("Sistemas").transform;
        var lm = new GameObject("LevelManager").AddComponent<LevelManager>();
        lm.transform.SetParent(sis, false);
        lm.nombreNivel = "Calle de la fuga";
        lm.objetivo = "Cierra la rotura [E], abre el paso al Centro de Salud [E] y confirma la presurización [P].";
        lm.siguienteEscena = "LVL_08_DepositoRegulacion";

        var gestor = new GameObject("GestorRedSuperficie").AddComponent<GestorRedSuperficie>();
        gestor.transform.SetParent(sis, false);
        gestor.modoEscenario = ModoRedSuperficie.Nivel07_AislamientoFuga;
        gestor.caudalFuente = 100f;
        gestor.permitirRegulacionRemotaEnHUD = false;
        gestor.valvulasEscena = new[] { vFuga, vSalud, vRefugio };
        gestor.indicadorZonaSalida = renSal;
        gestor.matSalidaPendiente = matRoja;
        gestor.matSalidaLista = matSalida;

        var hud = new GameObject("HUDRed3D").AddComponent<HUDRed3D>();
        hud.transform.SetParent(sis, false);
        hud.mostrarTarjetasImpacto = false;

        ConfigurarIluminacionSuperficie();
        EditorSceneManager.SaveScene(escena, R + "Scenes/Niveles/LVL_07_AislamientoFuga.unity");
    }

    // ================================================================== NIVEL 8 (DEPOSITO DE REGULACION ALTITUDINAL)
    public static void ConstruirNivel8()
    {
        var escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var raizPiso = new GameObject("Piso").transform;
        var raizEntorno = new GameObject("Entorno").transform;
        var raizTuberias = new GameObject("Tuberias").transform;
        var raizValvulas = new GameObject("Valvulas").transform;
        var raizObstaculos = new GameObject("ObstaculosMoviles").transform;
        var raizBaterias = new GameObject("Baterias").transform;

        // Piso continuo 30x26 m
        for (int x = -15; x < 15; x += 2)
        {
            for (int z = -13; z < 13; z += 2)
            {
                string losa = (Mathf.Abs(x) <= 7 && Mathf.Abs(z) <= 7) ? "Suelos/suelo_losas_azul" : "Suelos/suelo_losas_piedra";
                Poner(losa, new Vector3(x + 1f, 0f, z + 1f), Vector3.zero, Vector3.one, raizPiso);
            }
        }
        var colPiso = new GameObject("ColliderPiso");
        colPiso.transform.SetParent(raizPiso, false);
        colPiso.layer = capaSeguro >= 0 ? capaSeguro : 0;
        var bcPiso = colPiso.AddComponent<BoxCollider>();
        bcPiso.center = new Vector3(0f, -0.25f, 0f);
        bcPiso.size = new Vector3(30f, 0.5f, 26f);

        AgregarMuroPerimetral(raizEntorno, new Vector3(0f, 0f, 13.2f), new Vector3(30.4f, 1.2f, 0.5f), horizontal: true, largo: 30);
        AgregarMuroPerimetral(raizEntorno, new Vector3(0f, 0f, -13.2f), new Vector3(30.4f, 1.2f, 0.5f), horizontal: true, largo: 30);
        AgregarMuroPerimetral(raizEntorno, new Vector3(-15.2f, 0f, 0f), new Vector3(0.5f, 1.2f, 26.4f), horizontal: false, largo: 26);
        AgregarMuroPerimetral(raizEntorno, new Vector3(15.2f, 0f, 0f), new Vector3(0.5f, 1.2f, 26.4f), horizontal: false, largo: 26);

        // Infraestructura y sectores con distinta resistencia altitudinal (Mercado 4 m vs Ladera 42 m)
        PonerConBoxAjustado("Equipos/tanque", new Vector3(-11.5f, 0f, 9.2f), Vector3.zero, Vector3.one * 1.2f, raizEntorno, capaSeguro);
        PonerConBoxAjustado("Equipos/bomba_agua", new Vector3(-8.5f, 0f, 9.2f), new Vector3(0f, 90f, 0f), Vector3.one * 1.2f, raizEntorno, capaSeguro);
        PonerConBoxAjustado("Mercado/puesto", new Vector3(-10.5f, 0f, -9.2f), Vector3.zero, Vector3.one, raizEntorno, capaSeguro);
        PonerConBoxAjustado("Mercado/puesto", new Vector3(-6.8f, 0f, -9.5f), new Vector3(0f, 15f, 0f), Vector3.one, raizEntorno, capaSeguro);
        PonerConBoxAjustado("Mercado/garrafon", new Vector3(-4.8f, 0f, -9.2f), Vector3.zero, Vector3.one * 1.2f, raizEntorno, capaSeguro);

        PonerConBoxAjustado("Escaleras_y_plataformas/plataforma_2n", new Vector3(10.5f, 0f, 8.8f), Vector3.zero, Vector3.one * 1.15f, raizEntorno, capaSeguro);
        PonerConBoxAjustado("Edificios/casa_ladrillo", new Vector3(10.5f, 0f, 4.2f), new Vector3(0f, -90f, 0f), Vector3.one * 0.88f, raizEntorno, capaSeguro);
        PonerConBoxAjustado("Equipos/tanque", new Vector3(6.8f, 0f, 9.2f), Vector3.zero, Vector3.one * 1.05f, raizEntorno, capaSeguro);
        PonerConBoxAjustado("Edificios/marquesina", new Vector3(10.5f, 0f, -3.2f), new Vector3(0f, -90f, 0f), Vector3.one, raizEntorno, capaSeguro);

        foreach (var posArb in new[] { new Vector3(-13.0f, 0f, 1.5f), new Vector3(13.0f, 0f, 0.5f) })
        {
            var arb = Poner("Naturaleza/arbol", posArb, Vector3.zero, Vector3.one * 1.1f, raizEntorno);
            if (arb != null)
            {
                var bcTronco = arb.AddComponent<BoxCollider>();
                bcTronco.center = new Vector3(0f, 1.1f, 0f);
                bcTronco.size = new Vector3(0.5f, 2.2f, 0.5f);
                CapaRecursiva(arb, capaSeguro);
            }
        }
        Poner("Naturaleza/montana", new Vector3(16f, -3f, 50f), new Vector3(0f, 25f, 0f), new Vector3(6f, 4.5f, 6f), raizEntorno);

        Material matSeca = CargarMaterial("M_Tuberia_Seca");
        Material matCian = CargarMaterial("M_Tuberia_Cian");
        Material matAmbar = CargarMaterial("M_Ambar_Emisivo");
        Material matChorro = CargarMaterial("M_Chorro_Cian");
        Material matRoja = CargarMaterial("M_Alerta_Roja");
        Material matSalida = CargarMaterial("M_Salida_Lista");

        // Tuberias con BoxCollider ajustado
        CrearTramoTuberias(raizTuberias, "Troncal_N8", new[]
        {
            new Vector3(-7.2f, 0.05f, 8.8f),
            new Vector3(-5.8f, 0.05f, 6.2f),
            new Vector3(-5.5f, 0.05f, 3.2f)
        }, matCian);

        var tubMercado = CrearTramoTuberias(raizTuberias, "RamalMercado_N8", new[]
        {
            new Vector3(-5.8f, 0.05f, -4.6f),
            new Vector3(-7.2f, 0.05f, -6.5f)
        }, matCian);

        var tubLadera = CrearTramoTuberias(raizTuberias, "RamalLadera_N8", new[]
        {
            new Vector3(3.6f, 0.05f, 5.2f),
            new Vector3(5.6f, 0.05f, 6.6f)
        }, matSeca);

        // 2 Valvulas continuas (paso 0.20) para equilibrar ambos ramales de distinta altitud
        var vMercado = CrearConsolaValvula(
            "Valvula_RamalBajo_Mercado",
            "V1_Mercado",
            "Ramal Bajo Mercado (4 m)",
            new Vector3(-5.2f, 0f, -2.8f),
            esBinaria: false,
            aperturaObjetivo: 0.40f,
            raizValvulas,
            matSeca,
            matCian,
            null,
            tubMercado,
            pasoEscalon: 0.20f);

        var vLadera = CrearConsolaValvula(
            "Valvula_RamalAlto_Ladera",
            "V5_Ladera",
            "Ramal Alto Ladera (42 m)",
            new Vector3(2.2f, 0f, 3.8f),
            esBinaria: false,
            aperturaObjetivo: 1.00f,
            raizValvulas,
            matSeca,
            matCian,
            null,
            tubLadera,
            pasoEscalon: 0.20f);

        // Obstaculos moviles cinemáticos (Regla 5)
        CrearGeiserMovil(raizObstaculos, "Geiser_N8_1", new Vector3(-1.8f, 0f, 0.5f), 0.0f, matAmbar, matChorro);
        CrearGeiserMovil(raizObstaculos, "Geiser_N8_2", new Vector3(0.5f, 0f, -1.8f), 1.3f, matAmbar, matChorro);
        CrearGeiserMovil(raizObstaculos, "Geiser_N8_3", new Vector3(5.0f, 0f, 1.2f), 2.5f, matAmbar, matChorro);
        CrearBarreraOscilante(raizObstaculos, "Barrera_N8_Norte", new Vector3(-1.5f, 0f, 4.5f), new Vector3(4.2f, 0f, 0f), 1.35f, raizObstaculos);
        CrearBarreraOscilante(raizObstaculos, "Barrera_N8_Sur", new Vector3(3.8f, 0f, -4.8f), new Vector3(0f, 0f, 4.0f), 1.45f, raizObstaculos);

        // Baterias
        var pfBat = AssetDatabase.LoadAssetAtPath<GameObject>(R + "Prefabs/Kit_Tunel/PF_Bateria.prefab");
        if (pfBat != null)
        {
            foreach (var pos in new[] { new Vector3(-11.8f, 0.65f, -2.0f), new Vector3(0.0f, 0.65f, 8.8f), new Vector3(11.2f, 0.65f, -0.5f) })
            {
                var b = (GameObject)PrefabUtility.InstantiatePrefab(pfBat, raizBaterias);
                b.transform.position = pos;
            }
        }

        // Salida
        var salidaGo = Poner("Juego/plataforma_diana", new Vector3(10.2f, 0.02f, -8.5f), Vector3.zero, Vector3.one * 1.35f, raizEntorno);
        salidaGo.name = "PF_ZonaSalida";
        salidaGo.tag = "Finish";
        CapaRecursiva(salidaGo, capaSeguro);
        var bcSal = salidaGo.AddComponent<BoxCollider>();
        bcSal.isTrigger = true;
        bcSal.center = new Vector3(0f, 0.6f, 0f);
        bcSal.size = new Vector3(2.6f, 1.2f, 2.6f);

        var anillo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        anillo.name = "IndicadorSalida";
        anillo.transform.SetParent(salidaGo.transform, false);
        anillo.transform.localPosition = new Vector3(0f, 0.25f, 0f);
        anillo.transform.localScale = new Vector3(1.8f, 0.05f, 1.8f);
        Object.DestroyImmediate(anillo.GetComponent<Collider>());
        var renSal = anillo.GetComponent<Renderer>();
        renSal.sharedMaterial = matRoja;

        // Rover SCADA + PlayerStart
        Vector3 inicioRover = new Vector3(-11.2f, 0.1f, 3.8f);
        var pfRover = AssetDatabase.LoadAssetAtPath<GameObject>(R + "Prefabs/Personajes/PF_RoverSCADA.prefab");
        var rover = (GameObject)PrefabUtility.InstantiatePrefab(pfRover);
        rover.name = "RoverSCADA";
        rover.transform.position = inicioRover;

        var start = new GameObject("PlayerStart");
        start.tag = "Respawn";
        start.transform.position = inicioRover;

        ConfigurarCamaraIsometrica(rover.transform, inicioRover, 24f);

        var sis = new GameObject("Sistemas").transform;
        var lm = new GameObject("LevelManager").AddComponent<LevelManager>();
        lm.transform.SetParent(sis, false);
        lm.nombreNivel = "Depósito de regulación altitudinal";
        lm.objetivo = "Regula el ramal bajo (Mercado) y el ramal alto (Ladera) [E], simula [Q] y confirma [P].";
        lm.siguienteEscena = "LVL_09_PlazaControlEquidad";

        var gestor = new GameObject("GestorRedSuperficie").AddComponent<GestorRedSuperficie>();
        gestor.transform.SetParent(sis, false);
        gestor.modoEscenario = ModoRedSuperficie.Nivel08_DepositoRegulacion;
        gestor.caudalFuente = 100f;
        gestor.permitirRegulacionRemotaEnHUD = false;
        gestor.valvulasEscena = new[] { vMercado, vLadera };
        gestor.indicadorZonaSalida = renSal;
        gestor.matSalidaPendiente = matRoja;
        gestor.matSalidaLista = matSalida;

        var hud = new GameObject("HUDRed3D").AddComponent<HUDRed3D>();
        hud.transform.SetParent(sis, false);
        hud.mostrarTarjetasImpacto = false;

        ConfigurarIluminacionSuperficie();
        EditorSceneManager.SaveScene(escena, R + "Scenes/Niveles/LVL_08_DepositoRegulacion.unity");
    }

    // ================================================================== NIVEL 9 (PLAZA DE CONTROL DE EQUIDAD)
    public static void ConstruirNivel9()
    {
        var escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var raizPiso = new GameObject("Piso").transform;
        var raizEntorno = new GameObject("Entorno").transform;
        var raizTuberias = new GameObject("Tuberias").transform;
        var raizValvulas = new GameObject("Valvulas").transform;
        var raizObstaculos = new GameObject("ObstaculosMoviles").transform;
        var raizBaterias = new GameObject("Baterias").transform;

        // Gran Plaza 34x28 m
        for (int x = -17; x < 17; x += 2)
        {
            for (int z = -14; z < 14; z += 2)
            {
                string losa = (Mathf.Abs(x) <= 9 && Mathf.Abs(z) <= 7) ? "Suelos/suelo_losas_ocre" : "Suelos/suelo_losas_piedra";
                Poner(losa, new Vector3(x + 1f, 0f, z + 1f), Vector3.zero, Vector3.one, raizPiso);
            }
        }
        var colPiso = new GameObject("ColliderPiso");
        colPiso.transform.SetParent(raizPiso, false);
        colPiso.layer = capaSeguro >= 0 ? capaSeguro : 0;
        var bcPiso = colPiso.AddComponent<BoxCollider>();
        bcPiso.center = new Vector3(0f, -0.25f, 0f);
        bcPiso.size = new Vector3(34f, 0.5f, 28f);

        AgregarMuroPerimetral(raizEntorno, new Vector3(0f, 0f, 14.2f), new Vector3(34.4f, 1.2f, 0.5f), horizontal: true, largo: 34);
        AgregarMuroPerimetral(raizEntorno, new Vector3(0f, 0f, -14.2f), new Vector3(34.4f, 1.2f, 0.5f), horizontal: true, largo: 34);
        AgregarMuroPerimetral(raizEntorno, new Vector3(-17.2f, 0f, 0f), new Vector3(0.5f, 1.2f, 28.4f), horizontal: false, largo: 28);
        AgregarMuroPerimetral(raizEntorno, new Vector3(17.2f, 0f, 0f), new Vector3(0.5f, 1.2f, 28.4f), horizontal: false, largo: 28);

        // Hitos de los 5 barrios en la plaza con BoxCollider estricto
        PonerConBoxAjustado("Mercado/puesto", new Vector3(-12.5f, 0f, -9.8f), Vector3.zero, Vector3.one, raizEntorno, capaSeguro);
        PonerConBoxAjustado("Mercado/puesto", new Vector3(-9.0f, 0f, -10.2f), new Vector3(0f, 20f, 0f), Vector3.one, raizEntorno, capaSeguro);
        PonerConBoxAjustado("Puente/puente_tramo_arco", new Vector3(-13.2f, 0f, 8.8f), Vector3.zero, Vector3.one * 0.9f, raizEntorno, capaSeguro);
        PonerConBoxAjustado("Equipos/tanque", new Vector3(-9.5f, 0f, 10.2f), Vector3.zero, Vector3.one * 1.15f, raizEntorno, capaSeguro);
        PonerConBoxAjustado("Edificios/clinica", new Vector3(0.0f, 0f, 10.5f), Vector3.zero, Vector3.one * 0.9f, raizEntorno, capaSeguro);
        PonerConBoxAjustado("Edificios/casa_ladrillo", new Vector3(11.8f, 0f, 9.5f), new Vector3(0f, -90f, 0f), Vector3.one * 0.9f, raizEntorno, capaSeguro);
        PonerConBoxAjustado("Escaleras_y_plataformas/plataforma_2n", new Vector3(13.5f, 0f, 2.2f), Vector3.zero, Vector3.one * 1.1f, raizEntorno, capaSeguro);
        PonerConBoxAjustado("Equipos/bomba_agua", new Vector3(13.2f, 0f, -1.5f), new Vector3(0f, -90f, 0f), Vector3.one * 1.2f, raizEntorno, capaSeguro);

        // Fuente y Lavadero Comunitario junto a la meta (se iluminan con agua al ganar)
        PonerConBoxAjustado("Edificios/fuente", new Vector3(5.5f, 0f, -8.8f), Vector3.zero, Vector3.one, raizEntorno, capaSeguro);
        PonerConBoxAjustado("Equipos/lavadero", new Vector3(9.2f, 0f, -9.2f), new Vector3(0f, 180f, 0f), Vector3.one * 1.25f, raizEntorno, capaSeguro);

        foreach (var posEuc in new[] { new Vector3(-14.5f, 0f, -2.5f), new Vector3(14.5f, 0f, 5.8f) })
        {
            var euc = Poner("Naturaleza/eucalipto", posEuc, Vector3.zero, Vector3.one * 1.1f, raizEntorno);
            if (euc != null)
            {
                var bcTronco = euc.AddComponent<BoxCollider>();
                bcTronco.center = new Vector3(0f, 1.1f, 0f);
                bcTronco.size = new Vector3(0.5f, 2.2f, 0.5f);
                CapaRecursiva(euc, capaSeguro);
            }
        }
        Poner("Naturaleza/montana", new Vector3(20f, -3f, 56f), new Vector3(0f, 35f, 0f), new Vector3(6.5f, 4.8f, 6.5f), raizEntorno);

        Material matSeca = CargarMaterial("M_Tuberia_Seca");
        Material matCian = CargarMaterial("M_Tuberia_Cian");
        Material matChorro = CargarMaterial("M_Chorro_Cian");
        Material matAmbar = CargarMaterial("M_Ambar_Emisivo");
        Material matRoja = CargarMaterial("M_Alerta_Roja");
        Material matSalida = CargarMaterial("M_Salida_Lista");

        // Efecto visual del lavadero comunitario + fuente activos al equilibrar la red
        var aguaLavadero = CrearEfectoAguaLavadero(raizEntorno, matChorro);

        // Fuga activa en derivacion antigua
        PonerConBoxAjustado("Ruinas_e_industria/tubo_roto_base", new Vector3(-6.5f, 0f, 5.8f), new Vector3(0f, 90f, 0f), Vector3.one * 1.2f, raizTuberias, capaSeguro);
        var visualFuga = CrearVisualFugaActiva(new Vector3(-6.5f, 0.35f, 5.8f), raizTuberias, matChorro);

        // Tuberias por sector + tuberia del lavadero comunitario
        var tubMercado = CrearTramoTuberias(raizTuberias, "Mercado_N9", new[] { new Vector3(-9.5f, 0.05f, -7.2f), new Vector3(-11.0f, 0.05f, -8.2f) }, matCian);
        var tubPuente = CrearTramoTuberias(raizTuberias, "Puente_N9", new[] { new Vector3(-6.2f, 0.05f, 3.2f), new Vector3(-9.5f, 0.05f, 6.8f) }, matCian);
        var tubSalud = CrearTramoTuberias(raizTuberias, "Salud_N9", new[] { new Vector3(-1.0f, 0.05f, 7.2f), new Vector3(1.0f, 0.05f, 7.8f) }, matCian);
        var tubRefugio = CrearTramoTuberias(raizTuberias, "Refugio_N9", new[] { new Vector3(7.6f, 0.05f, 6.6f), new Vector3(9.4f, 0.05f, 7.6f) }, matCian);
        var tubLadera = CrearTramoTuberias(raizTuberias, "Ladera_N9", new[] { new Vector3(10.2f, 0.05f, 0.8f), new Vector3(11.8f, 0.05f, 0.5f) }, matSeca);
        var tubLavadero = CrearTramoTuberias(raizTuberias, "Lavadero_N9", new[] { new Vector3(3.2f, 0.05f, -7.8f), new Vector3(6.8f, 0.05f, -8.2f) }, matSeca);

        // 6 Consolas: 1 de rotura + 5 sectores con paso fino 0.10
        var vFuga = CrearConsolaValvula("Valvula_AislamientoFuga", "V_AislamientoFuga", "Aislamiento Rotura",
            new Vector3(-8.5f, 0f, 4.8f), esBinaria: true, aperturaObjetivo: 1f, raizValvulas, matSeca, matCian, visualFuga, null);
        var vMercado = CrearConsolaValvula("Valvula_Mercado", "V1_Mercado", "Ramal Mercado (4 m)",
            new Vector3(-8.2f, 0f, -5.8f), esBinaria: false, aperturaObjetivo: 0.30f, raizValvulas, matSeca, matCian, null, tubMercado, 0.10f);
        var vPuente = CrearConsolaValvula("Valvula_Puente", "V2_Puente", "Ramal Puente (10 m)",
            new Vector3(-4.2f, 0f, 1.8f), esBinaria: false, aperturaObjetivo: 0.40f, raizValvulas, matSeca, matCian, null, tubPuente, 0.10f);
        var vSalud = CrearConsolaValvula("Valvula_Salud", "V3_Salud", "Ramal Salud (18 m)",
            new Vector3(0.0f, 0f, 5.5f), esBinaria: false, aperturaObjetivo: 0.60f, raizValvulas, matSeca, matCian, null, tubSalud, 0.10f);
        var vRefugio = CrearConsolaValvula("Valvula_Refugio", "V4_Refugio", "Ramal Refugio (26 m)",
            new Vector3(5.8f, 0f, 5.2f), esBinaria: false, aperturaObjetivo: 0.60f, raizValvulas, matSeca, matCian, null, tubRefugio, 0.10f);
        var vLadera = CrearConsolaValvula("Valvula_Ladera", "V5_Ladera", "Ramal Ladera (42 m)",
            new Vector3(8.5f, 0f, 1.2f), esBinaria: false, aperturaObjetivo: 1.00f, raizValvulas, matSeca, matCian, null, tubLadera, 0.10f);

        // Obstaculos moviles (Regla 5)
        CrearGeiserMovil(raizObstaculos, "Geiser_N9_1", new Vector3(-5.2f, 0f, 1.5f), 0.0f, matAmbar, matChorro);
        CrearGeiserMovil(raizObstaculos, "Geiser_N9_2", new Vector3(-0.8f, 0f, -1.2f), 1.1f, matAmbar, matChorro);
        CrearGeiserMovil(raizObstaculos, "Geiser_N9_3", new Vector3(3.2f, 0f, 1.8f), 2.2f, matAmbar, matChorro);
        CrearGeiserMovil(raizObstaculos, "Geiser_N9_4", new Vector3(4.2f, 0f, -3.8f), 0.7f, matAmbar, matChorro);
        CrearBarreraOscilante(raizObstaculos, "Barrera_N9_Oeste", new Vector3(-6.5f, 0f, -1.8f), new Vector3(4.4f, 0f, 0f), 1.4f, raizObstaculos);
        CrearBarreraOscilante(raizObstaculos, "Barrera_N9_Este", new Vector3(7.2f, 0f, -3.5f), new Vector3(0f, 0f, 4.2f), 1.5f, raizObstaculos);

        // Baterias
        var pfBat = AssetDatabase.LoadAssetAtPath<GameObject>(R + "Prefabs/Kit_Tunel/PF_Bateria.prefab");
        if (pfBat != null)
        {
            foreach (var pos in new[] { new Vector3(-13.2f, 0.65f, 2.5f), new Vector3(-3.5f, 0.65f, 8.8f), new Vector3(8.5f, 0.65f, 7.2f), new Vector3(1.2f, 0.65f, -9.2f) })
            {
                var b = (GameObject)PrefabUtility.InstantiatePrefab(pfBat, raizBaterias);
                b.transform.position = pos;
            }
        }

        // Zona de salida junto al lavadero comunitario
        var salidaGo = Poner("Juego/plataforma_diana", new Vector3(12.2f, 0.02f, -9.2f), Vector3.zero, Vector3.one * 1.35f, raizEntorno);
        salidaGo.name = "PF_ZonaSalida";
        salidaGo.tag = "Finish";
        CapaRecursiva(salidaGo, capaSeguro);
        var bcSal = salidaGo.AddComponent<BoxCollider>();
        bcSal.isTrigger = true;
        bcSal.center = new Vector3(0f, 0.6f, 0f);
        bcSal.size = new Vector3(2.6f, 1.2f, 2.6f);

        var anillo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        anillo.name = "IndicadorSalida";
        anillo.transform.SetParent(salidaGo.transform, false);
        anillo.transform.localPosition = new Vector3(0f, 0.25f, 0f);
        anillo.transform.localScale = new Vector3(1.8f, 0.05f, 1.8f);
        Object.DestroyImmediate(anillo.GetComponent<Collider>());
        var renSal = anillo.GetComponent<Renderer>();
        renSal.sharedMaterial = matRoja;

        // Rover SCADA + PlayerStart
        Vector3 inicioRover = new Vector3(-13.2f, 0.1f, -4.5f);
        var pfRover = AssetDatabase.LoadAssetAtPath<GameObject>(R + "Prefabs/Personajes/PF_RoverSCADA.prefab");
        var rover = (GameObject)PrefabUtility.InstantiatePrefab(pfRover);
        rover.name = "RoverSCADA";
        rover.transform.position = inicioRover;

        var start = new GameObject("PlayerStart");
        start.tag = "Respawn";
        start.transform.position = inicioRover;

        ConfigurarCamaraIsometrica(rover.transform, inicioRover, 26f);

        var sis = new GameObject("Sistemas").transform;
        var lm = new GameObject("LevelManager").AddComponent<LevelManager>();
        lm.transform.SetParent(sis, false);
        lm.nombreNivel = "Plaza de control de equidad";
        lm.objetivo = "Aísla la fuga en campo [E], simula [Q] y equilibra los 5 barrios [P] para activar el lavadero.";
        lm.siguienteEscena = "MenuPrincipal";

        var gestor = new GameObject("GestorRedSuperficie").AddComponent<GestorRedSuperficie>();
        gestor.transform.SetParent(sis, false);
        gestor.modoEscenario = ModoRedSuperficie.Nivel09_PlazaControlEquidad;
        gestor.caudalFuente = 100f;
        gestor.permitirRegulacionRemotaEnHUD = true;
        gestor.valvulasEscena = new[] { vFuga, vMercado, vPuente, vSalud, vRefugio, vLadera };
        gestor.indicadorZonaSalida = renSal;
        gestor.matSalidaPendiente = matRoja;
        gestor.matSalidaLista = matSalida;
        gestor.objetosActivarAlEquilibrar = new[] { aguaLavadero };
        gestor.tuberiasActivarAlEquilibrar = tubLavadero;
        gestor.matTuberiaActiva = matCian;
        gestor.matTuberiaSeca = matSeca;

        var hud = new GameObject("HUDRed3D").AddComponent<HUDRed3D>();
        hud.transform.SetParent(sis, false);
        hud.mostrarTarjetasImpacto = true;

        ConfigurarIluminacionSuperficie();
        EditorSceneManager.SaveScene(escena, R + "Scenes/Niveles/LVL_09_PlazaControlEquidad.unity");
    }

    static void ConfigurarCamaraIsometrica(Transform seguirA, Vector3 inicioRover, float distancia)
    {
        var cams = new GameObject("Camaras").transform;
        var main = Camera.main;
        main.transform.SetParent(cams, true);
        main.transform.SetPositionAndRotation(inicioRover + new Vector3(-11f, 16f, -15f), Quaternion.Euler(42f, 35f, 0f));
        main.fieldOfView = 28f;
        main.farClipPlane = 200f;
        main.clearFlags = CameraClearFlags.Skybox;
        if (main.GetComponent<CinemachineBrain>() == null) main.gameObject.AddComponent<CinemachineBrain>();

        var cmGo = new GameObject("CM_Isometrica");
        cmGo.transform.SetParent(cams, false);
        cmGo.transform.SetPositionAndRotation(main.transform.position, Quaternion.Euler(42f, 35f, 0f));
        var cm = cmGo.AddComponent<CinemachineCamera>();
        cm.Follow = seguirA;
        var lens = cm.Lens;
        lens.FieldOfView = 28f;
        lens.NearClipPlane = 0.3f;
        lens.FarClipPlane = 200f;
        cm.Lens = lens;

        var pc = cmGo.AddComponent<CinemachinePositionComposer>();
        pc.CameraDistance = distancia;
        pc.Damping = new Vector3(0.45f, 0.45f, 0.45f);
        cmGo.AddComponent<CinemachineImpulseListener>();
    }

    static GameObject CrearEfectoAguaLavadero(Transform padre, Material matChorro)
    {
        var raiz = new GameObject("AguaLavaderoActivo");
        raiz.transform.SetParent(padre, false);

        void PrimitivaSinCol(PrimitiveType tipo, string nombre, Vector3 pos, Vector3 escala)
        {
            var go = GameObject.CreatePrimitive(tipo);
            go.name = nombre;
            go.transform.SetParent(raiz.transform, false);
            go.transform.position = pos;
            go.transform.localScale = escala;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            if (matChorro != null) go.GetComponent<Renderer>().sharedMaterial = matChorro;
        }

        PrimitivaSinCol(PrimitiveType.Cube, "EspejoAguaLavadero", new Vector3(9.2f, 0.78f, -9.2f), new Vector3(1.15f, 0.08f, 0.55f));
        PrimitivaSinCol(PrimitiveType.Cylinder, "ChorroLavadero", new Vector3(9.2f, 1.05f, -9.05f), new Vector3(0.14f, 0.28f, 0.14f));
        PrimitivaSinCol(PrimitiveType.Cylinder, "SurtidorFuente", new Vector3(5.5f, 1.05f, -8.8f), new Vector3(0.36f, 0.95f, 0.36f));
        PrimitivaSinCol(PrimitiveType.Cylinder, "EspejoAguaFuente", new Vector3(5.5f, 0.45f, -8.8f), new Vector3(1.75f, 0.06f, 1.75f));

        var luz = new GameObject("LuzLavaderoActivo").AddComponent<Light>();
        luz.transform.SetParent(raiz.transform, false);
        luz.transform.position = new Vector3(7.5f, 2.2f, -9.0f);
        luz.type = LightType.Point;
        luz.color = new Color(0.20f, 0.92f, 1f);
        luz.range = 6.0f;
        luz.intensity = 2.4f;
        luz.shadows = LightShadows.None;

        DesactivarEstaticoRecursivo(raiz);
        raiz.SetActive(false);
        return raiz;
    }

    static void AgregarMuroPerimetral(Transform padre, Vector3 centro, Vector3 tamCollider, bool horizontal, int largo)
    {
        var colGo = new GameObject("MuroCol");
        colGo.transform.SetParent(padre, false);
        colGo.transform.position = centro + Vector3.up * (tamCollider.y * 0.5f);
        colGo.layer = capaSeguro >= 0 ? capaSeguro : 0;
        var bc = colGo.AddComponent<BoxCollider>();
        bc.size = tamCollider;

        Vector3 euler = horizontal ? Vector3.zero : new Vector3(0f, 90f, 0f);
        for (int s = -largo / 2 + 1; s < largo / 2; s += 2)
        {
            Vector3 pos = horizontal ? new Vector3(s, 0f, centro.z) : new Vector3(centro.x, 0f, s);
            Poner("Muros/muro_bajo", pos, euler, Vector3.one, padre);
        }
    }

    static Renderer[] CrearTramoTuberias(Transform padre, string nombre, Vector3[] puntos, Material mat)
    {
        var list = new List<Renderer>();
        var grupo = new GameObject("Tuberia_" + nombre).transform;
        grupo.SetParent(padre, false);
        foreach (var p in puntos)
        {
            var t = PonerConBoxAjustado("Tuberías/tubo_largo", p, new Vector3(0f, 90f, 0f), Vector3.one * 1.15f, grupo, capaSeguro);
            if (t == null) continue;
            foreach (var r in t.GetComponentsInChildren<Renderer>(true))
            {
                if (mat != null) r.sharedMaterial = mat;
                list.Add(r);
            }
        }
        return list.ToArray();
    }

    static GameObject CrearVisualFugaActiva(Vector3 pos, Transform padre, Material matChorro)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "VisualFugaActiva";
        go.transform.SetParent(padre, false);
        go.transform.position = pos + new Vector3(0f, 0.9f, 0f);
        go.transform.localScale = new Vector3(0.45f, 1.1f, 0.45f);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        if (matChorro != null) go.GetComponent<Renderer>().sharedMaterial = matChorro;
        return go;
    }

    static NodoValvula3D CrearConsolaValvula(
        string nombre,
        string aristaId,
        string nombreVisible,
        Vector3 pos,
        bool esBinaria,
        float aperturaObjetivo,
        Transform padre,
        Material matSeca,
        Material matCian,
        GameObject objetoFuga,
        Renderer[] tuberias,
        float pasoEscalon = 0.20f)
    {
        var raiz = new GameObject(nombre);
        raiz.transform.SetParent(padre, false);
        raiz.transform.position = pos;

        Poner("Equipos/consola", pos, Vector3.zero, Vector3.one * 1.25f, raiz.transform);
        var manija = Poner("Obra/baliza", pos + new Vector3(0f, 1.12f, 0f), Vector3.zero, Vector3.one * 0.95f, raiz.transform);

        var luz = new GameObject("LuzValvula").AddComponent<Light>();
        luz.transform.SetParent(raiz.transform, false);
        luz.transform.localPosition = new Vector3(0f, 1.65f, 0f);
        luz.type = LightType.Point;
        luz.range = 4.2f;
        luz.intensity = 2.0f;
        luz.shadows = LightShadows.None;

        var col = raiz.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, 0.7f, 0f);
        col.size = new Vector3(1.1f, 1.4f, 1.0f);
        DesactivarEstaticoRecursivo(raiz);
        CapaRecursiva(raiz, capaInteractuable);

        var nv = raiz.AddComponent<NodoValvula3D>();
        nv.aristaId = aristaId;
        nv.nombreVisible = nombreVisible;
        nv.esBinaria = esBinaria;
        nv.aperturaObjetivo = aperturaObjetivo;
        nv.pasoEscalon = pasoEscalon;
        nv.manijaRotable = manija != null ? manija.transform : null;
        nv.luzIndicadora = luz;
        nv.objetoFugaAsociada = objetoFuga;
        nv.tuberiasAsociadas = tuberias;
        nv.matTuberiaSeca = matSeca;
        nv.matTuberiaActivaCian = matCian;
        return nv;
    }

    static void CrearGeiserMovil(Transform padre, string nombre, Vector3 pos, float desfase, Material matAmbar, Material matChorro)
    {
        var raiz = new GameObject(nombre);
        raiz.transform.SetParent(padre, false);
        raiz.transform.position = pos;
        raiz.layer = capaPeligro;

        Poner("Obra/rejilla_suelo", pos, Vector3.zero, Vector3.one * 1.1f, raiz.transform);

        var chorro = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        chorro.name = "Chorro";
        chorro.transform.SetParent(raiz.transform, false);
        chorro.transform.localPosition = new Vector3(0f, 1.1f, 0f);
        chorro.transform.localScale = new Vector3(0.7f, 1.1f, 0.7f);
        Object.DestroyImmediate(chorro.GetComponent<Collider>());

        var rb = raiz.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        var cap = raiz.AddComponent<CapsuleCollider>();
        cap.direction = 1;
        cap.radius = 0.35f;
        cap.height = 2.2f;
        cap.center = new Vector3(0f, 1.1f, 0f);
        cap.enabled = false;

        var cf = raiz.AddComponent<ChorroFugaMovil3D>();
        cf.modo = ModoObstaculoSuperficie.PulsoGeiser;
        cf.desfaseInicial = desfase;
        cf.radioChorro = 0.35f;
        cf.alturaMaximaChorro = 2.2f;
        cf.visualChorro = chorro.transform;
        cf.rendererChorro = chorro.GetComponent<Renderer>();
        cf.matPreAvisoAmbar = matAmbar;
        cf.matChorroCian = matChorro;
        DesactivarEstaticoRecursivo(raiz);
        CapaRecursiva(raiz, capaPeligro);
    }

    static void CrearBarreraOscilante(Transform padre, string nombre, Vector3 pos, Vector3 desplazamiento, float velocidad, Transform root)
    {
        var raiz = new GameObject(nombre);
        raiz.transform.SetParent(root, false);
        raiz.transform.position = pos;
        raiz.layer = capaPeligro;

        var barrera = Poner("Obra/barrera", pos, Vector3.zero, Vector3.one * 1.1f, raiz.transform);

        var rb = raiz.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        AgregarBoxAjustado(raiz, barrera != null ? barrera : raiz, capaPeligro);

        var cf = raiz.AddComponent<ChorroFugaMovil3D>();
        cf.modo = ModoObstaculoSuperficie.BarreraOscilante;
        cf.desplazamiento = desplazamiento;
        cf.velocidad = velocidad;
        DesactivarEstaticoRecursivo(raiz);
        CapaRecursiva(raiz, capaPeligro);
    }
}
