
// ===========================================================================
// CILPE - Partial Evaluator for Common Intermediate Language
// ===========================================================================
// File: 
//     PE.cs
//
// Description:
//     Launcher for partial evaluator
//
// Author: 
//     Sergei Skorobogatov (Sergei.Skorobogatov@supercompilers.com)
// ===========================================================================


using System;

namespace CILPE.Exceptions
{
    public abstract class ArgParserException: ApplicationException
    {
        public ArgParserException(string msg): base("Argument parser: " + msg)
        {  }
    }

    public class SourceNotFoundException: ArgParserException
    {
        public SourceNotFoundException(string fileName):
            base("file \""+fileName+"\" not found")
        { }
    }

    public class InvalidSourceExtException: ArgParserException
    {
        public InvalidSourceExtException(string fileName):
            base("file \""+fileName+"\" is not EXE or DLL")
        { }
    }

    public class InvalidTargetNameException: ArgParserException
    {
        public InvalidTargetNameException(string fileName):
            base("\""+fileName+"\" is invalid target name")
        { }
    }

    public class ArgSyntaxErrorException: ArgParserException
    {
        public ArgSyntaxErrorException(string arg):
            base("syntax error in \""+arg+"\"")
        { }
    }

    public class UnknownArgOptionException: ArgParserException
    {
        public UnknownArgOptionException(string opt):
            base("unknown option "+opt)
        { }
    }

    public class OptionsConflictException: ArgParserException
    {
        public OptionsConflictException(string opt1, string opt2):
            base("conflict between \""+opt1+"\" and \""+opt2+"\" options")
        { }
    }
}

namespace CILPE
{
    using System.IO;
    using System.Reflection;
    using CILPE.Exceptions;
	using CILPE.ReflectionEx;
    using CILPE.Config;
    using CILPE.CFG;
    using CILPE.BTA;
    using CILPE.Spec;

    class PELauncher
    {
        static string logo = 
            "\nPartial Evaluator for CIL.\n"+
            "Copyright (C) CILPE Development Team 2002-2003. All rights reserved.\n\n";

        static string usage =
            "Usage: PE <source file> [Options]\n\n"+
            "Options:\n"+
            "    /TARGET=<target file>      Put residual assembly to specified file\n"+
            "    /NOPOSTPROC                Disable postprocessing\n"+
            "    /CLOCK                     Measure and report partial evaluation times\n"+
            "    /SRCCFG                    Show source CFG\n"+
            "    /BTACFG                    Show annotated CFG\n"+
            "    /RESCFG                    Show residual CFG\n"+
            "    /POSTCFG                   Show postprocessed CFG\n"+
            "    /LOGO                      Don't type the logo\n"+
            "    /QUIET                     Don't report partial evaluation progress\n\n"+
            "Key may be \'/\' or \'-\'\n"+
            "Options are recognized by first character\n"+
            "Extensions for source and target assemblies are required\n";

        static string targetOptionStr = "";

        static string sourceAssemblyName = "";
        static string targetAssemblyName = "";
        static bool enablePostprocessing = true;
        static bool enableClock = false;
        static bool showSourceCFG = false;
        static bool showAnnotatedCFG = false;
        static bool showResidualCFG = false;
        static bool showPostprocessedCFG = false;
        static bool showLogo = true;
        static bool showProgress = true;
        static bool showUsage = false;

		static TimeSpan btaTime, specTime, pprocTime;
		static DateTime markedTime;

		static void markTime() { markedTime = DateTime.Now; }
		static TimeSpan getSpan() { return DateTime.Now - markedTime; }

