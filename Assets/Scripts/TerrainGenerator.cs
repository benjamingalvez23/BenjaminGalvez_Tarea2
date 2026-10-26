using UnityEngine;
using UnityEngine.Tilemaps;

public enum TerrainType { Water, Sand, Grass, Rock }
public enum BiomeType { Encantado, Marchito, Cristalino }

public class TerrainGenerator : MonoBehaviour
{
    [Header("Referencias")]
    public Tilemap terrainTilemap;
    public LSystemTreeGenerator trees;
    public DungeonGenerator dungeons;
    public CreatureGenerator creatures;

    [Header("Mapa")]
    public int width = 128;
    public int height = 128;
    public int seed = 12345;
    public bool randomSeed = false;

    [Header("Ruido fractal")]
    public float scale = 40f;
    [Range(1, 8)] public int octaves = 4;
    [Range(0f, 1f)] public float persistence = 0.5f;
    public float lacunarity = 2f;

    [Header("Niveles del terreno (0 a 1)")]
    [Range(0f, 1f)] public float waterLevel = 0.45f;
    [Range(0f, 1f)] public float sandLevel = 0.50f;
    [Range(0f, 1f)] public float grassLevel = 0.65f;

    [Header("Biomas de fantasía")]
    public float biomeScale = 70f;
    [Range(0f, 1f)] public float biomeLevel1 = 0.45f;
    [Range(0f, 1f)] public float biomeLevel2 = 0.58f;

    [HideInInspector] public TerrainType[,] terrainMap;
    [HideInInspector] public BiomeType[,] biomeMap;
    [HideInInspector] public long lastMs;

    Tile[] tilesByType;
    Tile[] grassByBiome;

    void Start()
    {
        Generate();
    }

    [ContextMenu("Generar")]
    public void Generate()
    {
        if (tilesByType == null) CreateTiles();
        if (randomSeed) seed = Random.Range(0, 100000);

        var timer = System.Diagnostics.Stopwatch.StartNew();
        float[,] heights = GenerateHeightMap();
        BuildTerrainMap(heights);
        BuildBiomeMap();
        DrawTerrain();
        timer.Stop();
        lastMs = timer.ElapsedMilliseconds;
        Debug.Log("Terreno generado en " + timer.ElapsedMilliseconds + " ms (semilla " + seed + ")");

        if (trees != null) trees.Generate(terrainMap, width, height, seed, biomeMap);
        if (dungeons != null) dungeons.Generate(terrainMap, width, height, seed);
        if (creatures != null)
            creatures.Generate(terrainMap, biomeMap, width, height, seed,
                               trees != null ? trees.treeTilemap : null, dungeons);
    }

    float[,] GenerateHeightMap()
    {
        float[,] map = new float[width, height];
        if (scale <= 0f) scale = 0.0001f;

        System.Random prng = new System.Random(seed);
        Vector2[] offsets = new Vector2[octaves];
        for (int i = 0; i < octaves; i++)
            offsets[i] = new Vector2(prng.Next(-10000, 10000), prng.Next(-10000, 10000));

        float maxPossible = 0f;
        float amp = 1f;
        for (int i = 0; i < octaves; i++)
        {
            maxPossible += amp;
            amp *= persistence;
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float amplitude = 1f;
                float frequency = 1f;
                float value = 0f;

                for (int o = 0; o < octaves; o++)
                {
                    float sx = (x + offsets[o].x) / scale * frequency;
                    float sy = (y + offsets[o].y) / scale * frequency;
                    value += Mathf.PerlinNoise(sx, sy) * amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                map[x, y] = value / maxPossible;
            }
        }
        return map;
    }

    void BuildTerrainMap(float[,] heights)
    {
        terrainMap = new TerrainType[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                terrainMap[x, y] = PickType(heights[x, y]);
    }

    TerrainType PickType(float h)
    {
        if (h < waterLevel) return TerrainType.Water;
        if (h < sandLevel) return TerrainType.Sand;
        if (h < grassLevel) return TerrainType.Grass;
        return TerrainType.Rock;
    }

    void BuildBiomeMap()
    {
        biomeMap = new BiomeType[width, height];
        if (biomeScale <= 0f) biomeScale = 0.0001f;

        System.Random prng = new System.Random(seed + 4242);
        float ox = prng.Next(-10000, 10000);
        float oy = prng.Next(-10000, 10000);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float n = Mathf.PerlinNoise((x + ox) / biomeScale, (y + oy) / biomeScale);
                biomeMap[x, y] = PickBiome(n);
            }
        }
    }

    BiomeType PickBiome(float n)
    {
        if (n < biomeLevel1) return BiomeType.Encantado;
        if (n < biomeLevel2) return BiomeType.Marchito;
        return BiomeType.Cristalino;
    }

    void DrawTerrain()
    {
        terrainTilemap.ClearAllTiles();
        TileBase[] tiles = new TileBase[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                TerrainType t = terrainMap[x, y];
                if (t == TerrainType.Grass)
                    tiles[x + y * width] = grassByBiome[(int)biomeMap[x, y]];
                else
                    tiles[x + y * width] = tilesByType[(int)t];
            }
        }

        terrainTilemap.SetTilesBlock(new BoundsInt(0, 0, 0, width, height, 1), tiles);
    }

    void CreateTiles()
    {
        tilesByType = new Tile[4];
        tilesByType[(int)TerrainType.Water] = TileFactory.Make(new Color32(40, 100, 200, 255));
        tilesByType[(int)TerrainType.Sand] = TileFactory.Make(new Color32(230, 200, 110, 255));
        tilesByType[(int)TerrainType.Grass] = TileFactory.Make(new Color32(70, 160, 60, 255));
        tilesByType[(int)TerrainType.Rock] = TileFactory.Make(new Color32(120, 120, 125, 255));

        grassByBiome = new Tile[3];
        grassByBiome[(int)BiomeType.Encantado] = TileFactory.Make(new Color32(50, 185, 140, 255));
        grassByBiome[(int)BiomeType.Marchito] = TileFactory.Make(new Color32(105, 75, 120, 255));
        grassByBiome[(int)BiomeType.Cristalino] = TileFactory.Make(new Color32(215, 170, 235, 255));
    }
}