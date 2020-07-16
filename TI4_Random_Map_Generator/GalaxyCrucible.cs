using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TI4_Random_Map_Generator
{
    class GalaxyCrucible
    {
        public Galaxy bestGal;
        public double bestScore = double.MinValue;

        /// <summary>
        /// Generates and scores a bunch of galaxies, and saves the best one
        /// </summary>
        /// <param name="batchSize">The amount of galaxies to generate in the current run</param>
        /// <param name="runParallel">Whether or not to attempt to generate galaxies in parallel</param>
        public void generateGalaxies(int batchSize = 1000, bool runParallel = true)
        {
            DateTime start = DateTime.Now;
            Shuffle shuffle = new Shuffle();
            bestGal = bestGal ?? new Galaxy(GalaxyShape.Standard, 3, 6, shuffle);
            Object scoreCheckLock = new object();

            if (runParallel)
            {
                Parallel.For(0, batchSize, i =>
                {
                    Galaxy genGal = new Galaxy(GalaxyShape.Standard, 3, 6, shuffle);
                    genGal.score = new Scorer().scoreGalaxy(genGal, contestMethod: ContestValue.ClaimSize);
                    lock (scoreCheckLock)
                    {
                        if (genGal.score > bestScore)
                        {
                            bestScore = genGal.score;
                            bestGal = genGal;
                            DateTime total = DateTime.Now;
                            Debug.WriteLine($"t: {(total - start).TotalSeconds} => {Math.Round(genGal.score, 3)}");
                            //Debug.WriteLine($"    {genGal.GetTTSString()}");
                        }
                    }
                });
            }
            else
            {
                for (int i = 0; i < batchSize; i++)
                {
                    Galaxy genGal = new Galaxy(GalaxyShape.Standard, 3, 6, shuffle);
                    genGal.score = new Scorer().scoreGalaxy(genGal, contestMethod: ContestValue.ClaimSize);
                    if (genGal.score > bestScore)
                    {
                        bestScore = genGal.score;
                        bestGal = genGal;
                        DateTime total = DateTime.Now;
                        Debug.WriteLine($"t: {(total - start).TotalSeconds} => {Math.Round(genGal.score, 3)}");
                    }
                }
            }
        }
    }
}
