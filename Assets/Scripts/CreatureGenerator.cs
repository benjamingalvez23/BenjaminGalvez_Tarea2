using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class CreatureGenerator : MonoBehaviour
{
    [Header("Referencia")]
    public Tilemap creatureTilemap;

    [Header("Animales (pasto)")]
    [Range(0f, 0.05f)] public float animalDensity = 0.010f;
    public int animalSpacing = 4;

    [Header("Enemigos (exterior)")]
    [Range(0f, 0.05f)] public float enemyDensity = 0.006f;
    public int enemySpacing = 5;

    [Header("Dentro de las mazmorras")]
    [Range(0, 12)] public int enemiesPerDungeon = 4;
    public int dungeonEnemySpacing = 3;
    public bool bossInDungeons = true;

    [HideInInspector] public int animalCount;
    [HideInInspector] public int enemyCount;
    [HideInInspector] public int bossCount;
    [HideInInspector] public long lastMs;

    Tile[] animalByBiome;
    Tile slimeTile, scorpionTile, golemTile, ghostTile, bossTile;

    public void Generate(TerrainType[,] terrain, BiomeType[,] biomes, int width, int height,
                         int seed, Tilemap treeTilemap, DungeonGenerator dungeons)
    {
        if (slimeTile == null) CreateTiles();

        var timer = System.Diagnostics.Stopwatch.StartNew();
        creatureTilemap.ClearAllTiles();
        animalCount = 0;
        enemyCount = 0;
        bossCount = 0;

        System.Random rng = new System.Random(seed + 2024);
        bool[,] occupied = new bool[width, height];
        Dictionary<Vector2Int, TileBase> cells = new Dictionary<Vector2Int, TileBase>();

        Tilemap dungeonMap = dungeons != null ? dungeons.dungeonTilemap : null;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                TerrainType t = terrain[x, y];
                if (t == TerrainType.Water) continue;

                Vector3Int cell = new Vector3Int(x, y, 0);
                if (treeTilemap != null && treeTilemap.HasTile(cell)) continue;
                if (dungeonMap != null && dungeonMap.HasTile(cell)) continue;
                double r = rng.NextDouble();
                float animalChance = (t == TerrainType.Grass) ? animalDensity : 0f;

                if (r < animalChance)
                {
                    if (HasNear(occupied, x, y, width, height, animalSpacing)) continue;
                    int biome = biomes != null ? (int)biomes[x, y] : 0;
                    occupied[x, y] = true;
                    cells[new Vector2Int(x, y)] = animalByBiome[biome];
                    animalCount++;
                }
                else if (r < animalChance + enemyDensity)
                {
                    if (HasNear(occupied, x, y, width, height, enemySpacing)) continue;
                    occupied[x, y] = true;
                    cells[new Vector2Int(x, y)] = EnemyFor(t);
                    enemyCount++;
                }
            }
        }

        if (dungeons != null)
        {
            for (int i = 0; i < dungeons.floorSpots.Count; i++)
            {
                Vector2Int boss = i < dungeons.bossSpots.Count ? dungeons.bossSpots[i] : new Vector2Int(-1, -1);
                bool hasBoss = bossInDungeons && boss.x >= 0;

                List<Vector2Int> spots = new List<Vector2Int>(dungeons.floorSpots[i]);
                Shuffle(spots, rng);

                int placed = 0;
                foreach (Vector2Int s in spots)
                {
                    if (placed >= enemiesPerDungeon) break;
                    if (hasBoss && Mathf.Abs(s.x - boss.x) + Mathf.Abs(s.y - boss.y) < 5) continue;
                    if (HasNear(occupied, s.x, s.y, width, height, dungeonEnemySpacing)) continue;

                    occupied[s.x, s.y] = true;
                    cells[s] = ghostTile;
                    enemyCount++;
                    placed++;
                }

                if (hasBoss)
                {
                    DrawBoss(boss, cells);
                    bossCount++;
                }
            }
        }

        Vector3Int[] positions = new Vector3Int[cells.Count];
        TileBase[] tiles = new TileBase[cells.Count];
        int k = 0;
        foreach (var kv in cells)
        {
            positions[k] = new Vector3Int(kv.Key.x, kv.Key.y, 0);
            tiles[k] = kv.Value;
            k++;
        }
        creatureTilemap.SetTiles(positions, tiles);

        timer.Stop();
        lastMs = timer.ElapsedMilliseconds;
        Debug.Log("Criaturas: " + animalCount + " animales, " + enemyCount + " enemigos, " +
                  bossCount + " bosses en " + timer.ElapsedMilliseconds + " ms");
    }

    Tile EnemyFor(TerrainType t)
    {
        if (t == TerrainType.Sand) return scorpionTile;
        if (t == TerrainType.Rock) return golemTile;
        return slimeTile;
    }

    void DrawBoss(Vector2Int c, Dictionary<Vector2Int, TileBase> cells)
    {
        cells[c] = bossTile;
        cells[c + Vector2Int.up] = bossTile;
        cells[c + Vector2Int.down] = bossTile;
        cells[c + Vector2Int.left] = bossTile;
        cells[c + Vector2Int.right] = bossTile;
    }

    bool HasNear(bool[,] occupied, int x, int y, int width, int height, int radius)
    {
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                int nx = x + dx;
                int ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
                if (occupied[nx, ny]) return true;
            }
        }
        return false;
    }

    void Shuffle(List<Vector2Int> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(0, i + 1);
            Vector2Int tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }
    }

    void CreateTiles()
    {
        animalByBiome = new Tile[3];
        animalByBiome[(int)BiomeType.Encantado] = TileFactory.Make(new Color32(255, 255, 255, 255));
        animalByBiome[(int)BiomeType.Marchito] = TileFactory.Make(new Color32(90, 170, 255, 255));
        animalByBiome[(int)BiomeType.Cristalino] = TileFactory.Make(new Color32(25, 25, 60, 255));

        slimeTile = TileFactory.Make(new Color32(255, 50, 50, 255));
        scorpionTile = TileFactory.Make(new Color32(140, 10, 10, 255));
        golemTile = TileFactory.Make(new Color32(230, 60, 200, 255));
        ghostTile = TileFactory.Make(new Color32(150, 255, 60, 255));
        bossTile = TileFactory.Make(new Color32(130, 0, 200, 255));
    }
}