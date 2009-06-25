
// ===========================================================================
// CILPE - Partial Evaluator for Common Intermediate Language
// ===========================================================================
// File: 
//     EvalStack.cs
//
// Description:
//     Emulation of evaluation stack
//
// Author: 
//     Sergei Skorobogatov (Sergei.Skorobogatov@supercompilers.com)
// Corrector:
//     Yuri Klimov (yuri.klimov@supercompilers.ru)
// ===========================================================================


using System;

namespace CILPE.Exceptions
{
    public abstract class EvalStackException : ApplicationException
    {
        public EvalStackException (string msg) : base("Evaluation stack: " + msg)
        {
        }
    }

    public class EmptyStackException : EvalStackException
    {
        public EmptyStackException () : base("cannot perform operation because stack is empty")
        {
        }
    }

    public class InvalidConstantException : EvalStackException
    {
        public InvalidConstantException () : base("cannot load constant (constant has invalid type)")
        {
        }
    }

    public class InvalidBinaryOpException : EvalStackException
    {
        public InvalidBinaryOpException () : base("invalid combination of binary operation, overflow flag and unsigned flag")
        {
        }
    }

    public class InvalidConvertOpException : EvalStackException
    {
        public InvalidConvertOpException () : base("invalid combination of convert operation, its type, overflow flag and unsigned flag")
        {
        }
    }

    public class InvalidLoadIndirectException : EvalStackException
    {
        public InvalidLoadIndirectException () : base("invalid type in indirect load opearation")
        {
        }
    }

    public class InvalidOperandException : EvalStackException
    {
        public InvalidOperandException () : base("operation failed due to invalid operand type")
        {
        }
    }
}

namespace CILPE.DataModel
{
    using System.Collections;
    using System.Reflection;
    using System.Runtime.Serialization;
    using CILPE.Exceptions;
    using CILPE.ReflectionEx;
    using CILPE.CFG;

    /* Class for passing method parameters */
    public class ParameterValues
    {
        #region Private and internal members

        private MethodBase method;
		private Type[] types;

        private int parmCount;
        private Location[] parms;

        /* Creates new instance of ParameterValues class */
        internal ParameterValues (MethodBase method, Value[] values)
        {
			this.method = method;
            ParameterInfo[] info = method.GetParameters();

            types = new Type[info.Length];
            for (int i = 0; i < info.Length; i++)
                types[i] = info[i].ParameterType;

            parmCount = GetParametersNumber(method);
            parms = new Location[parmCount];
			
			int pos = 0;
			if (! method.IsStatic)
			{
				parms[0] = new Location(method.DeclaringType);
				parms[0].Val = values[0];
				pos = 1;
			}

			for (int i = 0; i < types.Length; i++)
			{
				Value val = values[pos+i];
				parms[pos+i] = new Location(types[i]);
				parms[pos+i].Val = val;
			}
        }

		/* Constructor for cloning */
		private ParameterValues(ParameterValues paramVals)
		{
			method = paramVals.Method;

			types = paramVals.types;
			parmCount = paramVals.parmCount;

			parms = new Location[parmCount];
			for (int i = 0; i < parmCount; i++)
			{
				Value val = paramVals.parms[i].Val;
				parms[i] = new Location(paramVals.parms[i].Type);
				parms[i].Val = val.MakeCopy();
			}
		}

        #endregion

        /* Returns the method */
        public MethodBase Method { get { return method; } }

        /* Returns the number of parameters */
        public int Count { get { return parmCount; } }

        /* Returns parameter by index */
        public Location this [int index] { get { return parms[index]; } }

		/* Clones ParameterValues object */
		public ParameterValues Clone() { return new ParameterValues(this); }

		/* Looks for virtual method by type of _this_ pointer */
		public void ChooseVirtualMethod()
		{
			if (method.IsVirtual)
			{
				Value thisValue = parms[0].Val;

				if (! (thisValue is ObjectReferenceValue))
					throw new InvalidOperandException();

				ChooseVirtualMethod(thisValue.Type);
			}
		}

		/* Looks for virtual method by given exact type */
		public void ChooseVirtualMethod(Type exactType)
		{
			if (method.IsVirtual)
			{
				if (! method.DeclaringType.IsAssignableFrom(exactType))
					throw new InvalidOperandException();

				string methodName = method.Name;

				method = null;
				while (method == null)
				{
					method = exactType.GetMethod(methodName,
						(BindingFlags)(BindingFlags.DeclaredOnly | BindingFlags.Instance |
						BindingFlags.Public | BindingFlags.NonPublic),
						null, types, null);

					if (method == null)
						exactType = exactType.BaseType;
				}
			}
		}

