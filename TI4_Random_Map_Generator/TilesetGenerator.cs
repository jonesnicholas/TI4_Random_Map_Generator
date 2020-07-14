using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TI4_Random_Map_Generator
{
    class TilesetGenerator
    {
        public static List<SystemTile> GetAllTiles()
        {
            List<SystemTile> tiles = GetBlueTiles();
            tiles.AddRange(GetRedTiles());
            return tiles;
        }

        public static List<SystemTile> GetBlueTiles()
        {
            List<SystemTile> tiles = new List<SystemTile>();
            for (int tileNum = 19; tileNum <= 38; tileNum++)
            {
                tiles.Add(GetSystemTile(tileNum));
            }
            return tiles;
        }

        public static List<SystemTile> GetRedTiles()
        {
            List<SystemTile> tiles = new List<SystemTile>();
            for (int tileNum = 39; tileNum <= 50; tileNum++)
            {
                tiles.Add(GetSystemTile(tileNum));
            }
            return tiles;
        }

        public static SystemTile GetMecatol()
        {
            return GetSystemTile(18);
        }

        public static SystemTile GetBlankTile()
        {
            return new SystemTile(0, new List<Planet>());
        }

        static SystemTile GetSystemTile(int tileNum)
        {
            List<Planet> planets = GetPlanets(tileNum);
            Anomaly anomaly = GetAnomalyStatus(tileNum);
            Wormhole wormhole = GetWormholeStatus(tileNum);
            return new SystemTile(tileNum, planets, wormhole, anomaly);
        }

        static List<Planet> GetPlanets(int tileNum)
        {
            List<Planet> planets = new List<Planet>();
            switch (tileNum)
            {
                case 1:
                    planets.Add(new Planet(4, 2, "Jord"));
                    break;
                case 2:
                    planets.Add(new Planet(4, 1, "Moll Primus"));
                    break;
                case 3:
                    planets.Add(new Planet(4, 4, "Darien"));
                    break;
                case 4:
                    planets.Add(new Planet(4, 1, "Muaat"));
                    break;
                case 5:
                    planets.Add(new Planet(3, 2, "Nestaphar"));
                    break;
                case 6:
                    planets.Add(new Planet(5, 0, "0.0.0"));
                    break;
                case 7:
                    planets.Add(new Planet(3, 4, "Winnu"));
                    break;
                case 8:
                    planets.Add(new Planet(4, 0, "Mordai II"));
                    break;
                case 9:
                    planets.Add(new Planet(3, 1, "Druaa"));
                    planets.Add(new Planet(0, 2, "Maaluuk"));
                    break;
                case 10:
                    planets.Add(new Planet(4, 0, "Arc Prime"));
                    planets.Add(new Planet(2, 1, "Wren Terra"));
                    break;
                case 11:
                    planets.Add(new Planet(1, 0, "Lisis II"));
                    planets.Add(new Planet(2, 1, "Ragh"));
                    break;
                case 12:
                    planets.Add(new Planet(1, 2, "Jol"));
                    planets.Add(new Planet(2, 3, "Nar"));
                    break;
                case 13:
                    planets.Add(new Planet(3, 1, "Quinarra"));
                    planets.Add(new Planet(1, 0, "Tren'lak"));
                    break;
                case 14:
                    planets.Add(new Planet(2, 3, "Archon Ren"));
                    planets.Add(new Planet(1, 2, "Archon Tau"));
                    break;
                case 15:
                    planets.Add(new Planet(2, 3, "Retillion"));
                    planets.Add(new Planet(1, 2, "Shalloq"));
                    break;
                case 16:
                    planets.Add(new Planet(2, 0, "Arretze"));
                    planets.Add(new Planet(1, 1, "Hercant"));
                    planets.Add(new Planet(0, 1, "Kamdorn"));
                    break;
                case 18:
                    planets.Add(new Planet(1, 6, "Mecatol Rex"));
                    break;
                case 19:
                    planets.Add(new Planet(1, 2, "Wellon", Trait.Industrial, Specialty.Yellow));
                    break;
                case 20:
                    planets.Add(new Planet(2, 2, "Vefut II", Trait.Hazardous));
                    break;
                case 21:
                    planets.Add(new Planet(1, 1, "Thibah", Trait.Industrial, Specialty.Blue));
                    break;
                case 22:
                    planets.Add(new Planet(1, 1, "Tar'mann", Trait.Industrial, Specialty.Green));
                    break;
                case 23:
                    planets.Add(new Planet(2, 2, "Saudor", Trait.Industrial));
                    break;
                case 24:
                    planets.Add(new Planet(1, 3, "Mehar Xull", Trait.Hazardous, Specialty.Red));
                    break;
                case 25:
                    planets.Add(new Planet(2, 1, "Quann", Trait.Cultural));
                    break;
                case 26:
                    planets.Add(new Planet(3, 1, "Lodor", Trait.Cultural));
                    break;
                case 27:
                    planets.Add(new Planet(1, 1, "New Albion", Trait.Industrial, Specialty.Green));
                    planets.Add(new Planet(3, 1, "Starpoint", Trait.Hazardous));
                    break;
                case 28:
                    planets.Add(new Planet(2, 0, "Tequ'ran", Trait.Hazardous));
                    planets.Add(new Planet(0, 3, "Torkan", Trait.Cultural));
                    break;
                case 29:
                    planets.Add(new Planet(0, 3, "Rarron", Trait.Cultural));
                    planets.Add(new Planet(1, 2, "Qucen'n", Trait.Industrial));
                    break;
                case 30:
                    planets.Add(new Planet(0, 2, "Mellon", Trait.Cultural));
                    planets.Add(new Planet(3, 1, "Zohbat", Trait.Hazardous));
                    break;
                case 31:
                    planets.Add(new Planet(1, 0, "Lazar", Trait.Industrial, Specialty.Yellow));
                    planets.Add(new Planet(2, 1, "Sakulag", Trait.Hazardous));
                    break;
                case 32:
                    planets.Add(new Planet(0, 2, "Dal Bootha", Trait.Cultural));
                    planets.Add(new Planet(1, 1, "Xxehan", Trait.Cultural));
                    break;
                case 33:
                    planets.Add(new Planet(1, 2, "Corneeq", Trait.Cultural));
                    planets.Add(new Planet(2, 0, "Resculon", Trait.Cultural));
                    break;
                case 34:
                    planets.Add(new Planet(1, 3, "Centauri", Trait.Cultural));
                    planets.Add(new Planet(1, 1, "Gral", Trait.Industrial, Specialty.Blue));
                    break;
                case 35:
                    planets.Add(new Planet(3, 1, "Bereg", Trait.Hazardous));
                    planets.Add(new Planet(2, 3, "Lirta IV", Trait.Hazardous));
                    break;
                case 36:
                    planets.Add(new Planet(2, 1, "Arnor", Trait.Industrial));
                    planets.Add(new Planet(1, 2, "Lor", Trait.Industrial));
                    break;
                case 37:
                    planets.Add(new Planet(1, 2, "Arinam", Trait.Industrial));
                    planets.Add(new Planet(0, 4, "Meer", Trait.Industrial, Specialty.Red));
                    break;
                case 38:
                    planets.Add(new Planet(3, 0, "Abyz", Trait.Hazardous));
                    planets.Add(new Planet(2, 0, "Fria", Trait.Hazardous));
                    break;
                case 51:
                    planets.Add(new Planet(4, 2, "Creuss"));
                    break;
            }
            return planets;
        }

        static Wormhole GetWormholeStatus(int tileNum)
        {
            switch (tileNum)
            {
                case 25:
                case 40:
                    return Wormhole.Beta;
                case 26:
                case 39:
                    return Wormhole.Alpha;
                case 17:
                case 51:
                    return Wormhole.Delta;
            }
            return Wormhole.None;
        }

        static Anomaly GetAnomalyStatus(int tileNum)
        {
            switch (tileNum)
            {
                case 41:
                    return Anomaly.Rift;
                case 42:
                    return Anomaly.Nebula;
                case 43:
                    return Anomaly.Nova;
                case 44:
                case 45:
                    return Anomaly.Asteroids;
            }
            return Anomaly.None;
        }
    }
}
