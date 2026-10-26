using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DungeonGenerator : MonoBehaviour
{
    [Header("Referencia")]
    public Tilemap dungeonTilemap;

    [Header("Mazmorras en el mapa")]
    [Range(0, 10)] public int dungeonCount = 3;
    public int minSize = 24;
    public int maxSize = 36;
    [Range(0f, 0.3f)] public float maxWaterFraction = 0.05f;
    public int placementAttempts = 150;

    [Header("Autómata celular")]
    [Range(0.3f, 0.6f)] public float fillPercent = 0.46f;
    [Range(0, 10)] public int smoothIterations = 5;
    [Range(1, 7)] public int wallThreshold = 4;
    public bool keepLargestRegionOnly = true;

    [Header("Objetos en las mazmorras")]
    [Range(0, 6)] public int chestsPerDungeon = 3;
    [Range(0, 20)] public int cratesPerDungeon = 8;
    public int minDistanceFromEntrance = 6;

    [HideInInspector] public int placedCount;
    [HideInInspector] public int chestCount;
    [HideInInspector] public int crateCount;
    [HideInInspector] public long lastMs;

    // Datos que usa CreatureGenerator (coordenadas del mundo, una entrada por mazmorra)
    [System.NonSerialized] public List<List<Vector2Int>> floorSpots = new List<List<Vector2Int>>();
    [System.NonSerialized] public List<Vector2Int> bossSpots = new List<Vector2Int>();

    Tile wallTile, floorTile, entranceTile, chestTile, crateTile;

    public void Generate(TerrainType[,] terrain, int width, int height, int seed)
    {
        if (wallTile == null)
        {
            wallTile = TileFactory.Make(new Color32(45, 45, 58, 255));
            floorTile = TileFactory.Make(new Color32(165, 165, 175, 255));
            entranceTile = TileFactory.Make(new Color32(235, 140, 30, 255));
            chestTile = TileFactory.Make(new Color32(255, 215, 0, 255));
            crateTile = TileFactory.Make(new Color32(140, 95, 50, 255));
        }

        var timer = System.Diagnostics.Stopwatch.StartNew();
        dungeonTilemap.ClearAllTiles();
        placedCount = 0;
        chestCount = 0;
        crateCount = 0;
        floorSpots.Clear();
        bossSpots.Clear();

        System.Random rng = new System.Random(seed + 1337);
        List<RectInt> used = new List<RectInt>();
        List<Vector3Int> positions = new List<Vector3Int>();
        List<TileBase> tiles = new List<TileBase>();

        for (int d = 0; d < dungeonCount; d++)
        {
            RectInt? spot = FindSpot(terrain, width, height, rng, used);
            if (spot == null)
            {
                Debug.Log("No se encontró espacio para la mazmorra " + (d + 1));
                break;
            }

            RectInt rect = spot.Value;
            used.Add(rect);

            int regionsBefore, regionsAfter, entranceX;
            bool[,] walls = GenerateCave(rect.width, rect.height, rng,
                                         out regionsBefore, out regionsAfter, out entranceX);

            int chests, crates;
            int[,] kinds = PlaceObjects(walls, rect.width, rect.height, entranceX, rng,
                                        out chests, out crates);
            chestCount += chests;
            crateCount += crates;

            List<Vector2Int> freeSpots;
            Vector2Int bossSpot;
            CollectSpots(walls, kinds, rect, entranceX, out freeSpots, out bossSpot);
            floorSpots.Add(freeSpots);
            bossSpots.Add(bossSpot);

            int floorCount = 0;
            for (int x = 0; x < rect.width; x++)
            {
                for (int y = 0; y < rect.height; y++)
                {
                    TileBase t;
                    if (y == 0 && x == entranceX) t = entranceTile;
                    else if (walls[x, y]) t = wallTile;
                    else
                    {
                        floorCount++;
                        if (kinds[x, y] == 1) t = chestTile;
                        else if (kinds[x, y] == 2) t = crateTile;
                        else t = floorTile;
                    }

                    positions.Add(new Vector3Int(rect.x + x, rect.y + y, 0));
                    tiles.Add(t);
                }
            }

            placedCount++;
            float floorPercent = 100f * floorCount / (rect.width * rect.height);
            Debug.Log("Mazmorra " + (d + 1) + ": " + rect.width + "x" + rect.height +
                      ", suelo " + floorPercent.ToString("F1") + "%, regiones " +
                      regionsBefore + " -> " + regionsAfter +
                      ", cofres " + chests + ", cajas " + crates);
        }

        dungeonTilemap.SetTiles(positions.ToArray(), tiles.ToArray());

        timer.Stop();
        lastMs = timer.ElapsedMilliseconds;
        Debug.Log("Mazmorras generadas: " + placedCount + " en " + timer.ElapsedMilliseconds + " ms");
    }

    RectInt? FindSpot(TerrainType[,] terrain, int width, int height,
                      System.Random rng, List<RectInt> used)
    {
        RectInt? best = null;
        float bestScore = -1f;

        for (int a = 0; a < placementAttempts; a++)
        {
            int w = rng.Next(minSize, maxSize + 1);
            int h = rng.Next(minSize, maxSize + 1);
            if (w + 6 >= width || h + 6 >= height) continue;

            int x = rng.Next(2, width - w - 2);
            int y = rng.Next(2, height - h - 2);
            RectInt r = new RectInt(x, y, w, h);

            bool overlaps = false;
            foreach (RectInt u in used)
            {
                RectInt expanded = new RectInt(u.x - 3, u.y - 3, u.width + 6, u.height + 6);
                if (r.Overlaps(expanded)) { overlaps = true; break; }
            }
            if (overlaps) continue;

            int water = 0, rock = 0;
            for (int xx = x; xx < x + w; xx++)
            {
                for (int yy = y; yy < y + h; yy++)
                {
                    if (terrain[xx, yy] == TerrainType.Water) water++;
                    else if (terrain[xx, yy] == TerrainType.Rock) rock++;
                }
            }

            float total = w * h;
            if (water / total > maxWaterFraction) continue;

            float score = rock / total;
            if (score > bestScore)
            {
                bestScore = score;
                best = r;
            }
        }
        return best;
    }

    bool[,] GenerateCave(int w, int h, System.Random rng,
                         out int regionsBefore, out int regionsAfter, out int entranceX)
    {
        bool[,] walls = new bool[w, h];

        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                walls[x, y] = IsBorder(x, y, w, h) || rng.NextDouble() < fillPercent;

        for (int i = 0; i < smoothIterations; i++)
        {
            bool[,] next = new bool[w, h];
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    if (IsBorder(x, y, w, h)) { next[x, y] = true; continue; }

                    int n = CountWallNeighbors(walls, x, y, w, h);
                    if (n > wallThreshold) next[x, y] = true;
                    else if (n < wallThreshold) next[x, y] = false;
                    else next[x, y] = walls[x, y];
                }
            }
            walls = next;
        }

        if (CountFloor(walls, w, h) == 0)
        {
            for (int x = w / 2 - 2; x <= w / 2 + 2; x++)
                for (int y = h / 2 - 2; y <= h / 2 + 2; y++)
                    walls[x, y] = false;
        }

        List<List<Vector2Int>> regions = FindRegions(walls, w, h);
        regionsBefore = regions.Count;

        if (keepLargestRegionOnly && regions.Count > 1)
        {
            int largest = 0;
            for (int i = 1; i < regions.Count; i++)
                if (regions[i].Count > regions[largest].Count) largest = i;

            for (int i = 0; i < regions.Count; i++)
            {
                if (i == largest) continue;
                foreach (Vector2Int c in regions[i]) walls[c.x, c.y] = true;
            }
            regionsAfter = 1;
        }
        else
        {
            regionsAfter = regions.Count;
        }

        int lowestY = h;
        entranceX = w / 2;
        for (int y = 1; y < h - 1 && lowestY == h; y++)
        {
            for (int x = 1; x < w - 1; x++)
            {
                if (!walls[x, y])
                {
                    lowestY = y;
                    entranceX = x;
                    break;
                }
            }
        }
        for (int y = 0; y <= lowestY && y < h; y++) walls[entranceX, y] = false;

        return walls;
    }

    int[,] PlaceObjects(bool[,] walls, int w, int h, int entranceX, System.Random rng,
                        out int chests, out int crates)
    {
        int[,] kinds = new int[w, h];
        chests = 0;
        crates = 0;

        List<Vector2Int> chestSpots = new List<Vector2Int>();
        List<Vector2Int> crateSpots = new List<Vector2Int>();

        for (int x = 1; x < w - 1; x++)
        {
            for (int y = 1; y < h - 1; y++)
            {
                if (walls[x, y]) continue;

                // No poner objetos cerca de la entrada
                int distToEntrance = Mathf.Abs(x - entranceX) + y;
                if (distToEntrance < minDistanceFromEntrance) continue;

                int n = CountWallNeighbors(walls, x, y, w, h);
                if (n >= 5) chestSpots.Add(new Vector2Int(x, y));
                if (n >= 3) crateSpots.Add(new Vector2Int(x, y));
            }
        }

        Shuffle(chestSpots, rng);
        Shuffle(crateSpots, rng);

        // Cofres primero: bien separados entre sí
        foreach (Vector2Int s in chestSpots)
        {
            if (chests >= chestsPerDungeon) break;
            if (HasObjectNear(kinds, s.x, s.y, w, h, 3)) continue;
            kinds[s.x, s.y] = 1;
            chests++;
        }

        foreach (Vector2Int s in crateSpots)
        {
            if (crates >= cratesPerDungeon) break;
            if (kinds[s.x, s.y] != 0) continue;
            if (HasObjectNear(kinds, s.x, s.y, w, h, 1)) continue;
            kinds[s.x, s.y] = 2;
            crates++;
        }

        return kinds;
    }

    void CollectSpots(bool[,] walls, int[,] kinds, RectInt rect, int entranceX,
                      out List<Vector2Int> spots, out Vector2Int bossSpot)
    {
        int w = rect.width, h = rect.height;
        spots = new List<Vector2Int>();
        bossSpot = new Vector2Int(-1, -1);

        int[,] dist = new int[w, h];
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                dist[x, y] = -1;

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        if (!walls[entranceX, 0])
        {
            dist[entranceX, 0] = 0;
            queue.Enqueue(new Vector2Int(entranceX, 0));
        }

        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        while (queue.Count > 0)
        {
            Vector2Int c = queue.Dequeue();
            foreach (Vector2Int d in dirs)
            {
                int nx = c.x + d.x;
                int ny = c.y + d.y;
                if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                if (walls[nx, ny] || dist[nx, ny] >= 0) continue;
                dist[nx, ny] = dist[c.x, c.y] + 1;
                queue.Enqueue(new Vector2Int(nx, ny));
            }
        }

        int bestOpen = -1, bestAny = -1;
        Vector2Int openCell = bossSpot, anyCell = bossSpot;

        for (int x = 1; x < w - 1; x++)
        {
            for (int y = 1; y < h - 1; y++)
            {
                if (walls[x, y] || dist[x, y] < 0 || kinds[x, y] != 0) continue;

                Vector2Int world = new Vector2Int(rect.x + x, rect.y + y);
                if (dist[x, y] >= minDistanceFromEntrance) spots.Add(world);

                if (dist[x, y] > bestAny) { bestAny = dist[x, y]; anyCell = world; }

                if (CountWallNeighbors(walls, x, y, w, h) == 0 && dist[x, y] > bestOpen)
                {
                    bestOpen = dist[x, y];
                    openCell = world;
                }
            }
        }

        if (bestOpen >= 0) bossSpot = openCell;
        else if (bestAny >= 0) bossSpot = anyCell;
    }

    bool HasObjectNear(int[,] kinds, int x, int y, int w, int h, int radius)
    {
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                int nx = x + dx;
                int ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                if (kinds[nx, ny] != 0) return true;
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

    bool IsBorder(int x, int y, int w, int h)
    {
        return x == 0 || y == 0 || x == w - 1 || y == h - 1;
    }

    int CountWallNeighbors(bool[,] walls, int x, int y, int w, int h)
    {
        int count = 0;
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = x + dx;
                int ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= w || ny >= h) count++;
                else if (walls[nx, ny]) count++;
            }
        }
        return count;
    }

    int CountFloor(bool[,] walls, int w, int h)
    {
        int c = 0;
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                if (!walls[x, y]) c++;
        return c;
    }

    List<List<Vector2Int>> FindRegions(bool[,] walls, int w, int h)
    {
        List<List<Vector2Int>> regions = new List<List<Vector2Int>>();
        bool[,] visited = new bool[w, h];
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                if (walls[x, y] || visited[x, y]) continue;

                List<Vector2Int> region = new List<Vector2Int>();
                Queue<Vector2Int> queue = new Queue<Vector2Int>();
                queue.Enqueue(new Vector2Int(x, y));
                visited[x, y] = true;

                while (queue.Count > 0)
                {
                    Vector2Int c = queue.Dequeue();
                    region.Add(c);
                    foreach (Vector2Int d in dirs)
                    {
                        int nx = c.x + d.x;
                        int ny = c.y + d.y;
                        if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                        if (walls[nx, ny] || visited[nx, ny]) continue;
                        visited[nx, ny] = true;
                        queue.Enqueue(new Vector2Int(nx, ny));
                    }
                }
                regions.Add(region);
            }
        }
        return regions;
    }
}