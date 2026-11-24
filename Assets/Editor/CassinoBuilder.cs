using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>
/// Monta o projeto inteiro com um clique: sprites placeholder, ScriptableObjects, prefabs, cena e UI.
/// Menu: Cassino Survivals > Build Project.  Batch: -executeMethod CassinoBuilder.BuildAll
/// Rodar de novo recria prefabs e a cena Main.unity, mas preserva os .asset de armas/inimigos já existentes (seus ajustes de balanceamento).
/// </summary>
public static class CassinoBuilder
{
    const string ArtDir = "Assets/Art/Sprites";
    const string WeaponDir = "Assets/Data/Weapons";
    const string EnemyDir = "Assets/Data/Enemies";
    const string PrefabDir = "Assets/Prefabs";
    const string ScenePath = "Assets/Scenes/Main.unity";
    const float MapHalf = 30f;

    const string PipelineDir = "Assets/Settings";
    const string UnlitSpriteMat = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

    static Font font;
    static Material spriteMat;

    [MenuItem("Cassino Survivals/Build Project (Sprites, Data, Prefabs, Scene)")]
    public static void BuildAll()
    {
        SelfCheck();
        foreach (var d in new[] { ArtDir, WeaponDir, EnemyDir, PrefabDir, PipelineDir, "Assets/Scenes" }) Directory.CreateDirectory(d);
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        SetupURP2D();
        spriteMat = AssetDatabase.LoadAssetAtPath<Material>(UnlitSpriteMat);
        Check(spriteMat != null, "material Sprite-Unlit-Default do URP não encontrado");

        // ---------- Sprites primitivos ----------
        var white = new Color32(255, 255, 255, 255);
        var clear = new Color32(0, 0, 0, 0);
        var square = MakeSprite("Square", 4, 4, (x, y, n) => white);
        var circle = MakeSprite("Circle", 64, 64, (x, y, n) =>
            (new Vector2(x + 0.5f, y + 0.5f) - new Vector2(n / 2f, n / 2f)).sqrMagnitude <= n * n / 4f ? white : clear);
        var diamond = MakeSprite("Diamond", 64, 64, (x, y, n) =>
            Mathf.Abs(x + 0.5f - n / 2f) + Mathf.Abs(y + 0.5f - n / 2f) <= n / 2f ? white : clear);
        var triangle = MakeSprite("Triangle", 64, 64, (x, y, n) =>
            Mathf.Abs(x + 0.5f - n / 2f) <= (n - y) / 2f ? white : clear);
        var floorTile = MakeSprite("FloorTile", 32, 16, (x, y, n) =>
            x == 0 || y == 0 ? new Color32(120, 30, 140, 255) : new Color32(22, 12, 34, 255), tiled: true);

        // ---------- Armas (ScriptableObjects) ----------
        var pistol = Asset<WeaponData>($"{WeaponDir}/PistolaNeon.asset", w =>
        {
            w.weaponName = "Pistola Neon"; w.projectileColor = new Color(0.2f, 1f, 1f);
            w.baseDamage = 10; w.damagePerLevel = 4; w.baseMagazine = 8; w.magazinePerLevel = 1;
            w.baseReloadTime = 1.4f; w.fireInterval = 0.22f; w.range = 10; w.projectileSpeed = 16;
        });
        var shotgun = Asset<WeaponData>($"{WeaponDir}/EscopetaRoyalFlush.asset", w =>
        {
            w.weaponName = "Escopeta Royal Flush"; w.projectileColor = new Color(1f, 0.55f, 0.1f);
            w.baseDamage = 7; w.damagePerLevel = 3; w.baseMagazine = 10; w.magazinePerLevel = 1;
            w.baseReloadTime = 1.8f; w.fireInterval = 0.5f; w.projectilesPerShot = 5; w.spreadAngle = 35;
            w.range = 7; w.projectileSpeed = 13; w.projectileLifetime = 0.6f;
        });
        var smg = Asset<WeaponData>($"{WeaponDir}/SMGViceCity.asset", w =>
        {
            w.weaponName = "SMG Vice City"; w.projectileColor = new Color(1f, 0.3f, 0.8f);
            w.baseDamage = 4; w.damagePerLevel = 2; w.baseMagazine = 24; w.magazinePerLevel = 3;
            w.baseReloadTime = 2.2f; w.fireInterval = 0.07f; w.range = 9; w.projectileSpeed = 18;
            w.projectileScale = 0.22f;
        });
        var revolver = Asset<WeaponData>($"{WeaponDir}/RevolverHighRoller.asset", w =>
        {
            w.weaponName = "Revólver High Roller"; w.projectileColor = new Color(1f, 0.85f, 0.2f);
            w.baseDamage = 35; w.damagePerLevel = 12; w.baseMagazine = 6; w.magazinePerLevel = 0.5f;
            w.baseReloadTime = 2f; w.fireInterval = 0.6f; w.range = 14; w.projectileSpeed = 24;
            w.projectileScale = 0.4f;
        });
        var dice = Asset<WeaponData>($"{WeaponDir}/LancaDados.asset", w =>
        {
            w.weaponName = "Lança-Dados"; w.projectileColor = new Color(0.4f, 1f, 0.3f);
            w.baseDamage = 18; w.damagePerLevel = 6; w.baseMagazine = 3; w.magazinePerLevel = 0.34f;
            w.baseReloadTime = 2.5f; w.fireInterval = 0.35f; w.projectilesPerShot = 3; w.spreadAngle = 45;
            w.range = 9; w.projectileSpeed = 9; w.projectileLifetime = 2.5f; w.projectileScale = 0.55f;
        });
        var allWeapons = new[] { pistol, shotgun, smg, revolver, dice };

        // ---------- Inimigos (ScriptableObjects) ----------
        var goon = Asset<EnemyData>($"{EnemyDir}/Capanga.asset", e =>
        {
            e.enemyName = "Capanga"; e.sprite = square; e.color = new Color(0.9f, 0.2f, 0.25f); e.scale = 0.8f;
            e.maxHp = 20; e.contactDamage = 8; e.moveSpeed = 2.6f; e.points = 10;
        });
        var runner = Asset<EnemyData>($"{EnemyDir}/CrupieVeloz.asset", e =>
        {
            e.enemyName = "Crupiê Veloz"; e.sprite = diamond; e.color = new Color(0.8f, 0.2f, 1f); e.scale = 0.6f;
            e.maxHp = 12; e.contactDamage = 6; e.moveSpeed = 4f; e.points = 15;
        });
        var elite = Asset<EnemyData>($"{EnemyDir}/SegurancaElite.asset", e =>
        {
            e.enemyName = "Segurança Elite"; e.sprite = square; e.color = new Color(0.45f, 0.1f, 0.8f); e.scale = 1.4f;
            e.mass = 4; e.maxHp = 220; e.contactDamage = 18; e.moveSpeed = 2.2f; e.points = 100; e.guaranteedChip = true;
        });
        var tiger = Asset<EnemyData>($"{EnemyDir}/Tigrinho.asset", e =>
        {
            e.enemyName = "Tigrinho"; e.sprite = circle; e.color = new Color(1f, 0.5f, 0f); e.scale = 4f;
            e.mass = 1000; e.maxHp = 60000; e.fixedHp = true; e.contactDamage = 45; e.contactCooldown = 0.8f;
            e.moveSpeed = 2.4f; e.points = 10000; e.isBoss = true;
            e.chipDropChance = e.healDropChance = e.pointsDropChance = 0;
        });

        // ---------- Prefabs (pooled) ----------
        var projGo = SpriteGO("Projectile", circle, Color.white, 4, Vector2.zero, Vector2.one * 0.3f);
        projGo.gameObject.AddComponent<Projectile>();
        var projectilePrefab = SavePrefab(projGo.gameObject, "Projectile").GetComponent<Projectile>();

        var enemyGo = SpriteGO("Enemy", square, Color.red, 1, Vector2.zero, Vector2.one).gameObject;
        SetupBody(enemyGo.AddComponent<Rigidbody2D>(), 1f);
        enemyGo.AddComponent<CircleCollider2D>().radius = 0.5f;
        enemyGo.AddComponent<Enemy>();
        var enemyPrefab = SavePrefab(enemyGo, "Enemy").GetComponent<Enemy>();

        var dropGo = SpriteGO("Drop", diamond, Color.yellow, 0, Vector2.zero, Vector2.one * 0.5f).gameObject;
        dropGo.AddComponent<Drop>();
        var dropPrefab = SavePrefab(dropGo, "Drop").GetComponent<Drop>();

        // ---------- Cena ----------
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 9f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.02f, 0.08f);
        camGo.transform.position = new Vector3(0, 0, -10);
        camGo.AddComponent<AudioListener>();
        var follow = camGo.AddComponent<CameraFollow>();

