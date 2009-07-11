using System;
using CILPE.Config;

namespace VisitorPatternTest
{
	abstract class Visitor {
		[Inline] public Visitor () {}
        [Inline] public abstract void visit (Car car);
        [Inline] public abstract void visit (Wheel wheel);
	}

	abstract class Visitable {
		[Inline] public Visitable () {}
		[Inline] public abstract void accept (Visitor visitor);
	}

	class Car : Visitable 
	{
		Visitable[] parts;
		[Inline] public Car () {
            this.parts = new Visitable[] {new Wheel("front left"),
                                             new Wheel("front right"),
                                             new Wheel("back left"),
                                             new Wheel("back right")};
		}
		[Inline] public override void accept (Visitor visitor) {
			visitor.visit(this);
			for(int i=0; i < this.parts.Length; i++) this.parts[i].accept(visitor);
		}
	}

    class Wheel : Visitable 
    {
        string name;
        [Inline] public Wheel (string name) { this.name = name; }
        [Inline] public string getName () { return this.name; }
        [Inline] public override void accept (Visitor visitor) { visitor.visit(this); }
    }

    class PrintVisitor : Visitor 
	{
		[Inline] public PrintVisitor () {}
        [Inline] public override void visit (Car car) {
            Console.WriteLine("Visiting car"); }
        [Inline] public override void visit (Wheel wheel) {
			Console.WriteLine("Visiting {0} wheel", wheel.getName()); }
	}

	class CountVisitor : Visitor 
	{
		int WheelCount, CarCount;

		[Inline]
		public CountVisitor () {
			this.WheelCount = 0; this.CarCount = 0; }
		[Inline] public override void visit(Wheel wheel) {
			Console.WriteLine("Visiting {0} wheel", this.WheelCount++); }
		[Inline] public override void visit(Car car) {
			Console.WriteLine("Visiting {0} car", this.CarCount++); }
	}

	public class VisitorPatternTest 
	{
		[Inline] static Visitor CreateVisitor (bool p) {
            return p ? (Visitor) new CountVisitor() : new PrintVisitor();
		}
		[Inline] static Visitable CreateObject (bool q) {
			return q ? (Visitable) new Car() : new Wheel("one wheel");
		}

		[Specialize]
		static public void Test (bool p, bool q) {
			Visitor vis = CreateVisitor(p);
			Visitable obj = CreateObject(q);
			obj.accept(vis);
		}

		static public void Main (string[] args) {
			Test(true, true);
		}
	}
}