        /* Invokes method */
        public Value Invoke(out Exception exc)
        {
            exc = null;

            if (method.IsConstructor && method.DeclaringType.IsArray)
                return parms[0].Val;

            object obj = null;
            int pos = 0;

            if (! Method.IsStatic)
            {
                pos = 1;
                Value objVal = parms[0].Val;

                if (objVal is ObjectReferenceValue)
                    obj = (objVal as ObjectReferenceValue).Obj;
                else if (objVal is PointerValue)
                    obj = (objVal as PointerValue).GetReferencedObject();
                else if (objVal is NullValue)
                    obj = null;
                else
                    throw new InvalidOperandException();
            }
            else
            {
                pos = 0;
                obj = null;
            }

            object[] parameters = new object[Count-pos];
            for (int i = pos; i < Count; i++)
            {
                object p = this[i].Val.ToParameter();

                if (types[i-pos] == typeof(int) && p.GetType() == typeof(IntPtr))
                    parameters[i-pos] = (int)(IntPtr)p;
                else
                    parameters[i-pos] = p;
            }

            object retVal = null;
            try
            {
                retVal = method.Invoke(obj,parameters);
            }
            catch (Exception e)
            {
                exc = e.InnerException;
            }

            Value result = null;

            if (exc == null && ! Method.IsConstructor)
            {
                Type retType = (Method as MethodInfo).ReturnType;

                if (retType != typeof(void))
                {
                    if (retType.IsValueType)
                        result = new StructValue(retVal as ValueType);
                    else if (retVal != null)
                        result = new ObjectReferenceValue(retVal);
                    else
                        result = new NullValue();
                }
            }

            return result;
        }

        /* Returns the number of values that have to be popped from stack
         * as parameters to call the specified method
         */
        public static int GetParametersNumber(MethodBase method)
        {
            int result = method.GetParameters().Length;
            if (! method.IsStatic)
                result++;

            return result;
        }
    }

    /* Class emulating evaluation stack */
    public class EvaluationStack: IFormattable
    {
        #region Private and internal members

        /* Internal representation of the stack */
        private ArrayList stack;

        /* Inserts a value at the top of the stack
         * (without conversion of primitive data)
         */
        private void push(Value val) { stack.Add(val); }

        /* Categories of binary operations */
        private enum OpCategory
        {
            NumericOp     = 0, /* add, div, mul, rem, sub */
            ComparisonOp  = 1, /* ceq, cgt, cgt.un, clt, clt.un */
            IntegerOp     = 2, /* and, div.un, or, rem.un, xor */
            ShiftOp       = 3, /* shl, shr, shr_un */
            OverflowOp    = 4, /* add.ovf, add.ovf.un, mul.ovf, mul.ovf.un, sub.ovf, sub.ovf.un */
            InvalidOp     = 5  /* invalid operation */
        }

        /* Returns the category of binary operation */
        private OpCategory getCategory(BinaryOp.ArithOp op, bool overflow, bool unsigned)
        {
            OpCategory category = OpCategory.InvalidOp;

            if (! overflow)
            {
                if ((op == BinaryOp.ArithOp.ADD ||
                    op == BinaryOp.ArithOp.DIV ||
                    op == BinaryOp.ArithOp.MUL ||
                    op == BinaryOp.ArithOp.REM ||
                    op == BinaryOp.ArithOp.SUB) &&
                    ! unsigned)
                {
                    category = OpCategory.NumericOp;
                }
                else if (op == BinaryOp.ArithOp.CEQ && ! unsigned ||
                    op == BinaryOp.ArithOp.CGT ||
                    op == BinaryOp.ArithOp.CLT)
                {
                    category = OpCategory.ComparisonOp;
                }
                else if (op == BinaryOp.ArithOp.AND && ! unsigned ||
                    op == BinaryOp.ArithOp.DIV && unsigned ||
                    op == BinaryOp.ArithOp.OR && ! unsigned ||
                    op == BinaryOp.ArithOp.REM && unsigned ||
                    op == BinaryOp.ArithOp.XOR && ! unsigned)
                {
                    category = OpCategory.IntegerOp;
                }
                else if (op == BinaryOp.ArithOp.SHL && ! unsigned ||
                    op == BinaryOp.ArithOp.SHR)
                {
                    category = OpCategory.ShiftOp;
                }
            }
            else if (op == BinaryOp.ArithOp.ADD ||
                op == BinaryOp.ArithOp.MUL ||
                op == BinaryOp.ArithOp.SUB)
            {
                category = OpCategory.OverflowOp;
            }

            return category;
        }

