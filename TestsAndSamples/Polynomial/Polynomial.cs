using System;
using CILPE.Config;

namespace PolynomialTest
{

	class Polynomial
	{
		double[] coef;

		[Inline]
		public Polynomial (double[] coef)
		{
			this.coef = coef;
		}

		[Inline]
		public double Calc (double x)
		{
			double res = 0;
			for (int i = coef.Length-1; i>=0; i--)
				res = res*x+coef[i];
			return res;
		}
	}

	class VirtualTest
	{
        [Specialize]
        public static double Test (double x)
		{
			double[] coef = {1,2,3}; // p(x) = 1+2x+3x^2
			Polynomial p = new Polynomial(coef);
			return p.Calc(x);
		}

        [Specialize]
        public static double Test2 (double a0, double a1, double a2, double x)
		{
			double[] coef = {a0,a1,a2};
			Polynomial p = new Polynomial(coef);
			return p.Calc(x);
		}

		//[Specialize]
		public static double TestLarge (double x)
		{
			double[] coef = new double[64];
			for(int i = 0; i < coef.Length; i++)
				coef[i] = i;
			Polynomial p = new Polynomial(coef);
			return p.Calc(x);
		}

		static void Main(string[] args)
		{
			double x = -1;
			double res = 0;
			DateTime markedTime = DateTime.Now;
//			for (int i = 0; i < 10000000; i++)
//				res = TestLarge(x);
			Console.WriteLine(DateTime.Now - markedTime);
//			Console.WriteLine("p({0}) = {1}", x, Test(x));
		}
	}
}
