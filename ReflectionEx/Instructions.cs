
// ===========================================================================
// CILPE - Partial Evaluator for Common Intermediate Language
// ===========================================================================
// File: 
//     Instructions.cs
//
// Description:
//     Contains classes for IL instructions decoding
//
// Author: 
//     Sergei Skorobogatov (Sergei.Skorobogatov@supercompilers.com)
// ===========================================================================

using System;
using System.Collections;
using System.Reflection;
using CILPE.MdDecoder;

namespace CILPE.ReflectionEx
{
    /* Instruction codes */
    public enum InstructionCode
    {
        ADD         = 0,   /* Add two values, returning a new value */
        AND         = 1,   /* Bitwise AND of two integral values, returns an integral value */
        ARGLIST     = 2,   /* Return argument list handle for the current method */
        BEQ         = 3,   /* Branch if equal */
        BGE         = 4,   /* Branch if greater than or equal to */
        BGT         = 5,   /* Branch if greater than */
        BLE         = 6,   /* Branch if less than or equal to */
        BLT         = 7,   /* Branch if less than */
        BNE         = 8,   /* Branch if unequal or unordered */
        BOX         = 9,   /* Convert value type to a true object reference */
        BR          = 10,  /* Unconditional branch */
        BREAK       = 11,  /* Inform a debugger that a breakpoint has been reached */
        BRFALSE     = 12,  /* Branch if value is zero (false) */
        BRTRUE      = 13,  /* Branch if value is non-zero (true) */
        CALL        = 14,  /* Call method */
        CALLVIRT    = 15,  /* Call a method associated with specified object */
        CASTCLASS   = 16,  /* Cast object to specified class */
        CEQ         = 17,  /* Push 1 (int32) if equal, else 0 */
        CGT         = 18,  /* Push 1 (int32) if greater, else 0 */
        CKFINITE    = 19,  /* Throw ArithmeticException if value is not a finite number */
        CLT         = 20,  /* Push 1 (int32) if less, else 0 */
        CONV        = 21,  /* Convert value, pushing the result on stack */
        CPOBJ       = 22,  /* Copy a value type */
        DIV         = 23,  /* Divide two values to return a quatient or floating-point result */
        DUP         = 24,  /* Duplicate value on the top of the stack */
        ENDFILTER   = 25,  /* End filter clause of SHE exception handling */
        ENDFINALLY  = 26,  /* End finally (or fault) clause of an exception block */
        INITOBJ     = 27,  /* Initialize a value type */
        ISINST      = 28,  /* Test if object is an instance of class */
        LDARG       = 29,  /* Load argument onto stack */
        LDARGA      = 30,  /* Load the address of argument */
        LDC         = 31,  /* Push constant value onto the stack */
        LDELEM      = 32,  /* Load the element of array onto the top of the stack */
        LDELEMA     = 33,  /* Load the address of element of array onto the top of the stack */
        LDFLD       = 34,  /* Push the value of field of object or value type onto the stack */
        LDFLDA      = 35,  /* Push the address of field of object on the stack */
        LDFTN       = 36,  /* Push a pointer to a method on the stack */
        LDIND       = 37,  /* Indirect load */
        LDLEN       = 38,  /* Push the length (of type native unsigned int) of array on the stack */
        LDLOC       = 39,  /* Load local variable onto stack */
        LDLOCA      = 40,  /* Load address of local variable */
        LDNULL      = 41,  /* Push null reference on the stack */
        LDOBJ       = 42,  /* Copy instance of value type to the stack */
        LDSFLD      = 43,  /* Push the value of static field on the stack */
        LDSFLDA     = 44,  /* Push the address of the static field on the stack */
        LDSTR       = 45,  /* Push a string object on the stack */
        LDTOKEN     = 46,  /* Convert metadata token to its runtime representation */
        LDVIRTFTN   = 47,  /* Push address of virtual method on the stack */
        LEAVE       = 48,  /* Exit a protected region of code */
        MKREFANY    = 49,  /* Push a typed reference onto the stack */
        MUL         = 50,  /* Multiply values */
        NEG         = 51,  /* Negate value */  
        NEWARR      = 52,  /* Create a new array with elements of specified type */
        NEWOBJ      = 53,  /* Allocate an uninitialized object or value type and call its constructor */
        NOP         = 54,  /* Do nothing */
        NOT         = 55,  /* Bitwise complement */
        OR          = 56,  /* Bitwise OR of two integer values, returns as integer */
        POP         = 57,  /* Pop a value from the stack */
        REFANYTYPE  = 58,  /* Push the type token stored in a typed reference */
        REFANYVAL   = 59,  /* Push the address stored in a typed reference */
        REM         = 60,  /* Remainder of dividing */
        RET         = 61,  /* Return from method, possibly returnig a value */
        RETHROW     = 62,  /* Rethrow the current exception */
        SHL         = 63,  /* Shift an integer to the left */
        SHR         = 64,  /* Shift an integer to the right */
        SIZEOF      = 65,  /* Load the size in bytes of a value type */
        STARG       = 66,  /* Store a value in an argument */
        STELEM      = 67,  /* Replace array element with the value on the stack */
        STFLD       = 68,  /* Replace the value of field of the object with value */
        STIND       = 69,  /* Store value into memory at specified address */
        STLOC       = 70,  /* Pop value from stack into local variable */
        STOBJ       = 71,  /* Store a value of specified type from the stack into memory */
        STSFLD      = 72,  /* Replace the value of static field of the object with value */
        SUB         = 73,  /* Substract values */
        SWITCH      = 74,  /* Jump to one of n labels depending on the value on stack */
        THROW       = 75,  /* Throw an exception */
        UNBOX       = 76,  /* Extract the value type data from its boxed representation */
        XOR         = 77,  /* Bitwise XOR of integer values, returns an integer */

        /* Never virifiable instructions
         * (for internal use only, no instruction will have such code) 
         */
        CALLI       = 78,  /* Call method indicated on the stack with arguments described by call site descriptor */
        CPBLK       = 79,  /* Copy data from memory to memory */
        INITBLK     = 80,  /* Set a block of memory to a given byte */
        JMP         = 81,  /* Exit current method and jump to specified method */
        LOCALLOC    = 82,  /* Allocate space from the local memory pool */

        /* Instruction modifiers
         * (for internal use only, no instruction will have such code) 
         */
        TAIL        = 83,  /* Subsequent call terminates current method */
        VOLATILE    = 84,  /* Subsequent pointer reference is volatile */
        UNALIGNED   = 85   /* Subsequent pointer instruction may be unaligned */
    };

    /* Type suffix for some instructions
     * (for example, conv.i1 instruction has suffix InstructionTypeSuffix.I1) 
     */
    public enum InstructionTypeSuffix
    {
        Default     = 0,   /* Instruction has no type suffix */
        I           = 1,   /* Suffix .i */
        I1          = 2,   /* Suffix .i1 */
        I2          = 3,   /* Suffix .i2 */
        I4          = 4,   /* Suffix .i4 */
        I8          = 5,   /* Suffix .i8 */
        U           = 6,   /* Suffix .u */
        U1          = 7,   /* Suffix .u1 */
        U2          = 8,   /* Suffix .u2 */
        U4          = 9,   /* Suffix .u4 */
        U8          = 10,  /* Suffix .u8 */
        R           = 11,  /* Suffix .r */
        R4          = 12,  /* Suffix .r4 */
        R8          = 13,  /* Suffix .r8 */
        REF         = 14   /* Suffix .ref */
    };

    /* Class representing types on the evaluation stack */
    public class StackTypes: IEnumerable, ICloneable 
    {
        #region Private and internal members

        /* Internally stack is represented by ArrayList object */
        private ArrayList stack;