        int popArrayIndex()
        {
            Value indexVal = Pop();
            if (! (indexVal is StructValue && (indexVal as StructValue).IsPrimitive))
                throw new InvalidOperandException();

            ValueType Obj = (indexVal as StructValue).Obj;
            int index = 0;

            if (indexVal.Type == typeof(int))
                index = (int)Obj;
            else if (indexVal.Type == typeof(IntPtr))
                index = (int)(IntPtr)Obj;
            else
                throw new InvalidOperandException();

            return index;
        }

        Array popArray(out Exception exc)
        {
            Array array = null;
            exc = null;

            Value arrayVal = Pop();
            if (arrayVal is NullValue)
                exc = new NullReferenceException();
            else
            {
                if (! (arrayVal is ObjectReferenceValue))
                    throw new InvalidOperandException();

                if (! ((arrayVal as ObjectReferenceValue).Obj is Array))
                    throw new InvalidOperandException();

                array = (arrayVal as ObjectReferenceValue).Obj as Array;
            }

            return array;
        }

        private bool compareTypes(StructValue.TypeIndex type1, StructValue.TypeIndex type2)
        {
            if (type1 == StructValue.TypeIndex.UNATIVEINT && 
                type2 == StructValue.TypeIndex.NATIVEINT)
                type2 = StructValue.TypeIndex.UNATIVEINT;

            if (type1 == StructValue.TypeIndex.CHAR &&
                type2 == StructValue.TypeIndex.UINT16)
                type2 = StructValue.TypeIndex.CHAR;

            if (type1 == StructValue.TypeIndex.BOOL &&
                type2 == StructValue.TypeIndex.INT8)
                type2 = StructValue.TypeIndex.BOOL;

            return type1 == type2;
        }

        #endregion

        // ================================================================
        // General members
        // ----------------------------------------------------------------

        /* Creates new instance of EvaluationStack class */
        public EvaluationStack()
        {
            stack = new ArrayList();
        }

        /* Gets the number of values contained in the stack */
        public int Count { get { return stack.Count; } }

        /* Removes all values from the stack */
        public void Clear() { stack.Clear(); }

        /* Returns the value at the top of the stack without removing it */
        public Value this [int depth] 
        {
            get
            {
                if (stack.Count == 0)
                    throw new EmptyStackException();

                return stack[stack.Count-1-depth] as Value; 
            }

            set
            {
                if (stack.Count == 0)
                    throw new EmptyStackException();

                stack[stack.Count-1-depth] = value.ToStack();
            }
        }

        /* Removes and returns the value at the top of the stack */
        public Value Pop()
        {
            if (stack.Count == 0)
                throw new EmptyStackException();

            Value result = this[0];
            stack.RemoveAt(stack.Count-1);
            return result; 
        }

        public void RemoveAt (int depth)
        {
            stack.RemoveAt(stack.Count-1-depth);
        }

        /* Inserts a value at the top of the stack */
        public void Push(Value val) { push(val.ToStack()); }

        public override string ToString()
        {
            return ToString("CSharp",ReflectionFormatter.formatter);
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            string result = "";

            foreach (Value val in stack)
                result += ", " + val.ToString(format,formatProvider);

            if (result.Length == 0)
                result = "EvaluationStack: []";
            else
                result = "EvaluationStack: ["+result.Remove(0,2)+"]";

            return result;
        }

        // ================================================================
        // Methods in subsequent section correspond to CFG nodes
        // ----------------------------------------------------------------

        /* Duplicates stack top 
         * (corresponds to CILPE.CFG.DuplicateStackTop class)
         */
        public void Perform_DuplicateStackTop()
        {
            Value top = this[0];
            push(top.MakeCopy());
        }

        /* Removes stack top 
         * (corresponds to CILPE.CFG.RemoveStackTop class)
         */
        public void Perform_RemoveStackTop() { Pop(); }

        /* Loads constant value (primitive type, string, run-time handler for 
         * reflection or null) on top of the stack
         * (corresponds to CILPE.CFG.LoadConst class)
         */
        public void Perform_LoadConst(object obj)
        {
            if (obj == null)
                push(new NullValue());
            else if (obj is string)
                push(new ObjectReferenceValue(obj));
            else
            {
                Type type = obj.GetType();

                if (type == typeof(Int32) ||
                    type == typeof(Int64) ||
                    type == typeof(Double) ||
                    type == typeof(RuntimeTypeHandle) || 
                    type == typeof(RuntimeMethodHandle) ||
                    type == typeof(RuntimeFieldHandle))
                    push(new StructValue(obj as ValueType));
                else
                    throw new InvalidConstantException();
            }
        }

