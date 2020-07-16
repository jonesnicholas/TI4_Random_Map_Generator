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
        // TODO: Consider passing in scoring parameters as one single 'Parameters' object (?)
        // Otherwise as more scoring parameters are added in, this signature is going to get *Massive*
        /// <summary>
        /// Calculates the 'score' of a galaxy. 
        /// </summary>
        /// <param name="galaxy">The galaxy to score</param>
        /// <param name="bothHoles">If the galaxy should include both wormhole types</param>
        /// <param name="HoleCount">Desired wormhole count</param>
        /// <param name="hardHoleLimit">If the wormhole count is a strict limit (==)</param>
        /// <param name="allowAdjacentHoles">If an alpha wormhole can be placed next to a beta wormhole</param>
        /// <param name="contestMethod">Method used to convert distance claims into strength claims</param>
        /// <param name="resMethod">Method used to calculate a 'slice' resource value</param>
        /// <param name="ResInfRatio">ratio of value between resources and influence. e.g. 0.5 means 1 influence is worth 0.5 resources</param>
        /// <param name="ResScaling">Scales differences between most and least resource rich slices</param>
        /// <param name="claimExponent">The exponent to use when converting distance claims to strength claims. -2 by default, negative recommended</param>
        /// <returns>A double representing the 'fairness' of the galaxy, scored from 0.0 'unsuitable' to 1.0 'perfectly fair & symmetric'</returns>
        public double scoreGalaxy(
            Galaxy galaxy,
            bool bothHoles = true,
            int HoleCount = 2,
            bool hardHoleLimit = true,
            bool allowAdjacentHoles = false,
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
                HoleCount,
                bothHoles,
                hardHoleLimit,
                allowAdjacentHoles))
            {
                return 0.0;
            }

            if (!AnomaliesOK(galaxy))
            {
                return 0.0;
            }

            List<int> players = GetPlayers(galaxy);
            
            StakeClaims(galaxy);

            Dictionary<int, double> playerResources = 
                GetResourceScoreClaims(
                    galaxy, 
                    players, 
                    resMethod, 
                    contestMethod, 
                    ResInfRatio, 
                    claimExponent);

            Dictionary<int, double> playerTraitAccess = new Dictionary<int, double>();
            Dictionary<int, double> stage1ObjectiveAccess;
            Dictionary<int, double> secretObjectiveAccess = new Dictionary<int, double>();
            Dictionary<int, double> easiestTraitAccess = new Dictionary<int, double>();

            double minSlice = playerResources.Min(claim => claim.Value);
            double maxSlice = playerResources.Max(claim => claim.Value);

            resourceScore =  Math.Pow(minSlice / maxSlice, ResScaling);


            stage1ObjectiveAccess = GetS1Score(galaxy, players, contestMethod);

            score = resourceScore;

            return score;
        }

        // under construction
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

        /// <summary>
        /// Gets the 'resource score' of each 'slice' avaiable to each player based on their claims in a galaxy
        /// </summary>
        /// <param name="galaxy">The galaxy to get the resource score for</param>
        /// <param name="players">The players contesting the galaxy</param>
        /// <param name="resMethod">Which method to use to get the 'resource score' of a given system</param>
        /// <param name="contestMethod">Which method to use to convert distance claims into strength claims</param>
        /// <param name="ResInfRatio">ratio of value between resources and influence. e.g. 0.5 means 1 influence is worth 0.5 resources</param>
        /// <param name="claimExponent">The exponent to use when converting distance claims to strength claims. -2 by default, negative recommended</param>
        /// <returns>A dictionary mapping player ints to the double representing the resource value of their 'claimed' systems</returns>
        public Dictionary<int, double> GetResourceScoreClaims(
            Galaxy galaxy,
            List<int> players,
            ResourceScoreMethod resMethod = ResourceScoreMethod.MaxVal,
            ContestValue contestMethod = ContestValue.TopAndClose,
            double ResInfRatio = 1.0,
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
                        foreach (int pnum in tile.distClaims.Keys)
                        {
                            switch (contestMethod)
                            {
                                case ContestValue.Slices:
                                    if (claims[pnum] == tile.distClaims.Values.Max())
                                    {
                                        // full claim on resources for best claim (or tied)
                                        claims.Add(pnum, 1.0);
                                    }
                                    break;
                                case ContestValue.ClaimSize:
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
                                case ContestValue.TopAndClose:
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
                        foreach(KeyValuePair<int, double> claim in claims)
                        {
                            double val = 0.0;
                            switch (resMethod)
                            {
                                case ResourceScoreMethod.DirectSum:
                                    val = tile.GetResources() + ResInfRatio * tile.GetInfluence();
                                    break;
                                case ResourceScoreMethod.Separate:
                                    throw new Exception("Not yet supporting \"separate\" for claim method");
                                case ResourceScoreMethod.MaxVal:
                                    val = tile.planets.Sum(planet => Math.Max(planet.resources, ResInfRatio * planet.influence));
                                    break;
                            }
                            tile.strClaims.Add(claim.Key, claim.Value / claimScale);
                            resourceClaims[claim.Key] += val * claim.Value / claimScale;
                        }
                    }
                }
            }

            return resourceClaims;
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

            // at this point, all tiles should have a 'distance' value corresponding to each player
        }

        /// <summary>
        /// Determines if the wormhole placement of the current galaxy meets minimum criteria
        /// </summary>
        /// <param name="galaxy">The galaxy to check</param>
        /// <param name="HoleCount">set the minimum number of wormholes to be present (for each, if bothHoles is on)</param>
        /// <param name="bothHoles">set if both alpha and beta wormholes need to be present</param>
        /// <param name="hardHoleLimit">set if hole count needs to be exact</param>
        /// <param name="allowAdjacentHoles">set if an alpha wormhole is allowed to be adjacent to a beta wormhole</param>
        /// <returns>true if wormhole criteria met, otherwise false</returns>
        public bool wormholesOK(
            Galaxy galaxy,
            int HoleCount = 2,
            bool bothHoles = true,
            bool hardHoleLimit = true,
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
            // this block checks to see if any tiles are 'adjacent twice'. (caused by two wormholes of same type being naturally adjacent)
            // since this is not allowed, we return false
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
            if (bothHoles && alphaCount + betaCount < HoleCount)
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

        /// <summary>
        /// Checks if any anomalies are next to each other, and returns false if so, since this is not allowed
        /// </summary>
        /// <param name="galaxy">The galaxy to check</param>
        /// <returns>false if any anomalies adjacent, otherwise true</returns>
        public bool AnomaliesOK(Galaxy galaxy)
        {
            int MaxRadius = galaxy.MaxRadius;
            SystemTile[][] tiles = galaxy.tiles;

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

            return true;
        }

        /// <summary>
        /// Gets a list of player int identifiers from the galaxy
        /// </summary>
        /// <param name="galaxy">The galaxy to get the player list for</param>
        /// <returns>A List of ints, representing the player identifiers</returns>
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
