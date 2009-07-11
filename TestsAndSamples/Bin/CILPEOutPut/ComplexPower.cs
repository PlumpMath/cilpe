public static Complex ToPower3(Complex x)
{
    Complex complex = x;
    double d9 = (x.re * x.re) - (complex.im * x.im);
    double d10 = (complex.re * x.im) + (complex.im * x.re);
    double d6 = d10;
    double d8 = d9;
    double d7 = d6;
    double d4 = (1.0 * d8) - (0.0 * d7);
    double d5 = (1.0 * d7) + (0.0 * d8);
    double d1 = d5;
    double d3 = d4;
    double d2 = d1;
    return new Complex(d3, d2, null);
}
