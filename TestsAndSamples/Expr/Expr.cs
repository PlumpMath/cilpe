using System;
using CILPE.Config;

namespace ExprTest
{
	abstract class Expr
	{
		[Inline]
		public Expr () {}

		[Inline]
		public abstract double GetValue ();
	}

	class Plus : Expr
	{
		Expr x,y;

		[Inline]
		public Plus (Expr x, Expr y)
		{
			this.x = x;
			this.y = y;
		}

		[Inline]
		public override double GetValue ()
		{
			return x.GetValue()+y.GetValue();
		}
	}

	class Value : Expr
	{
		double x;

		[Inline]
		public Value ()
		{
			this.x = 0;
		}

		[Inline]
		public Value (double x)
		{
			this.x = x;
		}

		[Inline]
		public void SetValue (double x)
		{
			this.x = x;
		}

		[Inline]
		public override double GetValue ()
		{
			return x;
		}
	}


	class ExprTest
	{
		[Specialize]
		public static double Test (double x)
		{
			Value var = new Value();
			Expr expr = var;
			for (int i = 1; i < 3; i++)
				expr = new Plus(expr, new Value(i));
			var.SetValue(x);
			return expr.GetValue();
		}

		static void Main(string[] args)
		{
			double x = 3;
			Console.WriteLine("({0}+5)+5 = {1}", x, Test(x));
		}
	}
}