        static void parseArgs(string[] args)
        {
            if (args.Length == 0)
                showUsage = true;
            else
            {
                sourceAssemblyName = targetAssemblyName = args[0];
                if (! File.Exists(sourceAssemblyName))
                    throw new SourceNotFoundException(sourceAssemblyName);

                string ext = Path.GetExtension(sourceAssemblyName);
                if (ext != ".exe" && ext != ".dll")
                    throw new InvalidSourceExtException(sourceAssemblyName);

                for (int i = 1; i < args.Length; i++)
                {
                    string opt = args[i].ToUpper();

                    if (opt[0] != '/' && opt[0] != '-')
                        throw new ArgSyntaxErrorException(args[i]);

                    switch (opt[1])
                    {
                        case 'T':
                            if (targetOptionStr != "")
                                throw new OptionsConflictException(targetOptionStr,args[i]);

                            targetOptionStr = args[i];

                            string[] s = args[i].Split('=');
                            if (s.Length != 2)
                                throw new ArgSyntaxErrorException(args[i]);

                            targetAssemblyName = s[1];
                            string targetExt;

                            try
                            {
                                targetExt = Path.GetExtension(targetAssemblyName);
                            }
                            catch (ArgumentException)
                            {
                                throw new InvalidTargetNameException(targetAssemblyName);
                            }

                            if (! ext.Equals(targetExt) ||
                                targetAssemblyName.IndexOfAny(new char[] { '*', '?' }) != -1)
                                throw new InvalidTargetNameException(targetAssemblyName);

                            break;

                        case 'N':
                            enablePostprocessing = false;
                            break;

                        case 'C':
                            enableClock = true;
                            break;

                        case 'S':
                            showSourceCFG = true;
                            break;

                        case 'B':
                            showAnnotatedCFG = true;
                            break;

                        case 'R':
                            showResidualCFG = true;
                            break;

                        case 'P':
                            showPostprocessedCFG = true;
                            break;

                        case 'L':
                            showLogo = false;
                            break;

                        case 'Q':
                            showProgress = false;
                            break;

                        default:
                            throw new UnknownArgOptionException(args[i]);
                    }
                }

                if (showPostprocessedCFG && ! enablePostprocessing)
                    throw new OptionsConflictException("/NOPOSTPROC","/POSTCFG");
            }
        }

        static void Evaluate()
        {
            WhiteList whiteList = new WhiteList();
            whiteList.AddFromXml("wlist.xml");

            if (showProgress)
                Console.WriteLine("White list reading - OK");

            Assembly assembly = Assembly.LoadFrom(sourceAssemblyName);
			AssemblyHolder srcHolder = new AssemblyHolder(assembly);

			if (showProgress)
				Console.WriteLine("Source assembly reading - OK");
            
            if (showSourceCFG)
            {
                Console.WriteLine("\nSource CFG:\n----------\n");
                Console.Write(srcHolder);
            }

			markTime();
            AnnotatedAssemblyHolder btaHolder = new AnnotatedAssemblyHolder(srcHolder, whiteList);
			btaTime = getSpan();

			if (showProgress)
				Console.WriteLine("Assembly annotation - OK");

            if (showAnnotatedCFG)
            {
                Console.WriteLine("\nAnnotated CFG:\n-------------\n");
				Console.Write(btaHolder.ToString("CSharp",ReflectionFormatter.formatter,new string[] { Annotation.BTTypeOption, Annotation.MethodBTTypeOption }));
            }

			markTime();
			ResidualAssemblyHolder resHolder = new ResidualAssemblyHolder(btaHolder);
			specTime = getSpan();

			if (showProgress)
				Console.WriteLine("Assembly specialization - OK");

			if (showResidualCFG)
			{
				Console.WriteLine("\nResidual CFG:\n-------------\n");
				Console.Write(resHolder.ToString("CSharp",ReflectionFormatter.formatter));
			}

			if (enablePostprocessing)
			{
				markTime();
				resHolder.Optimize();
				pprocTime = getSpan();

				if (showProgress)
					Console.WriteLine("Assembly postprocessing - OK");

				if (showPostprocessedCFG)
				{
					Console.WriteLine("\nPostprocessed CFG:\n-----------------\n");
					Console.Write(resHolder.ToString("CSharp",ReflectionFormatter.formatter));
				}
			}

			Exporter.Export(resHolder, targetAssemblyName);

			if (showProgress)
				Console.WriteLine("Assembly export - OK");

			if (enableClock)
			{
				Console.WriteLine("Timings:");
				Console.WriteLine("    BTA             - " + btaTime);
				Console.WriteLine("    Specializer     - " + specTime);

				if (enablePostprocessing)
					Console.WriteLine("    Postprocessing  - " + pprocTime);
			}
        }

        static void Main(string[] args)
        {
            try
            {
                parseArgs(args);

//                try
//                {
                    if (showLogo)
                        Console.Write(logo);

                    if (showUsage)
                        Console.Write(usage);
                    else
                        Evaluate();
//                }
//                catch (Exception e)
//                {
//                    Console.WriteLine(e.Message);
//                }
            }
            catch (ArgParserException e)
            {
                if (showLogo)
                    Console.Write(logo);

                Console.WriteLine(e.Message);
            }

            Console.ReadLine();
        }
    }
}
