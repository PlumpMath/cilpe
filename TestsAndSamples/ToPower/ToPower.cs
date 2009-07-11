using System;
using CILPE.Config;

class Power
{
    [Inline]
    public static double ToPower (double x, int n)
    {
        double result = 1.0;
        while (n != 0)
            if (n % 2 == 1)
            {
                n--;
                result *= x;
            }
            else
            {
                n /= 2;
                x *= x;
            }
        return result;
    }

    [Specialize]
    public static double ToPower20 (double x)
    {
        return ToPower(x, 20);
    }

    [Specialize]
    public static double ToPower4 (double x)
    {
        return ToPower(x, 4);
    }

    public static double ToPower20a (double x) 
    {
        x = x*x;
        x = x*x;
        double r = 1.0;
        r = r*x;
        x = x*x;
        x = x*x;
        r = r*x;
        return r;
    }

    static void Main(string[] args)
    {
        double x = 0;

        DateTime markedTime = DateTime.Now;
        for (int i = 0; i < 1; i++)
            x = ToPower20a(2);
        Console.WriteLine(DateTime.Now - markedTime);

        Console.WriteLine("2^38 = {0}", x);
    }
}