        /* Performs unary operation on the value on top of the stack
         * (corresponds to CILPE.CFG.UnaryOp class)
         */
        public void Perform_UnaryOp(UnaryOp.ArithOp op)
        {
            Value val = Pop();
            ValueType res = null;
            
            if (val is StructValue && (val as StructValue).IsPrimitive)
            {
                StructValue strVal = val as StructValue;

                ValueType obj = strVal.Obj;
                StructValue.TypeIndex typeIndex = strVal.getTypeIndex();

                if (! DataModelUtils.Unary((int)op,obj,(int)typeIndex,out res))
                    throw new InvalidOperandException();
            }
            else
                throw new InvalidOperandException();
            
            push(new StructValue(res));
        }

        /* Performs binary operation on two values on top of the stack
         * (corresponds to CILPE.CFG.BinaryOp class)
         */
        public void Perform_BinaryOp(BinaryOp.ArithOp op, bool overflow, bool unsigned,
            out Exception exc)
        {
            exc = null;

            /* Determining category of binary operation */
            OpCategory category = getCategory(op,overflow,unsigned);
            if (category == OpCategory.InvalidOp)
                throw new InvalidBinaryOpException();

            /* Getting operands from stack */
            Value val1, val2;
            val2 = Pop();
            val1 = Pop();

            /* Performing binary operation */
            object res = null;

            if (val1 is StructValue && val2 is StructValue)
            {
                StructValue.TypeIndex
                    typeA = (val1 as StructValue).getTypeIndex(),
                    typeB = (val2 as StructValue).getTypeIndex();

                if (typeA == StructValue.TypeIndex.INVALID ||
                    typeB == StructValue.TypeIndex.INVALID)
                    throw new InvalidOperandException();

                ValueType 
                    a = (val1 as StructValue).Obj,
                    b = (val2 as StructValue).Obj;

                int operandsKind = 0;
                bool success = true;
                
                if (category == OpCategory.ShiftOp) /* shl, shr, shr_un */
                {
                    if (typeA == StructValue.TypeIndex.FLOAT64 ||
                        typeB == StructValue.TypeIndex.FLOAT64 ||
                        typeB == StructValue.TypeIndex.INT64)
                        throw new InvalidOperandException();

                    operandsKind = ((typeB == StructValue.TypeIndex.INT32) ? 0 : 3) + (int)typeA;
                    DataModelUtils.ShiftOp((int)op,unsigned,a,b,operandsKind,out res);
                }
                else
                {
                    if (typeA == typeB)
                        operandsKind = (int)typeA;
                    else if (typeA == StructValue.TypeIndex.INT32 && typeB == StructValue.TypeIndex.NATIVEINT)
                        operandsKind = 4;
                    else if (typeA == StructValue.TypeIndex.NATIVEINT && typeB == StructValue.TypeIndex.INT32)
                        operandsKind = 5;
                    else
                        throw new InvalidOperandException();

                    switch (category)
                    {
                        case OpCategory.NumericOp: /* add, div, mul, rem, sub */
                            success = DataModelUtils.NumericOp((int)op,a,b,operandsKind,out res);
                            break;

                        case OpCategory.ComparisonOp: /* ceq, cgt, cgt.un, clt, clt.un */
                            DataModelUtils.ComparisonOp((int)op,unsigned,a,b,operandsKind,out res);
                            break;

                        case OpCategory.IntegerOp: /* and, div.un, or, rem.un, xor */
                        case OpCategory.OverflowOp: /* add.ovf, add.ovf.un, mul.ovf, mul.ovf.un, sub.ovf, sub.ovf.un */
                            if (operandsKind == 3)
                                throw new InvalidOperandException();

                            success = DataModelUtils.IntOvfOp((int)op,unsigned,a,b,operandsKind,out res);
                            break;
                    }
                }

                if (! success)
                {
                    exc = res as Exception;
                    res = null;
                }
            }
            else if (op == BinaryOp.ArithOp.CEQ)
            {
                if ((val1 is NullValue || val1 is ObjectReferenceValue) && 
                    (val2 is NullValue || val2 is ObjectReferenceValue) ||
                    (val1 is NullValue || val1 is PointerValue) && 
                    (val2 is NullValue || val2 is PointerValue))
                    res = Equals(val1, val2) ? (Int32)1 : (Int32)0;
                else
                    throw new InvalidOperandException();
            }
            else if (op == BinaryOp.ArithOp.CGT)
            {
                if (val1 is ObjectReferenceValue && val2 is NullValue && unsigned)
                    res = (Int32)1;
                else if (val1 is NullValue && val2 is NullValue)
                    res = (Int32)0;
                else if (val1 is PointerValue && val2 is NullValue)
                    res = (Int32)1;
                else
                    throw new InvalidOperandException();
            }
            else if (op == BinaryOp.ArithOp.CLT)
            {
                if (val1 is NullValue && val2 is ObjectReferenceValue && unsigned)
                    res = (Int32)1;
                else if (val1 is NullValue && val2 is NullValue)
                    res = (Int32)0;
                else if (val1 is NullValue && val2 is PointerValue)
                    res = (Int32)1;
                else
                    throw new InvalidOperandException();
            }
            else
                throw new InvalidOperandException();

            if (res != null)
                push(new StructValue(res as ValueType));
        }

