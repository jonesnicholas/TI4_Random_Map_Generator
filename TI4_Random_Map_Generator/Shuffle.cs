using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TI4_Random_Map_Generator
{
    class Shuffle
    {
        public Random random = new Random();

        public Shuffle(int seed)
        {
            random = new Random(seed);
        }

        public Shuffle()
        {

        }

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
