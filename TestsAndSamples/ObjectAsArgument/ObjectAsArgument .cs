using System;
using CILPE.Config;

namespace AckermannTest
{
	class Obj
	{
		public int x;

		public Obj (int x)
		{
			this.x = x;
		}
	}

    class Obj2
    {
        public Obj o;

        [Inline]
        public Obj2 (Obj o)
        {
            this.o = o;
        }
    }

    class AckermannTest
	{
        public static void Test (bool t, Obj y, Obj z)
        {
            y.x = 5;
            z.x = 7;
            (t ? y : z).x = 57;
        }

        [Specialize]
		public static int Test (bool t)
		{
            Obj y = new Obj(0);
            Obj z = new Obj(0);
            Obj2 o = new Obj2(y);
            o.o = z;
            Test(t, y, z);
			return y.x+z.x;
		}

		static void Main(string[] args)
		{
			Console.WriteLine("Test({0}) = {1}", true, Test(true));
            Console.WriteLine("Test({0}) = {1}", false, Test(false));
        }
	}
}