        /* Performs conversion of primitive value on top of the stack
         * (corresponds to CILPE.CFG.ConvertValue class)
         */
        public void Perform_ConvertValue(Type type, bool overflow, bool unsigned,
            out Exception exc)
        {
            exc = null;
            Value val = Pop();

            if (! (val is StructValue && (val as StructValue).IsPrimitive))
                throw new InvalidOperandException();

            StructValue primVal = val as StructValue;
            StructValue.TypeIndex typeIndex = StructValue.getTypeIndex(type),
                valTypeIndex = primVal.getTypeIndex();

            if (! overflow && unsigned && typeIndex != StructValue.TypeIndex.FLOAT64 ||
                overflow && typeIndex == StructValue.TypeIndex.FLOAT32 ||
                overflow && typeIndex == StructValue.TypeIndex.FLOAT64)
                throw new InvalidConvertOpException();
            
            object res;
            bool success = DataModelUtils.Convert(
                (int)typeIndex,overflow,unsigned,
                primVal.Obj,(int)valTypeIndex,
                out res
                );

            if (! success)
                exc = res as Exception;
            else
                push(new StructValue(res as ValueType));
        }

        /* Performs casting of object to a class
         * (corresponds to CILPE.CFG.CastClass class)
         */
        public void Perform_CastClass(Type type, bool throwException, out Exception exc)
        {
            exc = null;
            Value val = Pop();

            if (! (val is ObjectReferenceValue))
                throw new InvalidOperandException();

            if (type.IsAssignableFrom(val.Type))
                push(val);
            else if (throwException)
                exc = new InvalidCastException();
            else
                push(new NullValue());
        }

        /* Performs check for a finite real number
         * (corresponds to CILPE.CFG.CheckFinite class)
         */
        public void Perform_CheckFinite(out Exception exc)
        {
            exc = null;
            Value val = this[0];

            if (! (val is StructValue && (val as StructValue).IsPrimitive))
                throw new InvalidOperandException();

            StructValue primVal = val as StructValue;
            if (! (primVal.Obj is double))
                throw new InvalidOperandException();

            double number = (double)(primVal.Obj);
            if (Double.IsInfinity(number) || Double.IsNaN(number))
                exc = new ArithmeticException();
        }

        /* Performs indirect loading of value
         * (corresponds to CILPE.CFG.LoadIndirect class)
         */
        public void Perform_LoadIndirect(Type type, out Exception exc)
        {
            exc = null;
            Value val = Pop();

            if (val is NullValue)
                exc = new NullReferenceException();
            else
            {
                if (! (val is PointerValue))
                    throw new InvalidOperandException();

                object obj = (val as PointerValue).GetReferencedObject();

                if (type == typeof(object))
                    Push(new ObjectReferenceValue(obj));
                else
                {
                    StructValue.TypeIndex typeIndex = StructValue.getTypeIndex(type);

                    if (typeIndex != StructValue.TypeIndex.INVALID)
                    {
                        StructValue.TypeIndex objTypeIndex = StructValue.getTypeIndex(obj.GetType());
                        
                        if (! compareTypes(objTypeIndex,typeIndex))
                            throw new InvalidOperandException();

                        Push(new StructValue(obj as ValueType));
                    }
                    else if (type.IsValueType)
                    {
                        if (type != obj.GetType())
                            throw new InvalidOperandException();

                        Push(new StructValue(obj as ValueType));
                    }
                    else
                        throw new InvalidLoadIndirectException();
                }
            }
        }

