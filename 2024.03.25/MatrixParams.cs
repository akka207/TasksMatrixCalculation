using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _2024._03._25
{
    class MatrixParams
    {
        public readonly int dim;
        public readonly int i;
        public readonly int j;
        public readonly double[,] a;
        public readonly double[,] b;
        public readonly double[,] c;
        public readonly Action? rowCalc;
        public MatrixParams(int dim, int i, int j, double[,] a, double[,] b, double[,] c, Action? rowCalc)
        {
            this.a = a;
            this.dim = dim;
            this.i = i;
            this.j = j;
            this.c = c;
            this.b = b;
            this.rowCalc = rowCalc;
        }
    }
}
