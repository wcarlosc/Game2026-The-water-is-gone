using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Construye LVL_01 y LVL_02 (túneles 2.5D) con los prefabs de Assets/Prefabs.
// Menú: Tools > Hasta el último barrio > Construir túneles
public static class ConstructorTuneles
{
    const string R = "Assets/_HastaElUltimoBarrio/";
    const string KIT = R + "Prefabs/Kit_Pueblo/";
    const float ANCHO = 2f;          // ancho de columna del perfil
    const float FONDO_PISO = -7f;    // base del collider del piso
    const float TOPE_TECHO = 18f;    // tope del collider del techo

    static int capaSeguro, capaPeligro, capaRecolectable, capaJugador;
    static Dictionary<string, Material> matsKit;
    static Dictionary<string, Material> matsBloques;
    static Transform tSuelo, tTecho, tFondo, tObst, tPlat, tBat, tLuces, tCol;

    // ------------------------------------------------------------------ menú
    [MenuItem("Tools/Hasta el último barrio/Construir túneles (niveles 1 y 2)")]
    public static void ConstruirAmbos()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        PrepararAssets();
        ConstruirNivel1();
        ConstruirNivel2();
        RegistrarEnBuild();
    }

    // ------------------------------------------------------------------ assets comunes
    public static void PrepararAssets()
    {
        capaSeguro = LayerMask.NameToLayer("Seguro");
        capaPeligro = LayerMask.NameToLayer("Peligro");
        capaRecolectable = LayerMask.NameToLayer("Recolectable");
        capaJugador = LayerMask.NameToLayer("Jugador");

        matsKit = new Dictionary<string, Material>();
        foreach (var a in AssetDatabase.LoadAllAssetsAtPath(R + "Art/Models/Kit/kit_completo.glb"))
            if (a is Material m && !matsKit.ContainsKey(m.name)) matsKit[m.name] = m;

        MatEmisivo("M_Ambar_Emisivo", new Color(1f, 0.62f, 0.12f), 3f);
        MatHaz();
        PrefabPlataformaSegura();
        PrefabPlataformaLlegada();
        PrefabBateria();
        PrefabDron();
        MallaHaz();
    }

    static Material MatEmisivo(string nombre, Color c, float intensidad)
    {
        string p = R + "Art/Materials/" + nombre + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, p);
        }
        m.SetColor("_BaseColor", c);
        m.EnableKeyword("_EMISSION");
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        m.SetColor("_EmissionColor", c * intensidad);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material MatHaz()
    {
        string p = R + "Art/Materials/M_Haz_Luz.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (m == null)
        {
            m = new Material(Shader.Find("Sprites/Default"));
            AssetDatabase.CreateAsset(m, p);
        }
        m.color = new Color(1f, 0.78f, 0.4f, 1f);
        m.renderQueue = 3100;
        EditorUtility.SetDirty(m);
        return m;
    }

    static Mesh MallaHaz()
    {
        string p = R + "Art/Models/MallaHazLuz.asset";
        var malla = AssetDatabase.LoadAssetAtPath<Mesh>(p);
        if (malla != null) AssetDatabase.DeleteAsset(p);
        malla = new Mesh { name = "HazLuz" };
        // Trapecio vertical: ancho arriba 1, abajo 2.6, alto 1 (se escala en Y), inclinado.
        malla.vertices = new[] {
            new Vector3(-0.5f, 0f, 0f), new Vector3(0.5f, 0f, 0f),
            new Vector3(-1.3f - 0.6f, -1f, 0f), new Vector3(1.3f - 0.6f, -1f, 0f) };
        malla.colors = new[] {
            new Color(1, 1, 1, 0.16f), new Color(1, 1, 1, 0.16f),
            new Color(1, 1, 1, 0f), new Color(1, 1, 1, 0f) };
        malla.uv = new[] { new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 0), new Vector2(1, 0) };
        malla.triangles = new[] { 0, 1, 2, 2, 1, 3, 0, 2, 1, 2, 3, 1 };
        malla.RecalculateBounds();
        AssetDatabase.CreateAsset(malla, p);
        return malla;
    }

    static GameObject CargarKit(string ruta)
    {
        var go = AssetDatabase.LoadAssetAtPath<GameObject>(KIT + ruta + ".prefab");
        if (go == null) Debug.LogError("Falta prefab del kit: " + ruta);
        return go;
    }

    static void CapaRecursiva(GameObject go, int capa)
    {
        foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = capa;
    }

    static void QuitarColliders(GameObject go)
    {
        foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c, true);
    }

    static void PrefabPlataformaSegura()
    {
        string p = R + "Prefabs/Kit_Tunel/PF_PlataformaSegura.prefab";
        var raiz = new GameObject("PF_PlataformaSegura");
        var vis = (GameObject)PrefabUtility.InstantiatePrefab(CargarKit("Juego/placa_presion"));
        vis.transform.SetParent(raiz.transform, false);
        vis.transform.localScale = new Vector3(2.2f, 1.3f, 1.6f);
        QuitarColliders(vis);
        var bc = raiz.AddComponent<BoxCollider>();
        bc.size = new Vector3(2.2f, 0.25f, 1.6f);
        bc.center = new Vector3(0f, 0.125f, 0f);
        CapaRecursiva(raiz, capaSeguro);
        PrefabUtility.SaveAsPrefabAsset(raiz, p);
        Object.DestroyImmediate(raiz);
    }

    static void PrefabPlataformaLlegada()
    {
        string p = R + "Prefabs/Kit_Tunel/PF_PlataformaLlegada.prefab";
        var raiz = new GameObject("PF_PlataformaLlegada");
        var vis = (GameObject)PrefabUtility.InstantiatePrefab(CargarKit("Juego/plataforma_diana"));
        vis.transform.SetParent(raiz.transform, false);
        vis.transform.localScale = new Vector3(1.3f, 0.8f, 1.1f);
        QuitarColliders(vis);
        var bc = raiz.AddComponent<BoxCollider>();
        bc.size = new Vector3(2.6f, 0.46f, 2.2f);
        bc.center = new Vector3(0f, 0.23f, 0f);
        raiz.tag = "Finish";
        CapaRecursiva(raiz, capaSeguro);
        PrefabUtility.SaveAsPrefabAsset(raiz, p);
        Object.DestroyImmediate(raiz);
    }

    static void PrefabBateria()
    {
        string p = R + "Prefabs/Kit_Tunel/PF_Bateria.prefab";
        var raiz = new GameObject("PF_Bateria");
        var vis = (GameObject)PrefabUtility.InstantiatePrefab(CargarKit("Juego/bateria"));
        vis.transform.SetParent(raiz.transform, false);
        vis.transform.localScale = Vector3.one * 1.2f;
        vis.transform.localPosition = new Vector3(0f, -0.4f, 0f);
        QuitarColliders(vis);
        foreach (var t in raiz.GetComponentsInChildren<Transform>(true)) t.gameObject.isStatic = false;
        var sc = raiz.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = 0.55f;
        raiz.AddComponent<Bateria>().energia = 2.5f;
        var luz = new GameObject("Brillo").AddComponent<Light>();
        luz.transform.SetParent(raiz.transform, false);
        luz.type = LightType.Point;
        luz.color = new Color(1f, 0.7f, 0.2f);
        luz.range = 2.2f;
        luz.intensity = 1.5f;
        luz.shadows = LightShadows.None;
        CapaRecursiva(raiz, capaRecolectable);
        PrefabUtility.SaveAsPrefabAsset(raiz, p);
        Object.DestroyImmediate(raiz);
    }

    static void PrefabDron()
    {
        string p = R + "Prefabs/Personajes/PF_Dron.prefab";
        var modelo = AssetDatabase.LoadAssetAtPath<GameObject>(R + "Art/Models/Personajes/hover_bot.glb");
        var raiz = new GameObject("PF_Dron");
        raiz.tag = "Player";
        var visual = new GameObject("Visual");
        visual.transform.SetParent(raiz.transform, false);
        visual.transform.localPosition = new Vector3(0f, 0.4f, 0f);
        visual.transform.localRotation = Quaternion.Euler(0f, 145f, 0f);
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(modelo);
        inst.transform.SetParent(visual.transform, false);
        inst.transform.localPosition = new Vector3(0f, -0.4f, 0f);
        inst.transform.localScale = Vector3.one * 1.5f;
        foreach (var t in raiz.GetComponentsInChildren<Transform>(true)) t.gameObject.isStatic = false;

        var rb = raiz.AddComponent<Rigidbody>();
        rb.mass = 1f;
        rb.useGravity = false;
        rb.linearDamping = 0.3f;
        rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        var cap = raiz.AddComponent<CapsuleCollider>();
        cap.direction = 1;
        cap.center = new Vector3(0f, 0.36f, 0f);
        cap.height = 0.66f;
        cap.radius = 0.26f;
        var pm = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(R + "Art/Materials/PM_Dron.asset");
        if (pm == null)
        {
            pm = new PhysicsMaterial("PM_Dron") { dynamicFriction = 0.6f, staticFriction = 0.6f, bounciness = 0f };
            AssetDatabase.CreateAsset(pm, R + "Art/Materials/PM_Dron.asset");
        }
        cap.sharedMaterial = pm;

        var imp = raiz.AddComponent<CinemachineImpulseSource>();
        imp.ImpulseDefinition.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump;
        imp.ImpulseDefinition.ImpulseDuration = 0.25f;
        imp.DefaultVelocity = new Vector3(0.35f, 0.35f, 0f);

        var dc = raiz.AddComponent<DronController>();
        dc.visual = visual.transform;
        dc.animacion = inst.GetComponent<Animation>();
        dc.sacudida = imp;
        if (dc.animacion != null) { dc.animacion.playAutomatically = true; dc.animacion.clip = dc.animacion.GetClip("Idle"); }

        var llama = new GameObject("LuzPropulsor").AddComponent<Light>();
        llama.transform.SetParent(raiz.transform, false);
        llama.transform.localPosition = new Vector3(0f, -0.05f, -0.2f);
        llama.type = LightType.Point;
        llama.color = new Color(1f, 0.6f, 0.15f);
        llama.range = 2.5f;
        llama.intensity = 1.2f;
        llama.shadows = LightShadows.None;

        CapaRecursiva(raiz, capaJugador);
        PrefabUtility.SaveAsPrefabAsset(raiz, p);
        Object.DestroyImmediate(raiz);
    }

    // ------------------------------------------------------------------ utilidades de escena
    static GameObject Poner(string ruta, Vector3 pos, Vector3 euler, Vector3 escala, Transform padre, Dictionary<string, Material> cambioMats = null)
    {
        var pf = CargarKit(ruta);
        if (pf == null) return null;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, padre);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(euler);
        go.transform.localScale = escala;
        foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
        if (cambioMats != null) CambiarMateriales(go, cambioMats);
        return go;
    }

    static GameObject PonerPrefab(string ruta, Vector3 pos, Transform padre)
    {
        var pf = AssetDatabase.LoadAssetAtPath<GameObject>(R + "Prefabs/" + ruta + ".prefab");
        var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, padre);
        go.transform.position = pos;
        return go;
    }

    static void CambiarMateriales(GameObject go, Dictionary<string, Material> mapa)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            var ms = r.sharedMaterials;
            bool cambio = false;
            for (int i = 0; i < ms.Length; i++)
                if (ms[i] != null && mapa.TryGetValue(ms[i].name, out var nuevo)) { ms[i] = nuevo; cambio = true; }
            if (cambio) r.sharedMaterials = ms;
        }
    }

    static BoxCollider Caja(string nombre, Vector3 centro, Vector3 tam, int capa)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(tCol, false);
        go.transform.position = centro;
        go.layer = capa;
        var bc = go.AddComponent<BoxCollider>();
        bc.size = tam;
        return bc;
    }

    // Collider de obstáculo: caja 12 % más pequeña que la malla, ocupando el carril Z = 0.
    static void ColliderObstaculo(GameObject go, float reduccion = 0.88f)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return;
        Bounds b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        var tam = new Vector3(Mathf.Max(0.25f, b.size.x * reduccion), Mathf.Max(0.25f, b.size.y * reduccion), 1.2f);
        Caja("Col_" + go.name, new Vector3(b.center.x, b.center.y, 0f), tam, capaPeligro);
    }

    static Material Mat(string nombre)
    {
        matsKit.TryGetValue(nombre, out var m);
        return m;
    }

    static void CrearJerarquia(string nombreNivel)
    {
        var nivel = new GameObject("Nivel");
        Transform Hijo(string n) { var g = new GameObject(n); g.transform.SetParent(nivel.transform, false); return g.transform; }
        tSuelo = Hijo("Suelo");
        tTecho = Hijo("Techo");
        tFondo = Hijo("Fondo");
        tObst = Hijo("Obstaculos");
        tPlat = Hijo("Plataformas");
        tBat = Hijo("Baterias");
        tLuces = Hijo("Luces");
        tCol = Hijo("Colisiones");
    }

    // ------------------------------------------------------------------ perfil piso/techo
    // suelo[i] = altura superior del piso en la columna i (x de 2i a 2i+2)
    // techo[i] = altura inferior del techo; topeVisual[i] = hasta dónde se ve el bloque de techo
    static void ConstruirPerfil(float[] suelo, float[] techo, float[] topeVisual, Dictionary<string, Material> mats)
    {
        matsBloques = mats;
        int n = suelo.Length;
        for (int i = 0; i < n; i++)
        {
            float x = i * ANCHO + 1f;
            // Piso: bloques de 2 m apilados hacia abajo desde la superficie
            for (float top = suelo[i]; top > -4f; top -= 2f)
                Poner("Escaleras_y_plataformas/plataforma_2n", new Vector3(x, top - 2f, 2f), Vector3.zero, new Vector3(1f, 1f, 3f), tSuelo, mats);
            // Techo: bloques hacia arriba desde la cara inferior
            for (float baseY = techo[i]; baseY < topeVisual[i]; baseY += 2f)
                Poner("Escaleras_y_plataformas/plataforma_2n", new Vector3(x, baseY, 2f), Vector3.zero, new Vector3(1f, 1f, 3f), tTecho, mats);
        }
        // Colliders agrupando columnas iguales
        int a = 0;
        for (int i = 1; i <= n; i++)
        {
            if (i == n || suelo[i] != suelo[a])
            {
                float x0 = a * ANCHO, x1 = i * ANCHO;
                float h = suelo[a] - FONDO_PISO;
                Caja("Col_Piso_" + a, new Vector3((x0 + x1) / 2f, FONDO_PISO + h / 2f, 0f), new Vector3(x1 - x0, h, 2f), capaPeligro);
                a = i;
            }
        }
        a = 0;
        for (int i = 1; i <= n; i++)
        {
            if (i == n || techo[i] != techo[a])
            {
                float x0 = a * ANCHO, x1 = i * ANCHO;
                float h = TOPE_TECHO - techo[a];
                Caja("Col_Techo_" + a, new Vector3((x0 + x1) / 2f, techo[a] + h / 2f, 0f), new Vector3(x1 - x0, h, 2f), capaPeligro);
                a = i;
            }
        }
        // Muros de inicio y fin
        Caja("Col_MuroInicio", new Vector3(-1f, 5f, 0f), new Vector3(2f, 30f, 2f), capaPeligro);
        Caja("Col_MuroFin", new Vector3(n * ANCHO + 1f, 5f, 0f), new Vector3(2f, 30f, 2f), capaPeligro);
        for (float y = -4f; y < 12f; y += 2f)
        {
            Poner("Escaleras_y_plataformas/plataforma_2n", new Vector3(-1f, y, 2f), Vector3.zero, new Vector3(1f, 1f, 3f), tSuelo, mats);
            Poner("Escaleras_y_plataformas/plataforma_2n", new Vector3(n * ANCHO + 1f, y, 2f), Vector3.zero, new Vector3(1f, 1f, 3f), tSuelo, mats);
        }
    }

    static float Suelo(float[] suelo, float x) { int i = Mathf.Clamp(Mathf.FloorToInt(x / ANCHO), 0, suelo.Length - 1); return suelo[i]; }

    // ------------------------------------------------------------------ fondo
    static void ConstruirFondo(float[] suelo, float[] techo, float[] topeVisual, string matPanel, Dictionary<string, Material> matsArco, bool arcos, float yTuberia1, float yTuberia2)
    {
        int n = suelo.Length;
        var panelMat = new Dictionary<string, Material>();
        if (Mat(matPanel) != null) { panelMat["piedra_azul"] = Mat(matPanel); panelMat["piedra_azul_oscura"] = Mat(matPanel); }
        // Muro trasero (z = +5.4)
        for (int i = -1; i <= n; i++)
        {
            float x = i * ANCHO + 1f;
            int k = Mathf.Clamp(i, 0, n - 1);
            float alto = (topeVisual[k] < 13f ? topeVisual[k] : topeVisual[k] + 4f) + 4f;
            Poner("Muros/muro_panel_azul", new Vector3(x, -4f, 5.4f), Vector3.zero, new Vector3(1f, alto / 3f, 1f), tFondo, panelMat);
        }
        // Arcada (z = +3.6): pilares cada 6 m y arcos entre ellos
        for (float x = 3f; x < n * ANCHO - 1f; x += 6f)
        {
            float h = Mathf.Max(Suelo(suelo, x - 0.5f), Suelo(suelo, x + 0.5f));
            int ci = Mathf.Clamp(Mathf.FloorToInt(x / ANCHO), 0, n - 1);
            float c = Mathf.Min(techo[ci], techo[Mathf.Clamp(ci + 1, 0, n - 1)]);
            c = Mathf.Max(c, topeVisual[ci] - 0.5f);
            float alto = c - h + 2f;
            if (alto < 1f) continue;
            Poner("Muros/columna_ladrillo", new Vector3(x, h, 3.6f), Vector3.zero, new Vector3(1.1f, alto / 3f, 1.1f), tFondo, matsArco);
            if (arcos)
            {
                float hm = Mathf.Max(Suelo(suelo, x + 1f), Mathf.Max(Suelo(suelo, x + 3f), Suelo(suelo, x + 5f)));
                int cj = Mathf.Clamp(Mathf.FloorToInt((x + 3f) / ANCHO), 0, n - 1);
                float techoArco = Mathf.Max(techo[cj], topeVisual[cj] - 0.5f);
                float altoArco = techoArco - hm + 1.5f;
                if (altoArco > 2f)
                    Poner("Muros/muro_arco", new Vector3(x + 3f, hm, 3.75f), Vector3.zero, new Vector3(2.6f, altoArco / 3f, 0.6f), tFondo, matsArco);
            }
            // Lámpara ámbar en el pilar
            if (alto > 3f)
            {
                var lamp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                lamp.name = "Lampara";
                lamp.transform.SetParent(tLuces, false);
                lamp.transform.position = new Vector3(x, h + 2.6f, 3.1f);
                lamp.transform.localScale = new Vector3(0.18f, 0.34f, 0.1f);
                Object.DestroyImmediate(lamp.GetComponent<Collider>());
                lamp.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(R + "Art/Materials/M_Ambar_Emisivo.mat");
                if (Mathf.RoundToInt(x / 6f) % 2 == 0)
                {
                    var l = new GameObject("LuzLampara").AddComponent<Light>();
                    l.transform.SetParent(lamp.transform.parent, false);
                    l.transform.position = new Vector3(x, h + 2.6f, 2.4f);
                    l.type = LightType.Point;
                    l.color = new Color(1f, 0.68f, 0.3f);
                    l.range = 6f;
                    l.intensity = 2.5f;
                    l.shadows = LightShadows.None;
                }
            }
        }
        // Tuberías cian de fondo (z = +4.7)
        foreach (float yt in new[] { yTuberia1, yTuberia2 })
        {
            if (yt <= -50f) continue;
            for (int i = 0; i < n; i++)
            {
                float x = i * ANCHO + 1f;
                if (yt < suelo[i] + 0.3f || yt + 0.6f > techo[i] - 0.1f) continue;
                Poner("Tuberías/tubo_largo", new Vector3(x, yt, 4.7f), Vector3.zero, new Vector3(1f, 1.2f, 1.2f), tFondo, MapaCian());
            }
        }
    }

    static Dictionary<string, Material> MapaCian()
    {
        string p = R + "Art/Materials/M_Tuberia_Cian.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", new Color(0.12f, 0.62f, 0.68f));
            m.SetFloat("_Smoothness", 0.45f);
            m.SetFloat("_Metallic", 0.3f);
            AssetDatabase.CreateAsset(m, p);
        }
        return new Dictionary<string, Material> { { "metal_tuberia", m }, { "banda_tuberia", m } };
    }

    static void HazDeLuz(float x, float yTecho, float ySuelo)
    {
        var go = new GameObject("HazLuz");
        go.transform.SetParent(tLuces, false);
        go.transform.position = new Vector3(x, yTecho, 1.6f);
        go.transform.localScale = new Vector3(1.4f, yTecho - ySuelo, 1f);
        go.AddComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(R + "Art/Models/MallaHazLuz.asset");
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(R + "Art/Materials/M_Haz_Luz.mat");
        mr.shadowCastingMode = ShadowCastingMode.Off;
        var spot = new GameObject("Foco").AddComponent<Light>();
        spot.transform.SetParent(go.transform, false);
        spot.transform.position = new Vector3(x + 0.3f, yTecho - 0.2f, 0.5f);
        spot.transform.rotation = Quaternion.Euler(80f, 0f, -20f);
        spot.type = LightType.Spot;
        spot.spotAngle = 45f;
        spot.range = 14f;
        spot.intensity = 18f;
        spot.color = new Color(1f, 0.8f, 0.5f);
        spot.shadows = LightShadows.Soft;
    }

    // Montón de residuos: piezas del kit agrandadas y apiladas en pirámide, cada una con su collider.
    static void Monton(float x, float ySuelo, string[] piezas, float escala, int semilla)
    {
        var rnd = new System.Random(semilla);
        int total = piezas.Length;
        int base_ = 1;
        while (base_ * (base_ + 1) / 2 < total) base_++;
        float paso = 0.78f * escala;
        int idx = 0;
        for (int fila = 0; fila < base_ && idx < total; fila++)
        {
            int enFila = base_ - fila;
            float x0 = x - (enFila - 1) * paso / 2f;
            for (int k = 0; k < enFila && idx < total; k++, idx++)
            {
                float px = x0 + k * paso + ((float)rnd.NextDouble() - 0.5f) * 0.25f * escala;
                float py = ySuelo + fila * 0.62f * escala;
                float rot = (float)rnd.NextDouble() * 60f - 30f;
                float pz = ((float)rnd.NextDouble() - 0.5f) * 0.4f;
                var go = Poner(piezas[idx], new Vector3(px, py, pz), new Vector3(0f, rot, 0f), Vector3.one * escala, tObst);
                if (go != null) ColliderObstaculo(go);
            }
        }
    }

    static GameObject Obstaculo(string ruta, Vector3 pos, Vector3 euler, Vector3 escala)
    {
        var go = Poner(ruta, pos, euler, escala, tObst);
        if (go != null) ColliderObstaculo(go);
        return go;
    }

    // Obstáculo sobre una base de piedra (1 o 2 m) para que el zigzag sea más marcado.
    static GameObject ObstaculoConBase(string ruta, float x, float ySuelo, int alturaBase, Vector3 euler, Vector3 escala)
    {
        string bloque = alturaBase >= 2 ? "Escaleras_y_plataformas/plataforma_2n" : "Escaleras_y_plataformas/plataforma_1n";
        Poner(bloque, new Vector3(x, ySuelo, 0f), Vector3.zero, Vector3.one, tObst, matsBloques);
        Caja("Col_Base_" + x, new Vector3(x, ySuelo + alturaBase / 2f, 0f), new Vector3(2f, alturaBase, 2f), capaPeligro);
        return Obstaculo(ruta, new Vector3(x, ySuelo + alturaBase, 0f), euler, escala);
    }

    // Rejilla colgando del techo, de cara a la cámara e inclinada.
    static void RejillaColgante(float x, float yTecho, float inclinacion, string ruta = "Ruinas_e_industria/rejilla_doblada", float escala = 2.3f)
    {
        var go = Poner(ruta, new Vector3(x, yTecho - 0.1f, 0f), new Vector3(-90f, 0f, inclinacion), Vector3.one * escala, tObst);
        if (go == null) return;
        // la pieza gira alrededor de su base: colgarla por encima del pivote
        var b = go.GetComponentInChildren<Renderer>().bounds;
        go.transform.position += new Vector3(0f, yTecho - b.max.y, 0f);
        ColliderObstaculo(go);
        var soporte = Poner("Tuberías/abrazadera", new Vector3(x, yTecho - 0.45f, 0f), Vector3.zero, new Vector3(1f, 1f, 1f), tObst);
    }

    static void TuboColgante(float x, float yTecho, float escala, float rotY = 0f)
    {
        var go = Poner("Ruinas_e_industria/tubo_colgante", new Vector3(x, yTecho - 2.38f * escala, 0f), new Vector3(0f, rotY, 0f), Vector3.one * escala, tObst);
        if (go != null) ColliderObstaculo(go, 0.8f);
    }

    static void Plataforma(float x, float y) { PonerPrefab("Kit_Tunel/PF_PlataformaSegura", new Vector3(x, y, 0f), tPlat); }
    static void Bateria(float x, float y) { PonerPrefab("Kit_Tunel/PF_Bateria", new Vector3(x, y, 0f), tBat); }

    // ------------------------------------------------------------------ sistemas comunes
    static GameObject Sistemas(string nombre, string objetivo, float largo, float camYmin, float camYmax, Vector3 inicio)
    {
        var sis = new GameObject("Sistemas");
        var lm = new GameObject("LevelManager").AddComponent<LevelManager>();
        lm.transform.SetParent(sis.transform, false);
        lm.nombreNivel = nombre;
        lm.objetivo = objetivo;
        var hud = new GameObject("HUD").AddComponent<HUDTunel>();
        hud.transform.SetParent(sis.transform, false);

        var dron = PonerPrefab("Personajes/PF_Dron", inicio, null);
        dron.name = "Dron";
        var start = new GameObject("PlayerStart");
        start.transform.position = inicio;
        start.tag = "Respawn";

        // Cámaras
        var cams = new GameObject("Camaras");
        var main = Camera.main;
        main.transform.SetParent(cams.transform, true);
        main.transform.SetPositionAndRotation(new Vector3(inicio.x + 2f, inicio.y + 4f, -15f), Quaternion.identity);
        main.fieldOfView = 35f;
        main.farClipPlane = 250f;
        main.clearFlags = CameraClearFlags.Skybox;
        if (main.GetComponent<CinemachineBrain>() == null) main.gameObject.AddComponent<CinemachineBrain>();
        var urp = main.GetComponent<UniversalAdditionalCameraData>();
        if (urp != null) urp.renderPostProcessing = true;

        var cmGo = new GameObject("CM_Dron");
        cmGo.transform.SetParent(cams.transform, false);
        cmGo.transform.SetPositionAndRotation(main.transform.position, Quaternion.identity);
        var cm = cmGo.AddComponent<CinemachineCamera>();
        cm.Follow = dron.transform;
        var lens = cm.Lens;
        lens.FieldOfView = 35f;
        lens.FarClipPlane = 250f;
        lens.NearClipPlane = 0.3f;
        cm.Lens = lens;
        var pc = cmGo.AddComponent<CinemachinePositionComposer>();
        pc.CameraDistance = 15f;
        pc.Damping = new Vector3(0.5f, 0.5f, 0.5f);
        var la = pc.Lookahead;
        la.Enabled = true;
        la.Time = 0.4f;
        la.Smoothing = 6f;
        la.IgnoreY = true;
        pc.Lookahead = la;
        var comp = pc.Composition;
        comp.ScreenPosition = new Vector2(-0.1f, 0.05f);
        pc.Composition = comp;
        cmGo.AddComponent<CinemachineImpulseListener>();

        var lim = new GameObject("LimitesCamara");
        lim.transform.SetParent(cams.transform, false);
        lim.layer = 2; // Ignore Raycast
        var bc = lim.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        float xMin = 8.5f, xMax = largo - 8.5f;
        bc.center = new Vector3((xMin + xMax) / 2f, (camYmin + camYmax) / 2f, -15f);
        bc.size = new Vector3(xMax - xMin, camYmax - camYmin, 1f);
        var conf = cmGo.AddComponent<CinemachineConfiner3D>();
        conf.BoundingVolume = bc;
        return dron;
    }

    static void Iluminacion(Color ambCielo, Color ambEcuador, Color ambSuelo, Color colorSol, float intSol, Color niebla, float nieblaIni, float nieblaFin)
    {
        var sol = Object.FindAnyObjectByType<Light>();
        if (sol != null && sol.type == LightType.Directional)
        {
            sol.name = "Luz_Principal";
            sol.transform.rotation = Quaternion.Euler(28f, 18f, 0f);
            sol.color = colorSol;
            sol.intensity = intSol;
            sol.shadows = LightShadows.Soft;
            sol.shadowStrength = 0.6f;
        }
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = ambCielo;
        RenderSettings.ambientEquatorColor = ambEcuador;
        RenderSettings.ambientGroundColor = ambSuelo;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = niebla;
        RenderSettings.fogStartDistance = nieblaIni;
        RenderSettings.fogEndDistance = nieblaFin;

        // Volumen global con bloom suave
        string pv = R + "Settings/Volumes/Settings_Volumen_Tunel.asset";
        var perfil = AssetDatabase.LoadAssetAtPath<VolumeProfile>(pv);
        if (perfil == null)
        {
            perfil = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(perfil, pv);
            var bloom = perfil.Add<Bloom>(true);
            bloom.intensity.Override(0.8f);
            bloom.threshold.Override(1.0f);
            var vig = perfil.Add<Vignette>(true);
            vig.intensity.Override(0.22f);
            var tm = perfil.Add<Tonemapping>(true);
            tm.mode.Override(TonemappingMode.Neutral);
            foreach (var c in perfil.components) AssetDatabase.AddObjectToAsset(c, perfil);
            AssetDatabase.SaveAssets();
        }
        var vol = new GameObject("Volumen_Global").AddComponent<Volume>();
        vol.isGlobal = true;
        vol.sharedProfile = perfil;
    }

    // ================================================================== NIVEL 1
    public static void ConstruirNivel1()
    {
        if (matsKit == null) PrepararAssets();
        var escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        CrearJerarquia("LVL_01");

        // 68 columnas de 2 m = 136 m. Valores en metros.
        float[] suelo = {
            0,0,0,0, 0.5f, 1,1,1, 1,1, 2.5f,2.5f, 1,1,1,1,            // T1-T2 (0-32)
            0.5f,0.5f, 0.5f,0.5f,0.5f,0.5f, 1,1, 1.5f,2, 2.5f,2.5f, 2,     // T2-T3 (32-58)
            1,1,1,1,1,                                                // T4 (58-68)
            1.5f,1.5f, 3,3,3,3, 2, 1,1,                                // T5 (68-86)
            0.5f,0.5f,0.5f,0.5f, 0.5f,0.5f,0.5f,0.5f,                  // T6 (86-102)
            0.5f,0.5f,0.5f,0.5f,0.5f,0.5f,0.5f,0.5f,0.5f,              // T7 (102-120)
            0.5f,0.5f, 0.5f,1, 1,1,1,1 };                              // T8 (120-136)
        float[] techo = {
            8,8,8,8,8,8,8,8,8, 8.5f,8.5f,8.5f,                         // 0-24
            8,8, 6.5f,6.5f,6.5f,6.5f, 8,8,8,8,                         // 24-44
            9,9,9,9,9,9, 8,8,8,8,8,8,                                  // 44-68
            7.5f,7.5f,7.5f,7.5f,7.5f,7.5f, 7,7,7,                      // 68-86
            7.5f,7.5f,7.5f,7.5f, 5.5f,5.5f,5.5f,5.5f,                  // 86-102
            7,7,7, 6.5f,6.5f,6.5f, 6,6,6,                              // 102-120
            2.5f,2.5f, 5,5, 7.5f,7.5f,7.5f,7.5f };                     // 120-136
        int n = suelo.Length;
        float[] tope = new float[n];
        for (int i = 0; i < n; i++) tope[i] = i < 12 ? techo[i] + 1.6f : 14f; // T1: techo delgado, se ve el pueblo encima

        ConstruirPerfil(suelo, techo, tope, null);
        var terracota = new Dictionary<string, Material>();
        if (Mat("ladrillo_claro") != null) { terracota["piedra_arena"] = Mat("ladrillo_claro"); terracota["piedra_remate"] = Mat("ladrillo"); }
        ConstruirFondo(suelo, techo, tope, "piedra_azul_oscura", terracota, true, 2.6f, 5.4f);

        // Plataformas seguras (ámbar)
        Plataforma(3f, 0f);
        Plataforma(41f, 0.5f);
        Plataforma(65f, 1f);
        Plataforma(93f, 0.5f);
        PonerPrefab("Kit_Tunel/PF_PlataformaLlegada", new Vector3(131f, 1f, 0f), tPlat);

        // Obstáculos
        string[] bolsas = { "Restos/rocas_pila", "Restos/bidon_oxidado", "Restos/bolsa_rota", "Restos/rocas_pila", "Restos/bolsa_negra" };
        Monton(13f, 1f, bolsas, 2.1f, 1);
        Obstaculo("Restos/caja_rota", new Vector3(21f, 2.5f, 0f), new Vector3(0, 15, 0), Vector3.one * 3f);
        Obstaculo("Restos/bolsa_rota", new Vector3(22.6f, 2.5f, 0.2f), new Vector3(0, -30, 0), Vector3.one * 2.1f);
        RejillaColgante(35f, 6.5f, -25f);
        string[] granMonton = { "Restos/rocas_pila", "Restos/caja_rota", "Restos/bidon_oxidado", "Restos/bolsa_rota", "Restos/rocas_pila", "Restos/bolsa_negra" };
        Monton(54f, 2.5f, granMonton, 2.1f, 2);
        TuboColgante(61f, 8f, 1.3f);
        RejillaColgante(75f, 7.5f, 20f);
        Obstaculo("Restos/neumatico_roca", new Vector3(79f, 3f, 0f), new Vector3(0, 20, 0), Vector3.one * 1.8f);
        RejillaColgante(97f, 5.5f, -20f, "Ruinas_e_industria/rejilla_rota");
        TuboColgante(103.5f, 7f, 1.2f, 30f);
        Monton(106.5f, 0.5f, new[] { "Restos/bolsa_negra", "Restos/caja_rota", "Restos/rocas_pila", "Restos/bolsa_rota" }, 2.2f, 3);
        TuboColgante(109.3f, 6.5f, 1.3f, -20f);
        Monton(112f, 0.5f, new[] { "Restos/rocas_pila", "Restos/bidon_oxidado", "Restos/bolsa_rota" }, 2.2f, 4);
        Monton(116.2f, 0.5f, new[] { "Restos/neumatico_roca", "Restos/caja_rota", "Restos/rocas_pila" }, 2.0f, 5);

        // Baterías (algunas fuera de la ruta directa)
        Bateria(9f, 3.2f);
        Bateria(25f, 6.2f);
        Bateria(46f, 2.4f);
        Bateria(58f, 7.2f);
        Bateria(84f, 2.2f);
        Bateria(101f, 2f);
        Bateria(116f, 4.6f);
        Bateria(125f, 3.6f);

        // Luz y haces
        HazDeLuz(45f, 9f, 0.5f);
        HazDeLuz(87f, 7.5f, 0.5f);
        HazDeLuz(131f, 7.5f, 1f);

        // Fondo lejano: pueblo y volcán sobre el primer tramo
        Poner("Edificios/casa_ladrillo", new Vector3(-2f, 7.5f, 26f), new Vector3(0, 20, 0), Vector3.one * 1.6f, tFondo);
        Poner("Edificios/clinica", new Vector3(8f, 7.2f, 30f), new Vector3(0, -15, 0), Vector3.one * 1.6f, tFondo);
        Poner("Edificios/casa_ladrillo", new Vector3(17f, 7.5f, 28f), new Vector3(0, 40, 0), Vector3.one * 1.5f, tFondo);
        Poner("Naturaleza/montana", new Vector3(14f, 4f, 70f), new Vector3(0, 30, 0), new Vector3(6f, 4f, 6f), tFondo);

        Iluminacion(new Color(0.55f, 0.5f, 0.48f), new Color(0.38f, 0.33f, 0.32f), new Color(0.2f, 0.18f, 0.18f),
            new Color(1f, 0.9f, 0.78f), 1.3f, new Color(0.32f, 0.3f, 0.35f), 25f, 90f);

        Sistemas("Galería de captación", "Lleva el dron a la central para localizar las fallas de la red.", n * ANCHO, 3.2f, 5.5f, new Vector3(3f, 0.3f, 0f));
        EditorSceneManager.SaveScene(escena, R + "Scenes/Niveles/LVL_01_GaleriaCaptacion.unity");
    }

    // ================================================================== NIVEL 2
    public static void ConstruirNivel2()
    {
        if (matsKit == null) PrepararAssets();
        var escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        CrearJerarquia("LVL_02");

        float[] suelo = {
            0,0,0,0,0,0,0,0, 0,                                        // 0-18
            1,1,1,1,1,1,1, 0.5f,0.5f,                                  // 18-36
            0.5f,0.5f,0.5f,0.5f,0.5f,0.5f,0.5f,0.5f,                   // 36-52
            1,1,1,1,1,1,1,1,                                           // 52-68
            0.5f,0.5f,0.5f,0.5f, 2.5f,2.5f, 0.5f,0.5f,0.5f,            // 68-86
            0.5f,0.5f,0.5f,0.5f,0.5f,0.5f,0.5f,0.5f,                   // 86-102
            0.5f,0.5f,0.5f, 1,1,1,1,1,                                 // 102-118
            4,4,4,4,4, -0.5f,-0.5f,-0.5f,-0.5f };                      // 118-136: repisa y cámara baja
        float[] techo = {
            7.5f,7.5f,7.5f,7.5f,7.5f,7.5f,7.5f,7.5f,7.5f,
            7.5f,7.5f,7.5f,7.5f,7.5f,7.5f,7.5f,7.5f,7.5f,
            7.5f,7.5f, 6,6,6,6,6,6,
            7,7,7,7,7,7,7,7,
            8,8,8,8,8,8,8,8,8,
            6.5f,6.5f,6.5f,6.5f,6.5f,6.5f,6.5f,6.5f,
            7,7,7,7,7,7,7,7,
            7.5f,7.5f,7.5f,7.5f,7.5f, 7.5f, 3,3,3 };                             // abertura en x 128-130, techo bajo sobre la llegada
        int n = suelo.Length;
        float[] tope = new float[n];
        for (int i = 0; i < n; i++) tope[i] = 14f;

        var pizarra = new Dictionary<string, Material>();
        if (Mat("piedra_azul") != null) { pizarra["piedra_arena"] = Mat("piedra_azul"); pizarra["piedra_remate"] = Mat("piedra_azul_oscura"); }
        ConstruirPerfil(suelo, techo, tope, pizarra);
        var arcoAzul = new Dictionary<string, Material>();
        if (Mat("metal_azulado") != null) { arcoAzul["ladrillo_claro"] = Mat("piedra_azul"); arcoAzul["piedra_arena"] = Mat("piedra_azul"); arcoAzul["piedra_remate"] = Mat("piedra_azul_oscura"); }
        ConstruirFondo(suelo, techo, tope, "piedra_azul_oscura", arcoAzul, true, 2.2f, 4.8f);

        Plataforma(3f, 0f);
        Plataforma(37f, 0.5f);
        Plataforma(71f, 0.5f);
        Plataforma(105f, 0.5f);
        PonerPrefab("Kit_Tunel/PF_PlataformaLlegada", new Vector3(133.5f, -0.5f, 0f), tPlat);

        // Zigzag piso / techo (pasos de 2 m o más)
        ObstaculoConBase("Ruinas_e_industria/tubo_roto_base", 10f, 0f, 1, Vector3.zero, Vector3.one * 2.6f);
        TuboColgante(15f, 7.5f, 2.4f);
        ObstaculoConBase("Equipos/bomba_agua", 21f, 1f, 2, Vector3.zero, Vector3.one * 2.6f);
        TuboColgante(26f, 7.5f, 2.4f, 25f);
        ObstaculoConBase("Restos/bidon_oxidado", 31f, 1f, 2, Vector3.zero, new Vector3(2.2f, 1.3f, 2.2f));
        ObstaculoConBase("Ruinas_e_industria/carcasa_rota", 44f, 0.5f, 1, Vector3.zero, Vector3.one * 2.4f);
        RejillaColgante(48.5f, 6f, 25f);
        ObstaculoConBase("Ruinas_e_industria/tubo_roto_base", 54f, 1f, 1, new Vector3(0, 180, 0), Vector3.one * 2.4f);
        Poner("Ruinas_e_industria/chapa_colgante", new Vector3(58.5f, 7f - 2.4f * 1.9f, 0f), Vector3.zero, Vector3.one * 1.9f, tObst);
        ColliderObstaculo(tObst.GetChild(tObst.childCount - 1).gameObject, 0.85f);
        ObstaculoConBase("Equipos/bomba_agua", 62.5f, 1f, 2, new Vector3(0, 180, 0), Vector3.one * 2.4f);
        TuboColgante(66.5f, 7f, 2.2f, -30f);
        Obstaculo("Equipos/bomba_agua", new Vector3(79f, 2.5f, 0f), Vector3.zero, Vector3.one * 2.2f);
        TuboColgante(83.5f, 8f, 2.4f);
        ObstaculoConBase("Restos/bidon_oxidado", 88f, 0.5f, 2, Vector3.zero, new Vector3(2.4f, 1.6f, 2.4f));
        RejillaColgante(92f, 6.5f, -25f);
        ObstaculoConBase("Ruinas_e_industria/tubo_roto_base", 96f, 0.5f, 1, Vector3.zero, Vector3.one * 2.4f);
        TuboColgante(100f, 6.5f, 2.0f, 40f);
        ObstaculoConBase("Ruinas_e_industria/carcasa_rota", 110f, 1f, 1, new Vector3(0, 180, 0), Vector3.one * 2.4f);
        TuboColgante(113.5f, 7f, 2.2f);
        ObstaculoConBase("Restos/bidon_oxidado", 117f, 1f, 1, new Vector3(0, 0, 80), new Vector3(2f, 1.3f, 2f));

        Bateria(7f, 3.5f);
        Bateria(18f, 1.6f);
        Bateria(33f, 6.6f);
        Bateria(51f, 2f);
        Bateria(68f, 6.2f);
        Bateria(86f, 3.2f);
        Bateria(102f, 5.6f);
        Bateria(121f, 5.4f);
        Bateria(131f, 1.2f);

        HazDeLuz(39f, 6f, 0.5f);
        HazDeLuz(74f, 8f, 0.5f);
        HazDeLuz(129f, 7.5f, -0.5f);

        Iluminacion(new Color(0.3f, 0.38f, 0.42f), new Color(0.18f, 0.24f, 0.27f), new Color(0.08f, 0.1f, 0.12f),
            new Color(0.75f, 0.85f, 0.95f), 0.9f, new Color(0.1f, 0.15f, 0.18f), 20f, 75f);

        Sistemas("Túnel de presión", "Atraviesa el túnel de presión y acopla el dron a la central.", n * ANCHO, 3f, 5.2f, new Vector3(3f, 0.3f, 0f));
        EditorSceneManager.SaveScene(escena, R + "Scenes/Niveles/LVL_02_TunelPresion.unity");
    }

    // ------------------------------------------------------------------ build
    public static void RegistrarEnBuild()
    {
        string n1 = R + "Scenes/Niveles/LVL_01_GaleriaCaptacion.unity";
        string n2 = R + "Scenes/Niveles/LVL_02_TunelPresion.unity";
        var lista = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(n1, true), new EditorBuildSettingsScene(n2, true) };
        foreach (var s in EditorBuildSettings.scenes)
            if (s.path != n1 && s.path != n2) lista.Add(s);
        EditorBuildSettings.scenes = lista.ToArray();
    }
}
