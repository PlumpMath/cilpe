
using System;
using System.IO;
using System.Security;
using System.Reflection;
using CILPE.ReflectionEx;
using CILPE.CFG;
using CILPE.DataModel;
using CILPE.Interpreter;
using CILPE.Exceptions;

class Test
{
    static void Main(string[] args)
    {
        Console.WriteLine("Prototype CIL interpreter\n-------------------------");
        if (args.Length == 0)
        {
            Console.WriteLine("usage: TestInt <assembly name> <command line>");
        }
        else
        {
            try
            {
                Assembly assembly = Assembly.LoadFrom(args[0]);
                AssemblyHolder holder = new AssemblyHolder(assembly);

                if (holder.EntryPoint == null)
                    Console.WriteLine("Assembly \""+args[0]+"\" has no entry point !");
                else
                {
                    try
                    {
                        string[] cmdLine = new string[args.Length-1];
                        for (int i = 1; i < args.Length; i++)
                            cmdLine[i-1] = args[i];

                        Run(holder,cmdLine);
                    }
                    catch (FileNotFoundException e)
                    {
                        Console.WriteLine("Assembly module "+e.FileName+"was not found !");
                    }
                }
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("File \""+args[0]+"\" was not found !");
            }
            catch (BadImageFormatException)
            {
                Console.WriteLine("File \""+args[0]+"\" has invalid format !");
            }
            catch (SecurityException)
            {
                Console.WriteLine("Security error while opening file \""+args[0]+"\" !");
            }
            catch (FileLoadException)
            {
                Console.WriteLine("File \""+args[0]+"\" was found but cannot be loaded !");
            }
        }
    }

    static void Run(AssemblyHolder holder, string[] cmdLine)
    {
        try
        {
            EvaluationStack stack = new EvaluationStack();
            stack.Push(new ObjectReferenceValue(cmdLine));

            ParameterValues paramVals = stack.Perform_CallMethod(holder.Assembly.EntryPoint,false);

            Exception exc;
            IntVisitor.InterpretMethod(holder,holder.EntryPoint,paramVals,out exc,"");

            if (exc != null)
            {
                Console.WriteLine(
                    "-------------------------\n"+
                    "CIL interpreter catched unhandled exception:\n"+
                    exc
                    );
            }

            Console.WriteLine("-------------------------\nPress Enter...");
            Console.ReadLine();
        }
        catch (ConvertionException)
        {
            Console.WriteLine("Conversion to CFG failed !");
        }
    }
}
