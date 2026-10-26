using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum TreeType { Roble, Arbusto }

public class LSystemTreeGenerator : MonoBehaviour
{
    [Header("Referencia")]
    public Tilemap treeTilemap;

    [Header("L-System")]
    public TreeType treeType = TreeType.Roble;
    [Range(1, 4)] public int iterations = 2;
    public float angle = 22.5f;
    public float angleJitter = 8f;
    public float stepLength = 1f;
    [Range(0, 2)] public int leafRadius = 1;

    [Header("Distribución")]
    [Range(0f, 0.3f)] public float density = 0.06f;
    public float forestScale = 18f;
    [Range(0f, 1f)] public float forestThreshold = 0.55f;
    public int minSpacing = 3;

    [HideInInspector] public int treeCount;
    [HideInInspector] public long lastMs;

    Tile trunkTile;
    Tile[] leafByBiome;

    struct TurtleState
    {
        public Vector2 pos;
        public float heading;
    }

    public void Generate(TerrainType[,] terrain, int width, int height, int seed, BiomeType[,] biomes = null)
    {
        if (trunkTile == null)
        {
            trunkTile = TileFactory.Make(new Color32(110, 70, 40, 255));
            leafByBiome = new Tile[3];
            leafByBiome[(int)BiomeType.Encantado] = TileFactory.Make(new Color32(20, 120, 100, 255));
            leafByBiome[(int)BiomeType.Marchito] = TileFactory.Make(new Color32(60, 40, 70, 255));
            leafByBiome[(int)BiomeType.Cristalino] = TileFactory.Make(new Color32(150, 90, 190, 255));
        }

        var timer = System.Diagnostics.Stopwatch.StartNew();
        treeTilemap.ClearAllTiles();
        treeCount = 0;

        string sentence = BuildSentence();
        System.Random rng = new System.Random(seed + 777);
        float forestOffset = rng.Next(0, 10000);

        bool[,] occupied = new bool[width, height];
        Dictionary<Vector2Int, TileBase> cells = new Dictionary<Vector2Int, TileBase>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (terrain[x, y] != TerrainType.Grass) continue;

                if (forestThreshold > 0f)
                {
                    float f = Mathf.PerlinNoise((x + forestOffset) / forestScale, (y + forestOffset) / forestScale);
                    if (f < forestThreshold) continue;
                }

                if (rng.NextDouble() > density) continue;
                if (!IsFree(occupied, x, y, width, height)) continue;

                occupied[x, y] = true;

                Tile leaf = biomes != null ? leafByBiome[(int)biomes[x, y]] : leafByBiome[0];
                PlaceTree(sentence, x, y, rng, terrain, width, height, cells, leaf);
                treeCount++;
            }
        }

        Vector3Int[] positions = new Vector3Int[cells.Count];
        TileBase[] tiles = new TileBase[cells.Count];
        int i = 0;
        foreach (var kv in cells)
        {
            positions[i] = new Vector3Int(kv.Key.x, kv.Key.y, 0);
            tiles[i] = kv.Value;
            i++;
        }
        treeTilemap.SetTiles(positions, tiles);

        timer.Stop();
        lastMs = timer.ElapsedMilliseconds;
        Debug.Log("Árboles generados: " + treeCount + " en " + timer.ElapsedMilliseconds + " ms");
    }

    string BuildSentence()
    {
        Dictionary<char, string> rules = new Dictionary<char, string>();
        string axiom;

        if (treeType == TreeType.Roble)
        {
            axiom = "F";
            rules['F'] = "FF+[+F-F-F]-[-F+F+F]";
        }
        else
        {
            axiom = "X";
            rules['X'] = "F[+X]F[-X]+X";
            rules['F'] = "FF";
        }

        string current = axiom;
        for (int i = 0; i < iterations; i++)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in current)
            {
                string replacement;
                if (rules.TryGetValue(c, out replacement)) sb.Append(replacement);
                else sb.Append(c);
            }
            current = sb.ToString();
            if (current.Length > 200000) break;
        }
        return current;
    }

    bool IsFree(bool[,] occupied, int x, int y, int width, int height)
    {
        for (int dx = -minSpacing; dx <= minSpacing; dx++)
        {
            for (int dy = -minSpacing; dy <= minSpacing; dy++)
            {
                int nx = x + dx;
                int ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
                if (occupied[nx, ny]) return false;
            }
        }
        return true;
    }

    void PlaceTree(string sentence, int bx, int by, System.Random rng,
                   TerrainType[,] terrain, int width, int height,
                   Dictionary<Vector2Int, TileBase> cells, Tile leaf)
    {
        List<Vector2Int> trunk = new List<Vector2Int>();
        List<Vector2Int> tips = new List<Vector2Int>();
        Interpret(sentence, rng, trunk, tips);

        // Primero las hojas (alrededor de las puntas de las ramas)
        foreach (Vector2Int tip in tips)
        {
            for (int dx = -leafRadius; dx <= leafRadius; dx++)
            {
                for (int dy = -leafRadius; dy <= leafRadius; dy++)
                {
                    if (dx * dx + dy * dy > leafRadius * leafRadius + leafRadius) continue;
                    TryWrite(bx + tip.x + dx, by + tip.y + dy, leaf, terrain, width, height, cells);
                }
            }
        }

        foreach (Vector2Int t in trunk)
            TryWrite(bx + t.x, by + t.y, trunkTile, terrain, width, height, cells);
    }

    void TryWrite(int x, int y, Tile tile, TerrainType[,] terrain, int width, int height,
                  Dictionary<Vector2Int, TileBase> cells)
    {
        if (x < 0 || y < 0 || x >= width || y >= height) return;
        if (terrain[x, y] == TerrainType.Water) return;   // no hay árboles en el agua
        cells[new Vector2Int(x, y)] = tile;
    }

    void Interpret(string s, System.Random rng, List<Vector2Int> trunk, List<Vector2Int> tips)
    {
        Stack<TurtleState> stack = new Stack<TurtleState>();
        Vector2 pos = Vector2.zero;
        float heading = 90f;
        trunk.Add(Vector2Int.zero);

        foreach (char c in s)
        {
            switch (c)
            {
                case 'F':
                {
                    Vector2 dir = new Vector2(Mathf.Cos(heading * Mathf.Deg2Rad),
                                              Mathf.Sin(heading * Mathf.Deg2Rad));
                    Vector2 next = pos + dir * stepLength;
                    AddLine(pos, next, trunk);
                    pos = next;
                    break;
                }
                case '+': heading += Jitter(rng); break;
                case '-': heading -= Jitter(rng); break;
                case '[':
                    stack.Push(new TurtleState { pos = pos, heading = heading });
                    break;
                case ']':
                    tips.Add(Round(pos));
                    if (stack.Count > 0)
                    {
                        TurtleState st = stack.Pop();
                        pos = st.pos;
                        heading = st.heading;
                    }
                    break;
            }
        }
        tips.Add(Round(pos));
    }

    float Jitter(System.Random rng)
    {
        return angle + ((float)rng.NextDouble() * 2f - 1f) * angleJitter;
    }

    void AddLine(Vector2 a, Vector2 b, List<Vector2Int> trunk)
    {
        int n = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b) * 3f));
        for (int i = 1; i <= n; i++)
            trunk.Add(Round(Vector2.Lerp(a, b, i / (float)n)));
    }

    Vector2Int Round(Vector2 v)
    {
        return new Vector2Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y));
    }
}