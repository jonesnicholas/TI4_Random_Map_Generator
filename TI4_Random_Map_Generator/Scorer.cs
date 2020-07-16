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

    public enum StrContestMethod
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
            StrContestMethod contestMethod = StrContestMethod.ClaimSize,
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
            galaxy.StakeDistClaims();
            galaxy.StakeStrClaims(contestMethod, claimExponent);

            Dictionary<int, double> playerResources = 
                GetResourceScoreClaims(
                    galaxy, 
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


            stage1ObjectiveAccess = GetS1Score(galaxy, contestMethod);

            score = resourceScore;

            return score;
        }

        // under construction
        public Dictionary<int, double> GetS1Score(
            Galaxy galaxy,
            StrContestMethod contestMethod)
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
        /// <param name="resMethod">Which method to use to get the 'resource score' of a given system</param>
        /// <param name="contestMethod">Which method to use to convert distance claims into strength claims</param>
        /// <param name="ResInfRatio">ratio of value between resources and influence. e.g. 0.5 means 1 influence is worth 0.5 resources</param>
        /// <param name="claimExponent">The exponent to use when converting distance claims to strength claims. -2 by default, negative recommended</param>
        /// <returns>A dictionary mapping player ints to the double representing the resource value of their 'claimed' systems</returns>
        public Dictionary<int, double> GetResourceScoreClaims(
            Galaxy galaxy,
            ResourceScoreMethod resMethod = ResourceScoreMethod.MaxVal,
            StrContestMethod contestMethod = StrContestMethod.TopAndClose,
            double ResInfRatio = 1.0,
            double claimExponent = -2.0)
        {
            List<int> players = galaxy.GetPlayers();
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
                        foreach(KeyValuePair<int, double> strClaim in tile.strClaims)
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
                            resourceClaims[strClaim.Key] += val * strClaim.Value;
                        }
                    }
                }
            }

            return resourceClaims;
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
    }
}
