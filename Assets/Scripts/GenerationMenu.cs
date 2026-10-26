using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GenerationMenu : MonoBehaviour
{
    public static bool MouseOverMenu;

    [Header("Referencia")]
    public TerrainGenerator terrain;

    [Header("Interfaz")]
    public float uiScale = 1.15f;
    public bool startVisible = true;
    public bool showHelp = true;
    public bool autoApply = true;

    enum Part { All, Trees, Dungeons, Creatures }

    static readonly Color32 cPanel = new Color32(14, 18, 32, 245);
    static readonly Color32 cCard = new Color32(26, 32, 54, 255);
    static readonly Color32 cCardHi = new Color32(44, 54, 88, 255);
    static readonly Color32 cTrack = new Color32(52, 62, 94, 255);
    static readonly Color32 cKnob = new Color32(240, 244, 255, 255);
    static readonly Color32 cText = new Color32(236, 240, 250, 255);
    static readonly Color32 cMuted = new Color32(150, 162, 192, 255);
    static readonly Color32 cBorder = new Color32(120, 132, 166, 255);

    static readonly Color32 cGold = new Color32(205, 145, 30, 255);
    static readonly Color32 cIndigo = new Color32(100, 112, 225, 255);
    static readonly Color32 cBlue = new Color32(50, 135, 225, 255);
    static readonly Color32 cViolet = new Color32(150, 95, 220, 255);
    static readonly Color32 cGreen = new Color32(50, 165, 90, 255);
    static readonly Color32 cOrange = new Color32(215, 120, 40, 255);
    static readonly Color32 cRed = new Color32(205, 65, 80, 255);
    static readonly Color32 cTeal = new Color32(40, 160, 165, 255);
    static readonly Color32 cGray = new Color32(95, 108, 140, 255);

    bool visible;
    Vector2 scroll;

    bool showIntro = true, showGeneral = true, showLayers = true, showTerrain = false,
         showBiomes = false, showTrees = false, showDungeons = false, showCreatures = false,
         showStats = false, showLegend = false;

    bool[] layers = { true, true, true, true };

    bool pendAll, pendTrees, pendDungeons, pendCreatures, applyNow;

    float copiedUntil;

    TerrainType[,] cachedMap;
    float[] percents = new float[4];
    float[] biomePercents = new float[3];

    GUISkin skin;
    GUIStyle sLabel, sMuted, sBody, sTitle, sSub, sValue, sHeaderText, sHeaderSign, sBtnText, sWarn, sCard;
    Dictionary<ulong, GUIStyle> boxStyles = new Dictionary<ulong, GUIStyle>();

    void Start()
    {
        visible = startVisible;
    }

    Rect PanelRect()
    {
        return new Rect(10, 10, 400, Screen.height / uiScale - 20);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab)) visible = !visible;

        Vector2 m = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y) / uiScale;
        MouseOverMenu = visible && PanelRect().Contains(m);

        if (terrain == null) return;

        ApplyLayerVisibility();

        if (HasPending() && (autoApply || applyNow) && !Input.GetMouseButton(0))
            ApplyPending();
    }

    void ApplyLayerVisibility()
    {
        SetLayer(terrain.terrainTilemap, layers[0]);
        if (terrain.trees != null) SetLayer(terrain.trees.treeTilemap, layers[1]);
        if (terrain.dungeons != null) SetLayer(terrain.dungeons.dungeonTilemap, layers[2]);
        if (terrain.creatures != null) SetLayer(terrain.creatures.creatureTilemap, layers[3]);
    }

    void SetLayer(Tilemap tm, bool show)
    {
        if (tm == null) return;
        TilemapRenderer r = tm.GetComponent<TilemapRenderer>();
        if (r != null && r.enabled != show) r.enabled = show;
    }

    bool HasPending()
    {
        return pendAll || pendTrees || pendDungeons || pendCreatures;
    }

    void Mark(Part p)
    {
        switch (p)
        {
            case Part.All: pendAll = true; break;
            case Part.Trees: pendTrees = true; break;
            case Part.Dungeons: pendDungeons = true; break;
            case Part.Creatures: pendCreatures = true; break;
        }
    }

    void ApplyPending()
    {
        if (pendAll)
        {
            terrain.Generate();
        }
        else if (terrain.terrainMap != null)
        {
            if (pendTrees && terrain.trees != null)
                terrain.trees.Generate(terrain.terrainMap, terrain.width, terrain.height, terrain.seed, terrain.biomeMap);
            if (pendDungeons && terrain.dungeons != null)
                terrain.dungeons.Generate(terrain.terrainMap, terrain.width, terrain.height, terrain.seed);
            if (pendTrees || pendDungeons || pendCreatures)
                RegenerateCreatures();
        }

        pendAll = false;
        pendTrees = false;
        pendDungeons = false;
        pendCreatures = false;
        applyNow = false;
    }

    void OnGUI()
    {
        if (terrain == null) return;

        InitStyles();
        GUI.skin = skin;
        GUI.matrix = Matrix4x4.Scale(new Vector3(uiScale, uiScale, 1f));

        if (!visible)
        {
            Rect small = new Rect(10, 10, 200, 32);
            Box(small, cPanel, 10);
            if (GUI.Button(small, "Abrir menú  (Tab)", sBtnText)) visible = true;
            return;
        }

        Rect panel = PanelRect();
        Box(panel, cPanel, 14);

        GUILayout.BeginArea(new Rect(panel.x + 14, panel.y + 12, panel.width - 28, panel.height - 24));

        GUILayout.Label("Generador de mundos", sTitle);
        GUILayout.Label("Mundo de fantasía procedural   |   Tab: ocultar menú", sMuted);
        GUILayout.Space(4);

        GUILayout.BeginHorizontal();
        showHelp = Check("Explicaciones", showHelp, cGold);
        autoApply = Check("Aplicar automático", autoApply, cGold);
        GUILayout.EndHorizontal();

        if (!autoApply && HasPending())
            GUILayout.Label("Hay cambios sin aplicar: usa el botón 'Aplicar cambios' de la sección.", sWarn);
        GUILayout.Space(4);

        scroll = GUILayout.BeginScrollView(scroll, false, false, GUIStyle.none, skin.verticalScrollbar);

        DrawIntro();
        DrawGeneral();
        DrawLayers();
        DrawTerrain();
        DrawBiomes();
        DrawTrees();
        DrawDungeons();
        DrawCreatures();
        DrawStats();
        DrawLegend();

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    void DrawIntro()
    {
        showIntro = Header(showIntro, "Cómo funciona", cGold);
        if (!showIntro) return;

        GUILayout.BeginVertical(sCard);
        GUILayout.Label("Este mundo se crea en 4 capas, una encima de otra. Cada capa usa una técnica distinta:", sBody);
        GUILayout.Label("1. Suelo (ruido fractal): agua, arena, pasto y roca. Los biomas le dan color al pasto.", sBody);
        GUILayout.Label("2. Árboles (L-System): una regla de texto hace crecer cada árbol.", sBody);
        GUILayout.Label("3. Mazmorras (autómata celular): cuevas que se forman por la regla de vecinos.", sBody);
        GUILayout.Label("4. Criaturas: animales y enemigos según el bioma y el terreno.", sBody);
        GUILayout.Space(4);
        GUILayout.Label("Cómo usarlo: elige un tipo de mundo, mueve las barras y mira el resultado. Con 'Capas visibles' puedes ocultar o aislar cada parte para ver qué hace.", sBody);
        GUILayout.Space(4);
        GUILayout.Label("Cámara: WASD o flechas para moverte (Shift = más rápido). Click derecho y arrastrar mueve el mapa. La rueda del mouse hace zoom.", sBody);
        GUILayout.EndVertical();
    }

    void DrawGeneral()
    {
        showGeneral = Header(showGeneral, "Empezar aquí", cIndigo);
        if (!showGeneral) return;

        GUILayout.BeginVertical(sCard);
        Desc("Paso 1: elige un tipo de mundo. Paso 2: ajusta lo que quieras en las secciones de abajo. Paso 3: prueba otros mundos con la semilla al azar.");

        GUILayout.Label("Tipos de mundo", sSub);
        GUILayout.BeginHorizontal();
        if (Btn("Reino de exploración", cGreen)) { ApplyExplorationPreset(); RegenerateAll(); }
        GUILayout.Space(6);
        if (Btn("Tierras de mazmorras", cOrange)) { ApplyDungeonPreset(); RegenerateAll(); }
        GUILayout.EndHorizontal();
        if (showHelp)
        {
            GUILayout.Label("Reino de exploración: mucho pasto, bosques grandes de robles y pocas mazmorras.", sMuted);
            GUILayout.Label("Tierras de mazmorras: más roca, arbustos dispersos y varias mazmorras grandes.", sMuted);
        }

        GUILayout.Space(6);
        if (Btn("Crear otro mundo (semilla al azar)", cBlue))
        {
            terrain.seed = Random.Range(0, 100000);
            RegenerateAll();
        }

        GUILayout.Space(4);
        GUILayout.BeginHorizontal();
        if (Btn("Regenerar", cGray, 28)) RegenerateAll();
        GUILayout.Space(6);
        if (Btn("Centrar cámara", cGray, 28))
        {
            Camera cam = Camera.main;
            if (cam != null)
                cam.transform.position = new Vector3(terrain.width / 2f, terrain.height / 2f, cam.transform.position.z);
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(8);
        terrain.seed = IntSlider("Semilla", terrain.seed, 0, 99999,
            "Es el número del mundo: la misma semilla crea siempre el mismo mundo.", Part.All, cIndigo);
        terrain.width = Snap(IntSlider("Ancho del mapa", terrain.width, 32, 256,
            "Tamaño en casillas. Más grande = más lento de generar.", Part.All, cIndigo), 16);
        terrain.height = Snap(IntSlider("Alto del mapa", terrain.height, 32, 256,
            "Tamaño en casillas. Más grande = más lento de generar.", Part.All, cIndigo), 16);

        GUILayout.EndVertical();
    }

    void DrawLayers()
    {
        showLayers = Header(showLayers, "Capas visibles", cTeal);
        if (!showLayers) return;

        GUILayout.BeginVertical(sCard);
        Desc("Oculta o aísla cada capa para entender qué aporta cada técnica. 'Solo' deja visible únicamente esa capa.");

        LayerRow(0, "Suelo", "Ruido fractal: agua, arena, pasto y roca.", new Color32(70, 160, 60, 255), true);
        LayerRow(1, "Árboles", "L-System: bosques sobre el pasto.", new Color32(25, 105, 40, 255), terrain.trees != null);
        LayerRow(2, "Mazmorras", "Autómata celular: cuevas con una entrada naranja.", new Color32(165, 165, 175, 255), terrain.dungeons != null);
        LayerRow(3, "Criaturas", "Animales, enemigos y bosses.", new Color32(255, 50, 50, 255), terrain.creatures != null);

        GUILayout.Space(4);
        if (Btn("Ver todas las capas", cTeal, 28))
        {
            for (int i = 0; i < layers.Length; i++) layers[i] = true;
        }
        GUILayout.EndVertical();
    }

    void LayerRow(int idx, string name, string desc, Color32 chip, bool available)
    {
        if (!available) return;

        GUILayout.BeginHorizontal();
        Chip(chip);
        GUILayout.Space(8);
        layers[idx] = Check(name, layers[idx], cTeal);
        if (Btn("Solo", cCardHi, 24, 56))
        {
            for (int i = 0; i < layers.Length; i++) layers[i] = (i == idx);
        }
        GUILayout.EndHorizontal();
        if (showHelp) GUILayout.Label(desc, sMuted);
        GUILayout.Space(2);
    }

    void DrawTerrain()
    {
        showTerrain = Header(showTerrain, "Paisaje (ruido fractal)", cBlue);
        if (!showTerrain) return;

        GUILayout.BeginVertical(sCard);
        Desc("Crea el suelo sumando ruido a varias escalas. Cada suma de ruido se llama octava: la primera da las formas grandes y las siguientes agregan detalle.");

        terrain.scale = Slider("Escala", terrain.scale, 5f, 100f, "F0",
            "Bajo = muchas islas pequeñas. Alto = continentes grandes.", Part.All, cBlue);
        terrain.octaves = IntSlider("Octavas", terrain.octaves, 1, 8,
            "Más octavas = costas más detalladas y rugosas.", Part.All, cBlue);
        terrain.persistence = Slider("Persistencia", terrain.persistence, 0f, 1f, "F2",
            "Cuánto pesan los detalles finos. Alto = terreno más irregular.", Part.All, cBlue);
        terrain.lacunarity = Slider("Lacunaridad", terrain.lacunarity, 1f, 4f, "F2",
            "Qué tan rápido crece el detalle en cada octava.", Part.All, cBlue);
        terrain.waterLevel = Slider("Nivel de agua", terrain.waterLevel, 0f, 1f, "F2",
            "Más alto = más agua y menos tierra.", Part.All, cBlue);
        terrain.sandLevel = Slider("Nivel de arena", terrain.sandLevel, 0f, 1f, "F2",
            "Hasta dónde llega la playa.", Part.All, cBlue);
        terrain.grassLevel = Slider("Nivel de pasto", terrain.grassLevel, 0f, 1f, "F2",
            "Hasta dónde llega el pasto. Lo que sobra es roca.", Part.All, cBlue);

        terrain.sandLevel = Mathf.Max(terrain.sandLevel, terrain.waterLevel);
        terrain.grassLevel = Mathf.Max(terrain.grassLevel, terrain.sandLevel);

        ApplyBar(Part.All, cBlue);
        GUILayout.EndVertical();
    }

    void DrawBiomes()
    {
        showBiomes = Header(showBiomes, "Biomas de fantasía", cViolet);
        if (!showBiomes) return;

        GUILayout.BeginVertical(sCard);
        Desc("Un segundo ruido fractal reparte el pasto en 3 biomas: Bosque Encantado (verde esmeralda), Tierras Marchitas (morado) y Valle Cristalino (lila). Cada bioma cambia el color de las hojas y el tipo de animal.");

        terrain.biomeScale = Slider("Tamaño biomas", terrain.biomeScale, 20f, 150f, "F0",
            "Bajo = zonas pequeñas de color. Alto = zonas grandes.", Part.All, cViolet);
        terrain.biomeLevel1 = Slider("Límite 1", terrain.biomeLevel1, 0.2f, 0.8f, "F2",
            "Cuánto pasto es Bosque Encantado (verde esmeralda).", Part.All, cViolet);
        terrain.biomeLevel2 = Slider("Límite 2", terrain.biomeLevel2, 0.2f, 0.8f, "F2",
            "Hasta dónde llegan las Tierras Marchitas. El resto es Valle Cristalino.", Part.All, cViolet);
        terrain.biomeLevel2 = Mathf.Max(terrain.biomeLevel2, terrain.biomeLevel1);

        ApplyBar(Part.All, cViolet);
        GUILayout.EndVertical();
    }

    void DrawTrees()
    {
        showTrees = Header(showTrees, "Árboles (L-System)", cGreen);
        if (!showTrees) return;

        GUILayout.BeginVertical(sCard);
        LSystemTreeGenerator t = terrain.trees;
        if (t == null)
        {
            GUILayout.Label("Falta conectar el generador de árboles (campo Trees de WorldGenerator).", sWarn);
            GUILayout.EndVertical();
            return;
        }

        Desc("Cada árbol crece desde una palabra (por ejemplo F) que se reescribe con reglas. Una 'tortuga' recorre la palabra: F avanza, + y - giran, [ y ] abren y cierran ramas.");

        GUILayout.Label("Tipo de árbol", sSub);
        GUILayout.BeginHorizontal();
        if (Choice("Roble", t.treeType == TreeType.Roble, cGreen) && t.treeType != TreeType.Roble)
        {
            t.treeType = TreeType.Roble;
            Mark(Part.Trees);
        }
        GUILayout.Space(6);
        if (Choice("Arbusto", t.treeType == TreeType.Arbusto, cGreen) && t.treeType != TreeType.Arbusto)
        {
            t.treeType = TreeType.Arbusto;
            Mark(Part.Trees);
        }
        GUILayout.EndHorizontal();
        if (showHelp) GUILayout.Label("Roble = alto y ramificado. Arbusto = bajo y frondoso.", sMuted);
        GUILayout.Space(6);

        t.iterations = IntSlider("Iteraciones", t.iterations, 1, 4,
            "Cuántas veces crece la regla. Más = árbol más grande (con 4 puede tardar).", Part.Trees, cGreen);
        t.angle = Slider("Ángulo", t.angle, 5f, 60f, "F1",
            "Qué tan abiertas salen las ramas.", Part.Trees, cGreen);
        t.angleJitter = Slider("Variación", t.angleJitter, 0f, 30f, "F1",
            "Cambio al azar del ángulo, para que no todos los árboles sean iguales.", Part.Trees, cGreen);
        t.leafRadius = IntSlider("Tamaño de copa", t.leafRadius, 0, 2,
            "Tamaño de las hojas. 0 = solo puntitos.", Part.Trees, cGreen);
        t.density = Slider("Densidad", t.density, 0f, 0.3f, "F2",
            "Cuántos árboles hay dentro de cada bosque.", Part.Trees, cGreen);
        t.forestScale = Slider("Tamaño bosques", t.forestScale, 5f, 50f, "F0",
            "Bajo = bosquecitos. Alto = bosques grandes.", Part.Trees, cGreen);
        t.forestThreshold = Slider("Umbral de bosque", t.forestThreshold, 0f, 1f, "F2",
            "Más alto = menos bosques. En 0 hay árboles por todo el pasto.", Part.Trees, cGreen);
        t.minSpacing = IntSlider("Separación", t.minSpacing, 1, 6,
            "Distancia mínima entre árboles.", Part.Trees, cGreen);

        ApplyBar(Part.Trees, cGreen);
        GUILayout.EndVertical();
    }

    void DrawDungeons()
    {
        showDungeons = Header(showDungeons, "Mazmorras (autómata celular)", cOrange);
        if (!showDungeons) return;

        GUILayout.BeginVertical(sCard);
        DungeonGenerator d = terrain.dungeons;
        if (d == null)
        {
            GUILayout.Label("Falta conectar el generador de mazmorras (campo Dungeons de WorldGenerator).", sWarn);
            GUILayout.EndVertical();
            return;
        }

        Desc("Cada casilla empieza como pared o suelo al azar. Luego, varias veces, cada casilla copia lo que hace la mayoría de sus 8 vecinas. El caos se convierte en cuevas. Después se rellenan las cuevas aisladas y se abre una entrada naranja.");

        d.dungeonCount = IntSlider("Cantidad", d.dungeonCount, 0, 10,
            "Cuántas mazmorras se colocan en el mapa.", Part.Dungeons, cOrange);
        d.minSize = IntSlider("Tamaño mínimo", d.minSize, 12, 60,
            "Lado más chico posible de una mazmorra.", Part.Dungeons, cOrange);
        d.maxSize = IntSlider("Tamaño máximo", d.maxSize, 12, 60,
            "Lado más grande posible de una mazmorra.", Part.Dungeons, cOrange);
        if (d.maxSize < d.minSize) d.maxSize = d.minSize;
        d.fillPercent = Slider("Relleno inicial", d.fillPercent, 0.3f, 0.6f, "F2",
            "Más alto = más paredes y cuevas estrechas. Más bajo = salas amplias.", Part.Dungeons, cOrange);
        d.smoothIterations = IntSlider("Suavizado", d.smoothIterations, 0, 10,
            "0 = ruido sin forma. Con 4 o 5 se forman cuevas naturales.", Part.Dungeons, cOrange);
        d.wallThreshold = IntSlider("Umbral de pared", d.wallThreshold, 1, 7,
            "Qué tan fácil es que una casilla se vuelva pared.", Part.Dungeons, cOrange);
        d.maxWaterFraction = Slider("Agua permitida", d.maxWaterFraction, 0f, 0.3f, "F2",
            "Cuánta agua se tolera bajo la mazmorra.", Part.Dungeons, cOrange);

        bool keep = Check("Conectar toda la cueva", d.keepLargestRegionOnly, cOrange);
        if (keep != d.keepLargestRegionOnly) { d.keepLargestRegionOnly = keep; Mark(Part.Dungeons); }
        if (showHelp) GUILayout.Label("Activo = toda la cueva queda conectada. Apagado = quedan islas sueltas.", sMuted);

        d.chestsPerDungeon = IntSlider("Cofres por mazmorra", d.chestsPerDungeon, 0, 6,
            "Cofres dorados en rincones, lejos de la entrada.", Part.Dungeons, cOrange);
        d.cratesPerDungeon = IntSlider("Cajas por mazmorra", d.cratesPerDungeon, 0, 20,
            "Cajas cafés pegadas a las paredes.", Part.Dungeons, cOrange);

        ApplyBar(Part.Dungeons, cOrange);
        GUILayout.EndVertical();
    }

    void DrawCreatures()
    {
        showCreatures = Header(showCreatures, "Criaturas", cRed);
        if (!showCreatures) return;

        GUILayout.BeginVertical(sCard);
        CreatureGenerator c = terrain.creatures;
        if (c == null)
        {
            GUILayout.Label("Falta conectar el generador de criaturas (campo Creatures de WorldGenerator).", sWarn);
            GUILayout.EndVertical();
            return;
        }

        Desc("Los animales viven en el pasto y su tipo depende del bioma. Los enemigos de afuera dependen del terreno (slime en pasto, escorpión en arena, golem en roca). Dentro de las mazmorras hay espectros y un boss al fondo.");

        GUILayout.Label("Animales", sSub);
        c.animalDensity = Slider("Cantidad", c.animalDensity, 0f, 0.05f, "F3",
            "Cuántos animales hay en el pasto.", Part.Creatures, cRed);
        c.animalSpacing = IntSlider("Separación", c.animalSpacing, 1, 10,
            "Distancia mínima entre animales.", Part.Creatures, cRed);

        GUILayout.Space(4);
        GUILayout.Label("Enemigos de afuera", sSub);
        c.enemyDensity = Slider("Cantidad", c.enemyDensity, 0f, 0.05f, "F3",
            "Cuántos enemigos hay fuera de las mazmorras.", Part.Creatures, cRed);
        c.enemySpacing = IntSlider("Separación", c.enemySpacing, 1, 10,
            "Distancia mínima entre enemigos.", Part.Creatures, cRed);

        GUILayout.Space(4);
        GUILayout.Label("Dentro de las mazmorras", sSub);
        c.enemiesPerDungeon = IntSlider("Espectros", c.enemiesPerDungeon, 0, 12,
            "Espectros verdes por cada mazmorra.", Part.Creatures, cRed);

        bool boss = Check("Boss al fondo de cada mazmorra", c.bossInDungeons, cRed);
        if (boss != c.bossInDungeons) { c.bossInDungeons = boss; Mark(Part.Creatures); }
        if (showHelp) GUILayout.Label("El boss es una cruz violeta, en la zona abierta más lejana de la entrada.", sMuted);

        ApplyBar(Part.Creatures, cRed);
        GUILayout.EndVertical();
    }

    void DrawStats()
    {
        showStats = Header(showStats, "Resultados y estadísticas", cTeal);
        if (!showStats) return;

        if (terrain.terrainMap != null && terrain.terrainMap != cachedMap)
        {
            cachedMap = terrain.terrainMap;
            CountTerrain();
        }

        GUILayout.BeginVertical(sCard);
        Desc("Estos números salen del mundo que se generó. Sirven como resultados cuantitativos para el informe.");

        GUILayout.Label("Tiempos y cantidades", sSub);
        Row("Suelo (ruido fractal)", terrain.lastMs + " ms");
        if (terrain.trees != null)
            Row("Árboles (L-System)", terrain.trees.lastMs + " ms   |   " + terrain.trees.treeCount + " árboles");
        if (terrain.dungeons != null)
        {
            Row("Mazmorras (autómata)", terrain.dungeons.lastMs + " ms   |   " + terrain.dungeons.placedCount + " mazmorras");
            Row("Cofres y cajas", terrain.dungeons.chestCount + " cofres   |   " + terrain.dungeons.crateCount + " cajas");
        }
        if (terrain.creatures != null)
        {
            Row("Criaturas", terrain.creatures.lastMs + " ms");
            Row("Animales / enemigos / bosses",
                terrain.creatures.animalCount + " / " + terrain.creatures.enemyCount + " / " + terrain.creatures.bossCount);
        }

        GUILayout.Space(6);
        GUILayout.Label("Tipos de terreno", sSub);
        Bar("Agua", percents[(int)TerrainType.Water], new Color32(40, 100, 200, 255));
        Bar("Arena", percents[(int)TerrainType.Sand], new Color32(230, 200, 110, 255));
        Bar("Pasto", percents[(int)TerrainType.Grass], new Color32(70, 170, 80, 255));
        Bar("Roca", percents[(int)TerrainType.Rock], new Color32(120, 120, 125, 255));

        GUILayout.Space(6);
        GUILayout.Label("Biomas (porcentaje del pasto)", sSub);
        Bar("Bosque Encantado", biomePercents[(int)BiomeType.Encantado], new Color32(50, 185, 140, 255));
        Bar("Tierras Marchitas", biomePercents[(int)BiomeType.Marchito], new Color32(105, 75, 120, 255));
        Bar("Valle Cristalino", biomePercents[(int)BiomeType.Cristalino], new Color32(215, 170, 235, 255));

        GUILayout.Space(8);
        if (Btn("Copiar resultados al portapapeles", cTeal, 30))
        {
            GUIUtility.systemCopyBuffer = BuildSummary();
            copiedUntil = Time.time + 2.5f;
        }
        if (Time.time < copiedUntil)
            GUILayout.Label("Copiado. Ya puedes pegarlo en tu informe.", sMuted);

        GUILayout.EndVertical();
    }

    void DrawLegend()
    {
        showLegend = Header(showLegend, "Leyenda de colores", cGray);
        if (!showLegend) return;

        GUILayout.BeginVertical(sCard);
        Desc("Qué significa cada color que ves en el mapa.");

        GUILayout.Label("Suelo", sSub);
        LegendRow(new Color32(40, 100, 200, 255), "Agua");
        LegendRow(new Color32(230, 200, 110, 255), "Arena");
        LegendRow(new Color32(120, 120, 125, 255), "Roca");
        LegendRow(new Color32(50, 185, 140, 255), "Pasto del Bosque Encantado");
        LegendRow(new Color32(105, 75, 120, 255), "Pasto de las Tierras Marchitas");
        LegendRow(new Color32(215, 170, 235, 255), "Pasto del Valle Cristalino");

        GUILayout.Space(6);
        GUILayout.Label("Árboles", sSub);
        LegendRow(new Color32(110, 70, 40, 255), "Tronco y ramas");
        LegendRow(new Color32(20, 120, 100, 255), "Hojas en el Bosque Encantado");
        LegendRow(new Color32(60, 40, 70, 255), "Hojas en las Tierras Marchitas");
        LegendRow(new Color32(150, 90, 190, 255), "Hojas en el Valle Cristalino");

        GUILayout.Space(6);
        GUILayout.Label("Mazmorras", sSub);
        LegendRow(new Color32(45, 45, 58, 255), "Pared de la cueva");
        LegendRow(new Color32(165, 165, 175, 255), "Suelo de la cueva");
        LegendRow(new Color32(235, 140, 30, 255), "Entrada");
        LegendRow(new Color32(255, 215, 0, 255), "Cofre");
        LegendRow(new Color32(140, 95, 50, 255), "Caja");

        GUILayout.Space(6);
        GUILayout.Label("Criaturas", sSub);
        LegendRow(new Color32(255, 255, 255, 255), "Ciervo de luz (Bosque Encantado)");
        LegendRow(new Color32(90, 170, 255, 255), "Cuervo (Tierras Marchitas)");
        LegendRow(new Color32(25, 25, 60, 255), "Conejo de cristal (Valle Cristalino)");
        LegendRow(new Color32(255, 50, 50, 255), "Slime (pasto)");
        LegendRow(new Color32(140, 10, 10, 255), "Escorpión (arena)");
        LegendRow(new Color32(230, 60, 200, 255), "Golem (roca)");
        LegendRow(new Color32(150, 255, 60, 255), "Espectro (dentro de mazmorras)");
        LegendRow(new Color32(130, 0, 200, 255), "Boss (cruz de 5 casillas)");

        GUILayout.EndVertical();
    }

    void RegenerateAll()
    {
        terrain.Generate();
    }

    void RegenerateCreatures()
    {
        CreatureGenerator c = terrain.creatures;
        if (c == null || terrain.terrainMap == null) return;

        c.Generate(terrain.terrainMap, terrain.biomeMap, terrain.width, terrain.height, terrain.seed,
                   terrain.trees != null ? terrain.trees.treeTilemap : null, terrain.dungeons);
    }

    void CountTerrain()
    {
        int[] counts = new int[4];
        int[] biomeCounts = new int[3];
        int grass = 0;
        int w = terrain.width, h = terrain.height;

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                counts[(int)cachedMap[x, y]]++;
                if (cachedMap[x, y] == TerrainType.Grass)
                {
                    biomeCounts[(int)terrain.biomeMap[x, y]]++;
                    grass++;
                }
            }
        }

        float total = w * h;
        for (int i = 0; i < 4; i++) percents[i] = 100f * counts[i] / total;
        for (int i = 0; i < 3; i++) biomePercents[i] = grass > 0 ? 100f * biomeCounts[i] / grass : 0f;
    }

    string BuildSummary()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Semilla: " + terrain.seed + "   Mapa: " + terrain.width + "x" + terrain.height);
        sb.AppendLine("Ruido fractal (suelo): " + terrain.lastMs + " ms");
        sb.AppendLine("Agua " + percents[(int)TerrainType.Water].ToString("F1") + " %, arena " +
                      percents[(int)TerrainType.Sand].ToString("F1") + " %, pasto " +
                      percents[(int)TerrainType.Grass].ToString("F1") + " %, roca " +
                      percents[(int)TerrainType.Rock].ToString("F1") + " %");
        sb.AppendLine("Biomas (del pasto): Bosque Encantado " + biomePercents[(int)BiomeType.Encantado].ToString("F1") +
                      " %, Tierras Marchitas " + biomePercents[(int)BiomeType.Marchito].ToString("F1") +
                      " %, Valle Cristalino " + biomePercents[(int)BiomeType.Cristalino].ToString("F1") + " %");
        if (terrain.trees != null)
            sb.AppendLine("L-System: " + terrain.trees.treeCount + " árboles en " + terrain.trees.lastMs + " ms");
        if (terrain.dungeons != null)
            sb.AppendLine("Autómata celular: " + terrain.dungeons.placedCount + " mazmorras en " + terrain.dungeons.lastMs +
                          " ms, " + terrain.dungeons.chestCount + " cofres, " + terrain.dungeons.crateCount + " cajas");
        if (terrain.creatures != null)
            sb.AppendLine("Criaturas: " + terrain.creatures.animalCount + " animales, " + terrain.creatures.enemyCount +
                          " enemigos, " + terrain.creatures.bossCount + " bosses en " + terrain.creatures.lastMs + " ms");
        return sb.ToString();
    }

    void ApplyExplorationPreset()
    {
        terrain.scale = 45f;
        terrain.octaves = 4;
        terrain.persistence = 0.5f;
        terrain.lacunarity = 2f;
        terrain.waterLevel = 0.42f;
        terrain.sandLevel = 0.47f;
        terrain.grassLevel = 0.70f;
        terrain.biomeScale = 80f;
        terrain.biomeLevel1 = 0.42f;
        terrain.biomeLevel2 = 0.60f;

        LSystemTreeGenerator t = terrain.trees;
        if (t != null)
        {
            t.treeType = TreeType.Roble;
            t.iterations = 2;
            t.angle = 22.5f;
            t.angleJitter = 8f;
            t.leafRadius = 1;
            t.density = 0.10f;
            t.forestScale = 18f;
            t.forestThreshold = 0.45f;
            t.minSpacing = 3;
        }

        DungeonGenerator d = terrain.dungeons;
        if (d != null)
        {
            d.dungeonCount = 2;
            d.minSize = 22;
            d.maxSize = 30;
            d.fillPercent = 0.46f;
            d.smoothIterations = 5;
            d.wallThreshold = 4;
            d.chestsPerDungeon = 2;
            d.cratesPerDungeon = 5;
        }

        CreatureGenerator c = terrain.creatures;
        if (c != null)
        {
            c.animalDensity = 0.015f;
            c.animalSpacing = 4;
            c.enemyDensity = 0.004f;
            c.enemySpacing = 6;
            c.enemiesPerDungeon = 3;
            c.bossInDungeons = true;
        }
    }

    void ApplyDungeonPreset()
    {
        terrain.scale = 30f;
        terrain.octaves = 5;
        terrain.persistence = 0.55f;
        terrain.lacunarity = 2f;
        terrain.waterLevel = 0.40f;
        terrain.sandLevel = 0.44f;
        terrain.grassLevel = 0.58f;
        terrain.biomeScale = 50f;
        terrain.biomeLevel1 = 0.50f;
        terrain.biomeLevel2 = 0.58f;

        LSystemTreeGenerator t = terrain.trees;
        if (t != null)
        {
            t.treeType = TreeType.Arbusto;
            t.iterations = 2;
            t.angle = 25f;
            t.angleJitter = 10f;
            t.leafRadius = 1;
            t.density = 0.05f;
            t.forestScale = 14f;
            t.forestThreshold = 0.60f;
            t.minSpacing = 3;
        }

        DungeonGenerator d = terrain.dungeons;
        if (d != null)
        {
            d.dungeonCount = 5;
            d.minSize = 28;
            d.maxSize = 40;
            d.fillPercent = 0.48f;
            d.smoothIterations = 4;
            d.wallThreshold = 4;
            d.chestsPerDungeon = 4;
            d.cratesPerDungeon = 12;
        }

        CreatureGenerator c = terrain.creatures;
        if (c != null)
        {
            c.animalDensity = 0.006f;
            c.animalSpacing = 5;
            c.enemyDensity = 0.010f;
            c.enemySpacing = 4;
            c.enemiesPerDungeon = 7;
            c.bossInDungeons = true;
        }
    }

    void Desc(string text)
    {
        if (!showHelp) return;
        GUILayout.Label(text, sBody);
        GUILayout.Space(4);
    }

    void ApplyBar(Part part, Color32 c)
    {
        if (autoApply) return;
        GUILayout.Space(6);
        if (Btn("Aplicar cambios", c, 30))
        {
            Mark(part);
            applyNow = true;
        }
    }

    bool Header(bool open, string title, Color32 c)
    {
        Rect r = GUILayoutUtility.GetRect(10, 34, GUILayout.ExpandWidth(true));
        bool hover = r.Contains(Event.current.mousePosition);
        Box(r, hover ? Lighten(c) : c, 9);
        bool clicked = GUI.Button(r, GUIContent.none, GUIStyle.none);
        GUI.Label(new Rect(r.x + 14, r.y, r.width - 50, r.height), title, sHeaderText);
        GUI.Label(new Rect(r.xMax - 34, r.y, 24, r.height), open ? "-" : "+", sHeaderSign);
        GUILayout.Space(3);
        return clicked ? !open : open;
    }

    bool Btn(string text, Color32 c, int height = 32, int width = 0)
    {
        Rect r = width > 0
            ? GUILayoutUtility.GetRect(width, height, GUILayout.Width(width))
            : GUILayoutUtility.GetRect(10, height, GUILayout.ExpandWidth(true));

        bool hover = r.Contains(Event.current.mousePosition);
        bool down = hover && Input.GetMouseButton(0);
        Box(r, down ? Darken(c) : (hover ? Lighten(c) : c), 8);
        return GUI.Button(r, text, sBtnText);
    }

    bool Choice(string text, bool selected, Color32 accent)
    {
        Rect r = GUILayoutUtility.GetRect(10, 30, GUILayout.ExpandWidth(true));
        bool hover = r.Contains(Event.current.mousePosition);
        Color32 col = selected ? accent : (hover ? Lighten(cCardHi) : cCardHi);
        Box(r, col, 8);
        return GUI.Button(r, text, sBtnText);
    }

    bool Check(string label, bool value, Color32 accent)
    {
        Rect r = GUILayoutUtility.GetRect(10, 24, GUILayout.ExpandWidth(true));
        bool clicked = GUI.Button(r, GUIContent.none, GUIStyle.none);

        Rect b = new Rect(r.x, r.center.y - 9, 18, 18);
        Box(b, cTrack, 5);
        if (value) Box(new Rect(b.x + 3, b.y + 3, 12, 12), accent, 3);
        GUI.Label(new Rect(r.x + 26, r.y, r.width - 26, r.height), label, sLabel);

        return clicked ? !value : value;
    }

    float SliderCore(string label, float value, float min, float max, string format, string hint,
                     Color32 accent, bool integer, out bool changed)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, sLabel, GUILayout.Width(125), GUILayout.Height(24));
        Rect r = GUILayoutUtility.GetRect(60, 24, GUILayout.ExpandWidth(true));
        float nv = DrawSlider(r, value, min, max, accent);
        if (integer) nv = Mathf.Round(nv);
        GUILayout.Label(nv.ToString(format), sValue, GUILayout.Width(50), GUILayout.Height(24));
        GUILayout.EndHorizontal();

        if (showHelp && !string.IsNullOrEmpty(hint)) GUILayout.Label(hint, sMuted);
        changed = !Mathf.Approximately(nv, value);
        return nv;
    }

    float Slider(string label, float value, float min, float max, string format,
                 string hint, Part part, Color32 accent)
    {
        bool ch;
        float nv = SliderCore(label, value, min, max, format, hint, accent, false, out ch);
        if (ch) Mark(part);
        return nv;
    }

    int IntSlider(string label, int value, int min, int max, string hint, Part part, Color32 accent)
    {
        bool ch;
        float nv = SliderCore(label, value, min, max, "F0", hint, accent, true, out ch);
        if (ch) Mark(part);
        return Mathf.RoundToInt(nv);
    }

    float DrawSlider(Rect r, float value, float min, float max, Color32 accent)
    {
        int id = GUIUtility.GetControlID(FocusType.Passive);
        Event e = Event.current;
        float pad = 8f;
        float x0 = r.x + pad;
        float x1 = r.xMax - pad;

        switch (e.GetTypeForControl(id))
        {
            case EventType.MouseDown:
                if (e.button == 0 && r.Contains(e.mousePosition))
                {
                    GUIUtility.hotControl = id;
                    value = Mathf.Lerp(min, max, Mathf.InverseLerp(x0, x1, e.mousePosition.x));
                    e.Use();
                }
                break;

            case EventType.MouseDrag:
                if (GUIUtility.hotControl == id)
                {
                    value = Mathf.Lerp(min, max, Mathf.InverseLerp(x0, x1, e.mousePosition.x));
                    e.Use();
                }
                break;

            case EventType.MouseUp:
                if (GUIUtility.hotControl == id)
                {
                    GUIUtility.hotControl = 0;
                    e.Use();
                }
                break;

            case EventType.Repaint:
            {
                float t = Mathf.InverseLerp(min, max, value);
                float cy = r.y + r.height / 2f;
                Box(new Rect(x0, cy - 3, x1 - x0, 6), cTrack, 3);
                Box(new Rect(x0, cy - 3, Mathf.Max(6f, (x1 - x0) * t), 6), accent, 3);
                float hx = Mathf.Lerp(x0, x1, t);
                Box(new Rect(hx - 8, cy - 8, 16, 16), cKnob, 8);
                break;
            }
        }
        return Mathf.Clamp(value, min, max);
    }

    void Row(string label, string value)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, sLabel, GUILayout.Height(20));
        GUILayout.FlexibleSpace();
        GUILayout.Label(value, sValue, GUILayout.Height(20));
        GUILayout.EndHorizontal();
    }

    void Bar(string label, float percent, Color32 color)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, sLabel, GUILayout.Width(125), GUILayout.Height(20));
        Rect r = GUILayoutUtility.GetRect(60, 20, GUILayout.ExpandWidth(true));
        Box(new Rect(r.x, r.y + 4, r.width, 12), cTrack, 5);
        float w = r.width * Mathf.Clamp01(percent / 100f);
        if (w > 0.5f) Box(new Rect(r.x, r.y + 4, Mathf.Max(w, 10f), 12), color, 5);
        GUILayout.Label(percent.ToString("F1") + " %", sValue, GUILayout.Width(58), GUILayout.Height(20));
        GUILayout.EndHorizontal();
    }

    void Chip(Color32 c)
    {
        Rect r = GUILayoutUtility.GetRect(18, 24, GUILayout.Width(18));
        Rect chip = new Rect(r.x, r.center.y - 9, 18, 18);
        Box(chip, cBorder, 5);
        Box(new Rect(chip.x + 1, chip.y + 1, chip.width - 2, chip.height - 2), c, 4);
    }

    void LegendRow(Color32 c, string text)
    {
        GUILayout.BeginHorizontal();
        Chip(c);
        GUILayout.Space(8);
        GUILayout.Label(text, sLabel, GUILayout.Height(24));
        GUILayout.EndHorizontal();
    }

    int Snap(int value, int step)
    {
        return Mathf.Max(step, Mathf.RoundToInt(value / (float)step) * step);
    }

    static Color32 Lighten(Color32 c)
    {
        return new Color32((byte)Mathf.Min(255, c.r + 28), (byte)Mathf.Min(255, c.g + 28), (byte)Mathf.Min(255, c.b + 28), c.a);
    }

    static Color32 Darken(Color32 c)
    {
        return new Color32((byte)Mathf.Max(0, c.r - 30), (byte)Mathf.Max(0, c.g - 30), (byte)Mathf.Max(0, c.b - 30), c.a);
    }

    void Box(Rect r, Color32 c, int radius)
    {
        if (Event.current.type != EventType.Repaint) return;

        ulong key = ((ulong)c.r << 24) | ((ulong)c.g << 16) | ((ulong)c.b << 8) | c.a | ((ulong)radius << 32);
        GUIStyle s;
        if (!boxStyles.TryGetValue(key, out s))
        {
            s = new GUIStyle();
            s.normal.background = MakeRoundTex(c, radius);
            s.border = new RectOffset(radius, radius, radius, radius);
            boxStyles[key] = s;
        }
        s.Draw(r, GUIContent.none, false, false, false, false);
    }

    Texture2D MakeRoundTex(Color32 c, int r)
    {
        int s = r * 2 + 2;
        Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.hideFlags = HideFlags.HideAndDontSave;

        Color32[] px = new Color32[s * s];
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                float cx = Mathf.Clamp(x, r, s - r - 1);
                float cy = Mathf.Clamp(y, r, s - r - 1);
                float dx = x - cx;
                float dy = y - cy;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(r + 0.5f - d);
                px[y * s + x] = new Color32(c.r, c.g, c.b, (byte)(c.a * a));
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }

    GUIStyle MakeText(int size, FontStyle fs, Color c, TextAnchor a, bool wrap)
    {
        GUIStyle s = new GUIStyle(skin.label);
        s.fontSize = size;
        s.fontStyle = fs;
        s.alignment = a;
        s.wordWrap = wrap;
        s.margin = new RectOffset(0, 0, 0, 0);
        s.padding = new RectOffset(0, 0, 0, 0);
        s.normal.textColor = c;
        s.hover.textColor = c;
        s.active.textColor = c;
        s.focused.textColor = c;
        return s;
    }

    void InitStyles()
    {
        if (skin != null) return;

        skin = Instantiate(GUI.skin);

        GUIStyle vs = skin.verticalScrollbar;
        vs.normal.background = MakeRoundTex(cTrack, 4);
        vs.border = new RectOffset(4, 4, 4, 4);
        vs.fixedWidth = 8;
        vs.margin = new RectOffset(4, 0, 0, 0);

        GUIStyle th = skin.verticalScrollbarThumb;
        Texture2D thTex = MakeRoundTex(cMuted, 4);
        th.normal.background = thTex;
        th.hover.background = thTex;
        th.active.background = thTex;
        th.border = new RectOffset(4, 4, 4, 4);
        th.fixedWidth = 8;

        skin.verticalScrollbarUpButton.fixedHeight = 0;
        skin.verticalScrollbarUpButton.fixedWidth = 0;
        skin.verticalScrollbarDownButton.fixedHeight = 0;
        skin.verticalScrollbarDownButton.fixedWidth = 0;

        sLabel = MakeText(13, FontStyle.Normal, cText, TextAnchor.MiddleLeft, false);
        sMuted = MakeText(11, FontStyle.Normal, cMuted, TextAnchor.UpperLeft, true);
        sMuted.padding = new RectOffset(0, 0, 1, 5);
        sBody = MakeText(12, FontStyle.Normal, cText, TextAnchor.UpperLeft, true);
        sBody.padding = new RectOffset(0, 0, 0, 3);
        sTitle = MakeText(21, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft, false);
        sSub = MakeText(12, FontStyle.Bold, cMuted, TextAnchor.MiddleLeft, false);
        sSub.padding = new RectOffset(0, 0, 2, 4);
        sValue = MakeText(12, FontStyle.Bold, cText, TextAnchor.MiddleRight, false);
        sHeaderText = MakeText(14, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft, false);
        sHeaderSign = MakeText(20, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, false);
        sBtnText = MakeText(13, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, false);
        sWarn = MakeText(11, FontStyle.Bold, new Color32(255, 170, 70, 255), TextAnchor.UpperLeft, true);
        sWarn.padding = new RectOffset(0, 0, 2, 4);

        sCard = new GUIStyle();
        sCard.normal.background = MakeRoundTex(cCard, 8);
        sCard.border = new RectOffset(8, 8, 8, 8);
        sCard.padding = new RectOffset(12, 12, 10, 10);
        sCard.margin = new RectOffset(0, 0, 0, 8);
    }
}