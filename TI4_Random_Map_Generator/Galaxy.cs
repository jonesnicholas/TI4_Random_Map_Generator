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
        Standard, // 3 rings around Mecatol, Home systems on outer ring
        ExtraRing, // 4 rings around Mecatol, Home systems on outer ring
        MecatolMegaHex, // mecatol 'takes up' 7 tiles, still 3 rings around, but rings are larger
    }

    class Galaxy
    {
        public int MaxRadius;
        public int players;
        public double score = 0.0;
        public SystemTile[][] tiles;
        public List<Tuple<int,int>> HSLocations;

        public Galaxy(int maxRad, int playerCount = 6)
        {
            init(maxRad, playerCount);
        }

        public Galaxy(GalaxyShape shape, int rad = 3, int playerCount = 6, Shuffle shuffle = null)
        {
            if (shape == GalaxyShape.Standard)
            {
                init(rad, playerCount);
                GenerateStandard(rad, playerCount, shuffle);
            }
        }

        /// <summary>
        /// Initializes a new galaxy. This adds placeholder tiles and initializes data structures.
        /// </summary>
        /// <param name="maxRad">The maximum 'radius' of the galaxy.</param>
        /// <param name="playerCount">The number of players to initialize the galaxy for</param>
        public void init(int maxRad, int playerCount = 6)
        {
            players = playerCount;
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

        /// <summary>
        /// Generates a 'standard' galaxy, X rings around Mecatol w/ home systems on outside edge.
        /// </summary>
        /// <param name="rad">'radius' of the galaxy to be generated. For this shape, also equals the number of rings around Mecatol</param>
        /// <param name="players">Number of players in current game</param>
        /// <param name="shuffle">The 'Shuffle' object used for randomization. Passing one in makes it less likely that seeding weirdness produces identical galaxies</param>
        public void GenerateStandard(int rad, int players = 6, Shuffle shuffle = null)
        {
            // TODO: Set HSLocations for player counts other than 6
            // TODO: Maybe pull this out as a helper function?
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
            // Set player# of HS tiles (used for staking claims & making sure not to put an actual tile in that spot)
            for (int i = 1; i <= players; i++)
            {
                Tuple<int, int> tuple = HSLocations[i - 1];
                tiles[tuple.Item1][tuple.Item2].playerNum = i;
            }

            int placeableTileCount = 3 * rad * (rad + 1) - players;

            // TODO: Consider making tileset configurable. For now, just keep adding a full set of game tiles until the required amount is reached.
            // May require multiple game copies, and/or the expansion.
            List<SystemTile> tileset = new List<SystemTile>();
            while (tileset.Count < placeableTileCount)
            {
                tileset.AddRange(TilesetGenerator.GetAllTiles());
            }

            shuffle = shuffle ?? new Shuffle();
            shuffle.ShuffleList<SystemTile>(tileset);
            tileset = tileset.GetRange(0, placeableTileCount);

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
                            // for tiles within shape of galaxy that are not Home Systems, assign a tile.
                            tiles[x][y] = tileset[index];
                            index++;
                        }
                    }
                    else if (x == rad && y == rad)
                    {
                        // "He who controls the Mecatol controls the universe"
                        tiles[x][y] = TilesetGenerator.GetMecatol();
                    }
                    else
                    {
                        // tiles outside of the galaxy are assigned player number -1
                        tiles[x][y].playerNum = -1;
                    }
                }
            }

            // make sure that all tiles adjacent to tile 'A' are within 'A's adjacent list
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

            connectWormholes();
        }

        /// <summary>
        /// Sets tiles with complimentary wormhole types as adjacent to each other
        /// </summary>
        /// <param name="tiles">The tileset for which wormholes need to be connected</param>
        public void connectWormholes()
        {
            List<SystemTile> galaxyTiles = new List<SystemTile>();
            galaxyTiles = tiles.SelectMany(row => row).ToList();
            List<SystemTile> alphas = new List<SystemTile>();
            List<SystemTile> betas = new List<SystemTile>();
            foreach (SystemTile tile in galaxyTiles)
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

        //TODO: write a method that generates a galaxy from a TTSString
        /// <summary>
        /// Gets the TTS map string for this galaxy
        /// </summary>
        /// <returns>the TTS string for this galaxy</returns>
        public string GetTTSString()
        {
            List<int> tileNums = new List<int>();
            for (int r = 1; r <= MaxRadius; r++)
            {
                int x = 0 + MaxRadius;
                int y = -r + MaxRadius;
                tileNums.Add(tiles[x][y].sysNum);
                for (int i = 0; i < r; i++)
                {
                    x++;
                    tileNums.Add(tiles[x][y].sysNum);
                }
                for (int i = 0; i < r; i++)
                {
                    y++;
                    tileNums.Add(tiles[x][y].sysNum);
                }
                for (int i = 0; i < r-1; i++)
                {
                    x--;
                    y++;
                    tileNums.Add(tiles[x][y].sysNum);
                }
                x = 0 + MaxRadius;
                y = r + MaxRadius;
                tileNums.Add(tiles[x][y].sysNum);
                for (int i = 0; i < r; i++)
                {
                    x--;
                    tileNums.Add(tiles[x][y].sysNum);
                }
                for (int i = 0; i < r; i++)
                {
                    y--;
                    tileNums.Add(tiles[x][y].sysNum);
                }
                for (int i = 0; i < r - 1; i++)
                {
                    x++;
                    y--;
                    tileNums.Add(tiles[x][y].sysNum);
                }
            }
            List<string> tileStrings = tileNums.Select(num => num <= 0 ? "0" : num.ToString()).ToList();
            return tileStrings.Aggregate((i, j) => i + " " + j);
        }
    }


}
