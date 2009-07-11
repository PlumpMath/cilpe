using System;
using System.IO;
using CILPE.Config;

public class ray {

    class Vec {
        public double x, y, z;
        [Inline]
        public Vec (double x2, double y2, double z2) { x=x2; y=y2; z=z2; }
    }

    [Inline]
    static Vec add (Vec a, Vec b) { return new Vec(a.x+b.x, a.y+b.y, a.z+b.z); }
    [Inline]
    static Vec sub (Vec a, Vec b) { return new Vec(a.x-b.x, a.y-b.y, a.z-b.z); }
    [Inline]
    static Vec scale (double s, Vec a) { return new Vec(s*a.x, s*a.y, s*a.z); }

    [Inline]
    static double dot (Vec a, Vec b) { return a.x*b.x + a.y*b.y + a.z*b.z; }

    [Inline]
    static Vec unitise (Vec a) { return scale(1 / Math.Sqrt(dot(a, a)), a); }

    class Ray {
        public Vec orig, dir;
        [Inline]
        public Ray (Vec o, Vec d) { orig=o; dir=d; }
    }

    class Hit {
        public double lambda;
        public Vec normal;
        [Inline]
        public Hit(double l, Vec n) { lambda=l; normal=n; }
    }

    abstract class Scene {
        [Inline]
        public abstract Hit intersect (Hit i, Ray ray);
    }

    class Sphere : Scene {
        public Vec center;
        public double radius;

        [Inline]
        public Sphere (Vec c, double r) { center=c; radius=r; }

        [Inline]
        public double ray_sphere (Ray ray) 
        {
            double infinity=Double.PositiveInfinity;
            Vec v = sub(center, ray.orig);
            double b = dot(v, ray.dir),
                disc = b*b - dot(v, v) + radius*radius;
            if (disc < 0) return infinity;
            double d = Math.Sqrt(disc), t2 = b+d;
            if (t2 < 0) return infinity;
            double t1 = b-d;
            return (t1 > 0 ? t1 : t2);
        }

        [Inline]
        public override Hit intersect (Hit i, Ray ray) 
        {
            double l = ray_sphere(ray);
            if (l >= i.lambda) return i;
            Vec n = add(ray.orig, sub(scale(l, ray.dir), center));
            return new Hit(l, unitise(n));
        }
    }

    class Group : Scene {
        public Sphere bound;
        public Scene obj0;
        public Scene obj1;
        public Scene obj2;
        public Scene obj3;
        public Scene obj4;

        [Inline]
        public Group(Sphere b) 
        {
            bound = b;
        }

        [Inline]
        public override Hit intersect(Hit i, Ray ray) 
        {
            double l = bound.ray_sphere(ray);
            if (l >= i.lambda) return i;
            i = obj0.intersect(i, ray);
            i = obj1.intersect(i, ray);
            i = obj2.intersect(i, ray);
            i = obj3.intersect(i, ray);
            i = obj4.intersect(i, ray);
            return i;
        }
    }

    [Specialize]
    static double ray_trace(double x, double y, double z,
        double ox, double oy, double oz,
        double dx, double dy, double dz) 
    //double ray_trace(Vec light, Ray ray, Scene scene) 
    {
        Vec light = new Vec(x,y,z);
        Ray ray = new Ray(new Vec(ox,oy,oz), new Vec(dx,dy,dz));
        Scene scene = create(3, new Vec(0, -1, 0), 1);
        double delta=1.4901161193847656E-8;
        double infinity=Double.PositiveInfinity;
        Hit i = scene.intersect(new Hit(infinity, new Vec(0, 0, 0)), ray);
        if (i.lambda == infinity) return 0;
        Vec o = add(ray.orig, add(scale(i.lambda, ray.dir),
            scale(delta, i.normal)));
        double g = dot(i.normal, light);
        if (g >= 0) return 0.0;
        Ray sray = new Ray(o, scale(-1, light));
        Hit si = scene.intersect(new Hit(infinity, new Vec(0, 0, 0)), sray);
        return (si.lambda == infinity ? -g : 0);
    }

    [Inline]
    static Scene create(int level, Vec c, double r) 
    {
        Sphere sphere = new Sphere(c, r);
        if (level == 1) return sphere;
        Group group = new Group(new Sphere(c, 3*r));
        group.obj0 = sphere;
        double rn = 3*r/Math.Sqrt(12);
        for (int dz=-1; dz<=1; dz+=2)
            for (int dx=-1; dx<=1; dx+=2) 
            {
                Vec c2 = new Vec(c.x+dx*rn, c.y+rn, c.z+dz*rn);
                switch(1+(dz+1)+(dx+1)/2) 
                {
                    case 1: group.obj1 = create(level-1, c2, r/2); break;
                    case 2: group.obj2 = create(level-1, c2, r/2); break;
                    case 3: group.obj3 = create(level-1, c2, r/2); break;
                    case 4: group.obj4 = create(level-1, c2, r/2); break;
                }
                //group.objs[1+(dz+1)+(dx+1)/2] = create(level-1, c2, r/2);
            }
        return group;
    }

    static int D (int x) 
    {
        return x;
    }

    void run(int n, int level, int ss) /*throws java.io.IOException*/ 
    {
        //Scene scene = create(level, new Vec(0, -1, 0), 1);
        FileStream fos = new FileStream("image.pgm", FileMode.Create);
        StreamWriter sw = new StreamWriter(fos);
        sw.Write("P5\n"+n+" "+n+"\n255\n");
        sw.Flush();
        for (int y=n-1; y>=D(0); --y)
            for (int x=D(0); x<n; ++x) 
            {
                double g=0;
                for (int dx=0; dx<ss; ++dx)
                    for (int dy=0; dy<ss; ++dy) 
                    {
                        Vec d = new Vec(x+dx*1.0/ss-n/2.0, y+dy*1.0/ss-n/2.0, n);
                        Ray ray = new Ray(new Vec(0, 0, -4), unitise(d));
                        Vec vx = unitise(new Vec(-1, -3, 2));
                        g += ray_trace(vx.x, vx.y, vx.z, ray.orig.x, ray.orig.y, ray.orig.z, ray.dir.x, ray.dir.y, ray.dir.z);
                            //ray, null/*scene*/);
                    }
                fos.WriteByte((byte)(0.5+255.0*g/(ss*ss)));
            }
        fos.Close();
    }

    public static void Main(String[] args) /*throws java.io.IOException*/ 
    {
        (new ray()).run(Int32.Parse(args[1]),
            Int32.Parse(args[0]), 4);
    }
}