        /* Private constructor is used for cloning */
        private StackTypes(ArrayList stack)
        {
            this.stack = stack;
        }

        #endregion

        /* Used by verifier to push type on the stack */
        public void Push(TypeEx x)
        { 
            stack.Add(x);
        }

        /* Used by verifier to push type on the stack */
        public void Push(Type x)
        { 
            Push(new TypeEx(x));
        }

        /* Used by verifier to pop type from the stack */
        public TypeEx Pop()
        { 
            if (Count == 0)
                throw new VerifierException();

            TypeEx type = (TypeEx)stack[Count-1];
            stack.RemoveAt(Count-1);

            return type; 
        }
             
        /* Returns value on top of the stack (used by verifier) */
        public TypeEx Peek()
        { 
            if (Count == 0)
                throw new VerifierException();

            return (TypeEx)stack[Count-1];
        }

        /* Creates a copy of current object */
        public object Clone()
        {
            return new StackTypes(stack.Clone() as ArrayList);
        }

        /* Initializes a new instance of the StackTypes class */
        public StackTypes() 
        { 
            stack = new ArrayList();
        }

        /* Returns stack size */
        public int Count { get { return stack.Count; } }

        /* Returns type on the stack by index */
        public TypeEx this [int index]
        {
            get { return (TypeEx)stack[index]; }
            set { stack[index] = value; }
        }

        /* Returns value on top of the stack */
        public TypeEx Top()
        { 
            return Peek();
        }

        /* Returns an enumerator that can iterate through types on stack */
        public IEnumerator GetEnumerator() { return stack.GetEnumerator(); }
    };

    /* IL instruction class */
    public class Instruction: IFormattable
    {
        #region Private and internal members

        private enum ParamType
        {
            pNone = 0,
            pInt8 = 1,
            pInt32 = 2,
            pInt64 = 3,
            pUint8 = 4,
            pUint16 = 5,
            pFloat32 = 6,
            pFloat64 = 7,
            pToken = 8,
            pSwitch = 9
        };

        private enum AdditionalParamType
        {
            apNone = 0,
            apInt32 = 1,
            apInt64 = 2,
            apUint16 = 3,
            apFloat32 = 4,
            apFloat64 = 5,
            apU0 = 6,
            apU1 = 7,
            apU2 = 8,
            apU3 = 9,
            apIM1 = 10,
            apI0 = 11,
            apI1 = 12,
            apI2 = 13,
            apI3 = 14,
            apI4 = 15,
            apI5 = 16,
            apI6 = 17,
            apI7 = 18,
            apI8 = 19,
            apOfs = 20,
            apOfsArray = 21,
            apToken = 22,
            apTypeTok = 23,
            apValTypeTok = 24,
            apClassTok = 25,
            apCtorTok = 26,
            apMethodTok = 27,
            apFieldTok = 28,
            apCallSiteDescr = 29,
            apString = 30
        };

        private struct Info
        {
            public InstructionCode code;
            public bool ovf;
            public bool un;
            public string name;
            public ParamType pType;
            public InstructionTypeSuffix typeSuffix;
            public AdditionalParamType apType;

            public Info(InstructionCode code,bool ovf,bool un,string name,
                ParamType pType,InstructionTypeSuffix typeSuffix,AdditionalParamType apType)
            {
                this.code = code;
                this.ovf = ovf;
                this.un = un;
                this.name = name;
                this.pType = pType;
                this.typeSuffix = typeSuffix;
                this.apType = apType;
            }
        };

