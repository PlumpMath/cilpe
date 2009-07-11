using System;
using CILPE.Config;

namespace DelegateTest
{
    delegate int D1 ();
    delegate void D2 (int i);

	class A
	{
		public D1 x;
        public D2 y;

		public A () {
//            int i = 0;

//            x = () => { return i; };
//            y = (z) => { i = z; };
		}

        public static int F (ref int x, out int y) 
        {
            x = 1;
            y = 2;
            return y;
        }
	}

	class TestClass
	{
		[Specialize]
		public static A Test (int x)
		{
			return new A();
		}

		static void Main(string[] args)
		{
			int y = 7;
			Console.WriteLine("A(1,{0}) = {1}", y, Test(y));
		}
	}
}
