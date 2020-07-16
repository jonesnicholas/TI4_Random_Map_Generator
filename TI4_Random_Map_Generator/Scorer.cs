using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TI4_Random_Map_Generator
{
    public enum ResourceScoreMethod
    {
        Separate, // slices are scored based on res equality and inf equality
        DirectSum, // slices are scored based on (res + inf) equality
        MaxVal, // slices are scored based on Max(res, inf) equality
    }

    public enum ContestValue
    {
        Slices, // systems are divided equally amongs those players tied for best claim
        ClaimSize, // systems are divided proportionally based on how good a claim is (e.g. twice as good a claim -> twice the value from the system)
        TopAndClose, // same as claim size, but only those tied for best claim, and those 'close' have valid claims
    }

    class Scorer
    {
        public double scoreGalaxy(
            Galaxy galaxy,
            bool bothHoles = true,
            bool hardHoleLimit = true,
            int HoleCount = 2,
            bool allowAdjacentHoles = false,
            bool allowAdjacentAnomalies = false,
            ContestValue contestMethod = ContestValue.ClaimSize,
            ResourceScoreMethod resMethod = ResourceScoreMethod.MaxVal,
            double ResInfRatio = 1.0,
            double ResScaling = 2.0,
            double claimExponent = -3.0)
        {
            double score = 0.0;
            double resourceScore = 0.0;

            int MaxRadius = galaxy.MaxRadius;
            SystemTile[][] tiles = galaxy.tiles;
            List<Tuple<int, int>> HSLocations = galaxy.HSLocations;

            if (!wormholesOK(
                galaxy,
                bothHoles,
                hardHoleLimit,
                HoleCount,
                allowAdjacentHoles))
            {
                return 0.0;
            }

            if (!AnomaliesOK(galaxy, allowAdjacentAnomalies))
            {
                return 0.0;
            }

            List<int> players = GetPlayers(galaxy);
            
            StakeClaims(galaxy);

            Dictionary<int, double> playerResources = new Dictionary<int, double>();
            Dictionary<int, double> playerTraitAccess = new Dictionary<int, double>();
            Dictionary<int, double> stage1ObjectiveAccess;
            Dictionary<int, double> secretObjectiveAccess = new Dictionary<int, double>();
            Dictionary<int, double> easiestTraitAccess = new Dictionary<int, double>();
                
            resourceScore =
                GetResourceScoreClaims(
                    galaxy,
                    players,
                    resMethod,
                    contestMethod,
                    ResInfRatio,
                    ResScaling,
                    claimExponent);


            stage1ObjectiveAccess = GetS1Score(galaxy, players, contestMethod);

            score = resourceScore;

            return score;
        }

        /// <summary>
        /// Scores the galaxy based on distribution of planet traits. 1.0 means all slices have an equally easy time at getting 4/6 of planet traits
        /// </summary>
        /// <param name="galaxy">The galaxy to score</param>
        /// <param name="players">list containing the int identifiers of players</param>
        /// <param name="sliceClaim">dictionary of the tiles claimed by each player</param>
        /// <param name="sliceContest">dictionary of the tiles contested by each player</param>
        /// <returns></returns>
        public double GetTraitScore(
            Galaxy galaxy,
            List<int> players,
            Dictionary<int, List<SystemTile>> sliceClaim,
            Dictionary<int, List<SystemTile>> sliceContest)
        {
            return 0.0;
        }

        public Dictionary<int, double> GetS1Score(
            Galaxy galaxy,
            List<int> players,
            ContestValue contestMethod)
        {
            Dictionary<int, double> Stage1ViabilityScores = new Dictionary<int, double>();
            // for each 'slice', determine how 'viable' each map-dependent stage 1 objective is

            // Spend 8 resources
            // Spend 8 influence
            // 2 adjacent to Mecatol
            // 4 of a trait
            // 3 tech specialties

            return Stage1ViabilityScores;
        }

        public double GetResourceScoreClaims(
            Galaxy galaxy,
            List<int> players,
            ResourceScoreMethod resMethod = ResourceScoreMethod.MaxVal,
            ContestValue contestMethod = ContestValue.TopAndClose,
            double ResInfRatio = 1.0,
            double ResScaling = 2.0,
            double claimExponent = -2.0)
        {
            Dictionary<int, double> resourceClaims = new Dictionary<int, double>();
            foreach (int pnum in players)
            {
                resourceClaims.Add(pnum, 0);
            }

            int MaxRadius = galaxy.MaxRadius;
            for (int x = 0; x <= 2 * MaxRadius; x++)
            {
                for (int y = 0; y <= 2 * MaxRadius; y++)
                {
                    SystemTile tile = galaxy.tiles[x][y];
                    if (tile.sysNum > 0 && tile.planets.Count() > 0)
                    {
                        Dictionary<int, double> claims = new Dictionary<int, double>();
                        foreach (int pnum in tile.claims.Keys)
                        {
                            switch (contestMethod)
                            {
                                case ContestValue.Slices:
                                    if (claims[pnum] == tile.claims.Values.Max())
                                    {
                                        // full claim on resources for best claim (or tied)
                                        claims.Add(pnum, 1.0);
                                    }
                                    break;
                                case ContestValue.ClaimSize:
                                    if (tile.claims.Values.Min() == 0 && tile.claims.Values.Min() == claims[pnum])
                                    {
                                        // home systems (distance = 0) are only claimed by owner
                                        // TODO: might be a way to refactor a bit so this check isn't needed
                                        claims.Add(pnum, 1.0);
                                    }
                                    else
                                    {
                                        // all claims scaled by distance, inverted to a power
                                        // e.g. distances 10 and 20 might become claims of 1/10 and 1/20
                                        claims.Add(pnum, Math.Pow(tile.claims[pnum], claimExponent));
                                    }
                                    break;
                                case ContestValue.TopAndClose:
                                    if (tile.claims.Values.Min() == 0 && tile.claims.Values.Min() == claims[pnum])
                                    {
                                        // home systems (distance = 0) are only claimed by owner
                                        // TODO: might be a way to refactor a bit so this check isn't needed
                                        claims.Add(pnum, 1.0);
                                    }
                                    else if (claims[pnum] < 1.1 * tile.claims.Values.Min())
                                    {
                                        // TODO: Refactor to make 'close' a configurable setting somehow
                                        // all claims scaled by distance, inverted to a power
                                        // e.g. distances 10 and 20 might become claims of 1/10 and 1/20
                                        claims.Add(pnum, Math.Pow(tile.claims[pnum], claimExponent));
                                    }
                                    break;
                            }
                        }
                        if (claims.Count() == 0)
                        {
                            throw new Exception("Every system should have at least one claim, right?");
                        }
                        double claimScale = claims.Sum(claim => claim.Value);
                        foreach(KeyValuePair<int, double> claim in claims)
                        {
                            double val = 0.0;
                            switch (resMethod)
                            {
                                case ResourceScoreMethod.DirectSum:
                                    val = tile.GetResources() + ResInfRatio * tile.GetInfluence();
                                    break;
                                case ResourceScoreMethod.Separate:
                                    throw new Exception("Not supporting \"separate\" for claim method");
                                case ResourceScoreMethod.MaxVal:
                                    val = tile.planets.Sum(planet => Math.Max(planet.resources, ResInfRatio * planet.influence));
                                    break;
                            }
                            tile.adjClaims.Add(claim.Key, claim.Value / claimScale);
                            resourceClaims[claim.Key] += val * claim.Value / claimScale;
                        }
                    }
                }
            }

            double minSlice = resourceClaims.Min(claim => claim.Value);
            double maxSlice = resourceClaims.Max(claim => claim.Value);

            return Math.Pow(minSlice / maxSlice, ResScaling);
        }


        public void StakeClaims(
            Galaxy galaxy, 
            int walk = 10, 
            int emptyWalk = 11,
            int asteroidWalk = 13, 
            int novaWalk = 100,
            int nebulaWalk = 20,
            int riftWalk = 08, 
            int wormWalk = 12)
        {
            int MaxRadius = galaxy.MaxRadius;
            SystemTile[][] tiles = galaxy.tiles;
            List<Tuple<int, int>> HSLocations = galaxy.HSLocations;
            
            // for each home system (and therefore each player) determine how 'far' they are from each tile.
            // 'distances' are weighted by tile type, e.g. a system two tiles away with a planet in between is 
            // 'closer' than the same setup where the middle tile is a nebula
            foreach (Tuple<int, int> start in HSLocations)
            {
                SystemTile startTile = tiles[start.Item1][start.Item2];
                int playerNum = startTile.playerNum;
                // smaller number claims are better, a player's own home system has claim value 0 for themselves
                startTile.claims[playerNum] = 0;
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
                    if (!closestTile.claims.ContainsKey(playerNum))
                    {
                        closestTile.claims.Add(playerNum, firstKey);
                        foreach (SystemTile next in closestTile.adjacent)
                        {
                            if (!next.claims.ContainsKey(playerNum))
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

            // at this point, all tiles should have a 'distance' value corresponding to each player
        }

        public bool wormholesOK(
            Galaxy galaxy,
            bool bothHoles = true,
            bool hardHoleLimit = true,
            int HoleCount = 2,
            bool allowAdjacentHoles = false)
        {
            int MaxRadius = galaxy.MaxRadius;
            SystemTile[][] tiles = galaxy.tiles;

            int alphaCount = 0;
            int betaCount = 0;

            for (int x = 0; x <= MaxRadius * 2; x++)
            {
                for (int y = 0; y <= MaxRadius * 2; y++)
                {
                    if (tiles[x][y].wormholes == Wormhole.Alpha)
                    {
                        alphaCount++;
                    }
                    if (tiles[x][y].wormholes == Wormhole.Beta)
                    {
                        betaCount++;
                    }
                }
            }
            for (int x = 0; x <= MaxRadius * 2; x++)
            {
                for (int y = 0; y <= MaxRadius * 2; y++)
                {
                    if (tiles[x][y].adjacent.Count() != tiles[x][y].adjacent.Distinct().Count())
                    {
                        return false;
                    }
                }
            }
            if (bothHoles && alphaCount == 0 && betaCount == 0)
            {
                return false;
            }
            if (hardHoleLimit && alphaCount != HoleCount && betaCount != HoleCount)
            {
                return false;
            }
            if (alphaCount < HoleCount || betaCount < HoleCount)
            {
                return false;
            }
            if (!allowAdjacentHoles)
            {
                for (int x = 0; x <= MaxRadius * 2; x++)
                {
                    for (int y = 0; y <= MaxRadius * 2; y++)
                    {
                        SystemTile thisTile = tiles[x][y];
                        if (thisTile.wormholes != Wormhole.None)
                        {
                            foreach (SystemTile adjTile in thisTile.adjacent)
                            {
                                if (adjTile.wormholes != Wormhole.None && adjTile.wormholes != thisTile.wormholes)
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }
            }

            return true;
        }

        public bool AnomaliesOK(
            Galaxy galaxy, 
            bool allowAdjacentAnomalies = false
            )
        {
            int MaxRadius = galaxy.MaxRadius;
            SystemTile[][] tiles = galaxy.tiles;

            if (!allowAdjacentAnomalies)
            {
                for (int x = 0; x <= MaxRadius * 2; x++)
                {
                    for (int y = 0; y <= MaxRadius * 2; y++)
                    {
                        SystemTile tile = tiles[x][y];
                        if (tile.anomaly != Anomaly.None)
                        {
                            foreach (SystemTile adj in tile.adjacent)
                            {
                                if (adj.anomaly != Anomaly.None)
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }
            }

            return true;
        }

        public double sliceVal(List<SystemTile> claimed, List<SystemTile> contested, bool res)
        {
            double value = claimed.Select(
                tile => tile.GetVal(res))
                .Aggregate((i, j) => i + j);

            value += contested.Select(
                tile => (double)tile.GetVal(res) / tile.contestedBy.Count())
                .Aggregate((i, j) => i + j);

            return value;
        }

        public List<int> GetPlayers(Galaxy galaxy)
        {
            List<int> players = new List<int>();
            foreach (Tuple<int, int> start in galaxy.HSLocations)
            {
                SystemTile startTile = galaxy.tiles[start.Item1][start.Item2];
                int playerNum = startTile.playerNum;
                players.Add(playerNum);
            }
            return players;
        }
    }
}
