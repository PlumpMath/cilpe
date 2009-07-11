using System;
using CILPE.Config;

namespace Fib
{
    public class Container
    {
        public Container prev;
        public Container next;
        public int val;
        [Inline]
        public Container () {}
    }

    public class FibTest
    {
        [Inline]
        static public Container Create (int n) 
        {
            Container first = new Container();
            Container last = first;
            for (int i = 1; i < n; i++) 
            {
                last.next = new Container();
                last.next.prev = last;
                last = last.next;
            }
            return first;
        }

        [Inline]
        static public void Calucale (Container con) 
        {
            if (con == null)
                return;
            con.val = 0;
            con = con.next;
            if (con == null)
                return;
            con.val = 1;
            con = con.next;
            for (;con != null; con = con.next)
                con.val =  con.prev.val + con.prev.prev.val;
        }

        [Specialize]
        static public string Test ()
        {
            Container con = Create(11);
            Calucale(con);
            string res = null;
            for (;con != null; con = con.next) 
                res = (res == null ? "" : res + " ") + con.val;
            return res;
        }

        static public void Main (string[] args)
        {
            Console.WriteLine("{0}", Test());
        }
    }
}
