## What is CILPE? ##

CILPE is a _program specializer_ (based on _partial evaluation_) for a subset of CIL (Common Intermediate Language).

It includes a _binding time analyzer_, and a _residual program generator_.

## How to build CILPE ##

  * Open the solution `CILPE.sln` in Visual Studio 2003, and build.

  * Find the file `PE.exe` in the folder `PE/bin/Debug/` .

  * Run `PE.exe` to see a short manual on how to use it.

## How to prepare an example ##

An example project should contain one or more method definitions with the attribute `[Specialize]`. When CILPE sees such a method, it generates a specialized version of the methods, with all its parameters being classified as `D` (dynamic).

The original version of the method is removed from the program.

If a method in the source program is annotated with the attribute `[Inline]`, CILPE unfolds all calls to the method in the residual program.

In order for the attributes `[Specialize]` and `[Inline]` to be used in the source program, the example project has to include the assembly `Config.dll`, which is produced by building the project `Config`.

## How to submit an example to CIPE ##

  * Build a sample project, to produce an assembly (for example, `sample.dll` or `sample.exe`).

  * Run `PE.exe sample.dll` or `PE.exe sample.exe` from the command line.

  * Find the output assembly (`sample.dll` or `sample.exe`) in the subfolder `CILPEOutPut`.

  * Inspect the output assembly with your favorite disassembly tool.