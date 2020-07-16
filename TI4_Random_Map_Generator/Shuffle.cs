using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TI4_Random_Map_Generator
{
    class Shuffle
    {
        public Random random;

        public Shuffle(int seed)
        {
            random = new Random(seed);
        }

        public Shuffle()
        {
            random = new Random();
        }

        /// <summary>
        /// Given a list of items, randomizes its order
        /// </summary>
        /// <typeparam name="T">The type of object held within the list</typeparam>
        /// <param name="list">The list to be randomized</param>
        public void ShuffleList<T>(List<T> list)
        {
            if (list.Count > 1)
            {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    T tmp = list[i];
                    int randomIndex;
                    lock (this)
                    {
                        randomIndex = random.Next(i + 1);
                    }
                    list[i] = list[randomIndex];
                    list[randomIndex] = tmp;
                }
            }
        }
    }
}
