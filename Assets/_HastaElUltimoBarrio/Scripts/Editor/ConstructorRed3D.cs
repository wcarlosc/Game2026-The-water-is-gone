using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Construye por codigo MenuPrincipal y los niveles 3D isometricos de superficie (Parte 1: Menu + LVL_07)
// usando prefabs reales de Kit_Pueblo, sin tocar EditorBuildSettings ni las escenas 2.5D de main.
public static class ConstructorRed3D
{
    const string R = "Assets/_HastaElUltimoBarrio/";
    const string KIT = R + "Prefabs/Kit_Pueblo/";

    static int capaJugador, capaSeguro, capaPeligro, capaRecolectable, capaInteractuable;

    [MenuItem("Tools/Hasta el último barrio/Construir menú y nivel 7 (Red 3D)")]
    public static void ConstruirMenuYNivel7Interactivo()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EjecutarConstruccionParte1();
    }

    // Punto de entrada sin dialogos para batchmode (-executeMethod ConstructorRed3D.ConstruirMenuYNivel7Batch).
    public static void ConstruirMenuYNivel7Batch()
    {
        EjecutarConstruccionParte1();
    }

    static void EjecutarConstruccionParte1()
    {
        InicializarCapasYMateriales();
        AsegurarPrefabRover();
        ConstruirMenuPrincipal();
        ConstruirNivel7();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ConstructorRed3D] MenuPrincipal y LVL_07_AislamientoFuga generados correctamente.");
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
        if (m == null)
        {
            var sh = Shader.Find("Skybox/Procedural");
            m = new Material(sh != null ? sh : Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, p);
        }
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
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, p);
        }
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

    static GameObject PonerSolido(string ruta, Vector3 pos, Vector3 euler, Vector3 escala, Transform padre, Vector3 colCenter, Vector3 colSize)
    {
        var go = Poner(ruta, pos, euler, escala, padre);
        if (go == null) return null;
        var bc = go.AddComponent<BoxCollider>();
        bc.center = colCenter;
        bc.size = colSize;
        return go;
    }

    static void QuitarColliders(GameObject go)
    {
        foreach (var c in go.GetComponentsInChildren<Collider>(true))
            Object.DestroyImmediate(c, true);
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
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        var cap = raiz.AddComponent<CapsuleCollider>();
        cap.direction = 1;
        cap.center = new Vector3(0f, 0.55f, 0f);
        cap.height = 1.1f;
        cap.radius = 0.48f;

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
        var bcPiso = colPiso.AddComponent<BoxCollider>();
        bcPiso.center = new Vector3(0f, -0.25f, 0f);
        bcPiso.size = new Vector3(28f, 0.5f, 24f);

        // Muros perimetrales (altura 1.2m)
        AgregarMuroPerimetral(raizEntorno, new Vector3(0f, 0f, 12.2f), new Vector3(28.8f, 1.5f, 0.6f), horizontal: true, largo: 28);
        AgregarMuroPerimetral(raizEntorno, new Vector3(0f, 0f, -12.2f), new Vector3(28.8f, 1.5f, 0.6f), horizontal: true, largo: 28);
        AgregarMuroPerimetral(raizEntorno, new Vector3(-14.2f, 0f, 0f), new Vector3(0.6f, 1.5f, 24.8f), horizontal: false, largo: 24);
        AgregarMuroPerimetral(raizEntorno, new Vector3(14.2f, 0f, 0f), new Vector3(0.6f, 1.5f, 24.8f), horizontal: false, largo: 24);

        // Edificios e infraestructura del pueblo
        PonerSolido("Equipos/tanque", new Vector3(-10.5f, 0f, 8.2f), Vector3.zero, Vector3.one * 1.15f, raizEntorno, new Vector3(0f, 0.9f, 0f), new Vector3(2.1f, 1.8f, 2.0f));
        PonerSolido("Equipos/bomba_agua", new Vector3(-8.0f, 0f, 8.2f), new Vector3(0f, 90f, 0f), Vector3.one * 1.25f, raizEntorno, new Vector3(0f, 0.5f, 0f), new Vector3(1.2f, 1.0f, 0.8f));
        PonerSolido("Edificios/clinica", new Vector3(9.0f, 0f, 6.2f), new Vector3(0f, -90f, 0f), Vector3.one * 0.95f, raizEntorno, new Vector3(0f, 2.0f, 0.4f), new Vector3(4.6f, 4.0f, 5.2f));
        PonerSolido("Edificios/casa_ladrillo", new Vector3(-9.5f, 0f, -8.5f), new Vector3(0f, 90f, 0f), Vector3.one * 0.9f, raizEntorno, new Vector3(0f, 1.9f, 0f), new Vector3(4.2f, 3.8f, 4.2f));
        PonerSolido("Edificios/fuente", new Vector3(0.5f, 0f, -7.8f), Vector3.zero, Vector3.one, raizEntorno, new Vector3(0f, 1.0f, 0f), new Vector3(2.9f, 2.0f, 2.9f));
        PonerSolido("Equipos/lavadero", new Vector3(4.5f, 0f, -8.5f), new Vector3(0f, 180f, 0f), Vector3.one * 1.15f, raizEntorno, new Vector3(0f, 0.85f, 0f), new Vector3(1.5f, 1.7f, 0.9f));
        PonerSolido("Mercado/puesto", new Vector3(-1.5f, 0f, 8.5f), Vector3.zero, Vector3.one, raizEntorno, new Vector3(0f, 1.0f, 0f), new Vector3(2.2f, 2.0f, 1.5f));
        Poner("Naturaleza/eucalipto", new Vector3(-12.2f, 0f, 2.5f), Vector3.zero, Vector3.one * 1.1f, raizEntorno);
        Poner("Naturaleza/eucalipto", new Vector3(12.2f, 0f, -2.5f), Vector3.zero, Vector3.one * 1.1f, raizEntorno);
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
        var tuboRoto = Poner("Ruinas_e_industria/tubo_roto_base", new Vector3(-3.0f, 0f, 1.6f), new Vector3(0f, 90f, 0f), Vector3.one * 1.25f, raizTuberias);
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
        lm.siguienteEscena = "";

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

        ConfigurarIluminacionSuperficie();
        EditorSceneManager.SaveScene(escena, R + "Scenes/Niveles/LVL_07_AislamientoFuga.unity");
    }

    static void AgregarMuroPerimetral(Transform padre, Vector3 centro, Vector3 tamCollider, bool horizontal, int largo)
    {
        var colGo = new GameObject("MuroCol");
        colGo.transform.SetParent(padre, false);
        colGo.transform.position = centro + Vector3.up * (tamCollider.y * 0.5f);
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
            var t = Poner("Tuberías/tubo_largo", p, new Vector3(0f, 90f, 0f), Vector3.one * 1.15f, grupo);
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
        Renderer[] tuberias)
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
        CapaRecursiva(raiz, capaInteractuable);

        var nv = raiz.AddComponent<NodoValvula3D>();
        nv.aristaId = aristaId;
        nv.nombreVisible = nombreVisible;
        nv.esBinaria = esBinaria;
        nv.aperturaObjetivo = aperturaObjetivo;
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
        chorro.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        chorro.transform.localScale = new Vector3(0.7f, 1f, 0.7f);
        Object.DestroyImmediate(chorro.GetComponent<Collider>());

        var rb = raiz.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        var bc = raiz.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, 1.0f, 0f);
        bc.size = new Vector3(0.85f, 2.0f, 0.85f);

        var cf = raiz.AddComponent<ChorroFugaMovil3D>();
        cf.modo = ModoObstaculoSuperficie.PulsoGeiser;
        cf.desfaseInicial = desfase;
        cf.visualChorro = chorro.transform;
        cf.rendererChorro = chorro.GetComponent<Renderer>();
        cf.matPreAvisoAmbar = matAmbar;
        cf.matChorroCian = matChorro;
        CapaRecursiva(raiz, capaPeligro);
    }

    static void CrearBarreraOscilante(Transform padre, string nombre, Vector3 pos, Vector3 desplazamiento, float velocidad, Transform root)
    {
        var raiz = new GameObject(nombre);
        raiz.transform.SetParent(root, false);
        raiz.transform.position = pos;
        raiz.layer = capaPeligro;

        Poner("Obra/barrera", pos, Vector3.zero, Vector3.one * 1.1f, raiz.transform);

        var rb = raiz.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        var bc = raiz.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, 0.6f, 0f);
        bc.size = new Vector3(2.2f, 1.2f, 0.45f);

        var cf = raiz.AddComponent<ChorroFugaMovil3D>();
        cf.modo = ModoObstaculoSuperficie.BarreraOscilante;
        cf.desplazamiento = desplazamiento;
        cf.velocidad = velocidad;
        CapaRecursiva(raiz, capaPeligro);
    }
}
