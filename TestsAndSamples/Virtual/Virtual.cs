using System;
using CILPE.Config;

namespace VirtualTest
{
	abstract class A
	{
		[Inline]
		public A () {}

		[Inline]
		public abstract int Get ();
	}

	class B : A
	{
		[Inline]
		public B () {}

		[Inline]
		public override int Get ()
		{
			return 1;
		}
	}

	class C : A
	{
		[Inline]
		public C () {}

		[Inline]
		public override int Get ()
		{
			return 2;
		}
	}


	class VirtualTest
	{
		[Inline]
		public static A GetObj (bool p)
		{
			if (p)
				return new B();
			else
				return new C();
		}

		[Specialize]
		public static int Test (bool p)
		{
			A a = GetObj(p);

			return a.Get();
		}

		static void Main(string[] args)
		{
			bool p = true;
			Console.WriteLine("{0} - {1}", p, Test(p));
		}
	}
}