        /* Performs indirect storing of value
         * (corresponds to CILPE.CFG.StoreIndirect class)
         */
        public void Perform_StoreIndirect(Type type, out Exception exc)
        {
            exc = null;
            Value val = Pop();
            Value addrVal = Pop();

            if (addrVal is NullValue)
                exc = new NullReferenceException();
            else
            {
                if (! (addrVal is PointerValue))
                    throw new InvalidOperandException();

                (addrVal as PointerValue).SetReferencedValue(val);
            }
        }

        /* Performs loading of array element
         * (corresponds to CILPE.CFG.LoadElement class)
         */
        public void Perform_LoadElement(Type type,out Exception exc)
        {
            int index = popArrayIndex();
            Array array = popArray(out exc);

            if (exc == null)
            {
                if (type != typeof(object))
                {
                    StructValue.TypeIndex typeIndex = StructValue.getTypeIndex(type);
                    StructValue.TypeIndex arrTypeIndex = 
                        StructValue.getTypeIndex(array.GetType().GetElementType());

                    if (! compareTypes(arrTypeIndex,typeIndex))
                        exc = new ArrayTypeMismatchException();
                }

                if (exc == null)
                {
                    object elem = null;
                    try
                    {
                        elem = array.GetValue(index);
                    }
                    catch (IndexOutOfRangeException e)
                    {
                        exc = e;
                    }

                    if (exc == null)
                    {
                        if (StructValue.getTypeIndex(type) != StructValue.TypeIndex.INVALID)
                            Push(new StructValue(elem as ValueType));
                        else
                        {
                            if (elem != null)
                                Push(new ObjectReferenceValue(elem));
                            else
                                Push(new NullValue());
                        }
                    }
                }
            }
        }

        /* Performs loading of address of array element
         * (corresponds to CILPE.CFG.LoadElementAddr class)
         */
        public void Perform_LoadElementAddr(Type type,out Exception exc)
        {
            int index = popArrayIndex();
            Array array = popArray(out exc);

            if (exc == null)
            {
                if (array.GetType().GetElementType() != type)
                    exc = new ArrayTypeMismatchException();
                else
                {
                    if (index >= 0 && index < array.Length)
                        Push(new PointerToElementValue(array,index));
                    else
                        exc = new IndexOutOfRangeException();
                }
            }
        }

        /* Performs storing to array element
         * (corresponds to CILPE.CFG.StoreElement class)
         */
        public void Perform_StoreElement(Type type,out Exception exc)
        {
            Value objVal = Pop();
            int index = popArrayIndex();
            Array array = popArray(out exc);
            Type elementType = array.GetType().GetElementType();

            if (exc == null)
            {
                objVal = objVal.FromStack(elementType);
                object val = null;

                if (objVal is StructValue && (objVal as StructValue).IsPrimitive)
                    val = (objVal as StructValue).Obj;
                else if (objVal is NullValue)
                    val = null;
                else if (objVal is ObjectReferenceValue)
                    val = (objVal as ObjectReferenceValue).Obj;
                else
                    throw new InvalidOperandException();

                if (type != typeof(object) && TypeFixer.WeakFixType(elementType) != TypeFixer.WeakFixType(type)) //Andrew!
                    exc = new ArrayTypeMismatchException();
                else if (type == typeof(object) &&
                    ! elementType.IsAssignableFrom(val.GetType()))
                    exc = new InvalidCastException();
                else
                {
                    try
                    {
                        array.SetValue(val,index);
                    }
                    catch (IndexOutOfRangeException e)
                    {
                        exc = e;
                    }
                }
            }
        }

        /* Performs loading of array length
         * (corresponds to CILPE.CFG.LoadLength class)
         */
        public void Perform_LoadLength(out Exception exc)
        {
            Array array = popArray(out exc);

            if (exc == null)
            {
                UIntPtr length = (UIntPtr)(array.Length);
                Push(new StructValue(length));
            }
        }

        /* Performs creating of array
         * (corresponds to CILPE.CFG.NewArray class)
         */
        public void Perform_NewArray(Type type, out Exception exc)
        {
            exc = null;

            int numElems = popArrayIndex();
            if (numElems < 0)
                exc = new OverflowException();
            else
            {
                Array array = Array.CreateInstance(type,numElems);
                push(new ObjectReferenceValue(array));
            }
        }

