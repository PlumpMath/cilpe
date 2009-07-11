using System;
using CILPE.Config;

namespace NewObjectTest
{
	class Container
	{
		int n;
        double x, y;
        [Inline]
		public Container (int n, int x) {
			this.n = n;
            this.x = x;
            this.y = this.n*this.x;
        }
        [Inline]
        public double GetX () {
            return this.x;
        }
        [Inline]
        public double GetY () {
            return this.y;
        }
    }

	class NewObjectTest
	{
        [Specialize]
		public static double Test (int x)
		{
            Container[] cs = new Container[3];
			for (int i = 0; i < cs.Length; i++)
				cs[i] = new Container(i+1, x);
            double sx = 0;
            for (int i = 0; i < cs.Length; i++)
                sx += cs[i].GetX();
            double sy = 0;
            for (int i = 0; i < cs.Length; i++)
                sy += cs[i].GetY();
            return sx/sy;
		}

		static void Main(string[] args)
		{
			int x = 5;
			Console.WriteLine("{0} = {1}", x, Test(x));
		}
	}
}