        // Mapa: piso em grid neon (Tiled = 1 draw call) + 4 paredes com collider
        var map = new GameObject("Map").transform;
        var floor = SpriteGO("Floor", floorTile, Color.white, -10, Vector2.zero, Vector2.one, map);
        floor.drawMode = SpriteDrawMode.Tiled;
        floor.size = Vector2.one * MapHalf * 2f;
        var wallColor = new Color(1f, 0.2f, 0.7f);
        float len = MapHalf * 2f + 2f;
        Wall("Wall_N", square, wallColor, new Vector2(0, MapHalf + 0.5f), new Vector2(len, 1), map);
        Wall("Wall_S", square, wallColor, new Vector2(0, -MapHalf - 0.5f), new Vector2(len, 1), map);
        Wall("Wall_E", square, wallColor, new Vector2(MapHalf + 0.5f, 0), new Vector2(1, len), map);
        Wall("Wall_W", square, wallColor, new Vector2(-MapHalf - 0.5f, 0), new Vector2(1, len), map);

        // Jogador
        var playerSr = SpriteGO("Player", circle, new Color(0.6f, 1f, 1f), 3, Vector2.zero, Vector2.one * 0.8f);
        var playerGo = playerSr.gameObject;
        playerGo.tag = "Player";
        SetupBody(playerGo.AddComponent<Rigidbody2D>(), 3f);
        playerGo.AddComponent<CircleCollider2D>().radius = 0.5f;
        playerGo.AddComponent<PlayerController>();
        var weapons = playerGo.AddComponent<WeaponSystem>();
        weapons.startingWeapons = new[] { pistol };
        var arrow = SpriteGO("WayfinderArrow", triangle, new Color(1f, 0.9f, 0.2f), 5, Vector2.zero, Vector2.one * 0.5f, playerGo.transform);
        playerGo.AddComponent<Wayfinder>().arrow = arrow.transform;
        follow.target = playerGo.transform;