        /* Performs loading from field
         * (corresponds to CILPE.CFG.LoadField class)
         */
        public void Perform_LoadField(FieldInfo field, out Exception exc)
        {
            exc = null;
            object fld = null;

            if (field.IsStatic)
                fld = field.GetValue(null);
            else
            {
                Value objVal = Pop();

                if (objVal is NullValue)
                    exc = new NullReferenceException();
                else
                {
                    object obj = null;

                    if (objVal is ObjectReferenceValue)
                        obj = (objVal as ObjectReferenceValue).Obj;
                    else if (objVal is PointerValue)
                        obj = (objVal as PointerValue).GetReferencedObject();
                    else if (objVal is StructValue)
                        obj = (objVal as StructValue).Obj;
                    else
                        throw new InvalidOperandException();

                    try
                    {
                        fld = field.GetValue(obj);
                    }
                    catch (ArgumentException)
                    {
                        exc = new MissingFieldException();
                    }
                }
            }

            if (exc == null)
            {
                Type type = field.FieldType;
                if (type.IsValueType)
                    Push(new StructValue(fld as ValueType));
                else if (fld == null)
                    Push(new NullValue());
                else
                    Push(new ObjectReferenceValue(fld));
            }
        }

        /* Performs loading of address of field
         * (corresponds to CILPE.CFG.LoadFieldAddr class)
         */
        public void Perform_LoadFieldAddr(FieldInfo field, out Exception exc)
        {
            exc = null;

            if (! field.IsStatic)
            {
                Value objVal = Pop();

                if (objVal is NullValue)
                    exc = new NullReferenceException();
                else
                {
                    object obj = null;

                    if (objVal is ObjectReferenceValue)
                        obj = (objVal as ObjectReferenceValue).Obj;
                    else if (objVal is PointerValue)
                        obj = (objVal as PointerValue).GetReferencedObject();
                    else
                        throw new InvalidOperandException();

                    try 
                    { 
                        field.GetValue(obj);
                    }
                    catch (ArgumentException)
                    {
                        exc = new MissingFieldException();
                    }

                    if (exc == null)
                    {
                        if (objVal is ObjectReferenceValue)
                            push(new PointerToObjectFieldValue(obj,field));
                        else
                            push(new PointerToStructFieldValue(objVal as PointerValue,field));
                    }
                }
            }
            else
                push(new PointerToObjectFieldValue(field));
        }

        /* Performs storing to field
         * (corresponds to CILPE.CFG.StoreField class)
         */
        public void Perform_StoreField(FieldInfo field, out Exception exc)
        {
            exc = null;
            Value fldVal = Pop().FromStack(field.FieldType);

            object val;
            if (fldVal is StructValue)
                val = (fldVal as StructValue).Obj;
            else if (fldVal is NullValue)
                val = null;
            else if (fldVal is ObjectReferenceValue)
                val = (fldVal as ObjectReferenceValue).Obj;
            else
                throw new InvalidOperandException();

            Value objVal = null;
            object obj = null;
            if (! field.IsStatic)
            {
                objVal = Pop();

                if (objVal is ObjectReferenceValue)
                    obj = (objVal as ObjectReferenceValue).Obj;
                else if (objVal is PointerValue)
                    obj = (objVal as PointerValue).GetReferencedObject();
                else if (objVal is NullValue)
                    exc = new NullReferenceException();
                else
                    throw new InvalidOperandException();
            }

            if (exc == null)
            {
                try
                {
                    field.SetValue(obj, val);
                }
                catch (ArgumentException)
                {
                    exc = new MissingFieldException();
                }

                if (objVal is PointerValue && obj.GetType().IsValueType)
                    (objVal as PointerValue).SetReferencedValue(new StructValue(obj as ValueType));
            }
        }

        /* Performs boxing of value
         * (corresponds to CILPE.CFG.BoxValue class)
         */
        public void Perform_BoxValue(Type type)
        {
            Value val = Pop().FromStack(type);
            object obj = null;

            if (val is StructValue)
            {
                obj = (val as StructValue).Obj;

                if (obj.GetType() != type)
                    throw new InvalidOperandException();
            }
            else
                throw new InvalidOperandException();

            push(new ObjectReferenceValue(obj));
        }

        /* Performs unboxing of value
         * (corresponds to CILPE.CFG.UnboxValue class)
         */
        public void Perform_UnboxValue(Type type, out Exception exc)
        {
            exc = null;

            Value val = Pop();
            if (val is NullValue)
                exc = new NullReferenceException();
            else if (! (val is ObjectReferenceValue))
                throw new InvalidOperandException();

            if (exc == null)
            {
                object obj = (val as ObjectReferenceValue).Obj;
                
                Type objType = obj.GetType();
                if (objType.IsEnum)
                    objType = StructValue.getEnumType(objType);
                
                if (objType != type)
                    exc = new InvalidCastException();
                else
                    push(new PointerToUnboxedValue(obj));
            }
        }

