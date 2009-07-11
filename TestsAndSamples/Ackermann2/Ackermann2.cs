using System;
using CILPE.Config;

namespace Ackermann
{
    class Ackermann
    {
        public static ulong A (ulong x, ulong y) {
            if (x == 0) return y+1;
            else if (y == 0) return A(x-1, 1);
            else return A(x-1, A(x, y-1));
        }

        [Specialize]
        public static ulong Test (ulong y) {
            return A(3, y);
        }

        public static ulong A_3 (ulong y) 
        {
            return y == 0 ? A_2_1() : A_2(A_3(y-1));
        }
        public static ulong A_2 (ulong y) 
        {
            return y == 0 ? A_1_1() : A_1(A_2(y-1));
        }
        public static ulong A_1 (ulong y) 
        {
            return y == 0 ? A_0_1() : A_0(A_1(y-1));
        }
        public static ulong A_0 (ulong y) { return y+1; }
        public static ulong A_2_1 () { return A_1(A_2_0()); }
        public static ulong A_2_0 () { return A_1_1(); }
        public static ulong A_1_1 () { return A_0(A_1_0()); }
        public static ulong A_1_0 () { return A_0_1(); }
        public static ulong A_0_1 () { return 2; }
        public static ulong Test2 (ulong y) { return A_3(y); }

        public static void Main(string[] args)
        {
            ulong x = 3;
            for (int y = 6; y <= 10; y++) {
                double res = 0;
                DateTime markedTime = DateTime.Now;
                for (int i = 0; i < 50; i++)
                    res = Test((ulong)y);
                Console.WriteLine(DateTime.Now - markedTime);
                Console.WriteLine("A({0},{1}) = {2}", x, y, res);
                Console.WriteLine("A({0},{1}) = {2}", x, y, Test2((ulong)y));
            }
        }
    }
}
