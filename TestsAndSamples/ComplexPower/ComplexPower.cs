using System;
using CILPE.Config;
using Microsoft.Win32;

class Complex 
{
    public readonly double re, im;

    [Inline]
    public Complex (double re, double im)
    {
        this.re = re;
        this.im = im;
    }

    [Inline]
    public Complex Times (Complex c) 
    {
        double re = this.re * c.re - this.im * c.im;
        double im = this.re * c.im + this.im * c.re;
        return new Complex(re, im);
    }
}

class CompexPower
{
    [Inline]
    public static Complex Copy (Complex c) 
    {
        return new Complex(c.re, c.im);
    }

    [Inline]
    public static Complex ToPower (Complex x, int n)
    {
        Complex result = new Complex(1.0, 0.0);

        while (n != 0)
            if (n % 2 == 1)
            {
                n--;
                result = result.Times(x);
            }
            else
            {
                n /= 2;
                x = x.Times(x);
            }

        return new Complex(result.re, result.im);
    }

    [Specialize]
    public static Complex ToPower2 (Complex x)
    {
        return ToPower(x, 2);
    }

    [Specialize]
    public static Complex ToPower3 (Complex x)
    {
        return ToPower(x, 2);
    }

    static void Main(string[] args)
    {
        Complex x = null;

        DateTime markedTime = DateTime.Now;
        for (int i = 0; i < 100000000; i++)
            x = ToPower2(new Complex(2, 0));
        Console.WriteLine(DateTime.Now - markedTime);

        Console.WriteLine("2^38 = {0}", x);
    }
}
