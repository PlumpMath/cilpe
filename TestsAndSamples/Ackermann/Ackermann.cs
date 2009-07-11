using System;
using CILPE.Config;

namespace AckermannTest
{
	class Ackermann
	{
		int x;
		Ackermann a;

		[Inline]
		public Ackermann (int x)
		{
			this.x = x;
			if (x != 0)
				a = new Ackermann(x-1);
		}

		public int Calculate (int y)
		{
			if (this.x == 0)
				return y+1;
			else if (y == 0)
				return this.a.Calculate(1);
			else
				return this.a.Calculate(this.Calculate(y-1));
		}
	}

	class AckermannTest
	{
		[Specialize]
		public static int Test (int y)
		{
			Ackermann a = new Ackermann(1);
			return a.Calculate(y);
		}
		static void Main(string[] args)
		{
			int y = 7;
			Console.WriteLine("A(1,{0}) = {1}", y, Test(y));
		}
	}
}
