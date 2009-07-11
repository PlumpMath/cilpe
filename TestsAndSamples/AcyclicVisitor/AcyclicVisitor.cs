using System;
using CILPE.Config;

namespace AcyclicVisitorTest
{
	public abstract class Modem 
	{
		[Inline]
		public Modem () {}
		[Inline]
		public abstract void dial (String pro);
		[Inline]
		public abstract void hangup ();
		[Inline]
		public abstract void send (char c);
		[Inline]
		public abstract char recv ();
		[Inline]
		public abstract void accept (ModemVisitor v);
	}

	public interface ModemVisitor 
	{
	}

	public interface ErnieModemVisitor  : ModemVisitor
	{
		[Inline]
		void visit (ErnieModem m);
	}

	public interface HayesModemVisitor  : ModemVisitor
	{
		[Inline]
		void visit (HayesModem m);
	}

	public interface ZoomModemVisitor  : ModemVisitor
	{
		[Inline]
		void visit (ZoomModem m);
	}

	public class ErnieModem : Modem 
	{
		internal String internalPattern;
		[Inline]
		public ErnieModem ()
		{
			this.internalPattern = null;
		}
		[Inline]
		public override void dial (String pro) {}
		[Inline]
		public override void hangup () {}
		[Inline]
		public override void send (char c) {}
		[Inline]
		public override char recv () {return (char)0;}
		[Inline]
		public override void accept (ModemVisitor v) 
		{
			((ErnieModemVisitor)v).visit(this);
		}
	}

	public class HayesModem
	{
		internal String configurationString;
		[Inline]
		public HayesModem ()
		{
			this.configurationString = null;
		}
		[Inline]
		public void dial (String pro) {}
		[Inline]
		public void hangup () {}
		[Inline]
		public void send (char c) {}
		[Inline]
		public char recv () {return (char)1;}
		[Inline]
		public void accept (ModemVisitor v) 
		{
			((HayesModemVisitor)v).visit(this);
		}
	}

	public class ZoomModem : Modem 
	{
		internal int configurationValue;
		[Inline]
		public ZoomModem ()
		{
			this.configurationValue = 0;
		}
		[Inline]
		public override void dial (String pro) {}
		[Inline]
		public override void hangup () {}
		[Inline]
		public override void send (char c) {}
		[Inline]
		public override char recv () {return (char)2;}
		[Inline]
		public override void accept (ModemVisitor v) 
		{
			((ZoomModemVisitor)v).visit(this);
		}
	}

	public class UnixModemConfigurator : ErnieModemVisitor,HayesModemVisitor,ZoomModemVisitor
	{
		[Inline]
		public UnixModemConfigurator () {}

		[Inline]
		public void visit (ErnieModem m)
		{
			m.internalPattern = "C is too slow";
		}
		[Inline]
		public void visit (HayesModem m)
		{
			m.configurationString = "&s1=4&D=3";
		}
		[Inline]
		public void visit (ZoomModem m)
		{
			m.configurationValue = 42;
		}
	}

	public class AcyclicVisitorTest
	{
		[Specialize]
		static public String Test ()
		{
			ModemVisitor u = new UnixModemConfigurator();
			HayesModem h = new HayesModem();
			ErnieModem e = new ErnieModem();
			h.accept(u);
			e.accept(u);
			return h.configurationString+" "+e.internalPattern;
		}

		[Specialize]
		static public HayesModem Test2 ()
		{
			ModemVisitor u = new UnixModemConfigurator();
			HayesModem h = new HayesModem();
			h.accept(u);
			return h;
		}

		static public void Main (string[] args)
		{
			Console.WriteLine("{0}", Test());
		}
	}
}