        // Managers
        var managers = new GameObject("Managers");
        managers.AddComponent<GameManager>();
        managers.AddComponent<AudioSource>();
        managers.AddComponent<AudioManager>();
        var pools = managers.AddComponent<PoolManager>();
        pools.projectilePrefab = projectilePrefab;
        pools.enemyPrefab = enemyPrefab;
        pools.dropPrefab = dropPrefab;
        var spawner = managers.AddComponent<EnemySpawner>();
        spawner.commonTypes = new[] { goon, runner };
        spawner.eliteType = elite;
        spawner.bossType = tiger;
        spawner.mapHalfExtents = Vector2.one * MapHalf;
        var drops = managers.AddComponent<DropManager>();
        drops.chipSprite = diamond;
        drops.healSprite = circle;
        drops.pointsSprite = square;

        // Caça-níqueis em pontos fixos
        var slots = new GameObject("SlotMachines").transform;
        foreach (var p in new[] { new Vector2(-18, 18), new Vector2(18, 18), new Vector2(-18, -18), new Vector2(18, -18), new Vector2(0, 14) })
            SlotMachineGO(p, square, slots);

        // Ajudantes espalhados
        var helpers = new GameObject("Helpers").transform;
        foreach (var p in new[] { new Vector2(10, -6), new Vector2(-14, 4), new Vector2(6, 24) })
            SpriteGO("Helper", circle, Color.green, 2, p, Vector2.one * 0.55f, helpers).gameObject.AddComponent<Helper>();

        BuildUI(weapons, allWeapons);

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

