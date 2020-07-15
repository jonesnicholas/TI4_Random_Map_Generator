using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TI4_Random_Map_Generator
{
    class Parallelizer
    {
        public Galaxy bestGal;
        public double bestScore = double.MinValue;

        public void parallelize(int batchSize = 1000)
        {
            DateTime start = DateTime.Now;
            bestGal = bestGal ?? new Galaxy(GalaxyShape.Standard, new Shuffle(), 3, 6);
            Object winnerLock = new object();
            Shuffle shuffle = new Shuffle();

                Parallel.For(0, batchSize, i =>
                //for (int i = 0; i < batchSize; i++)
                {
                    Galaxy genGal = new Galaxy(GalaxyShape.Standard, shuffle, 3, 6);
                    genGal.score = new Scorer().scoreGalaxy(genGal, contestMethod: ContestValue.ClaimSize);
                    if (genGal.score > bestScore)
                    {
                        lock (winnerLock)
                        {
                            bestScore = genGal.score;
                            bestGal = genGal;
                            DateTime total = DateTime.Now;
                            Debug.WriteLine($"t: {(total - start).TotalSeconds} => {Math.Round(genGal.score, 3)}");
                            //Debug.WriteLine($"    {genGal.GetTTSString()}");
                        }
                    }
                });

            /*for (int pnum = 1; pnum <= bestGal.players; pnum++)
            {
                Debug.Write($"   {pnum}   ");
            }
            Debug.WriteLine("");

            for (int x = 0; x <= bestGal.MaxRadius * 2; x++)
            {
                for (int y = 0; y <= bestGal.MaxRadius * 2; y++)
                {
                    SystemTile tile = bestGal.tiles[x][y];
                    if (tile.sysNum > 0)
                    {
                        for (int pnum = 1; pnum <= bestGal.players; pnum++)
                        {
                            double val = 0.0;
                            if (tile.adjClaims.ContainsKey(pnum))
                            {
                                val = Math.Round(tile.adjClaims[pnum], 3);
                            }
                            string print = $"{val}";
                            if (val < 10)
                            {
                                print = " " + print;
                            }
                            while (print.Length < 6)
                            {
                                print = print + "0";
                            }
                            print = print.Substring(0, 6);
                            Debug.Write($"{print} ");
                        }
                        Debug.Write($"{tile.ToString()}\n");
                    }
                }
                Debug.Write("\n");
            }*/
            //Debug.WriteLine($"\n{bestGal.GetTTSString()}");
        }
    }
}
