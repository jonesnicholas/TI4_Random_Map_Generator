using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TI4RandomMapGenerator
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
        TopAndRunnerUp, // same as claim size, but only those tied for best claim, and those tied for next highest claim have valid claims
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

            List<int> players;
            Dictionary<int, List<SystemTile>> sliceClaim;
            Dictionary<int, List<SystemTile>> sliceContest;


            (players, sliceClaim, sliceContest) = StakeClaims(galaxy);
            if (contestMethod == ContestValue.Slices)
            {
                resourceScore =
                    GetResourceScoreSlice(
                        galaxy,
                        players,
                        sliceClaim,
                        sliceContest,
                        resMethod,
                        ResInfRatio,
                        ResScaling);
            }
            else
            {
                resourceScore =
                    GetResourceScoreClaims(
                        galaxy,
                        players,
                        resMethod,
                        contestMethod,
                        ResInfRatio,
                        ResScaling,
                        claimExponent);
            }

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

        public double GetResourceScoreClaims(
            Galaxy galaxy,
            List<int> players,
            ResourceScoreMethod resMethod = ResourceScoreMethod.MaxVal,
            ContestValue contestMethod = ContestValue.TopAndRunnerUp,
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
                                    if (claims[pnum] == tile.bestClaim)
                                    {
                                        claims.Add(pnum, 1.0);
                                    }
                                    break;
                                case ContestValue.ClaimSize:
                                    if (tile.bestClaim > 0)
                                    {
                                        claims.Add(pnum, Math.Pow(tile.claims[pnum], claimExponent));
                                    }
                                    else if (claims[pnum] == tile.bestClaim)
                                    {
                                        claims.Add(pnum, 1.0);
                                    }
                                    break;
                                case ContestValue.TopAndRunnerUp:
                                    if (tile.bestClaim == 0 && claims[pnum] == tile.bestClaim)
                                    {
                                        claims.Add(pnum, 1.0);
                                    }
                                    else if (claims[pnum] == tile.bestClaim || claims[pnum] == tile.secondBestClaim)
                                    {
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

        public double GetResourceScoreSlice(
            Galaxy galaxy,
            List<int> players,
            Dictionary<int, List<SystemTile>> sliceClaim,
            Dictionary<int, List<SystemTile>> sliceContest,
            ResourceScoreMethod resMethod = ResourceScoreMethod.MaxVal,
            double ResInfRatio = 1.0,
            double ResScaling = 2.0)
        {
            double resourceScore = 0;
            
            switch (resMethod)
            {
                case ResourceScoreMethod.Separate:

                    double bestRes = double.MinValue;
                    double worstRes = double.MaxValue;

                    double bestInf = double.MinValue;
                    double worstInf = double.MaxValue;

                    foreach (int player in players)
                    {
                        double res = sliceVal(sliceClaim[player], sliceContest[player], res: true);

                        bestRes = res > bestRes ? res : bestRes;
                        worstRes = res < worstRes ? res : worstRes;

                        double inf = sliceVal(sliceClaim[player], sliceContest[player], res: false);

                        bestInf = inf > bestInf ? inf : bestInf;
                        worstInf = inf < worstInf ? inf : worstInf;
                    }

                    resourceScore =
                        (Math.Pow(worstRes / bestRes, ResScaling) +
                        ResInfRatio * Math.Pow(worstInf / bestInf, ResScaling));

                    resourceScore /= (1 + ResInfRatio);

                    break;
                case ResourceScoreMethod.DirectSum:

                    double best = double.MinValue;
                    double worst = double.MaxValue;
                    foreach (int player in players)
                    {
                        double resValue = sliceVal(sliceClaim[player], sliceContest[player], res: true);
                        double infValue = sliceVal(sliceClaim[player], sliceContest[player], res: true);

                        double value = resValue + ResInfRatio * infValue;
                        best = value > best ? value : best;
                        worst = value < worst ? value : worst;
                    }
                    resourceScore = Math.Pow(worst / best, ResScaling);

                    break;
                case ResourceScoreMethod.MaxVal:
                default:

                    double bestVal = double.MinValue;
                    double worstVal = double.MaxValue;

                    foreach (int player in players)
                    {
                        double maxVal = 0;
                        if (sliceClaim[player].Where(tile => tile.planets.Count > 0).Count() > 0)
                        {
                            maxVal +=
                            sliceClaim[player].Where(tile => tile.planets.Count > 0).Select(tile =>
                                tile.planets.Select(planet =>
                                    Math.Max(planet.resources, ResInfRatio * planet.influence)
                                ).Aggregate((i, j) => i + j)
                            ).Aggregate((i, j) => i + j);
                        }
                        if (sliceContest[player].Where(tile => tile.planets.Count > 0).Count() > 0)
                        {
                            maxVal +=
                            sliceContest[player].Where(tile => tile.planets.Count > 0).Select(tile =>
                                (double)(tile.planets.Select(planet =>
                                    Math.Max(planet.resources, ResInfRatio * planet.influence)
                                ).Aggregate((i, j) => i + j)) / tile.contestedBy.Count()
                            ).Aggregate((i, j) => i + j);
                        }

                        bestVal = maxVal > bestVal ? maxVal : bestVal;
                        worstVal = maxVal < worstVal ? maxVal : worstVal;
                    }

                    resourceScore = Math.Pow(worstVal / bestVal, ResScaling);
                    break;
            }

            return resourceScore;
        }

        public (List<int>, Dictionary<int,List<SystemTile>>, Dictionary<int,List<SystemTile>>) StakeClaims(
            Galaxy galaxy, 
            int walk = 10, 
            int emptyWalk = 11,
            int asteroidWalk = 13, 
            int novaWalk = 100,
            int nebulaWalk = 50,
            int riftWalk = 05, 
            int wormWalk = 12)
        {
            int MaxRadius = galaxy.MaxRadius;
            SystemTile[][] tiles = galaxy.tiles;
            List<Tuple<int, int>> HSLocations = galaxy.HSLocations;

            foreach (Tuple<int, int> start in HSLocations)
            {
                SystemTile startTile = tiles[start.Item1][start.Item2];
                int playerNum = startTile.playerNum;
                startTile.claims[playerNum] = 0;
                SortedList<int, List<SystemTile>> adjacent = new SortedList<int, List<SystemTile>>();
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

            for (int x = 0; x <= MaxRadius * 2; x++)
            {
                for (int y = 0; y <= MaxRadius * 2; y++)
                {
                    SystemTile tile = tiles[x][y];
                    if (tile.claims.Count() > 0)
                    {
                        tile.bestClaim = tile.claims.Min(claim => claim.Value);
                        if (tile.claims.Count(claim => claim.Value > tile.bestClaim) > 0)
                        {
                            // TODO: no second best claim if > half players have best claim
                            // TODO: rework code so "top half" of claims are allowed
                            tile.secondBestClaim = tile.claims.Where(claim => claim.Value > tile.bestClaim).Min(claim => claim.Value);
                        }
                    }
                }
            }

            Dictionary<int, List<SystemTile>> sliceClaim = new Dictionary<int, List<SystemTile>>();
            Dictionary<int, List<SystemTile>> sliceContest = new Dictionary<int, List<SystemTile>>();

            List<int> players = new List<int>();

            foreach (Tuple<int, int> start in HSLocations)
            {
                SystemTile startTile = tiles[start.Item1][start.Item2];
                int playerNum = startTile.playerNum;
                players.Add(playerNum);
                List<SystemTile> sliceTiles = new List<SystemTile>();
                List<SystemTile> contestedTiles = new List<SystemTile>();

                for (int x = 0; x <= MaxRadius * 2; x++)
                {
                    for (int y = 0; y <= MaxRadius * 2; y++)
                    {
                        SystemTile tile = tiles[x][y];
                        int bestClaim = tile.bestClaim;
                        if (tile.claims.Count() == 0)
                        {
                            continue;
                        }
                        int playerClaim = tile.claims[playerNum];
                        int contesting = tile.claims.Count(claim => claim.Value == bestClaim);
                        bool playerHasClaim = playerClaim == bestClaim;
                        bool contestedClaim = contesting > 1;
                        if (playerHasClaim)
                        {
                            tile.contestedBy.Add(playerNum);
                        }
                        if (playerHasClaim && contestedClaim)
                        {
                            contestedTiles.Add(tile);
                        }
                        if (playerHasClaim && !contestedClaim)
                        {
                            sliceTiles.Add(tile);
                        }
                    }
                }

                sliceClaim.Add(playerNum, sliceTiles);
                sliceContest.Add(playerNum, contestedTiles);
            }

            return (players, sliceClaim, sliceContest);
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

    }
}
