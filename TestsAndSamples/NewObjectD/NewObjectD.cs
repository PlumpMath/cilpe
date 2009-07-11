using System;
using CILPE.Config;

namespace NewObjectDTest
{
	class A
	{
		public int f;
		[Inline]
		public A (int x)
		{
			this.f = x;
		}
	}

	class B
	{
		public int f;
		public int g;
		[Inline]
		public B (int x, int y)
		{
			this.f = x;
			this.g = y;
		}
	}

	class NewObjectDTest
	{
		public static int Get (A a)
		{
			return a.f;
		}

		public static int Getb (B b)
		{
			return b.f+b.g;
		}

		[Specialize]
		public static int Test (int x)
		{
			A a = new A(x);
			return Get(a);
		}

		[Specialize]
		public static int Test2 (int x)
		{
			B a = new B(x,5);
			return Getb(a);
		}
		static void Main(string[] args)
		{
			int x = 5;
			Console.WriteLine("{0}*5+({0}+1)*5 = {1}", x, Test(x));
		}
	}
}
