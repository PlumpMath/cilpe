using System;
using CILPE.Config;

namespace DynamicTest
{
    class ListElement 
    {
        public int val;
        public ListElement next;

        [Inline]
        protected ListElement ()
        {
            this.val = 0;
            this.next = this;
        }

        [Inline]
        public ListElement (int val, ListElement next)
        {
            this.val = val;
            this.next = next;
        }
    }

    class ListEnd : ListElement
    {
        [Inline]
        public ListEnd ()
        {}
    }

	class List
	{
        ListElement list;

		[Inline]
		public List ()
		{
            this.list = new ListEnd();
		}

		[Inline]
		public void InsertToBegin (int val)
		{
			this.list = new ListElement(val, this.list);
		}

        [Inline]
        public void InsertToEnd (int val)
        {
            if (this.list is ListEnd)
                this.list = new ListElement(val, this.list);
            else
            {
                ListElement elem = list;
                while (! (elem.next is ListEnd))
                    elem = elem.next;
                elem.next = new ListElement(val, elem.next);
            }
        }

        [Inline]
        public void Insert (int n, int val)
        {
            if (n == 0)
                this.list = new ListElement(val, this.list);
            else 
            {
                ListElement elem = list;
                for ( ; n > 1; n--)
                    elem = elem.next;

                elem.next = new ListElement(val, elem.next);
            }
        }
        [Inline]
        public int Get (int n)
        {
            ListElement elem = list;
            for ( ; n > 0; n--)
                elem = elem.next;
            return elem.val;
        }
    }

	class TestClass
	{
        [Specialize]
        public static int G (int[] values) 
        {
            int n = 10;
            List list = new List();
            for (int i = 0; i < n; i++)
                list.InsertToEnd(values[i]);
            return list.Get(3);
        }

		public static int Test ()
		{
            int[] values = new int[20];
            for (int i = 0; i < values.Length; i++)
                values[i] = i*i;
			return G(values);
		}

		static void Main(string[] args)
		{
			Console.WriteLine("3^2 = {0}", Test());
		}
	}
}