        // Linux + Intel Iris Xe: Vulkan (Mesa ANV) com fallback OpenGL Core
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneLinux64, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneLinux64, new[] { GraphicsDeviceType.Vulkan, GraphicsDeviceType.OpenGLCore });

        AssetDatabase.SaveAssets();
        Debug.Log("[CassinoBuilder] Projeto montado. Abra Assets/Scenes/Main.unity e aperte Play.");
    }

    // ================= UI =================

    static void BuildUI(WeaponSystem weapons, WeaponData[] allWeapons)
    {
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>(); // Input Manager clássico

        var neon = new Color(1f, 0.3f, 0.8f);
        var gold = new Color(1f, 0.85f, 0.15f);

        // Canvas estático: nunca muda depois de criado
        var stat = NewCanvas("Canvas_Static", 0, false).transform;
        Img(stat, "HpBarBG", TL, TL, new Vector2(30, -30), new Vector2(420, 34), new Color(0.1f, 0.05f, 0.15f, 0.9f));

        // Canvas dinâmico do HUD (sem GraphicRaycaster: nada clicável)
        var hudCanvas = NewCanvas("Canvas_HUD", 1, false);
        var hudT = hudCanvas.transform;
        var hud = hudCanvas.gameObject.AddComponent<HUD>();
        hud.weaponSystem = weapons;
        hud.hpFill = Img(hudT, "HpFill", TL, TL, new Vector2(34, -34), new Vector2(412, 26), neon).rectTransform;
        hud.hpText = Txt(hudT, "HpText", TL, TL, new Vector2(30, -30), new Vector2(420, 34), "", 22, TextAnchor.MiddleCenter, Color.white);
        hud.scoreText = Txt(hudT, "Score", TR, TR, new Vector2(-30, -30), new Vector2(500, 40), "", 32, TextAnchor.UpperRight, Color.white);
        hud.chipsText = Txt(hudT, "Chips", TR, TR, new Vector2(-30, -75), new Vector2(500, 40), "", 32, TextAnchor.UpperRight, gold);
        hud.timerText = Txt(hudT, "Timer", TC, TC, new Vector2(0, -30), new Vector2(700, 40), "", 30, TextAnchor.UpperCenter, new Color(1f, 0.6f, 0.2f));
        hud.weaponsText = Txt(hudT, "Weapons", TL, TL, new Vector2(30, -80), new Vector2(800, 400), "", 24, TextAnchor.UpperLeft, Color.white);
        hud.bannerText = Txt(hudT, "Banner", MC, MC, new Vector2(0, 220), new Vector2(1400, 100), "", 64, TextAnchor.MiddleCenter, new Color(1f, 0.5f, 0f));

        // Modal do caça-níquel
        var modalCanvas = NewCanvas("Canvas_SlotMachine", 10, true);
        var ui = modalCanvas.gameObject.AddComponent<SlotMachineUI>();
        var panel = Stretch(modalCanvas.transform, "Panel", new Color(0, 0, 0, 0.75f));
        Img(panel, "Box", MC, MC, Vector2.zero, new Vector2(1040, 560), new Color(0.12f, 0.04f, 0.18f, 0.97f));
        Txt(panel, "Title", MC, MC, new Vector2(0, 220), new Vector2(1000, 80), "CAÇA-NÍQUEL", 56, TextAnchor.MiddleCenter, gold);
        for (int i = 0; i < 3; i++)
        {
            var b = Img(panel, $"Slot{i}", MC, MC, new Vector2(-320 + 320 * i, 20), new Vector2(280, 260), Color.white, true);
            var btn = b.gameObject.AddComponent<Button>();
            btn.targetGraphic = b;
            ui.slotButtons[i] = btn;
            ui.slotImages[i] = b;
            var label = Txt(b.transform, "Label", MC, MC, Vector2.zero, new Vector2(260, 240), "?", 30, TextAnchor.MiddleCenter, Color.black);
            label.fontStyle = FontStyle.Bold;
            ui.slotLabels[i] = label;
        }
        ui.resultText = Txt(panel, "Result", MC, MC, new Vector2(0, -200), new Vector2(1000, 70), "", 30, TextAnchor.MiddleCenter, Color.white);
        ui.panel = panel.gameObject;
        ui.weaponPool = allWeapons;
        ui.weaponSystem = weapons;

        // Game Over / Vitória
        var endCanvas = NewCanvas("Canvas_EndScreen", 20, true);
        var end = endCanvas.gameObject.AddComponent<EndScreenUI>();
        var endPanel = Stretch(endCanvas.transform, "Panel", new Color(0, 0, 0, 0.85f));
        end.titleText = Txt(endPanel, "Title", MC, MC, new Vector2(0, 140), new Vector2(1600, 130), "", 80, TextAnchor.MiddleCenter, Color.white);
        end.statsText = Txt(endPanel, "Stats", MC, MC, new Vector2(0, 10), new Vector2(1000, 120), "", 36, TextAnchor.MiddleCenter, Color.white);
        var rb = Img(endPanel, "Restart", MC, MC, new Vector2(0, -150), new Vector2(380, 90), neon, true);
        end.restartButton = rb.gameObject.AddComponent<Button>();
        end.restartButton.targetGraphic = rb;
        Txt(rb.transform, "Label", MC, MC, Vector2.zero, new Vector2(380, 90), "REINICIAR", 36, TextAnchor.MiddleCenter, Color.white);
        end.panel = endPanel.gameObject;
    }

    static readonly Vector2 TL = new Vector2(0, 1), TR = new Vector2(1, 1), TC = new Vector2(0.5f, 1), MC = new Vector2(0.5f, 0.5f);

    static Canvas NewCanvas(string name, int order, bool raycaster)
    {
        var go = new GameObject(name);
        var c = go.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = order;
        var s = go.AddComponent<CanvasScaler>();
        s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        s.referenceResolution = new Vector2(1920, 1080);
        s.matchWidthOrHeight = 0.5f;
        if (raycaster) go.AddComponent<GraphicRaycaster>();
        return c;
    }

    static RectTransform Rect(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    static Image Img(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Color c, bool raycast = false)
    {
        var img = Rect(parent, name, anchor, pivot, pos, size).gameObject.AddComponent<Image>();
        img.color = c;
        img.raycastTarget = raycast;
        return img;
    }

    static Text Txt(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size,
        string text, int fontSize, TextAnchor align, Color c)
    {
        var t = Rect(parent, name, anchor, pivot, pos, size).gameObject.AddComponent<Text>();
        t.font = font;
        t.text = text;
        t.fontSize = fontSize;
        t.alignment = align;
        t.color = c;
        t.raycastTarget = false;
        t.supportRichText = true;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    static RectTransform Stretch(Transform parent, string name, Color c)
    {
        var rt = Rect(parent, name, MC, MC, Vector2.zero, Vector2.zero);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        var img = rt.gameObject.AddComponent<Image>();
        img.color = c; // raycastTarget ligado: bloqueia cliques no que está atrás
        return rt;
    }

    // ================= Mundo =================

    static SpriteRenderer SpriteGO(string name, Sprite s, Color c, int order, Vector2 pos, Vector2 scale, Transform parent = null)
    {
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sharedMaterial = spriteMat; // Unlit: sem custo de iluminação 2D
        sr.sprite = s;
        sr.color = c;
        sr.sortingOrder = order;
        return sr;
    }

    static void Wall(string name, Sprite s, Color c, Vector2 pos, Vector2 size, Transform parent)
    {
        SpriteGO(name, s, c, -5, pos, size, parent).gameObject.AddComponent<BoxCollider2D>();
    }

    static void SetupBody(Rigidbody2D rb, float mass)
    {
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.mass = mass;
    }

    static void SlotMachineGO(Vector2 pos, Sprite square, Transform parent)
    {
        var root = new GameObject("SlotMachine");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = pos;
        SpriteGO("Body", square, new Color(0.95f, 0.75f, 0.1f), 0, Vector2.zero, new Vector2(1.4f, 2f), root.transform);
        SpriteGO("Screen", square, new Color(0.15f, 0.02f, 0.2f), 1, new Vector2(0, 0.35f), new Vector2(1.1f, 0.6f), root.transform);
        root.AddComponent<BoxCollider2D>().size = new Vector2(1.4f, 2f);
        var sm = root.AddComponent<SlotMachine>();
        WorldText("Label", "777", new Vector2(0, 0.35f), new Color(1f, 0.3f, 0.5f), root.transform);
        sm.prompt = WorldText("Prompt", "[E] GIRAR", new Vector2(0, 1.6f), Color.white, root.transform);
        sm.prompt.gameObject.SetActive(false);
    }

    static TextMesh WorldText(string name, string text, Vector2 pos, Color c, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        var tm = go.AddComponent<TextMesh>();
        tm.font = font;
        tm.text = text;
        tm.fontSize = 48;
        tm.characterSize = 0.08f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = c;
        var mr = go.GetComponent<MeshRenderer>();
        mr.sharedMaterial = font.material;
        mr.sortingOrder = 6;
        return tm;
    }

    // ================= Render Pipeline =================

    /// <summary>URP com 2D Renderer no modo mais leve: sem HDR, MSAA, depth/opaque texture ou pós-processamento.</summary>
    static void SetupURP2D()
    {
        const string assetPath = PipelineDir + "/URP_2D.asset";
        var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(assetPath);
        if (urp == null)
        {
            var renderer = ScriptableObject.CreateInstance<Renderer2DData>();
            AssetDatabase.CreateAsset(renderer, PipelineDir + "/URP_2D_Renderer.asset");
            urp = UniversalRenderPipelineAsset.Create(renderer);
            AssetDatabase.CreateAsset(urp, assetPath);
        }
        urp.supportsHDR = false;
        urp.msaaSampleCount = 1;
        urp.renderScale = 1f;
        urp.supportsCameraDepthTexture = false;
        urp.supportsCameraOpaqueTexture = false;
        EditorUtility.SetDirty(urp);

        GraphicsSettings.defaultRenderPipeline = urp;
        QualitySettings.renderPipeline = null; // usa o default acima em qualquer nível de qualidade
    }

    // ================= Assets =================

    static Sprite MakeSprite(string name, int n, float ppu, Func<int, int, int, Color32> pixel, bool tiled = false)
    {
        string path = $"{ArtDir}/{name}.png";
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
                px[y * n + x] = pixel(x, y, n);
        tex.SetPixels32(px);
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path);
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = ppu;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.filterMode = tiled ? FilterMode.Point : FilterMode.Bilinear;
        if (tiled)
        {
            var s = new TextureImporterSettings();
            imp.ReadTextureSettings(s);
            s.spriteMeshType = SpriteMeshType.FullRect; // obrigatório para SpriteDrawMode.Tiled
            imp.SetTextureSettings(s);
            imp.wrapMode = TextureWrapMode.Repeat;
        }
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    /// <summary>Cria o asset com os valores padrão só se ainda não existir (preserva balanceamento feito no Inspector).</summary>
    static T Asset<T>(string path, Action<T> fill) where T : ScriptableObject
    {
        var a = AssetDatabase.LoadAssetAtPath<T>(path);
        if (a != null) return a;
        a = ScriptableObject.CreateInstance<T>();
        fill(a);
        AssetDatabase.CreateAsset(a, path);
        return a;
    }

    static GameObject SavePrefab(GameObject go, string name)
    {
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabDir}/{name}.prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    // ================= Self-check das regras =================

    static void SelfCheck()
    {
        var a = ScriptableObject.CreateInstance<WeaponData>();
        var b = ScriptableObject.CreateInstance<WeaponData>();
        try
        {
            var reels = new[] { a, a, b };
            Check(SlotRules.CountMatches(reels, a) == 2 && SlotRules.CountMatches(reels, b) == 1, "CountMatches");
            Check(SlotRules.LevelsFor(1) == 1 && SlotRules.LevelsFor(2) == 5 && SlotRules.LevelsFor(3) == 9, "LevelsFor");

            var w = new WeaponInstance(a, 1); // pente padrão = 8
            for (int i = 0; i < 8; i++) Check(w.Tick(1f, true), "deveria disparar com balas no pente");
            Check(w.IsReloading && !w.Tick(0.1f, true), "deveria estar recarregando");
            w.Tick(10f, true);
            Check(!w.IsReloading && w.Ammo == a.Magazine(1), "recarga deveria encher o pente");
            w.AddLevels(9);
            Check(w.Level == 10 && w.Ammo == a.Magazine(10) && a.ReloadTime(10) < a.ReloadTime(1), "escala por nível");
        }
        finally
        {
            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
        }
    }

    static void Check(bool ok, string what)
    {
        if (!ok) throw new Exception("[CassinoBuilder] Self-check falhou: " + what);
    }
}
