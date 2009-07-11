using System;
using CILPE.Config;

namespace ConcatExprTest
{
    class Expr 
    {
        public static int D (int x) 
        {
            return x;
        }

        private char[] arr;

        [Inline]
        private Expr (char[] a) 
        {
            arr = a;
        }

        [Inline]
        public Expr (String str)
        {
            arr = new char[str.Length];
            for (int i = D(0); i < str.Length; i++)
                arr[i] = str[i];
        }

        public Expr (Expr e) 
        {
            arr = e.arr;
        }

        [Inline]
        public Expr addRight (Expr e)
        {
            char[] a = new char[arr.Length+e.arr.Length];
            int i;
            for (i = D(0); i < arr.Length; i++)
                a[i] = arr[i];
            for (int j = D(0); j < e.arr.Length; j++)
                a[i+j] = e.arr[j];
            return new Expr(a);
        }

        public void print () 
        {
            for (int i = 0; i < arr.Length; i++)
                Console.Write("{0} ", arr[i]);
        }
    }

    class ConcatExpr
    {
        [Specialize]
        public static Expr Test (String[] strs)
        {
            Expr e = new Expr("");
            for (int i = Expr.D(0); i < strs.Length; i++)
                e = e.addRight(new Expr(strs[i]));
            return new Expr(e);
        }

        static void Main(string[] args)
        {
            String[] strs = { "abc", "123", "qwe" };
            Expr e = Test(strs);
            e.print();
        }
    }
}
