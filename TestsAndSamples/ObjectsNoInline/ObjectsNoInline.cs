using System;
using CILPE.Config;

namespace NewObjectTest
{
    class Container 
    {
        public int n;
        [Inline]
        public Container (int n) {
            this.n = n;
        }
        public Container () // n = 0
        {
            this.n = 0;
        }
    }

    class TestClass 
    {
        public static void Increase (Container c, int i) 
        {
            c.n += i;
            Console.WriteLine("{0}", c.n);
        }

        public static void Increase_n_5 (ref int cs0_n) // i = 5
        {
            cs0_n += 5;
            Console.WriteLine("{0}", cs0_n);
        }

        public static void Increase_n (ref int cs0_n, int i) 
        {
            cs0_n += i;
            Console.WriteLine("{0}", cs0_n);
        }

        public static void Increase_5 (Container c) // i = 5
        {
            c.n += 5;
            Console.WriteLine("{0}", c.n);
        }

        [Specialize]
        public static Container Test (int n) 
        {
            Container cs0 = new Container(n);
            Container cs1 = new Container(0);
            Increase(cs0, 5);
            Increase(cs0, n);
            Increase(cs1, 5);
            return cs1;
        }

        public static Container Test2 (int n) 
        {
            int cs0_n = n;
            Container cs1 = new Container();
            Increase_n_5(ref cs0_n);
            Increase_n(ref cs0_n, n);
            Increase_5(cs1);
            return cs1;
        }

        static void Main(string[] args)
		{
			int x = 3;
			Console.WriteLine("{0} = {1}", x, Test(x).n);
            Console.WriteLine("{0} = {1}", x, Test2(x).n);
        }
	}

}