        private static Info[] instructions =
                                                { 
                                                    /* 00 */    new Info(InstructionCode.NOP,        false,  false,  "nop",              ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* 01 */    new Info(InstructionCode.BREAK,      false,  false,  "break",            ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* 02 */    new Info(InstructionCode.LDARG,      false,  false,  "ldarg.0",          ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apU0),
                                                    /* 03 */    new Info(InstructionCode.LDARG,      false,  false,  "ldarg.1",          ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apU1),
                                                    /* 04 */    new Info(InstructionCode.LDARG,      false,  false,  "ldarg.2",          ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apU2),
                                                    /* 05 */    new Info(InstructionCode.LDARG,      false,  false,  "ldarg.3",          ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apU3),
                                                    /* 06 */    new Info(InstructionCode.LDLOC,      false,  false,  "ldloc.0",          ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apU0),
                                                    /* 07 */    new Info(InstructionCode.LDLOC,      false,  false,  "ldloc.1",          ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apU1),
                                                    /* 08 */    new Info(InstructionCode.LDLOC,      false,  false,  "ldloc.2",          ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apU2),
                                                    /* 09 */    new Info(InstructionCode.LDLOC,      false,  false,  "ldloc.3",          ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apU3),
                                                    /* 0A */    new Info(InstructionCode.STLOC,      false,  false,  "stloc.0",          ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apU0),
                                                    /* 0B */    new Info(InstructionCode.STLOC,      false,  false,  "stloc.1",          ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apU1),
                                                    /* 0C */    new Info(InstructionCode.STLOC,      false,  false,  "stloc.2",          ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apU2),
                                                    /* 0D */    new Info(InstructionCode.STLOC,      false,  false,  "stloc.3",          ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apU3),
                                                    /* 0E */    new Info(InstructionCode.LDARG,      false,  false,  "ldarg.s",          ParamType.pUint8,   InstructionTypeSuffix.Default, AdditionalParamType.apUint16),
                                                    /* 0F */    new Info(InstructionCode.LDARGA,     false,  false,  "ldarga.s",         ParamType.pUint8,   InstructionTypeSuffix.Default, AdditionalParamType.apUint16),
                                                    /* 10 */    new Info(InstructionCode.STARG,      false,  false,  "starg.s",          ParamType.pUint8,   InstructionTypeSuffix.Default, AdditionalParamType.apUint16),
                                                    /* 11 */    new Info(InstructionCode.LDLOC,      false,  false,  "ldloc.s",          ParamType.pUint8,   InstructionTypeSuffix.Default, AdditionalParamType.apUint16),
                                                    /* 12 */    new Info(InstructionCode.LDLOCA,     false,  false,  "ldloca.s",         ParamType.pUint8,   InstructionTypeSuffix.Default, AdditionalParamType.apUint16),
                                                    /* 13 */    new Info(InstructionCode.STLOC,      false,  false,  "stloc.s",          ParamType.pUint8,   InstructionTypeSuffix.Default, AdditionalParamType.apUint16),
                                                    /* 14 */    new Info(InstructionCode.LDNULL,     false,  false,  "ldnull",           ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* 15 */    new Info(InstructionCode.LDC,        false,  false,  "ldc.m1",           ParamType.pNone,    InstructionTypeSuffix.I4,      AdditionalParamType.apIM1),
                                                    /* 16 */    new Info(InstructionCode.LDC,        false,  false,  "ldc.0",            ParamType.pNone,    InstructionTypeSuffix.I4,      AdditionalParamType.apI0),
                                                    /* 17 */    new Info(InstructionCode.LDC,        false,  false,  "ldc.1",            ParamType.pNone,    InstructionTypeSuffix.I4,      AdditionalParamType.apI1),
                                                    /* 18 */    new Info(InstructionCode.LDC,        false,  false,  "ldc.2",            ParamType.pNone,    InstructionTypeSuffix.I4,      AdditionalParamType.apI2),
                                                    /* 19 */    new Info(InstructionCode.LDC,        false,  false,  "ldc.3",            ParamType.pNone,    InstructionTypeSuffix.I4,      AdditionalParamType.apI3),
                                                    /* 1A */    new Info(InstructionCode.LDC,        false,  false,  "ldc.4",            ParamType.pNone,    InstructionTypeSuffix.I4,      AdditionalParamType.apI4),
                                                    /* 1B */    new Info(InstructionCode.LDC,        false,  false,  "ldc.5",            ParamType.pNone,    InstructionTypeSuffix.I4,      AdditionalParamType.apI5),
                                                    /* 1C */    new Info(InstructionCode.LDC,        false,  false,  "ldc.6",            ParamType.pNone,    InstructionTypeSuffix.I4,      AdditionalParamType.apI6),
                                                    /* 1D */    new Info(InstructionCode.LDC,        false,  false,  "ldc.7",            ParamType.pNone,    InstructionTypeSuffix.I4,      AdditionalParamType.apI7),
                                                    /* 1E */    new Info(InstructionCode.LDC,        false,  false,  "ldc.8",            ParamType.pNone,    InstructionTypeSuffix.I4,      AdditionalParamType.apI8),
                                                    /* 1F */    new Info(InstructionCode.LDC,        false,  false,  "ldc.s",            ParamType.pInt8,    InstructionTypeSuffix.I4,      AdditionalParamType.apInt32),
                                                    /* 20 */    new Info(InstructionCode.LDC,        false,  false,  "ldc.i4",           ParamType.pInt32,   InstructionTypeSuffix.I4,      AdditionalParamType.apInt32),
                                                    /* 21 */    new Info(InstructionCode.LDC,        false,  false,  "ldc.i8",           ParamType.pInt64,   InstructionTypeSuffix.I8,      AdditionalParamType.apInt64),
                                                    /* 22 */    new Info(InstructionCode.LDC,        false,  false,  "ldc.r4",           ParamType.pFloat32, InstructionTypeSuffix.R4,      AdditionalParamType.apFloat32),
                                                    /* 23 */    new Info(InstructionCode.LDC,        false,  false,  "ldc.r8",           ParamType.pFloat64, InstructionTypeSuffix.R8,      AdditionalParamType.apFloat64),
                                                    /* 24 */    new Info(InstructionCode.NOP,        false,  false,  "-",                ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* 25 */    new Info(InstructionCode.DUP,        false,  false,  "dup",              ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* 26 */    new Info(InstructionCode.POP,        false,  false,  "pop",              ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* 27 */    new Info(InstructionCode.JMP,        false,  false,  "jmp",              ParamType.pToken,   InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* 28 */    new Info(InstructionCode.CALL,       false,  false,  "call",             ParamType.pToken,   InstructionTypeSuffix.Default, AdditionalParamType.apMethodTok),
                                                    /* 29 */    new Info(InstructionCode.CALLI,      false,  false,  "calli",            ParamType.pToken,   InstructionTypeSuffix.Default, AdditionalParamType.apCallSiteDescr),
                                                    /* 2A */    new Info(InstructionCode.RET,        false,  false,  "ret",              ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* 2B */    new Info(InstructionCode.BR,         false,  false,  "br.s",             ParamType.pInt8,    InstructionTypeSuffix.Default, AdditionalParamType.apOfs),
                                                    /* 2C */    new Info(InstructionCode.BRFALSE,    false,  false,  "brfalse.s",        ParamType.pInt8,    InstructionTypeSuffix.Default, AdditionalParamType.apOfs),
                                                    /* 2D */    new Info(InstructionCode.BRTRUE,     false,  false,  "brtrue.s",         ParamType.pInt8,    InstructionTypeSuffix.Default, AdditionalParamType.apOfs),
                                                    /* 2E */    new Info(InstructionCode.BEQ,        false,  false,  "beq.s",            ParamType.pInt8,    InstructionTypeSuffix.Default, AdditionalParamType.apOfs),
                                                    /* 2F */    new Info(InstructionCode.BGE,        false,  false,  "bge.s",            ParamType.pInt8,    InstructionTypeSuffix.Default, AdditionalParamType.apOfs),
                                                    /* 30 */    new Info(InstructionCode.BGT,        false,  false,  "bgt.s",            ParamType.pInt8,    InstructionTypeSuffix.Default, AdditionalParamType.apOfs),
                                                    /* 31 */    new Info(InstructionCode.BLE,        false,  false,  "ble.s",            ParamType.pInt8,    InstructionTypeSuffix.Default, AdditionalParamType.apOfs),
                                                    /* 32 */    new Info(InstructionCode.BLT,        false,  false,  "blt.s",            ParamType.pInt8,    InstructionTypeSuffix.Default, AdditionalParamType.apOfs),
                                                    /* 33 */    new Info(InstructionCode.BNE,        false,  true,   "bne.un.s",         ParamType.pInt8,    InstructionTypeSuffix.Default, AdditionalParamType.apOfs),
                                                    /* 34 */    new Info(InstructionCode.BGE,        false,  true,   "bge.un.s",         ParamType.pInt8,    InstructionTypeSuffix.Default, AdditionalParamType.apOfs),
                                                    /* 35 */    new Info(InstructionCode.BGT,        false,  true,   "bgt.un.s",         ParamType.pInt8,    InstructionTypeSuffix.Default, AdditionalParamType.apOfs),
                                                    /* 36 */    new Info(InstructionCode.BLE,        false,  true,   "ble.un.s",         ParamType.pInt8,    InstructionTypeSuffix.Default, AdditionalParamType.apOfs),
                                                    /* 37 */    new Info(InstructionCode.BLT,        false,  true,   "blt.un.s",         ParamType.pInt8,    InstructionTypeSuffix.Default, AdditionalParamType.apOfs),
                                                    /* 38 */    new Info(InstructionCode.BR,         false,  false,  "br",               ParamType.pInt32,   InstructionTypeSuffix.Default, AdditionalParamType.apOfs),
                                                    /* 39 */    new Info(InstructionCode.BRFALSE,    false,  false,  "brfalse",          ParamType.pInt32,   InstructionTypeSuffix.Default, AdditionalParamType.apOfs),
                                                    /* 3A */    new Info(InstructionCode.BRTRUE,     false,  false,  "brtrue",           ParamType.pInt32,   InstructionTypeSuffix.Default, AdditionalParamType.apOfs),
                                                    /* 3B */    new Info(InstructionCode.BEQ,        false,  false,  "beq",              ParamType.pInt32,   InstructionTypeSuffix.Default, AdditionalParamType.apOfs),
                                                    /* 3C */    new Info(InstructionCode.BGE,        false,  false,  "bge",              ParamType.pInt32,   InstructionTypeSuffix.Default, AdditionalParamType.apOfs),
                                                    /* 3D */    new Info(InstructionCode.BGT,        false,  false,  "bgt",              ParamType.pInt32,   InstructionTypeSuffix.Default, AdditionalParamType.apOfs),
                                                    /* 3E */    new Info(InstructionCode.BLE,        false,  false,  "ble",              ParamType.pInt32,   InstructionTypeSuffix.Default, AdditionalParamType.apOfs),
                                                    /* 3F */    new Info(InstructionCode.BLT,        false,  false,  "blt",              ParamType.pInt32,   InstructionTypeSuffix.Default, AdditionalParamType.apOfs),
                                                    /* 40 */    new Info(InstructionCode.BNE,        false,  true,   "bne.un",           ParamType.pInt32,   InstructionTypeSuffix.Default, AdditionalParamType.apOfs),
                                                    /* 41 */    new Info(InstructionCode.BGE,        false,  true,   "bge.un",           ParamType.pInt32,   InstructionTypeSuffix.Default, AdditionalParamType.apOfs),
                                                    /* 42 */    new Info(InstructionCode.BGT,        false,  true,   "bgt.un",           ParamType.pInt32,   InstructionTypeSuffix.Default, AdditionalParamType.apOfs),
                                                    /* 43 */    new Info(InstructionCode.BLE,        false,  true,   "ble.un",           ParamType.pInt32,   InstructionTypeSuffix.Default, AdditionalParamType.apOfs),
                                                    /* 44 */    new Info(InstructionCode.BLT,        false,  true,   "blt.un",           ParamType.pInt32,   InstructionTypeSuffix.Default, AdditionalParamType.apOfs),
                                                    /* 45 */    new Info(InstructionCode.SWITCH,     false,  false,  "switch",           ParamType.pSwitch,  InstructionTypeSuffix.Default, AdditionalParamType.apOfsArray),
                                                    /* 46 */    new Info(InstructionCode.LDIND,      false,  false,  "ldind.i1",         ParamType.pNone,    InstructionTypeSuffix.I1,      AdditionalParamType.apNone),
                                                    /* 47 */    new Info(InstructionCode.LDIND,      false,  false,  "ldind.u1",         ParamType.pNone,    InstructionTypeSuffix.U1,      AdditionalParamType.apNone),
                                                    /* 48 */    new Info(InstructionCode.LDIND,      false,  false,  "ldind.i2",         ParamType.pNone,    InstructionTypeSuffix.I2,      AdditionalParamType.apNone),
                                                    /* 49 */    new Info(InstructionCode.LDIND,      false,  false,  "ldind.u2",         ParamType.pNone,    InstructionTypeSuffix.U2,      AdditionalParamType.apNone),
                                                    /* 4A */    new Info(InstructionCode.LDIND,      false,  false,  "ldind.i4",         ParamType.pNone,    InstructionTypeSuffix.I4,      AdditionalParamType.apNone),
                                                    /* 4B */    new Info(InstructionCode.LDIND,      false,  false,  "ldind.u4",         ParamType.pNone,    InstructionTypeSuffix.U4,      AdditionalParamType.apNone),
                                                    /* 4C */    new Info(InstructionCode.LDIND,      false,  false,  "ldind.i8",         ParamType.pNone,    InstructionTypeSuffix.I8,      AdditionalParamType.apNone),
                                                    /* 4D */    new Info(InstructionCode.LDIND,      false,  false,  "ldind.i",          ParamType.pNone,    InstructionTypeSuffix.I,       AdditionalParamType.apNone),
                                                    /* 4E */    new Info(InstructionCode.LDIND,      false,  false,  "ldind.r4",         ParamType.pNone,    InstructionTypeSuffix.R4,      AdditionalParamType.apNone),
                                                    /* 4F */    new Info(InstructionCode.LDIND,      false,  false,  "ldind.r8",         ParamType.pNone,    InstructionTypeSuffix.R8,      AdditionalParamType.apNone),
                                                    /* 50 */    new Info(InstructionCode.LDIND,      false,  false,  "ldind.ref",        ParamType.pNone,    InstructionTypeSuffix.REF,     AdditionalParamType.apNone),
                                                    /* 51 */    new Info(InstructionCode.STIND,      false,  false,  "stind.ref",        ParamType.pNone,    InstructionTypeSuffix.REF,     AdditionalParamType.apNone),
                                                    /* 52 */    new Info(InstructionCode.STIND,      false,  false,  "stind.i1",         ParamType.pNone,    InstructionTypeSuffix.I1,      AdditionalParamType.apNone),
                                                    /* 53 */    new Info(InstructionCode.STIND,      false,  false,  "stind.i2",         ParamType.pNone,    InstructionTypeSuffix.I2,      AdditionalParamType.apNone),
                                                    /* 54 */    new Info(InstructionCode.STIND,      false,  false,  "stind.i4",         ParamType.pNone,    InstructionTypeSuffix.I4,      AdditionalParamType.apNone),
                                                    /* 55 */    new Info(InstructionCode.STIND,      false,  false,  "stind.i8",         ParamType.pNone,    InstructionTypeSuffix.I8,      AdditionalParamType.apNone),
                                                    /* 56 */    new Info(InstructionCode.STIND,      false,  false,  "stind.r4",         ParamType.pNone,    InstructionTypeSuffix.R4,      AdditionalParamType.apNone),
                                                    /* 57 */    new Info(InstructionCode.STIND,      false,  false,  "stind.r8",         ParamType.pNone,    InstructionTypeSuffix.R8,      AdditionalParamType.apNone),
                                                    /* 58 */    new Info(InstructionCode.ADD,        false,  false,  "add",              ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* 59 */    new Info(InstructionCode.SUB,        false,  false,  "sub",              ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* 5A */    new Info(InstructionCode.MUL,        false,  false,  "mul",              ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* 5B */    new Info(InstructionCode.DIV,        false,  false,  "div",              ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* 5C */    new Info(InstructionCode.DIV,        false,  true,   "div.un",           ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* 5D */    new Info(InstructionCode.REM,        false,  false,  "rem",              ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* 5E */    new Info(InstructionCode.REM,        false,  true,   "rem.un",           ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* 5F */    new Info(InstructionCode.AND,        false,  false,  "and",              ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* 60 */    new Info(InstructionCode.OR,         false,  false,  "or",               ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* 61 */    new Info(InstructionCode.XOR,        false,  false,  "xor",              ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* 62 */    new Info(InstructionCode.SHL,        false,  false,  "shl",              ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* 63 */    new Info(InstructionCode.SHR,        false,  false,  "shr",              ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* 64 */    new Info(InstructionCode.SHR,        false,  true,   "shr.un",           ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* 65 */    new Info(InstructionCode.NEG,        false,  false,  "neg",              ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* 66 */    new Info(InstructionCode.NOT,        false,  false,  "not",              ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* 67 */    new Info(InstructionCode.CONV,       false,  false,  "conv.i1",          ParamType.pNone,    InstructionTypeSuffix.I1,      AdditionalParamType.apNone),
                                                    /* 68 */    new Info(InstructionCode.CONV,       false,  false,  "conv.i2",          ParamType.pNone,    InstructionTypeSuffix.I2,      AdditionalParamType.apNone),
                                                    /* 69 */    new Info(InstructionCode.CONV,       false,  false,  "conv.i4",          ParamType.pNone,    InstructionTypeSuffix.I4,      AdditionalParamType.apNone),
                                                    /* 6A */    new Info(InstructionCode.CONV,       false,  false,  "conv.i8",          ParamType.pNone,    InstructionTypeSuffix.I8,      AdditionalParamType.apNone),
                                                    /* 6B */    new Info(InstructionCode.CONV,       false,  false,  "conv.r4",          ParamType.pNone,    InstructionTypeSuffix.R4,      AdditionalParamType.apNone),
                                                    /* 6C */    new Info(InstructionCode.CONV,       false,  false,  "conv.r8",          ParamType.pNone,    InstructionTypeSuffix.R8,      AdditionalParamType.apNone),
                                                    /* 6D */    new Info(InstructionCode.CONV,       false,  false,  "conv.u4",          ParamType.pNone,    InstructionTypeSuffix.U4,      AdditionalParamType.apNone),
                                                    /* 6E */    new Info(InstructionCode.CONV,       false,  false,  "conv.u8",          ParamType.pNone,    InstructionTypeSuffix.U8,      AdditionalParamType.apNone),
                                                    /* 6F */    new Info(InstructionCode.CALLVIRT,   false,  false,  "callvirt",         ParamType.pToken,   InstructionTypeSuffix.Default, AdditionalParamType.apMethodTok),
                                                    /* 70 */    new Info(InstructionCode.CPOBJ,      false,  false,  "cpobj",            ParamType.pToken,   InstructionTypeSuffix.Default, AdditionalParamType.apClassTok),
                                                    /* 71 */    new Info(InstructionCode.LDOBJ,      false,  false,  "ldobj",            ParamType.pToken,   InstructionTypeSuffix.Default, AdditionalParamType.apClassTok),
                                                    /* 72 */    new Info(InstructionCode.LDSTR,      false,  false,  "ldstr",            ParamType.pToken,   InstructionTypeSuffix.Default, AdditionalParamType.apString),
                                                    /* 73 */    new Info(InstructionCode.NEWOBJ,     false,  false,  "newobj",           ParamType.pToken,   InstructionTypeSuffix.Default, AdditionalParamType.apCtorTok),
                                                    /* 74 */    new Info(InstructionCode.CASTCLASS,  false,  false,  "castclass",        ParamType.pToken,   InstructionTypeSuffix.Default, AdditionalParamType.apClassTok),
                                                    /* 75 */    new Info(InstructionCode.ISINST,     false,  false,  "isinst",           ParamType.pToken,   InstructionTypeSuffix.Default, AdditionalParamType.apClassTok),
                                                    /* 76 */    new Info(InstructionCode.CONV,       false,  true,   "conv.r.un",        ParamType.pNone,    InstructionTypeSuffix.R,       AdditionalParamType.apNone),
                                                    /* 77 */    new Info(InstructionCode.NOP,        false,  false,  "-",                ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* 78 */    new Info(InstructionCode.NOP,        false,  false,  "-",                ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* 79 */    new Info(InstructionCode.UNBOX,      false,  false,  "unbox",            ParamType.pToken,   InstructionTypeSuffix.Default, AdditionalParamType.apValTypeTok),
                                                    /* 7A */    new Info(InstructionCode.THROW,      false,  false,  "throw",            ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* 7B */    new Info(InstructionCode.LDFLD,      false,  false,  "ldfld",            ParamType.pToken,   InstructionTypeSuffix.Default, AdditionalParamType.apFieldTok),
                                                    /* 7C */    new Info(InstructionCode.LDFLDA,     false,  false,  "ldflda",           ParamType.pToken,   InstructionTypeSuffix.Default, AdditionalParamType.apFieldTok),
                                                    /* 7D */    new Info(InstructionCode.STFLD,      false,  false,  "stfld",            ParamType.pToken,   InstructionTypeSuffix.Default, AdditionalParamType.apFieldTok),
                                                    /* 7E */    new Info(InstructionCode.LDSFLD,     false,  false,  "ldsfld",           ParamType.pToken,   InstructionTypeSuffix.Default, AdditionalParamType.apFieldTok),
                                                    /* 7F */    new Info(InstructionCode.LDSFLDA,    false,  false,  "ldsflda",          ParamType.pToken,   InstructionTypeSuffix.Default, AdditionalParamType.apFieldTok),
                                                    /* 80 */    new Info(InstructionCode.STSFLD,     false,  false,  "stsfld",           ParamType.pToken,   InstructionTypeSuffix.Default, AdditionalParamType.apFieldTok),
                                                    /* 81 */    new Info(InstructionCode.STOBJ,      false,  false,  "stobj",            ParamType.pToken,   InstructionTypeSuffix.Default, AdditionalParamType.apClassTok),
                                                    /* 82 */    new Info(InstructionCode.CONV,       true,   true,   "conv.ovf.i1.un",   ParamType.pNone,    InstructionTypeSuffix.I1,      AdditionalParamType.apNone),
                                                    /* 83 */    new Info(InstructionCode.CONV,       true,   true,   "conv.ovf.i2.un",   ParamType.pNone,    InstructionTypeSuffix.I2,      AdditionalParamType.apNone),
                                                    /* 84 */    new Info(InstructionCode.CONV,       true,   true,   "conv.ovf.i4.un",   ParamType.pNone,    InstructionTypeSuffix.I4,      AdditionalParamType.apNone),
                                                    /* 85 */    new Info(InstructionCode.CONV,       true,   true,   "conv.ovf.i8.un",   ParamType.pNone,    InstructionTypeSuffix.I8,      AdditionalParamType.apNone),
                                                    /* 86 */    new Info(InstructionCode.CONV,       true,   true,   "conv.ovf.u1.un",   ParamType.pNone,    InstructionTypeSuffix.U1,      AdditionalParamType.apNone),
                                                    /* 87 */    new Info(InstructionCode.CONV,       true,   true,   "conv.ovf.u2.un",   ParamType.pNone,    InstructionTypeSuffix.U2,      AdditionalParamType.apNone),
                                                    /* 88 */    new Info(InstructionCode.CONV,       true,   true,   "conv.ovf.u4.un",   ParamType.pNone,    InstructionTypeSuffix.U4,      AdditionalParamType.apNone),
                                                    /* 89 */    new Info(InstructionCode.CONV,       true,   true,   "conv.ovf.u8.un",   ParamType.pNone,    InstructionTypeSuffix.U8,      AdditionalParamType.apNone),
                                                    /* 8A */    new Info(InstructionCode.CONV,       true,   true,   "conv.ovf.i.un",    ParamType.pNone,    InstructionTypeSuffix.I,       AdditionalParamType.apNone),
                                                    /* 8B */    new Info(InstructionCode.CONV,       true,   true,   "conv.ovf.u.un",    ParamType.pNone,    InstructionTypeSuffix.U,       AdditionalParamType.apNone),
                                                    /* 8C */    new Info(InstructionCode.BOX,        false,  false,  "box",              ParamType.pToken,   InstructionTypeSuffix.Default, AdditionalParamType.apValTypeTok),
                                                    /* 8D */    new Info(InstructionCode.NEWARR,     false,  false,  "newarr",           ParamType.pToken,   InstructionTypeSuffix.Default, AdditionalParamType.apTypeTok),
                                                    /* 8E */    new Info(InstructionCode.LDLEN,      false,  false,  "ldlen",            ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* 8F */    new Info(InstructionCode.LDELEMA,    false,  false,  "ldelema",          ParamType.pToken,   InstructionTypeSuffix.Default, AdditionalParamType.apClassTok),
                                                    /* 90 */    new Info(InstructionCode.LDELEM,     false,  false,  "ldelem.i1",        ParamType.pNone,    InstructionTypeSuffix.I1,      AdditionalParamType.apNone),
                                                    /* 91 */    new Info(InstructionCode.LDELEM,     false,  false,  "ldelem.u1",        ParamType.pNone,    InstructionTypeSuffix.U1,      AdditionalParamType.apNone),
                                                    /* 92 */    new Info(InstructionCode.LDELEM,     false,  false,  "ldelem.i2",        ParamType.pNone,    InstructionTypeSuffix.I2,      AdditionalParamType.apNone),
                                                    /* 93 */    new Info(InstructionCode.LDELEM,     false,  false,  "ldelem.u2",        ParamType.pNone,    InstructionTypeSuffix.U2,      AdditionalParamType.apNone),
                                                    /* 94 */    new Info(InstructionCode.LDELEM,     false,  false,  "ldelem.i4",        ParamType.pNone,    InstructionTypeSuffix.I4,      AdditionalParamType.apNone),
                                                    /* 95 */    new Info(InstructionCode.LDELEM,     false,  false,  "ldelem.u4",        ParamType.pNone,    InstructionTypeSuffix.U4,      AdditionalParamType.apNone),
                                                    /* 96 */    new Info(InstructionCode.LDELEM,     false,  false,  "ldelem.i8",        ParamType.pNone,    InstructionTypeSuffix.I8,      AdditionalParamType.apNone),
                                                    /* 97 */    new Info(InstructionCode.LDELEM,     false,  false,  "ldelem.i",         ParamType.pNone,    InstructionTypeSuffix.I,       AdditionalParamType.apNone),
                                                    /* 98 */    new Info(InstructionCode.LDELEM,     false,  false,  "ldelem.r4",        ParamType.pNone,    InstructionTypeSuffix.R4,      AdditionalParamType.apNone),
                                                    /* 99 */    new Info(InstructionCode.LDELEM,     false,  false,  "ldelem.r8",        ParamType.pNone,    InstructionTypeSuffix.R8,      AdditionalParamType.apNone),
                                                    /* 9A */    new Info(InstructionCode.LDELEM,     false,  false,  "ldelem.ref",       ParamType.pNone,    InstructionTypeSuffix.REF,     AdditionalParamType.apNone),
                                                    /* 9B */    new Info(InstructionCode.STELEM,     false,  false,  "stelem.i",         ParamType.pNone,    InstructionTypeSuffix.I,       AdditionalParamType.apNone),
                                                    /* 9C */    new Info(InstructionCode.STELEM,     false,  false,  "stelem.i1",        ParamType.pNone,    InstructionTypeSuffix.I1,      AdditionalParamType.apNone),
                                                    /* 9D */    new Info(InstructionCode.STELEM,     false,  false,  "stelem.i2",        ParamType.pNone,    InstructionTypeSuffix.I2,      AdditionalParamType.apNone),
                                                    /* 9E */    new Info(InstructionCode.STELEM,     false,  false,  "stelem.i4",        ParamType.pNone,    InstructionTypeSuffix.I4,      AdditionalParamType.apNone),
                                                    /* 9F */    new Info(InstructionCode.STELEM,     false,  false,  "stelem.i8",        ParamType.pNone,    InstructionTypeSuffix.I8,      AdditionalParamType.apNone),
                                                    /* A0 */    new Info(InstructionCode.STELEM,     false,  false,  "stelem.r4",        ParamType.pNone,    InstructionTypeSuffix.R4,      AdditionalParamType.apNone),
                                                    /* A1 */    new Info(InstructionCode.STELEM,     false,  false,  "stelem.r8",        ParamType.pNone,    InstructionTypeSuffix.R8,      AdditionalParamType.apNone),
                                                    /* A2 */    new Info(InstructionCode.STELEM,     false,  false,  "stelem.ref",       ParamType.pNone,    InstructionTypeSuffix.REF,     AdditionalParamType.apNone),
                                                    /* A3 */    new Info(InstructionCode.NOP,        false,  false,  "-",                ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* A4 */    new Info(InstructionCode.NOP,        false,  false,  "-",                ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* A5 */    new Info(InstructionCode.NOP,        false,  false,  "-",                ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* A6 */    new Info(InstructionCode.NOP,        false,  false,  "-",                ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* A7 */    new Info(InstructionCode.NOP,        false,  false,  "-",                ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* A8 */    new Info(InstructionCode.NOP,        false,  false,  "-",                ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* A9 */    new Info(InstructionCode.NOP,        false,  false,  "-",                ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* AA */    new Info(InstructionCode.NOP,        false,  false,  "-",                ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* AB */    new Info(InstructionCode.NOP,        false,  false,  "-",                ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* AC */    new Info(InstructionCode.NOP,        false,  false,  "-",                ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* AD */    new Info(InstructionCode.NOP,        false,  false,  "-",                ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* AE */    new Info(InstructionCode.NOP,        false,  false,  "-",                ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* AF */    new Info(InstructionCode.NOP,        false,  false,  "-",                ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* B0 */    new Info(InstructionCode.NOP,        false,  false,  "-",                ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* B1 */    new Info(InstructionCode.NOP,        false,  false,  "-",                ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* B2 */    new Info(InstructionCode.NOP,        false,  false,  "-",                ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* B3 */    new Info(InstructionCode.CONV,       true,   false,  "conv.ovf.i1",      ParamType.pNone,    InstructionTypeSuffix.I1,      AdditionalParamType.apNone),
                                                    /* B4 */    new Info(InstructionCode.CONV,       true,   false,  "conv.ovf.u1",      ParamType.pNone,    InstructionTypeSuffix.U1,      AdditionalParamType.apNone),
                                                    /* B5 */    new Info(InstructionCode.CONV,       true,   false,  "conv.ovf.i2",      ParamType.pNone,    InstructionTypeSuffix.I2,      AdditionalParamType.apNone),
                                                    /* B6 */    new Info(InstructionCode.CONV,       true,   false,  "conv.ovf.u2",      ParamType.pNone,    InstructionTypeSuffix.U2,      AdditionalParamType.apNone),
                                                    /* B7 */    new Info(InstructionCode.CONV,       true,   false,  "conv.ovf.i4",      ParamType.pNone,    InstructionTypeSuffix.I4,      AdditionalParamType.apNone),
                                                    /* B8 */    new Info(InstructionCode.CONV,       true,   false,  "conv.ovf.u4",      ParamType.pNone,    InstructionTypeSuffix.U4,      AdditionalParamType.apNone),
                                                    /* B9 */    new Info(InstructionCode.CONV,       true,   false,  "conv.ovf.i8",      ParamType.pNone,    InstructionTypeSuffix.I8,      AdditionalParamType.apNone),
                                                    /* BA */    new Info(InstructionCode.CONV,       true,   false,  "conv.ovf.u8",      ParamType.pNone,    InstructionTypeSuffix.U8,      AdditionalParamType.apNone),
                                                    /* BB */    new Info(InstructionCode.NOP,        false,  false,  "-",                ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* BC */    new Info(InstructionCode.NOP,        false,  false,  "-",                ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* BD */    new Info(InstructionCode.NOP,        false,  false,  "-",                ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* BE */    new Info(InstructionCode.NOP,        false,  false,  "-",                ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* BF */    new Info(InstructionCode.NOP,        false,  false,  "-",                ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* C0 */    new Info(InstructionCode.NOP,        false,  false,  "-",                ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* C1 */    new Info(InstructionCode.NOP,        false,  false,  "-",                ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* C2 */    new Info(InstructionCode.REFANYVAL,  false,  false,  "refanyval",        ParamType.pToken,   InstructionTypeSuffix.Default, AdditionalParamType.apTypeTok),
                                                    /* C3 */    new Info(InstructionCode.CKFINITE,   false,  false,  "ckfinite",         ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* C4 */    new Info(InstructionCode.NOP,        false,  false,  "-",                ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* C5 */    new Info(InstructionCode.NOP,        false,  false,  "-",                ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* C6 */    new Info(InstructionCode.MKREFANY,   false,  false,  "mkrefany",         ParamType.pToken,   InstructionTypeSuffix.Default, AdditionalParamType.apClassTok),
                                                    /* C7 */    new Info(InstructionCode.NOP,        false,  false,  "-",                ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* C8 */    new Info(InstructionCode.NOP,        false,  false,  "-",                ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* C9 */    new Info(InstructionCode.NOP,        false,  false,  "-",                ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* CA */    new Info(InstructionCode.NOP,        false,  false,  "-",                ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* CB */    new Info(InstructionCode.NOP,        false,  false,  "-",                ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* CC */    new Info(InstructionCode.NOP,        false,  false,  "-",                ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* CD */    new Info(InstructionCode.NOP,        false,  false,  "-",                ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* CE */    new Info(InstructionCode.NOP,        false,  false,  "-",                ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* CF */    new Info(InstructionCode.NOP,        false,  false,  "-",                ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* D0 */    new Info(InstructionCode.LDTOKEN,    false,  false,  "ldtoken",          ParamType.pToken,   InstructionTypeSuffix.Default, AdditionalParamType.apToken),
                                                    /* D1 */    new Info(InstructionCode.CONV,       false,  false,  "conv.u2",          ParamType.pNone,    InstructionTypeSuffix.U2,      AdditionalParamType.apNone),
                                                    /* D2 */    new Info(InstructionCode.CONV,       false,  false,  "conv.u1",          ParamType.pNone,    InstructionTypeSuffix.U1,      AdditionalParamType.apNone),
                                                    /* D3 */    new Info(InstructionCode.CONV,       false,  false,  "conv.i",           ParamType.pNone,    InstructionTypeSuffix.I,       AdditionalParamType.apNone),
                                                    /* D4 */    new Info(InstructionCode.CONV,       true,   false,  "conv.ovf.i",       ParamType.pNone,    InstructionTypeSuffix.I,       AdditionalParamType.apNone),
                                                    /* D5 */    new Info(InstructionCode.CONV,       true,   false,  "conv.ovf.u",       ParamType.pNone,    InstructionTypeSuffix.U,       AdditionalParamType.apNone),
                                                    /* D6 */    new Info(InstructionCode.ADD,        true,   false,  "add.ovf",          ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* D7 */    new Info(InstructionCode.ADD,        true,   true,   "add.ovf.un",       ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* D8 */    new Info(InstructionCode.MUL,        true,   false,  "mul.ovf",          ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* D9 */    new Info(InstructionCode.MUL,        true,   true,   "mul.ovf.un",       ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* DA */    new Info(InstructionCode.SUB,        true,   false,  "sub.ovf",          ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* DB */    new Info(InstructionCode.SUB,        true,   true,   "sub.ovf.un",       ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* DC */    new Info(InstructionCode.ENDFINALLY, false,  false,  "endfinally",       ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* DD */    new Info(InstructionCode.LEAVE,      false,  false,  "leave",            ParamType.pInt32,   InstructionTypeSuffix.Default, AdditionalParamType.apOfs),
                                                    /* DE */    new Info(InstructionCode.LEAVE,      false,  false,  "leave.s",          ParamType.pInt8,    InstructionTypeSuffix.Default, AdditionalParamType.apOfs),
                                                    /* DF */    new Info(InstructionCode.STIND,      false,  false,  "stind.i",          ParamType.pNone,    InstructionTypeSuffix.I,       AdditionalParamType.apNone),
                                                    /* E0 */    new Info(InstructionCode.CONV,       false,  false,  "conv.u",           ParamType.pNone,    InstructionTypeSuffix.U,       AdditionalParamType.apNone),
                                                    /* FE 00 */ new Info(InstructionCode.ARGLIST,    false,  false,  "arglist",          ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* FE 01 */ new Info(InstructionCode.CEQ,        false,  false,  "ceq",              ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* FE 02 */ new Info(InstructionCode.CGT,        false,  false,  "cgt",              ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* FE 03 */ new Info(InstructionCode.CGT,        false,  true,   "cgt.un",           ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* FE 04 */ new Info(InstructionCode.CLT,        false,  false,  "clt",              ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* FE 05 */ new Info(InstructionCode.CLT,        false,  true,   "clt.un",           ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* FE 06 */ new Info(InstructionCode.LDFTN,      false,  false,  "ldftn",            ParamType.pToken,   InstructionTypeSuffix.Default, AdditionalParamType.apMethodTok),
                                                    /* FE 07 */ new Info(InstructionCode.LDVIRTFTN,  false,  false,  "ldvirtftn",        ParamType.pToken,   InstructionTypeSuffix.Default, AdditionalParamType.apMethodTok),
                                                    /* FE 08 */ new Info(InstructionCode.NOP,        false,  false,  "-",                ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* FE 09 */ new Info(InstructionCode.LDARG,      false,  false,  "ldarg",            ParamType.pUint16,  InstructionTypeSuffix.Default, AdditionalParamType.apUint16),
                                                    /* FE 0A */ new Info(InstructionCode.LDARGA,     false,  false,  "ldarga",           ParamType.pUint16,  InstructionTypeSuffix.Default, AdditionalParamType.apUint16),
                                                    /* FE 0B */ new Info(InstructionCode.STARG,      false,  false,  "starg",            ParamType.pUint16,  InstructionTypeSuffix.Default, AdditionalParamType.apUint16),
                                                    /* FE 0C */ new Info(InstructionCode.LDLOC,      false,  false,  "ldloc",            ParamType.pUint16,  InstructionTypeSuffix.Default, AdditionalParamType.apUint16),
                                                    /* FE 0D */ new Info(InstructionCode.LDLOCA,     false,  false,  "ldloca",           ParamType.pUint16,  InstructionTypeSuffix.Default, AdditionalParamType.apUint16),
                                                    /* FE 0E */ new Info(InstructionCode.STLOC,      false,  false,  "stloc",            ParamType.pUint16,  InstructionTypeSuffix.Default, AdditionalParamType.apUint16),
                                                    /* FE 0F */ new Info(InstructionCode.LOCALLOC,   false,  false,  "localloc",         ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* FE 10 */ new Info(InstructionCode.NOP,        false,  false,  "-",                ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* FE 11 */ new Info(InstructionCode.ENDFILTER,  false,  false,  "endfilter",        ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* FE 12 */ new Info(InstructionCode.UNALIGNED,  false,  false,  "-",                ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* FE 13 */ new Info(InstructionCode.VOLATILE,   false,  false,  "-",                ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* FE 14 */ new Info(InstructionCode.TAIL,       false,  false,  "-",                ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* FE 15 */ new Info(InstructionCode.INITOBJ,    false,  false,  "initobj",          ParamType.pToken,   InstructionTypeSuffix.Default, AdditionalParamType.apClassTok),
                                                    /* FE 16 */ new Info(InstructionCode.NOP,        false,  false,  "-",                ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* FE 17 */ new Info(InstructionCode.CPBLK,      false,  false,  "cpblk",            ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* FE 18 */ new Info(InstructionCode.INITBLK,    false,  false,  "initblk",          ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* FE 19 */ new Info(InstructionCode.NOP,        false,  false,  "-",                ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* FE 1A */ new Info(InstructionCode.RETHROW,    false,  false,  "rethrow",          ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* FE 1B */ new Info(InstructionCode.NOP,        false,  false,  "-",                ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone),
                                                    /* FE 1C */ new Info(InstructionCode.SIZEOF,     false,  false,  "sizeof",           ParamType.pToken,   InstructionTypeSuffix.Default, AdditionalParamType.apValTypeTok),
                                                    /* FE 1D */ new Info(InstructionCode.REFANYTYPE, false,  false,  "refanytype",       ParamType.pNone,    InstructionTypeSuffix.Default, AdditionalParamType.apNone)
                                                };

        internal int startOffset;
        private int afterEndOffset;
        private bool paramIsOffset;

        private InstructionCode instructionCode;
        private bool hasTail, hasVolatile, hasUnaligned;
        private int unalignedParam;
        private bool ovfFlag, unFlag;
        private InstructionTypeSuffix typeSuffix;
        private string name;
        private object param;
        private StackTypes stack;

        internal Instruction(ILMethodDecoder decoder)
        {
            stack = null;

            startOffset = decoder.GetOffset();
            int code = decoder.ReadCode();
            Info info = instructions[code];

            if (info.code == InstructionCode.TAIL)
            {
                hasTail = true;
                code = decoder.ReadCode();
                info = instructions[code];
            }
            else
                hasTail = false;

            hasUnaligned = false;
            hasVolatile = false;
            unalignedParam = 0;

            while (info.code == InstructionCode.VOLATILE || info.code == InstructionCode.UNALIGNED)
            {
                if (info.code == InstructionCode.VOLATILE)
                    hasVolatile = true;
                else
                {
                    hasUnaligned = true;
                    unalignedParam = (Int32)(decoder.ReadUint8());
                }

                code = decoder.ReadCode();
                info = instructions[code];
            }

            instructionCode = info.code;
            ovfFlag = info.ovf;
            unFlag = info.un;
            typeSuffix = info.typeSuffix;
            name = info.name;

            paramIsOffset = false;

            switch (info.pType)
            {
                case ParamType.pNone:
                    if (info.apType >= AdditionalParamType.apU0 && 
                        info.apType <= AdditionalParamType.apU3)
                        param = (Int32)(info.apType-AdditionalParamType.apU0);

                    if (info.apType >= AdditionalParamType.apIM1 && 
                        info.apType <= AdditionalParamType.apI8)
                        param = (Int32)(info.apType-AdditionalParamType.apIM1-1);
                    break;

                case ParamType.pInt8:
                    param = (Int32)(decoder.ReadInt8());
                    if (info.apType == AdditionalParamType.apOfs)
                        paramIsOffset = true;
                    break;

                case ParamType.pInt32:
                    param = decoder.ReadInt32();
                    if (info.apType == AdditionalParamType.apOfs)
                        paramIsOffset = true;
                    break;

                case ParamType.pInt64:
                    param = decoder.ReadInt64();
                    break;

                case ParamType.pUint8:
                    param = (Int32)(decoder.ReadUint8());
                    break;

                case ParamType.pUint16:
                    param = (Int32)(decoder.ReadUint16());
                    break;

                case ParamType.pFloat32:
                    param = (double)(decoder.ReadFloat32());
                    break;

                case ParamType.pFloat64:
                    param = decoder.ReadFloat64();
                    break;

                case ParamType.pToken:
                    param = decoder.ReadToken();
                    break;

                case ParamType.pSwitch:
                    param = decoder.ReadSwitch();
                    paramIsOffset = true;
                    break;
            }

            afterEndOffset = decoder.GetOffset();
        }

        internal void FixOffset(int[] offsetsMap)
        {
            if (paramIsOffset)
            {
                if (param is Int32)
                    param = offsetsMap[afterEndOffset+(Int32)param];
                else
                {
                    Int32[] switchOffsets = (Int32[])param;
                    for (int i = 0; i < switchOffsets.Length; i++)
                        switchOffsets[i] = offsetsMap[afterEndOffset+switchOffsets[i]];
                }
            }
        }

        internal void SetStack(StackTypes stack) { this.stack = stack; }

        #endregion

        /* Code of instruction */
        public InstructionCode Code { get { return instructionCode; } }

        /* Shows if instruction has .ovf suffix */
        public bool OverflowFlag { get { return ovfFlag; } }

        /* Shows if instruction has .un suffix */
        public bool UnsignedFlag { get { return unFlag; } }

        /* Type suffix of instruction */
        public InstructionTypeSuffix TypeSuffix { get { return typeSuffix; } }

        /* Shows if instruction has .tail modifier */
        public bool HasTail { get { return hasTail; } }

        /* Shows if instruction has .volatile modifier */
        public bool HasVolatile { get { return hasVolatile; } }

        /* Shows if instruction has .unaligned modifier */
        public bool HasUnaligned { get { return hasUnaligned; } }

        /* Parameter of .unaligned modifier */
        public int UnalignedParam { get { return unalignedParam; } }

        /* Name of instruction in IL assembler style */
        public string Name { get { return name; } }

        /* Parameter of instruction */
        public object Param { get { return param; } }

        /* Types on the evaluation stack */
        public StackTypes Stack { get { return stack; } }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            string result = "";

            if (HasTail)
                result += "TAIL.";

            if (HasVolatile)
                result += "VOLATILE.";

            if (HasUnaligned)
                result += "UNALIGNED." + UnalignedParam + ".";
                
            result += Code.ToString();

            if (OverflowFlag)
                result += ".OVF";

            if (TypeSuffix != InstructionTypeSuffix.Default)
                result += "." + TypeSuffix.ToString();

            if (UnsignedFlag)
                result += ".UN";
            
            if (Param != null)
            {
                if (Param is Int32[])
                {
                    foreach (Int32 Ofs in (Int32[])Param)
                        result += " " + Ofs;
                }
                else if (Param is Int32)
                    result += " " + (Int32)Param;
                else if (Param is Int64)
                    result += " " + (Int64)Param;
                else if (Param is double)
                    result += " " + (Double)Param;
                else if (Param is string)
                    result += " \"" + (string)Param + '\"';
                else
                    result += ' ' + String.Format(formatProvider,"{0:"+format+'}',Param);
            }

            return result;
        }

        public override string ToString()
        {
            return ToString("CSharp",ReflectionFormatter.formatter);
        }

        public Type TypeBySuffixOrParam()
        {
            Type result;

            switch(TypeSuffix) 
            {
                case InstructionTypeSuffix.I: 
                    result = typeof(IntPtr);
                    break;
                case InstructionTypeSuffix.I1: 
                    result = typeof(sbyte);
                    break;
                case InstructionTypeSuffix.I2: 
                    result = typeof(short);
                    break;
                case InstructionTypeSuffix.I4: 
                    result = typeof(int);
                    break;
                case InstructionTypeSuffix.I8: 
                    result = typeof(long);
                    break;
                case InstructionTypeSuffix.U: 
                    result = typeof(UIntPtr);
                    break;
                case InstructionTypeSuffix.U1: 
                    result = typeof(byte);
                    break;
                case InstructionTypeSuffix.U2: 
                    result = typeof(ushort);
                    break;
                case InstructionTypeSuffix.U4: 
                    result = typeof(uint);
                    break;
                case InstructionTypeSuffix.U8: 
                    result = typeof(ulong);
                    break;
                case InstructionTypeSuffix.R: 
                    result = typeof(double);
                    break;
                case InstructionTypeSuffix.R4: 
                    result = typeof(float);
                    break;
                case InstructionTypeSuffix.R8: 
                    result = typeof(double);
                    break;
                case InstructionTypeSuffix.REF: 
                    result = typeof(object);
                    break;
                default: 
                    result = Param as Type;
                    break;
            }

            return result;
        }

        #region Debug

        public static string GetHtmlInstructionTable()
        {
            string result = "";

            result += 
                @"
                <TABLE BORDER=""1"" ALIGN=""CENTER"" WIDTH=""100%"" rules=""all"" frame=""box"" cellspacing=""0"" cellpadding=""0"" bordercolor=""#A0A0A0"">
                    <THEAD bgcolor=""#C0C0C0"">
                        <TH style=""border-left: thin"">Simple</TH>
                        <TH style=""border-left: thin"">IL</TH>
                        <TH style=""border-left: thin"">Param</TH>
                    </THEAD>

                    <TBODY>
                ";

            string[] paramTypes = new string[10]
                                                                {
                                                                    "none",
                                                                    "Int32",
                                                                    "Int32",
                                                                    "Int64",
                                                                    "Int32",
                                                                    "Int32",
                                                                    "double",
                                                                    "double",
                                                                    "(reflection object)",
                                                                    "Int32[]"
                                                                };

            for (InstructionCode code = InstructionCode.ADD; code <= InstructionCode.XOR; code++)
            {
                SortedList hash = new SortedList();

                int i;
                string il = "", parms = "";
                for (i = 0; i < instructions.Length; i++)
                    if (instructions[i].code == code && instructions[i].name != "-")
                    {
                        il += instructions[i].name+" ";

                        ParamType pType = instructions[i].pType;
                        string pTypeStr = paramTypes[(int)pType];
                        if (pType != ParamType.pNone && ! hash.ContainsValue(pTypeStr))
                            hash.Add(i,pTypeStr);
                    }

                for (i = 0; i < hash.Count; i++)
                    parms += hash.GetByIndex(i)+" ";

                result += "                        <TR>\n";

                result += @"                            <TD style=""border-left: thin; border-top: thin"" align=""left"">"+code+"</TD>";
                result += @"                            <TD style=""border-left: thin; border-top: thin"" align=""left"">"+il+"</TD>";
                result += @"                            <TD style=""border-left: thin; border-top: thin"" align=""left"">"+parms+"</TD>";

                result += "                        </TR>\n";
            }

            result += 
                @"
                    </TBODY>
                </TABLE>
                ";

            return result;
        }

        #endregion
    }
}
