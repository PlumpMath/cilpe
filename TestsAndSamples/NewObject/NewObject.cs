using System;
using CILPE.Config;

namespace NewObjectTest
{
	struct A1 {public int x, y;}
	class A
	{
		int f;
		[Inline]
		public A (int x)
		{
			this.f = x;
		}
		[Inline]
		public int Get ()
		{
			return this.f;
		}
	}

	class NewObjectTest
	{
		[Specialize]
		public static int Test (int x)
		{
			A a = new A(x);
			return  a.Get();
		}
		[Specialize]
		public static int Test2 (int x)
		{
			int y = 0;
			for (int i = 0; i < 3; i++)
			{
				A a = new A(x);
				y += a.Get();
			}
			return y;
		}
		static void Main(string[] args)
		{
			int x = 5;
			Console.WriteLine("{0} = {1}", x, Test2(x));
		}
	}
}
