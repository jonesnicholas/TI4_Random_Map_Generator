using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TI4_Random_Map_Generator
{
    enum GalaxyShape
    {
        Standard,
        ExtraRing,
        MecatolMegaHex,
    }

    class Galaxy
    {
        public int MaxRadius;
        public int players;
        public double score = 0.0;
        public SystemTile[][] tiles;
        public List<Tuple<int,int>> HSLocations;

        public Galaxy(int maxRad, int players = 6)
        {
            init(maxRad, players);
        }

        public Galaxy(GalaxyShape shape, Shuffle shuffle, int rad = 3, int players = 6)
        {
            if (shape == GalaxyShape.Standard)
            {
                init(rad);
                GenerateStandard(shuffle, rad, players);
            }
        }

        public void init(int maxRad, int people = 6)
        {
            players = people;
            int width = 2 * maxRad + 1;
            tiles = new SystemTile[width][];
            for (int i = 0; i <= 2 * maxRad; i++)
            {
                SystemTile[] row = new SystemTile[width];
                for (int j = 0; j <= 2 * maxRad; j++)
                {
                    row[j] = TilesetGenerator.GetBlankTile();
                }
                tiles[i] = row;
            }
            MaxRadius = maxRad;
        }

        public void GenerateStandard(Shuffle shuffle, int rad, int players = 6)
        {
            List<SystemTile> tileset = new List<SystemTile>();
            int placeableTileCount = 3 * rad * (rad + 1) - players;
            while (tileset.Count < placeableTileCount)
            {
                tileset.AddRange(TilesetGenerator.GetAllTiles());
            }

            shuffle.ShuffleList<SystemTile>(tileset);
            tileset = tileset.GetRange(0, placeableTileCount);
            connectWormholes(tileset);

            HSLocations = new List<Tuple<int, int>>();
            if (players == 6)
            {
                HSLocations.Add(new Tuple<int, int>(0, rad));
                HSLocations.Add(new Tuple<int, int>(0, 2 * rad));
                HSLocations.Add(new Tuple<int, int>(rad, 0));
                HSLocations.Add(new Tuple<int, int>(rad, 2 * rad));
                HSLocations.Add(new Tuple<int, int>(2 * rad, 0));
                HSLocations.Add(new Tuple<int, int>(2 * rad, rad));
            }

            for (int i = 1; i <= players; i ++)
            {
                Tuple<int, int> tuple = HSLocations[i-1];
                tiles[tuple.Item1][tuple.Item2].playerNum = i;
            }

            int width = 2 * rad + 1;
            int index = 0;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < width; y++)
                {
                    if (x + y >= rad && x + y <= 3 * rad && !(x == rad && y == rad))
                    {
                        if (tiles[x][y].playerNum == 0)
                        {
                            tiles[x][y] = tileset[index];
                            index++;
                        }
                    }
                    else if (x == rad && y == rad)
                    {
                        tiles[x][y] = TilesetGenerator.GetMecatol();
                    }
                    else
                    {
                        tiles[x][y].playerNum = -1;
                    }
                }
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < width; y++)
                {
                    if (tiles[x][y].playerNum >=0)
                    {
                        if (x < 2 * rad && tiles[x + 1][y].playerNum >= 0)
                        {
                            tiles[x][y].adjacent.Add(tiles[x + 1][y]);
                            tiles[x + 1][y].adjacent.Add(tiles[x][y]);
                        }
                        if (y < 2 * rad && tiles[x][y + 1].playerNum >= 0)
                        {
                            tiles[x][y].adjacent.Add(tiles[x][y + 1]);
                            tiles[x][y + 1].adjacent.Add(tiles[x][y]);
                        }
                        if (x < 2 *rad && y > 0 && tiles[x+1][y-1].playerNum >= 0)
                        {
                            tiles[x][y].adjacent.Add(tiles[x + 1][y - 1]);
                            tiles[x + 1][y - 1].adjacent.Add(tiles[x][y]);
                        }
                    }
                }
            }
        }

        public static void connectWormholes(List<SystemTile> tiles)
        {
            List<SystemTile> alphas = new List<SystemTile>();
            List<SystemTile> betas = new List<SystemTile>();
            foreach (SystemTile tile in tiles)
            {
                if (tile.wormholes == Wormhole.Alpha)
                {
                    alphas.Add(tile);
                }
                if (tile.wormholes == Wormhole.Beta)
                {
                    betas.Add(tile);
                }
            }
            foreach (SystemTile tile in alphas)
            {
                foreach (SystemTile otherTile in alphas)
                {
                    if (tile.sysNum != otherTile.sysNum)
                    {
                        tile.adjacent.Add(otherTile);
                    }
                }
            }
            foreach (SystemTile tile in betas)
            {
                foreach (SystemTile otherTile in betas)
                {
                    if (tile.sysNum != otherTile.sysNum)
                    {
                        tile.adjacent.Add(otherTile);
                    }
                }
            }
        }

        

        public Dictionary<int,List<SystemTile>> GetClaims()
        {
            Dictionary<int, List<SystemTile>> allClaims = new Dictionary<int, List<SystemTile>>();

            foreach (Tuple<int, int> start in HSLocations)
            {
                SystemTile startTile = tiles[start.Item1][start.Item2];
                int playerNum = startTile.playerNum;
                List<SystemTile> claimedTiles = new List<SystemTile>();
                allClaims.Add(playerNum, claimedTiles);
            }

            for (int x = 0; x <= MaxRadius * 2; x++)
            {
                for (int y = 0; y <= MaxRadius * 2; y++)
                {
                    SystemTile tile = tiles[x][y];
                    foreach (int claimer in tile.contestedBy)
                    {
                        allClaims[claimer].Add(tile);
                    }
                }
            }

            return allClaims;
        }

        

        public void addElement(List<int> list, int x, int y)
        {
            list.Add(tiles[x][y].sysNum);
            //Debug.WriteLine($"({x},{y}): {tiles[x][y].ToString()}");
        }

        public string GetTTSString()
        {
            List<int> tileNums = new List<int>();
            for (int r = 1; r <= MaxRadius; r++)
            {
                int x = 0 + MaxRadius;
                int y = -r + MaxRadius;
                addElement(tileNums, x, y);
                for (int i = 0; i < r; i++)
                {
                    x++;
                    addElement(tileNums, x, y);
                }
                for (int i = 0; i < r; i++)
                {
                    y++;
                    addElement(tileNums, x, y);
                }
                for (int i = 0; i < r-1; i++)
                {
                    x--;
                    y++;
                    addElement(tileNums, x, y);
                }
                x = 0 + MaxRadius;
                y = r + MaxRadius;
                addElement(tileNums, x, y);
                for (int i = 0; i < r; i++)
                {
                    x--;
                    addElement(tileNums, x, y);
                }
                for (int i = 0; i < r; i++)
                {
                    y--;
                    addElement(tileNums, x, y);
                }
                for (int i = 0; i < r - 1; i++)
                {
                    x++;
                    y--;
                    addElement(tileNums, x, y);
                }
            }
            List<string> tileStrings = tileNums.Select(num => num <= 0 ? "0" : num.ToString()).ToList();
            return tileStrings.Aggregate((i, j) => i + " " + j);
        }
    }


}
