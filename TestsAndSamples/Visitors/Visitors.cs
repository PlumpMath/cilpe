using System;
using CILPE.Config;

namespace VisitorsTest
{
	abstract class Op
	{
		[Inline]
		public Op () {}

		[Inline]
		public abstract int Act (int u);
	}

	class Add : Op
	{
		int x;

		[Inline]
		public Add (int x)
		{
			this.x = x;
		}

		[Inline]
		public override int Act (int y)
		{
			return x+y;
		}
	}

	class Mul : Op
	{
		int x;

		[Inline]
		public Mul (int x)
		{
			this.x = x;
		}

		[Inline]
		public override int Act (int y)
		{
			return x*y;
		}
	}


	class VisitorsTest
	{
//		[Inline]
		public static int Act (Op x, int y)
		{
			return x.Act(y);
		}

		[Inline]
		public static Op Create (bool p, int x)
		{
			if (p)
				return new Add(x);
			else
				return new Mul(x);
		}

		[Specialize]
		public static int Test (bool p, int x, int y)
		{
			Op op = Create(p, x);
			return Act(op, y);
		}
		static void Main(string[] args)
		{
			int x = 3;
			int y = 5;
			Console.WriteLine("{0}+{1} = {2}", x, y, Test(true,x,y));
		}
	}
}
