using System;
using CILPE.Config;

namespace Ackermann
{
    class Ackermann
    {
        public static ulong A (ulong x, ulong y)
        {
            return B(x, y);
        }

        [Inline]
        public static ulong B (ulong x, ulong y)
        {
            if (x == 0)
                return y+1;
            else if (y == 0)
                return B(x-1, 1);
            else
                return B(x-1, A(x, y-1));
        }

        [Specialize]
        public static ulong Test (ulong y)
        {
            return A(3, y);
        }

        public static void Main(string[] args)
        {
            ulong x = 3;
            for (ulong y = 6; y <= 11; y++)
            {
                double res = 0;

                DateTime markedTime = DateTime.Now;
                for (int i = 0; i < 20; i++)
                    res = Test(y);
                Console.WriteLine(DateTime.Now - markedTime);

                Console.WriteLine("A({0},{1}) = {2}", x, y, res);
            }
        }
    }
}
