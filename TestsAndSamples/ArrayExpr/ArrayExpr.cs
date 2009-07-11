using System;
using CILPE.Config;

namespace ArrayExprTest
{
    abstract class Expr {
        [Inline]
        public Expr () {}

        //public abstract double this [int i] {
        //    [Inline]
        //    get;
        //}

        [Inline]
        public abstract double Get (int i);

        [Inline]
        public Expr Plus (Expr e) {
            return new BinaryOpExpr(this, e, new Plus());
        }

        [Inline]
        public Expr Times (Expr e) {
            return new BinaryOpExpr(this, e, new Times());
        }
    }

    class Array : Expr {
        double[] data;

        [Inline]
        public Array (int n) 
        {
            this.data = new double[n];
        }

        [Inline]
        public void Set (int i, double x) 
        {
            data[i] = x;
        }

        //public override double this [int i] {
        //    [Inline]
        //    get { return data[i]; }
        //}

        [Inline]
        public override double Get (int i) {
            return data[i];
        }

        private static int D (int n) 
        {
            return n;
        }

        [Inline]
        public void Assign (Expr e) {
            for (int i = D(0); i < data.Length; i++)
                data[i] = e.Get(i);
        }
    }

    class BinaryOpExpr : Expr {
        Expr a, b;
        BinaryOp op;

        [Inline]
        public BinaryOpExpr (Expr a, Expr b, BinaryOp op) {
            this.a = a;
            this.b = b;
            this.op = op;
        }

        //public override double this [int i] {
        //    [Inline]
        //    get { return op.Apply(a[i], b[i]); }
        //}

        [Inline]
        public override double Get (int i) {
            return op.Apply(a.Get(i), b.Get(i));
        }
    }

    abstract class BinaryOp {
        [Inline]
        public abstract double Apply (double x, double y);
    }

    class Plus : BinaryOp {
        [Inline]
        public override double Apply (double x, double y) {
            return x + y;
        }
    }

    class Times : BinaryOp {
        [Inline]
        public override double Apply (double x, double y) {
            return x * y;
        }
    }

    class ArrayExprTest {
        private static int D (int n) {
            return n;
        }

        [Specialize]
        public static void Calcucates (Array w, Array x, Array y, Array z) {
            w.Assign(x.Plus(y.Times(z)));
        }

        [Specialize]
        public static double Test (int n) 
        {
            Array w = new Array(n);
            Array x = new Array(n);
            Array y = new Array(n);
            Array z = new Array(n);

            for (int i = D(0); i < n; i++) {
                x.Set(i, 0.33*i);
                y.Set(i, 10+i);
                z.Set(i, 100*i);
            }

            w.Assign(x.Plus(y.Times(z)));

            return w.Get(1);
        }

        static void Main(string[] args) {
            double x = 3;
            Console.WriteLine("({0}+5)+5 = {1}", x, Test(100));
        }
    }
}
