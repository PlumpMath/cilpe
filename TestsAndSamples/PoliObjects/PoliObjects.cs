using System;
using CILPE.Config;

namespace NewObjectTest
{
	class Container
	{
		int x;
		public Container (int x) {
			this.x = x;
        }
        public static void Container_1 (ref int this_x, int x) 
        {
            this_x = x;
        }
        public int X (int i) 
        {
            this.x += i;
            return this.x;
        }
        public int X_2 () 
        {
            this.x += 5;
            return this.x;
        }
        public static int X_1 (ref int this_x) 
        {
            this_x += 5;
            return this_x;
        }
    }

	class NewObjectTest
	{

        [Specialize]
		public static Container Test (bool p, int x, int y)
		{
            Container c1 = new Container(x);
            Container c2 = new Container(y);
            Container c = p ? c1 : c2;
            Console.WriteLine(c.X(5));
            return c1;
		}

        public static Container TestResult (bool p, int x, int y)
        {
            Container c1 = new Container(x);
            int container1_x = 0;
            Container.Container_1(ref container1_x, x);
            if (p) {
                Console.WriteLine(c1.X_2());
                return c1;
            } else {
                Console.WriteLine(Container.X_1(ref container1_x));
                return c1;
            }
        }

        static void Main(string[] args)
		{
			int x = 5;
            int y = 10;
			Console.WriteLine("{0} = {1}", x, Test(true, x, y).X(0));
            Console.WriteLine("{0} = {1}", x, TestResult(true, x, y).X(0));
        }
	}
}
