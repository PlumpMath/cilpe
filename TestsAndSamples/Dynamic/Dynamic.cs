using System;
using CILPE.Config;

namespace DynamicTest
{
	class A
	{
		public int f;
		public int g;

		public A (int x, int y)
		{
			this.f = x+y;
			this.g = y*y;
		}
	}

	class TestClass
	{
		[Specialize]
		public static A Test (int x)
		{
			return new A(x, 5);
		}

		static void Main(string[] args)
		{
			int y = 7;
			Console.WriteLine("A(1,{0}) = {1}", y, Test(y));
		}
	}
}