		/* Performs initializing of value
		 * (corresponds to CILPE.CFG.InitValue class)
		 */
		public void Perform_InitValue(Type type)
		{
			Value val = Pop();

			if (! (val is PointerValue))
				throw new InvalidOperandException();

			PointerValue ptr = val as PointerValue;
			object obj = ptr.GetReferencedObject();

			if (obj.GetType() != type)
				throw new InvalidOperandException();

			ptr.SetZeroValue();
		}

        /* Decides whether to do conditional branch
         * (correcponds to CILPE.CFG.Branch class)
         */
        public bool Perform_Branch()
        {
            bool branchFlag = false;
            Value val = Pop();

            if (val is StructValue)
            {
                object obj = (val as StructValue).Obj;

                if (obj is Int32)
                    branchFlag = (Int32)obj != 0;
                else if (obj is Int64)
                    branchFlag = (Int64)obj != 0;
                else if (obj is IntPtr)
                    branchFlag = (Int64)(IntPtr)obj != 0;
                else
                    throw new InvalidOperandException();
            }
            else if (val is ObjectReferenceValue || val is PointerValue)
                branchFlag = true;
            else if (! (val is NullValue))
                throw new InvalidOperandException();

            return branchFlag;
        }

        /* Chooses the branch target of swicth instruction.
         * Returns index of branch target or -1.
         * (correcponds to CILPE.CFG.Switch class)
         */
        public int Perform_Switch(int targetNum)
        {
            long index;
            Value val = Pop();

            if (! (val is StructValue))
                throw new InvalidOperandException();
            
            object obj = (val as StructValue).Obj;

            if (obj is Int32)
                index = (long)(Int32)obj;
            else if (obj is Int64)
                index = (long)obj;
            else if (obj is IntPtr)
                index = (long)(IntPtr)obj;
            else
                throw new InvalidOperandException();

            if (index < 0 || index >= targetNum)
                index = -1;

            return (int)index;
        }

        /* Returns exception object
         * (correcponds to CILPE.CFG.Throw class)
         */
        public void Perform_Throw(out Exception obj, out Exception exc)
        {
            obj = null;
            exc = null;
            Value val = Pop();

            if (val is NullValue)
                exc = new NullReferenceException();
            else if (val is ObjectReferenceValue)
                obj = (val as ObjectReferenceValue).Obj as Exception;

            if (exc == null && obj == null)
                throw new InvalidOperandException();
        }

        /* Pops method parameters from the stack
         * (corresponds to CILPE.CFG.CallMethod class)
         */
        public ParameterValues Perform_CallMethod
            (MethodBase method, bool isVirtualCall)
        {
            int count = ParameterValues.GetParametersNumber(method);
            Value[] values = new Value[count];

            for (int i = 0; i < count; i++)
                values[count-1-i] = Pop();

            return new ParameterValues(method,values);
        }

        /* Performs object creation
         * (corresponds to CILPE.CFG.CreateObject class)
         */
        public ParameterValues Perform_CreateObject(ConstructorInfo ctor)
        {
            Type type = ctor.DeclaringType;

            int count = ParameterValues.GetParametersNumber(ctor);
            Value[] values = new Value[count];
            for (int i = 0; i < count-1; i++)
                values[count-1-i] = Pop();

            object obj;
            if (type.IsArray)
            {
                Type elementType = type.GetElementType();
                int rank = type.GetArrayRank();
                int[] lengths = new int[rank];
                for (int i = 0; i < rank; i++)
                    lengths[i] = (int)((values[i+1] as StructValue).Obj);

                obj = Array.CreateInstance(elementType,lengths);
            }
            else
                obj = FormatterServices.GetUninitializedObject(type);

            Value resultValue = null;
            if (type.IsValueType)
            {
                resultValue = new StructValue(obj as ValueType);
                values[0] = new PointerToUnboxedValue(obj);
            }
            else
                values[0] = resultValue = new ObjectReferenceValue(obj);

            push(resultValue);

            return new ParameterValues(ctor,values);
        }

        public ParameterValues Perform_CreateObject(ConstructorInfo ctor, Value val)
        {
            int count = ParameterValues.GetParametersNumber(ctor);
            Value[] values = new Value[count];

            values[0] = val;
            for (int i = 0; i < count-1; i++)
                values[count-1-i] = Pop();

            return new ParameterValues(ctor,values);
        }
    }
}
