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
            init(rad, playerCount);
            if (shape == GalaxyShape.Standard)
            {
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

        /// <summary>
        /// For the galaxy, fills the 'claims' for each tile for each players, where the claim is the 'distance'
        /// from their home system to the tile in question. Used for scoring 'slices'
        /// </summary>
        /// <param name="galaxy">The Galaxy to stake claims in</param>
        /// <param name="walk">'distance' for default walk</param>
        /// <param name="emptyWalk">'distance' for walk through empty space</param>
        /// <param name="asteroidWalk">'distance' for walk through asteroids</param>
        /// <param name="novaWalk">'distance' for walk through nova</param>
        /// <param name="nebulaWalk">'distance' for walk through nebula</param>
        /// <param name="riftWalk">'distance' for walk through gravity rift</param>
        /// <param name="wormWalk">'distance' for walk through wormhole</param>
        public void StakeDistClaims(
            int walk = 10,
            int emptyWalk = 11,
            int asteroidWalk = 13,
            int novaWalk = 100,
            int nebulaWalk = 20,
            int riftWalk = 08,
            int wormWalk = 12)
        {
            ResetDistanceClaims();

            // for each home system (and therefore each player) determine how 'far' they are from each tile.
            // 'distances' are weighted by tile type, e.g. a system two tiles away with a planet in between is 
            // 'closer' than the same setup where the middle tile is a nebula
            foreach (Tuple<int, int> start in HSLocations)
            {
                SystemTile startTile = tiles[start.Item1][start.Item2];
                int playerNum = startTile.playerNum;
                // smaller number claims are better, a player's own home system has claim value 0 for themselves
                startTile.distClaims[playerNum] = 0;
                SortedList<int, List<SystemTile>> adjacent = new SortedList<int, List<SystemTile>>();
                // start by getting sorted list of all adjacent tiles to HS, sorted by lowest 'distance' first
                foreach (SystemTile tile in startTile.adjacent)
                {
                    int walkDist = walk;
                    if (tile.anomaly == Anomaly.Nova)
                    {
                        walkDist = novaWalk;
                    }
                    else if (tile.anomaly == Anomaly.Nebula)
                    {
                        walkDist = nebulaWalk;
                    }
                    else if (tile.anomaly == Anomaly.Asteroids)
                    {
                        walkDist = asteroidWalk;
                    }
                    else if (tile.anomaly == Anomaly.Rift)
                    {
                        walkDist = riftWalk;
                    }
                    else if (tile.planets.Count() == 0)
                    {
                        walkDist = emptyWalk;
                    }
                    if (!adjacent.ContainsKey(walkDist))
                    {
                        adjacent.Add(walkDist, new List<SystemTile>());
                    }
                    adjacent[walkDist].Add(tile);
                }

                //dijkstra's (?) to find 'shortest path' from HS to all tiles on the board
                while (adjacent.Count() > 0)
                {
                    IList<int> keys = adjacent.Keys;
                    int firstKey = keys.First();
                    List<SystemTile> firstList = adjacent[firstKey];
                    SystemTile closestTile = firstList.First();
                    firstList.Remove(closestTile);
                    if (firstList.Count() == 0)
                    {
                        adjacent.Remove(firstKey);
                    }
                    if (!closestTile.distClaims.ContainsKey(playerNum))
                    {
                        closestTile.distClaims.Add(playerNum, firstKey);
                        foreach (SystemTile next in closestTile.adjacent)
                        {
                            if (!next.distClaims.ContainsKey(playerNum))
                            {
                                int walkDist = firstKey;
                                if (next.anomaly == Anomaly.Nova)
                                {
                                    walkDist += novaWalk;
                                }
                                else if (next.anomaly == Anomaly.Nebula)
                                {
                                    walkDist += nebulaWalk;
                                }
                                else if (next.anomaly == Anomaly.Asteroids)
                                {
                                    walkDist += asteroidWalk;
                                }
                                else if (next.anomaly.HasFlag(Anomaly.Rift))
                                {
                                    walkDist += riftWalk;
                                }
                                else if ((closestTile.wormholes & next.wormholes) != Wormhole.None)
                                {
                                    walkDist += wormWalk;
                                }
                                else if (next.planets.Count() == 0)
                                {
                                    walkDist += emptyWalk;
                                }
                                else
                                {
                                    walkDist += walk;
                                }
                                if (!adjacent.ContainsKey(walkDist))
                                {
                                    adjacent.Add(walkDist, new List<SystemTile>());
                                }
                                adjacent[walkDist].Add(next);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sets Strength of claims on systems. Based on selected contest method and exponent of claim strength to distance ratio
        /// </summary>
        /// <param name="contestMethod">The method used to determine how to get STR claims from DIST claims</param>
        /// <param name="claimExponent">The exponent used to determing STR of claims from DIST of claims</param>
        public void StakeStrClaims(
            StrContestMethod contestMethod = StrContestMethod.TopAndClose,
            double claimExponent = -2.0)
        {
            ResetStrengthClaims();
            for (int x = 0; x <= 2 * MaxRadius; x++)
            {
                for (int y = 0; y <= 2 * MaxRadius; y++)
                {
                    SystemTile tile = tiles[x][y];
                    if (tile.sysNum > 0 && tile.planets.Count() > 0)
                    {
                        Dictionary<int, double> claims = new Dictionary<int, double>();
                        foreach (int pnum in tile.distClaims.Keys)
                        {
                            switch (contestMethod)
                            {
                                case StrContestMethod.Slices:
                                    if (claims[pnum] == tile.distClaims.Values.Max())
                                    {
                                        // full claim on resources for best claim (or tied)
                                        claims.Add(pnum, 1.0);
                                    }
                                    break;
                                case StrContestMethod.ClaimSize:
                                    if (tile.distClaims.Values.Min() == 0 && tile.distClaims.Values.Min() == claims[pnum])
                                    {
                                        // home systems (distance = 0) are only claimed by owner
                                        // TODO: might be a way to refactor a bit so this check isn't needed
                                        claims.Add(pnum, 1.0);
                                    }
                                    else
                                    {
                                        // all claims scaled by distance, inverted to a power
                                        // e.g. distances 10 and 20 might become claims of 1/10 and 1/20
                                        claims.Add(pnum, Math.Pow(tile.distClaims[pnum], claimExponent));
                                    }
                                    break;
                                case StrContestMethod.TopAndClose:
                                    if (tile.distClaims.Values.Min() == 0 && tile.distClaims.Values.Min() == claims[pnum])
                                    {
                                        // home systems (distance = 0) are only claimed by owner
                                        // TODO: might be a way to refactor a bit so this check isn't needed
                                        claims.Add(pnum, 1.0);
                                    }
                                    else if (claims[pnum] < 1.1 * tile.distClaims.Values.Min())
                                    {
                                        // TODO: Refactor to make 'close' a configurable setting somehow
                                        // all claims scaled by distance, inverted to a power
                                        // e.g. distances 10 and 20 might become claims of 1/10 and 1/20
                                        claims.Add(pnum, Math.Pow(tile.distClaims[pnum], claimExponent));
                                    }
                                    break;
                            }
                        }
                        if (claims.Count() == 0)
                        {
                            throw new Exception("Every system should have at least one claim, right?");
                        }
                        double claimScale = claims.Sum(claim => claim.Value);
                        foreach (KeyValuePair<int, double> claim in claims)
                        {
                            tile.strClaims.Add(claim.Key, claim.Value / claimScale);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Resets the distance claim dictionary for all tiles in galaxy.
        /// </summary>
        public void ResetDistanceClaims()
        {
            foreach (SystemTile tile in tiles.SelectMany(row => row))
            {
                tile.distClaims = new Dictionary<int, int>();
            }
        }

        /// <summary>
        /// Resets the Strength claim dictionary for all tiles in galaxy.
        /// </summary>
        public void ResetStrengthClaims()
        {
            foreach (SystemTile tile in tiles.SelectMany(row => row))
            {
                tile.strClaims = new Dictionary<int, double>();
            }
        }

        /// <summary>
        /// Gets a list of player int identifiers from the galaxy
        /// </summary>
        /// <param name="galaxy">The galaxy to get the player list for</param>
        /// <returns>A List of ints, representing the player identifiers</returns>
        public List<int> GetPlayers()
        {
            List<int> players = new List<int>();
            foreach (Tuple<int, int> start in HSLocations)
            {
                SystemTile startTile = tiles[start.Item1][start.Item2];
                int playerNum = startTile.playerNum;
                players.Add(playerNum);
            }
            return players;
        }
    }
}
