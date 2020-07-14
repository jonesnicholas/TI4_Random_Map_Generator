using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TI4RandomMapGenerator
{
    enum Trait
    {
        None,
        Industrial,
        Cultural,
        Hazardous,
    }

    enum Specialty
    {
        None,
        Red,
        Yellow,
        Green,
        Blue
    }

    class Planet
    {
        public int resources;
        public int influence;
        public Trait trait;
        public Specialty specialty;
        public string name;

        public Planet(int res, int inf, string inName, Trait trt = Trait.None, Specialty spc = Specialty.None)
        {
            resources = res;
            influence = inf;
            trait = trt;
            specialty = spc;
            name = inName;
        }

        public override string ToString()
        {
            return $"{name}({resources},{influence})";
        }
    }
}
