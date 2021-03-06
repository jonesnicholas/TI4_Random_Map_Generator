﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TI4_Random_Map_Generator
{
    [Flags]
    enum Wormhole
    {
        None = 0,
        Alpha = 1,
        Beta = 2,
        Gamma = 4,
        Delta = 8,
    }
    
    enum Anomaly
    {
        None,
        Asteroids,
        Rift,
        Nova,
        Nebula,
    }

    class SystemTile
    {
        public List<Planet> planets;
        public List<SystemTile> adjacent;
        public Wormhole wormholes;
        public Anomaly anomaly;
        public int sysNum;
        public int playerNum = 0;
        public Dictionary<int, int> distClaims;
        public Dictionary<int, double> strClaims;

        public SystemTile(int tileNum, List<Planet> inPlanets, Wormhole wormholeType = Wormhole.None, Anomaly anm = Anomaly.None)
        {
            planets = inPlanets;
            wormholes = wormholeType;
            anomaly = anm;
            adjacent = new List<SystemTile>();
            sysNum = tileNum;
        }

        /// <summary>
        /// Gets the total resources of all planets in the system
        /// </summary>
        /// <returns>The total resources of all planets in the system</returns>
        public int GetResources()
        {
            return planets.Sum(planet => planet.resources);
        }
         
        /// <summary>
        /// Gets the total influence of all planets in the system
        /// </summary>
        /// <returns>The total influence of all planets in the system</returns>
        public int GetInfluence()
        {
            return planets.Sum(planet => planet.influence);
        }

        /// <summary>
        /// Gets a string summary of the system, useful primarily for debugging
        /// </summary>
        /// <returns>If no planets, the 'name' of the system (e.g. Beta Wormhole, Gravity Rift). Otherwise the planet strings separated by ' / '</returns>
        public override string ToString()
        {
            StringBuilder output = new StringBuilder();
            output.Append($"{sysNum}: \"");
            if (planets.Count() == 0)
            {
                if (anomaly.HasFlag(Anomaly.Asteroids))
                {
                    output.Append("Asteroid Field");
                }
                else if (anomaly.HasFlag(Anomaly.Nova))
                {
                    output.Append("Supernova");
                }
                else if (anomaly.HasFlag(Anomaly.Nebula))
                {
                    output.Append("Nebula");
                }
                else if (anomaly.HasFlag(Anomaly.Rift))
                {
                    output.Append("Gravity Rift");
                }
                else if (wormholes != Wormhole.None)
                {
                    output.Append($"{wormholes.ToString()} wormhole");
                }
                else if (sysNum > 0)
                {
                    output.Append("Empty Space");
                }
            }
            for (int i = 0; i < planets.Count; i++)
            {
                output.Append(planets[i].ToString());
                if (i != planets.Count - 1)
                {
                    output.Append(" / ");
                }
            }

            return output.ToString();
        }
    }
}
