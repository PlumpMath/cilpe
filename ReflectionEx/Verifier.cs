
// ===========================================================================
// CILPE - Partial Evaluator for Common Intermediate Language
// ===========================================================================
// File: 
//     Verifier.cs
//
// Description:
//     Verifier for 'Simple' code model
//
// Author: 
//     Andrei Mishchenko
// ===========================================================================

using System;
using System.Collections;
using System.Reflection;

namespace CILPE.ReflectionEx
{
	public class VerifierException : Exception
	{
		public  VerifierException()
		{
			//Place a breakpoint here
		}
	}

	public struct TypeEx
	{
		public Type type;
		public bool boxed;

		public TypeEx(Type type,bool boxed)
		{
			this.type = type;
			this.boxed = IsBoxableType(type) ? boxed : false;
		}

		public TypeEx(Type type)
			//:	this(type,false)
		{
			this.type = type;
			this.boxed = false;
		}

		public override bool Equals(object o)
		{
			if(!(o is TypeEx))
				return(false);
			TypeEx t = (TypeEx)o;
			if(t.boxed != boxed)
				return(false);
			if(Equals(t.type, type))
				return(true);
			//Patching MS bug with ModuleBuilder...
			if( t.type != null && 
				type != null && 
				t.type.IsByRef && 
				type.IsByRef && 
				Equals(t.type.GetElementType(), type.GetElementType())
			)
				return(true);
			if( t.type != null &&
			    type != null &&
				t.type.IsArray && 
				type.IsArray && 
				new TypeEx(t.type.GetElementType()).Equals(new TypeEx(type.GetElementType()))
			)
			    return(true);
			return(false);
		}

		public override int GetHashCode()
		{
			if(type == null)
				return(0);
			int x = type.GetHashCode();
			if(boxed)
				x = ~x;
			return(x);
		}

		static private bool IsBoxableType(Type type)
		{
			return( type != null  &&  
				(type.IsValueType || type.IsEnum) ); 
		}

		public bool IsBoxable
		{
			get 
			{
				return(IsBoxableType(type));
			}//this property determins if the field `boxed` has sence.
		} 

		static public Type BuildRefType(Type type)
		{
			string typeName = type.FullName; 
			typeName+= "&"; //Andrew: I'm going crazy!!! Couldn't find an easier way :((
			Type t = type.Module.GetType(typeName); 
			if(t == null)
				throw new VerifierException(); 
			return(t); 
		}
		
		public TypeEx BuildRefType()
		{
			return(new TypeEx(BuildRefType(this.type)));
		}
		
		static public Type BuildArrayType(Type type)
		{
			string typeName = type.FullName;
			typeName+= "[]"; 
			Type t = type.Module.GetType(typeName);
			if(t == null)
				throw new VerifierException();
			return(t); 
		}

		public TypeEx BuildArrayType()
		{
			return(new TypeEx(BuildArrayType(this.type)));
		}
	}

	public class MethodInfoExtention
	{
		protected MethodBase method;
		private Type[] parameters;
		private bool isVirtCall; //the Type of `this` parameter depends on this flag for value-types!

		private void Init(MethodBase method, bool isVirtCall, Type[] parameters)
		{
			this.method = method;
			this.isVirtCall = isVirtCall;
			this.parameters = parameters;
		}

		static private Type[] GetParameters(MethodBase method)
		{
			ParameterInfo[] parameters = method.GetParameters();
			Type[] result = new Type[parameters.Length];
			int I = result.Length;
			for(int i=0; i<I; i++)
				result[i] = parameters[i].ParameterType;
			return(result);
		}

		public MethodInfoExtention(MethodBase method)
		{
			Init(method, false, GetParameters(method));
		}

		public MethodInfoExtention(MethodBase method , bool isVirtCall)
		{
			Init(method, isVirtCall, GetParameters(method));
		}

		public MethodInfoExtention(MethodBase method , bool isVirtCall, Type[] parameters)
		{
			Init(method, isVirtCall, parameters);
		}

		public MethodInfoExtention Clone()
		{
			return(new MethodInfoExtention(method, isVirtCall, parameters));
		}

		public void Assign(MethodBase method, bool isVirtCall)
		{
			Init(method, isVirtCall, GetParameters(method));
		}

		public MethodBase Method
		{
			get{ return(method); }
		}

		public Type[] Params
		{
			get{ return(parameters); }
		}

		public bool IsVirtCall
		{
			get{ return(isVirtCall); }
		}

		public TypeEx GetArgType(int index)
		{
			if (!method.IsStatic)
				index--;

			if(index == -1)
				if(DeclaringType.IsBoxable)
					if(isVirtCall)  
						return(new TypeEx(  method.DeclaringType , true  ));
					else
						return(new TypeEx(  TypeEx.BuildRefType(method.DeclaringType) , false  ));
				else
					return(new TypeEx(method.DeclaringType));
			else
				return(new TypeEx(parameters[index]));
		}

		public int ArgCount 
		{
			get 
			{
				int count = parameters.Length;
				if(!method.IsStatic)
					count++; //`this`
				return(count);
			}
		}

		public TypeEx GetReturnType()
		{
			if (method is MethodInfo)
				return(new TypeEx((method as MethodInfo).ReturnType));
			else //ConstructorInfo
				return(new TypeEx(typeof(void))); //Andrew: constructors don't have a return type.
		}

		public TypeEx DeclaringType
		{
			get
			{
				return(new TypeEx(method.DeclaringType));
			}
		}
	}

	public class TypeFixer
	{
		public static bool IsInt32OrCompatible(TypeEx T)
		{
			if(T.boxed)
				return(false);
			Type t = T.type;
			if(t == null)
				return(false);
			if(t.IsEnum)
				if(t.UnderlyingSystemType.IsEnum)
					return(true);
				else
					return(IsInt32OrCompatible(new TypeEx(t.UnderlyingSystemType)));
			return
				(
				t.Equals(typeof(byte))   || 
				t.Equals(typeof(sbyte))  || 
				t.Equals(typeof(short))  ||
				t.Equals(typeof(ushort)) ||
				t.Equals(typeof(int))    ||
				t.Equals(typeof(uint))   ||
				t.Equals(typeof(bool))   ||
				t.Equals(typeof(char))  
				);

		}

		public static bool IsInt64OrCompatible(TypeEx T)
		{
			if(T.boxed)
				return(false);
			Type t = T.type;
			if(t == null)
				return(false);
			if(t.IsEnum)
				if(t.UnderlyingSystemType.IsEnum) //supposed int32
					return(false);
				else
					return(IsInt64OrCompatible(new TypeEx(t.UnderlyingSystemType)));
			return
				(
				t.Equals(typeof(long))   || 
				t.Equals(typeof(ulong))  
				);
		}

		public static bool IsIntPtrOrCompatible(TypeEx T)
		{
			if(T.boxed)
				return(false);
			Type t = T.type;
			if(t == null)
				return(false);
			return
				(
				t.Equals(typeof(IntPtr))   || 
				t.Equals(typeof(UIntPtr))  
				);
		}


		public static bool IsFloatOrCompatible(TypeEx T)
		{
			if(T.boxed)
				return(false);
			Type t = T.type;
			if(t == null)
				return(false);
			return
				(
				t.Equals(typeof(float))   || 
				t.Equals(typeof(double))  
				);
		}

		public static TypeEx WeakFixType(TypeEx T)
		{
			if(T.boxed)
				return(T);
			return(new TypeEx(WeakFixType(T.type)));
		}

		public static Type WeakFixType(Type t)
		{
			if(t.Equals(typeof(char)) || t.Equals(typeof(ushort)))
				return(typeof(short));
			if(t.Equals(typeof(uint)) || t.IsEnum)
				return(typeof(int));
			if(t.Equals(typeof(ulong)))
				return(typeof(long));
			if(t.Equals(typeof(byte)) || t.Equals(typeof(bool)))
				return(typeof(sbyte));
			if(t.Equals(typeof(UIntPtr)))
				return(typeof(IntPtr));
			return(t);
		}

		public static TypeEx FixType(TypeEx t)
		{
			if(IsInt32OrCompatible(t))
				return(new TypeEx(typeof(int)));
			if(IsInt64OrCompatible(t))
				return(new TypeEx(typeof(long)));
			if(IsIntPtrOrCompatible(t))
				return(new TypeEx(typeof(IntPtr)));//typeof(int)? 
			//PEVerify doesn't make difference between "int32" and "native int"...
			if(IsFloatOrCompatible(t))
				return(new TypeEx(typeof(double)));
			return(t);
		}

		//the same as FixType, but drops `int` into `IntPtr`
		public static TypeEx StrongFixType(TypeEx t)
		{
			if(IsInt32OrCompatible(t))
				return(new TypeEx(typeof(IntPtr)));
			if(IsInt64OrCompatible(t))
				return(new TypeEx(typeof(long)));
			if(IsIntPtrOrCompatible(t))
				return(new TypeEx(typeof(IntPtr)));
			if(IsFloatOrCompatible(t))
				return(new TypeEx(typeof(double)));
			return(t);
		}
	}
    
	public class Verifier
	{

		public static bool IsStackMoreGeneral(StackTypes s,StackTypes t)
		{
			if (s.Count != t.Count)
				return(false); //different lengths

			for (int i = 0; i < s.Count; i++)
				if(! TypeChecker.IsAssignable(s[i],t[i]))
					return(false);
			return(true);
		}

		private static void CheckStacks(StackTypes s,StackTypes t)
		{
			if(! IsStackMoreGeneral(s,t))
				throw new VerifierException();
		}

		public static StackTypes DoMergeStacks(StackTypes s,StackTypes t)
		{
			//assert(s!=null && t!=null)
			if (s.Count != t.Count)
				throw new VerifierException(); //different lengths
			StackTypes u = new StackTypes();
			for (int i = 0; i < s.Count; i++)
			{
				u.Push(TypeChecker.MergeTypes(s[i],t[i]));
			}
			return(u);
		}
           
		private static StackTypes MergeStacks(StackTypes s,StackTypes t)
		{
			if(s == null)
				if(t == null)
					//both null
					return new StackTypes(); //Andrew: we assume the stack to be empty in this case 
				else 
					//t != null
					return(t);  
			else
				if(t == null) 
				//s != null
				return(s);
			else 
				//both != null
				return DoMergeStacks(s,t);
		}
 
		private class Arithmetics : TypeFixer
		{

			public static TypeEx GetReturnType(TypeEx t1,TypeEx t2,bool isFloatOperation)
			{
				if(IsInt32OrCompatible(t1) && IsInt32OrCompatible(t2))
					return(new TypeEx(typeof(int)));
				if(IsInt64OrCompatible(t1) && IsInt64OrCompatible(t2))
					return(new TypeEx(typeof(long)));
				if((IsIntPtrOrCompatible(t1) || IsInt32OrCompatible(t1)) && 
					 (IsIntPtrOrCompatible(t2) || IsInt32OrCompatible(t2)))
					return(new TypeEx(typeof(IntPtr)));
				if(isFloatOperation)
					if(IsFloatOrCompatible(t1) && IsFloatOrCompatible(t2))
						return(new TypeEx(typeof(double)));
        
				throw new VerifierException(); //incompatible operands  
			}

			public static void CheckTypes(TypeEx t1,TypeEx t2) //for CLT CLE BEQ ...
			{ 
				if((!t1.IsBoxable || t1.boxed) && (!t2.IsBoxable || t2.boxed))
					return;//substracting object references is allowed
				if(!t1.boxed && !t2.boxed)
					GetReturnType(t1,t2,true);//Check for arguments compatibility (both Int32 || both Int64 || both IntPtr || both float )
				else
					throw new VerifierException(); // one value is boxed and the other one is not.
			}
		}
      
		private class TypeChecker : TypeFixer
		{


			
			public static void CheckAssignment(TypeEx T1,TypeEx T2)
			{
				if(! IsAssignable(T1,T2))
					throw new VerifierException();
			}

			public static bool IsAssignable(TypeEx T1,TypeEx T2)
			{
				T1 = TypeFixer.StrongFixType(T1);
				T2 = TypeFixer.StrongFixType(T2);//Int32 is assignable from IntPtr and vice verce
				if(T1.Equals(T2))
					return(true);
				Type t1 = T1.type;
				Type t2 = T2.type;
				//Checks if t1 is more general than t2
				if(t1 == null)
					if(t2 == null)
					  return(true);
			    else
						return(false);
				if(t2 == null) //this means that t2 is actually a null reference => of any object type
					if(T1.IsBoxable && !T1.boxed)
						return(false);
					else
						return(true);
				//Non-boxed values must be exactly equal when assigned
				if(T1.IsBoxable && !T1.boxed  ||  T2.IsBoxable && !T2.boxed)
					return(false); 
				//Patching ModuleBuilder fucking bugs :((
				if(T1.type.Equals(typeof(Enum)) && T2.type.IsEnum) 
					return(true);
				if(T1.type.Equals(typeof(Array)) && T2.type.IsArray) 
					return(true);

				if(!T1.type.IsAssignableFrom(T2.type))//Andrew: don't know how it actually works :)  
					return(false);
				return(true);
			}

			private static ArrayList GetParents(TypeEx T)
			{
				Type t = T.type;
				if(!t.IsClass && !T.boxed)
					throw new VerifierException();
				ArrayList ps = new ArrayList();
				for(; t!=null ; t=t.BaseType)
					ps.Add(t);
				ps.Reverse();
				return(ps);
			}

			private static TypeEx DoMergeClasses(TypeEx t1,TypeEx t2)
			{
				ArrayList ps1 = GetParents(t1); //t1 parents      
				ArrayList ps2 = GetParents(t2); //t2 parents
				int i = 1; //ps1[0] == ps2[0] == typeof(object)
				for(; i<ps1.Count && i<ps2.Count ;i++)
					if(! Equals(ps1[i],ps2[i]))
						break;
				return(new TypeEx(ps1[i-1] as Type , true));
			}

			private static Type[] GetInterfaces(TypeEx T)
			{
				Type t = T.type;
				if(t.IsClass || T.boxed)
					return(t.GetInterfaces());
				if(! t.IsInterface)
					throw new VerifierException();
				//t.IsInterface
				Type[] _ifaces_ = t.GetInterfaces(); 
				Type[] ifaces = new Type[_ifaces_.Length + 1];
				ifaces[0] = t;
				_ifaces_.CopyTo(ifaces,1);
				return(ifaces);
			}

			private static TypeEx FindCommonInterface(Type[] ifaces1, Type[] ifaces2)
			{
				for(int i1=0;i1<ifaces1.Length;i1++)
					for(int i2=0;i2<ifaces2.Length;i2++)
						if(Equals(ifaces1[i1],ifaces2[i2]))
							return(new TypeEx(ifaces1[i1]));
				throw new VerifierException();
			}

			private static TypeEx DoMergeInterfaces(TypeEx t1, TypeEx t2)
			{
				return(FindCommonInterface( GetInterfaces(t1), GetInterfaces(t2) )); 
			}

			private static TypeEx DoMergeTypes(TypeEx T1,TypeEx T2)
			{
				//returns t that is more general than t1, t2
				T1 = FixType(T1);
				T2 = FixType(T2);
				if(T1.Equals(T2))
					return(T1);

				//in the sequel T1 != T2
				Type t1 = T1.type; 
				Type t2 = T2.type;
				if(T1.IsBoxable && !T1.boxed)   
				{
					if(T2.boxed)
						throw new VerifierException();
					//mb Int32 and IntPtr ?
					if( (t1.Equals(typeof(int)) || t1.Equals(typeof(IntPtr))) &&
				 	    (t2.Equals(typeof(int)) || t2.Equals(typeof(IntPtr))) )
					    return(new TypeEx(typeof(IntPtr)));
					else
                        throw new VerifierException();
                } 
				if(t1.IsByRef) //Should be exactly equal
					throw new VerifierException();
				if(t1.IsArray && t2.IsArray)
				{
					Type t1Elem = t1.GetElementType();
					Type t2Elem = t2.GetElementType();
					if(t1Elem.IsValueType || t2Elem.IsValueType)
						return(new TypeEx(typeof(Array)));
					TypeEx T = DoMergeTypes(new TypeEx(t1Elem) , new TypeEx(t2Elem));
					return(T.BuildArrayType());  //Andrew!! may fail when working with ModuleBuilder
				}
				if(t1.IsInterface || t2.IsInterface)
					return(DoMergeInterfaces(T1,T2));
				return(DoMergeClasses(T1,T2));
			}

			public static TypeEx MergeTypes(TypeEx T1,TypeEx T2)
			{ 
				Type t1 = T1.type;
				Type t2 = T2.type;
				if(t1 == null && t2 == null)
					return(new TypeEx(null));
				if(t2 == null)
					if(T1.IsBoxable && !T1.boxed)
						throw new VerifierException();
					else
						return(T1);
				if(t1 == null)
					if(T2.IsBoxable && !T2.boxed)
						throw new VerifierException();
					else
						return(T2);
				TypeEx T = DoMergeTypes(T1,T2);
				if(T.type == null)
					throw new VerifierException();
				return(T);
			}

			public static void CheckBrTrueFalseType(TypeEx T)
			{
				Type t = T.type;
				if(t.IsByRef || t.IsEnum || t.IsPrimitive)
					return; //ok
				if(T.type.IsValueType && !T.boxed)
					throw new VerifierException();
			}
		}

		static private bool IsFloatOperation(Instruction i)
		{
			return(IsFloatOperation(i.Code,i.UnsignedFlag,i.OverflowFlag));
		}
    
		static public bool IsFloatOperation(InstructionCode code, bool un, bool ovf)
		{
			switch(code)
			{
				case InstructionCode.SUB: 
				case InstructionCode.ADD: 
					return(! ovf); //ovf flag is only allowed in integer arithmetics
				case InstructionCode.XOR:
				case InstructionCode.OR:
				case InstructionCode.AND:
					return(false);
				case InstructionCode.MUL:
				case InstructionCode.DIV: 
				case InstructionCode.REM: 
					return(! un &&  ! ovf); 
					//`div.un` is not a float operation, 
					// though simply `div` is.
					//`rem` is a float operation according to documentation!
				default:
					throw new VerifierException(); //impossible case 
			}
		}

		private static void ProcessBranch(int iNum,int INum,MethodEx method,StackTypes stack)
		{
			Instruction I = method[INum];
			if(INum > iNum)
				I.SetStack(MergeStacks(I.Stack,stack));
			else 
				CheckStacks(I.Stack,stack);
		}

		static private void CheckSameBlock(IEnumerable clauses, int iNum1, int iNum2)
		{
			BlockStruct b1,b2,b3;
			b1 = GetNearestBlock(clauses,iNum1);
			b2 = GetNearestBlock(clauses,iNum2);
			if(! b1.Equals(b2))
			{
				//the branch on the very beginning of a block is allowed
				//ZLP
				b3 = GetNearestBlock(clauses,iNum2-1);
				if(! b1.Equals(b3))
				  throw new VerifierException();
			}
		}

		private class EHClausesArrayWithOneClauseRemoved /*:)*/ : IEnumerable
		{
			private IEnumerable clauses;
			private EHClause removed;

			public EHClausesArrayWithOneClauseRemoved(IEnumerable clauses, EHClause removed)
			{
				this.clauses = clauses; this.removed = removed;
			}

			public IEnumerator GetEnumerator() { return new Enumerator(clauses,removed); }

			private class Enumerator : IEnumerator
			{
				private IEnumerable clauses;
				private EHClause removed;
				private IEnumerator enumerator;
				public Enumerator(IEnumerable clauses, EHClause removed)
				{
					this.clauses = clauses; this.removed = removed;
					enumerator = clauses.GetEnumerator();
				}

				public void Reset() {  enumerator.Reset();  }
          
				public object Current { get{ return( enumerator.Current ) ; } }

				public bool MoveNext()
				{
					bool b = enumerator.MoveNext();
					if(!b) return(false);
					if(Current == removed)
						return(MoveNext());
					return(true);
				}

			}
		}

		static private void CheckCanLeave(IEnumerable clauses, int iNum1, int iNum2)
		{
			BlockStruct b1,b2;
			EHClause clause;
			GetNearestBlock(clauses,iNum1,out clause);
			b2 = GetNearestBlock(clauses,iNum2);
			b1 = GetNearestBlock(new EHClausesArrayWithOneClauseRemoved(clauses,clause), iNum2);
			if(! b1.Equals(b2))
				throw new VerifierException();
		}

		private static void ProcessBr(int iNum,MethodEx method,StackTypes stack)
		{
			CheckSameBlock(method.EHClauses, iNum, (int)method[iNum].Param);
			ProcessBranch(iNum, (int)method[iNum].Param, method, stack);
		}

		public static void ProcessBrTrueFalse(StackTypes stack)
		{
			TypeEx t = stack.Pop();
			TypeChecker.CheckBrTrueFalseType(t);
		}

		private static void ProcessSwitch(int iNum,MethodEx method,StackTypes stack)
		{
			int[] INums = (int[])method[iNum].Param;
			for(int i = 0;i<INums.Length;i++)
			{
				CheckSameBlock(method.EHClauses, iNum, INums[i]);
				ProcessBranch(iNum,INums[i],method,stack);
			}
		}

		public static void ProcessSwitch(StackTypes stack)
		{
			TypeEx t = stack.Pop();
			if(!TypeFixer.IsInt32OrCompatible(t))
				throw new VerifierException();
		}

		public static void ProcessLeave(StackTypes stack)
		{
			if(stack.Count != 0)
				throw new VerifierException();
		}
		
		private static void ProcessLeave(int iNum,MethodEx method,StackTypes stack)
		{
			CheckCanLeave(method.EHClauses, iNum, (int)method[iNum].Param);
			ProcessLeave(stack);
			ProcessBranch(iNum,(int)method[iNum].Param,method,stack);
		}

		public static void ProcessRet(TypeEx returnType, StackTypes stack)
		{
			if(!returnType.type.Equals(typeof(void))) 
			{
				TypeEx t = stack.Pop();
				TypeChecker.CheckAssignment(returnType, t);
			}
			if(stack.Count != 0)
				throw new VerifierException();
		}

		public static void ProcessThrow(StackTypes stack)
		{
			TypeEx t = stack.Pop();
			TypeChecker.CheckAssignment(new TypeEx(typeof(Exception)), t);
		}

		public static void ProcessCallOrNewobj(MethodInfoExtention method,StackTypes stack, bool isNewObj)
		{
			for(int i = method.ArgCount-1; i >= (isNewObj ? 1 : 0); i--) 
			{ //when we are creating a new object `this` is not on stack
				TypeEx sourceType = stack.Pop();
				TypeEx targetType = method.GetArgType(i);
				TypeChecker.CheckAssignment(targetType, sourceType);
			}
			TypeEx returnType = method.GetReturnType();
			if(isNewObj)
				stack.Push(method.DeclaringType);
				//Wow! Value-types can be created on stack with NEWOBJ instruction -- not boxed
			else if(!returnType.type.Equals(typeof(void)))
				stack.Push(returnType);
		}

		public static void ProcessNewArr(StackTypes stack, Type T)
		{
			TypeEx t = stack.Pop();
			if(!TypeFixer.IsInt32OrCompatible(t) && !TypeFixer.IsIntPtrOrCompatible(t))
				throw new VerifierException();
			stack.Push( TypeEx.BuildArrayType(T) );
		}

		public static void ProcessLdFld(StackTypes stack, FieldInfo field, bool loadAddress)
		{
			TypeEx obj = stack.Pop();
			if(obj.type != null) //LDNULL LDFLD -- crazy!
			{
				if(obj.type.IsByRef)  //LDFLD to the structure accepts this structure address 
				{
					if(! field.DeclaringType.Equals(obj.type.GetElementType()) || !field.DeclaringType.IsValueType )
						throw new VerifierException();
				}
				else
					TypeChecker.CheckAssignment(new TypeEx(field.DeclaringType),obj);
			}
			Type fieldType = field.FieldType;
			if(loadAddress)
				fieldType = TypeEx.BuildRefType(fieldType);
			stack.Push(fieldType);
		}

		static public void ProcessLdInd(Type T, StackTypes stack)   
		{
			TypeEx addr = stack.Pop();
			if(!addr.type.IsByRef)
				throw new VerifierException();
			TypeEx sourceType = new TypeEx(addr.type.GetElementType());
			TypeEx targetType = new TypeEx(T);
			if(targetType.type.Equals(typeof(object))) //.ref suffix
				stack.Push(sourceType);
			else
			{
				TypeChecker.CheckAssignment(targetType,sourceType);
				stack.Push(targetType);
			}
		}

		public static void ProcessSt(TypeEx T, StackTypes stack)
		{
			TypeEx t = stack.Pop();
			TypeChecker.CheckAssignment(T,t);
		}

		public static void ProcessStFld(StackTypes stack, FieldInfo field)
		{
			TypeEx fldValue = stack.Pop(); 
			TypeEx obj = stack.Pop();
			if(obj.type.IsByRef)  //STFLD accepts both objects and managed pointers on value-types
			{
				if(! field.DeclaringType.Equals(obj.type.GetElementType()) || !field.DeclaringType.IsValueType )
					throw new VerifierException();
			}
			else
			  TypeChecker.CheckAssignment(new TypeEx(field.DeclaringType),obj);
			TypeChecker.CheckAssignment(new TypeEx(field.FieldType),fldValue);
		}

		public static void ProcessStInd(Type T, StackTypes stack)
		{
			TypeEx val = stack.Pop();
			TypeEx addr = stack.Pop();
			if(!addr.type.IsByRef)
				throw new VerifierException();
			TypeEx targetType = new TypeEx(addr.type.GetElementType());
			TypeEx sourceType = new TypeEx(T);
			TypeChecker.CheckAssignment(targetType,val);
			CheckPrimitiveIndirectAssignment(targetType,sourceType);
		}

		private static void CheckPrimitiveIndirectAssignment(TypeEx T1,TypeEx T2) 
		{
			T1 = TypeFixer.WeakFixType(T1);
			T2 = TypeFixer.WeakFixType(T2);
			Type t1 = T1.type;
			Type t2 = T2.type;
			if(t1.IsPrimitive && !t1.Equals(t2))
				throw new VerifierException();
		}

		private static TypeEx ProcessLdStElem(TypeEx array, TypeEx desiredArrayElem, TypeEx index)
		{
			if(!array.type.IsArray)
				throw new VerifierException();
			if(!(TypeFixer.IsInt32OrCompatible(index) || TypeFixer.IsIntPtrOrCompatible(index)))
				throw new VerifierException();
			TypeEx arrayElem = new TypeEx(array.type.GetElementType());
			CheckPrimitiveIndirectAssignment(arrayElem,desiredArrayElem); 
			return(arrayElem);
		}

		public static void ProcessLdElem(StackTypes stack,TypeEx desiredArrayElem,bool loadAddress)
		{
			TypeEx index = stack.Pop();
			TypeEx array = stack.Pop();
			TypeEx arrayElem = ProcessLdStElem(array,desiredArrayElem,index);
			if(loadAddress)
				arrayElem = arrayElem.BuildRefType();
			stack.Push(arrayElem);
		}

		public static void ProcessStElem(StackTypes stack, TypeEx desiredArrayElem)
		{
			TypeEx elemValue = stack.Pop();
			TypeEx index = stack.Pop();
			TypeEx array = stack.Pop();
			TypeEx arrayElem = ProcessLdStElem(array,desiredArrayElem,index);
			TypeChecker.CheckAssignment(arrayElem,elemValue);       
		}

		public static void ProcessLdLen(StackTypes stack)
		{
			TypeEx array = stack.Pop();
			if(!array.type.IsArray)
				throw new VerifierException();
			stack.Push(typeof(int));
		}

		public static void ProcessEndFilter(StackTypes stack)
		{
			TypeEx t = stack.Pop();
			if(!TypeFixer.IsInt32OrCompatible(t))
				throw new VerifierException();
			if(stack.Count != 0)
				throw new VerifierException();
		}

		public static void ProcessCastClass(StackTypes stack, TypeEx t)
		{
			TypeEx T = stack.Pop();
			if(T.IsBoxable && !T.boxed)
				throw new VerifierException();
			stack.Push(t);
		}

		public static void ProcessBox(StackTypes stack, Type T)
		{
			TypeEx valueType = stack.Pop();
			if(! valueType.IsBoxable || valueType.boxed)
				throw new VerifierException();
			TypeChecker.CheckAssignment(new TypeEx(T), valueType);
			
			// Skor >> this code was wrong !
			//stack.Push(new TypeEx(valueType.type ,   true   ));
			stack.Push(new TypeEx(T, true));
		}

		public static void ProcessUnBox(StackTypes stack, Type T)
		{
			TypeEx objType = stack.Pop();
			Type valueType = T;
			if(! ((objType.IsBoxable && objType.boxed) || !objType.IsBoxable) )
				throw new VerifierException();
			if(! valueType.IsValueType )
				throw new VerifierException();
			stack.Push( TypeEx.BuildRefType(valueType) );
		}

	

		public static void ProcessNot(StackTypes stack)
		{
			TypeEx t = stack.Pop();
			if(!t.type.IsPrimitive || t.boxed || TypeFixer.IsFloatOrCompatible(t))
				throw new VerifierException();
			stack.Push(TypeFixer.FixType(t));
		}

		public static void ProcessNeg(StackTypes stack)
		{
			TypeEx t = stack.Pop();
			if(!t.type.IsPrimitive || t.boxed)
				throw new VerifierException();
			stack.Push(TypeFixer.FixType(t));
		}

		public enum OpType{ FloatOrInt, Int, Shift, Compare }

		public static void ProcessBinOp(OpType opType, StackTypes stack)
		{
			switch(opType)
			{
				case OpType.Shift:
				{
					TypeEx shift = stack.Pop();
					TypeEx t = stack.Pop();
					if(!(TypeFixer.IsInt32OrCompatible(shift) || TypeFixer.IsIntPtrOrCompatible(shift)))
						throw new VerifierException();
					if(!(TypeFixer.IsInt32OrCompatible(shift) || 
						   TypeFixer.IsIntPtrOrCompatible(shift) || 
						   TypeFixer.IsInt64OrCompatible(shift)))
						throw new VerifierException();
					stack.Push(t);
				} break;
				case OpType.FloatOrInt:
				case OpType.Int:
				{
					TypeEx t1 = stack.Pop();
					TypeEx t2 = stack.Pop();
					TypeEx t3 = Arithmetics.GetReturnType(t1,t2,opType == OpType.FloatOrInt);
					stack.Push(t3);
				} break;
				case OpType.Compare:
				{
					TypeEx t1 = stack.Pop();
					TypeEx t2 = stack.Pop();
					Arithmetics.CheckTypes(t1,t2);
					stack.Push(typeof(int));
				} break;
			}
		}

		static public void ProcessCpObj(StackTypes stack, Type t)
		{
			TypeEx addr1 = stack.Pop();
			TypeEx addr2 = stack.Pop();
			if(!addr1.type.IsByRef || !addr2.type.IsByRef)
				throw new VerifierException();
				if(!t.IsValueType)
					throw new VerifierException();
			if(!t.Equals(addr1.type.GetElementType()) || !t.Equals(addr2.type.GetElementType()))
				throw new VerifierException();
		}

		static public void ProcessInitObj(StackTypes stack, Type t)
		{
			if(!t.IsValueType)
				throw new VerifierException(); 
			TypeEx tRef = stack.Pop();
			if(!tRef.type.IsByRef)
				throw new VerifierException();
			if(!t.Equals(tRef.type.GetElementType()))
				throw new VerifierException();
		}

		static public void ProcessLdObj(StackTypes stack, Type t)
		{
			TypeEx addr = stack.Pop();
			if(!addr.type.IsByRef)
				throw new VerifierException();
				if(!t.IsValueType)
					throw new VerifierException();
			if(!t.Equals(addr.type.GetElementType()))
				throw new VerifierException();
			stack.Push(t);
		}

		static public void ProcessStObj(StackTypes stack, Type t)
		{
			TypeEx valObj = stack.Pop();
			TypeEx addr = stack.Pop();
			if(!addr.type.IsByRef)
				throw new VerifierException();
			if(!t.IsValueType)
					throw new VerifierException();
			if(!t.Equals(valObj.type) || !t.Equals(addr.type.GetElementType()))
				throw new VerifierException();
		}

		static public void ProcessSizeOf(StackTypes stack, Type t)
		{
			if(!t.IsValueType)
				throw new VerifierException();
			stack.Push(typeof(int));
		}

		static public void ProcessCkFinite(StackTypes stack)
		{
			TypeEx t = stack.Pop();
			if(!TypeFixer.IsFloatOrCompatible(t) || t.boxed )
				throw new VerifierException();
			stack.Push(t);
		}

		static public void ProcessConv(Type T, StackTypes stack)
		{
			TypeEx t = stack.Pop();
			if(!t.type.IsPrimitive || t.boxed )
				throw new VerifierException();
			stack.Push(T);
		}

		static internal void FreeStacks(MethodEx method)
		{
			foreach(Instruction i in method)
				i.SetStack(null);
		}

		static void PushExceptionOnStack(int iNum, StackTypes stack, EHClausesArray clauses)
		{
			foreach(EHClause c in clauses)
			{
				if(c.Kind == EHClauseKind.UserFilteredHandler)
				{
					if(c.FilterStart == iNum)
					{
						if(stack.Count != 0) 
							throw new VerifierException();
						stack.Push(new TypeEx(typeof(object)));
						return;
					}
				}
				if(c.HandlerStart == iNum)
				{
					if(stack.Count != 0) 
						throw new VerifierException();
					switch(c.Kind)
					{
						case EHClauseKind.FaultHandler:
						case EHClauseKind.UserFilteredHandler:
						{
							stack.Push(new TypeEx(typeof(object)));
						} break;
						case EHClauseKind.TypeFilteredHandler:
						{
							stack.Push(new TypeEx(c.ClassObject));
						} break;
					}
					return;
				}
			}
		}

		private static bool IsTryExit(InstructionCode code)
		{
			return(code == InstructionCode.LEAVE || code == InstructionCode.THROW);
		}

		private static bool IsCatchExit(InstructionCode code)
		{
			return(code == InstructionCode.LEAVE || code == InstructionCode.THROW || code == InstructionCode.RETHROW);
		}

		private static bool IsFinallyExit(InstructionCode code)
		{
			return(code == InstructionCode.ENDFINALLY);
		}

		private static bool IsFilterExit(InstructionCode code)
		{
			return(code == InstructionCode.ENDFILTER);
		}

		private static void CheckBlockExits(MethodEx methodRepr)
		{
			foreach(EHClause c in methodRepr.EHClauses)
			{
				int handlerEnd = c.HandlerStart + c.HandlerLength - 1;
				int tryEnd = c.TryStart + c.TryLength - 1;
        
				if(!IsTryExit(methodRepr[tryEnd].Code))
					throw new VerifierException();
				if(c.Kind == EHClauseKind.FinallyHandler ? !IsFinallyExit(methodRepr[handlerEnd].Code) :
					!IsCatchExit(methodRepr[handlerEnd].Code) )
					throw new VerifierException();
				if(c.Kind == EHClauseKind.UserFilteredHandler)
					if(!IsFilterExit(methodRepr[c.HandlerStart - 1].Code))
						throw new VerifierException();
			}
		}

		private enum BlockType
		{
			Try,Catch,Finally,Filter,Global
		};

		private struct BlockStruct
		{

			public BlockType type;
			public int start;
			public int end;

			public BlockStruct(BlockType type,int start, int end)
			{
				this.type = type; this.start = start; this.end = end;
			}

		}

		private static bool DecreaseBlock(ref int start, ref int end, int iNum, int blockStart, int blockLength)
		{
			int blockEnd = blockStart + blockLength;
			if(iNum < blockStart)
				return(false);
			if(iNum >= blockEnd)
				return(false);
			if(start <= blockStart && end >= blockEnd)
			{  
				start = blockStart;
				end = blockEnd;
				return(true);
			}
			if(start >= blockStart && end <= blockEnd) 
				return(false);
			throw new VerifierException(); //Blocks overlap!
		}

		static public int FindNextHandler(int iNum,EHClausesArray clauses)
		{
			int nearest = 1<<30;
			foreach(EHClause c in clauses)
			{
				if(c.HandlerStart > iNum && c.HandlerStart < nearest)
					nearest = c.HandlerStart;
				if(c.Kind == EHClauseKind.UserFilteredHandler && c.FilterStart > iNum && c.FilterStart < nearest)
					nearest = c.FilterStart;
			}
			return nearest;
		}

		private static BlockStruct GetNearestBlock(IEnumerable clauses, int iNum)
		{
			EHClause _not_used_;
			return(GetNearestBlock(clauses,iNum,out _not_used_));
		}

		private static BlockStruct GetNearestBlock(IEnumerable clauses, int iNum, out EHClause clause)
		{
			int start = -1;
			int end = 1<<30;
			BlockType type = BlockType.Global;

			clause = null;
			foreach(EHClause c in clauses)
			{
				if(DecreaseBlock(ref start,ref end,iNum,c.TryStart,c.TryLength))
				{
					clause = c;
					type = BlockType.Try;
				}
				if(DecreaseBlock(ref start,ref end,iNum,c.HandlerStart,c.HandlerLength))
				{
					clause = c;
					if(c.Kind == EHClauseKind.FinallyHandler)
						type = BlockType.Finally;
					else
						type = BlockType.Catch;
				}
				if(c.Kind == EHClauseKind.UserFilteredHandler)
					if(DecreaseBlock(ref start,ref end,iNum,c.FilterStart,c.HandlerStart - c.FilterStart))
					{
						clause = c;
						type = BlockType.Filter;
					}
			}
			return(new BlockStruct(type,start,end));
		}

		static private bool SameSignatures(MethodInfo m1, MethodInfo m2)
		{
		  if(! m1.ReturnType.Equals(m2.ReturnType))
			  return(false);
			ParameterInfo [] p1 = m1.GetParameters();
 			ParameterInfo [] p2 = m2.GetParameters();
			if(p1.Length != p2.Length)
				return(false);
			for(int i=0;i<p1.Length;i++)
			{
        if(!p1[i].ParameterType.Equals(p2[i].ParameterType))
					return(false);
			  if(p1[i].IsOut != p2[i].IsOut)
					return(false);
			}
			return(true);
		}

		static public bool IsInstanceDispatch(MethodEx body,int iNum)
		{
			Instruction i1 = body[iNum-1];
			Instruction i2 = body[iNum];
			if(i2.Code != InstructionCode.NEWOBJ)
				 return(false);
			if(i1.Code != InstructionCode.LDFTN)
				return(false);
			return(true);
		}

		static public bool IsVirtualDispatch(MethodEx body,int iNum)
		{
			Instruction i0 = body[iNum-2];
			Instruction i1 = body[iNum-1];
			Instruction i2 = body[iNum];
			if(i2.Code != InstructionCode.NEWOBJ)
				return(false);
			if(i1.Code != InstructionCode.LDVIRTFTN)
				return(false);
			if(i0.Code != InstructionCode.DUP)
				return(false);
			return(true);
		}

		static public bool IsDelegate(Type t)
		{
			return(typeof(Delegate).Equals(t.BaseType) || typeof(MulticastDelegate).Equals(t.BaseType));
		}

		static private void ProcessDelegateConstruction(MethodEx method,int iNum,StackTypes stack)
		{
			if(!(IsInstanceDispatch(method,iNum) || IsVirtualDispatch(method,iNum)))
			  throw new VerifierException();
			TypeEx intPtr = stack.Pop();
			MethodInfo ftn = method[iNum-1].Param as MethodInfo;
			ConstructorInfo ctor = method[iNum].Param as ConstructorInfo;
			ProcessDelegateConstruction(stack,ftn,ctor);

		}

		static public void ProcessDelegateConstruction(StackTypes stack, MethodInfo ftn, ConstructorInfo ctor)
		{
			TypeEx obj = stack.Pop();
			TypeChecker.CheckAssignment(new TypeEx(ftn.DeclaringType , true), obj);
			Type delegateType = ctor.DeclaringType;
			if(!IsDelegate(delegateType))
				throw new VerifierException();
			MethodInfo prototype = delegateType.GetMethod("Invoke");
			if(!SameSignatures(prototype, ftn))
				throw new VerifierException();
			stack.Push(delegateType);
		}

		static public void ProcessStSFld(StackTypes stack, FieldInfo field)
		{
			TypeEx fldValue = stack.Pop(); 
			TypeChecker.CheckAssignment(new TypeEx(field.FieldType),fldValue);
		}

		internal static bool Check(MethodEx methodRepr)
		{
			//Attention: the `null` value on stack means a null reference that is of any object type 
			//As `typeof(object)` is the most general Type, so `null` is the most exact object type 
			FreeStacks(methodRepr);
			try
			{
				Instruction lastI = methodRepr[methodRepr.Count-1]; 
				if(    lastI.Code != InstructionCode.RET 
					&& lastI.Code != InstructionCode.BR
					&& lastI.Code != InstructionCode.THROW
					&& lastI.Code != InstructionCode.RETHROW
					&& lastI.Code != InstructionCode.LEAVE
					&& lastI.Code != InstructionCode.ENDFINALLY
					&& lastI.Code != InstructionCode.ENDFILTER )
						throw new VerifierException();
				MethodInfoExtention method = new MethodInfoExtention(methodRepr.Method);
				StackTypes stack = new StackTypes(); //initially the stack is empty
				int nextHandler = FindNextHandler(-1,methodRepr.EHClauses);
				CheckBlockExits(methodRepr);
				for (int iNum = 0; iNum < methodRepr.Count ; iNum ++)
				{  
					Instruction i = methodRepr[iNum]; 
					i.SetStack(MergeStacks(i.Stack,stack));
					if(nextHandler == iNum)
					{
						PushExceptionOnStack(iNum,i.Stack,methodRepr.EHClauses);
						nextHandler = FindNextHandler(iNum,methodRepr.EHClauses);
					}
					stack = i.Stack.Clone() as StackTypes;  
					switch(i.Code)
					{
						case InstructionCode.DUP :    
						{
							stack.Push(stack.Peek()); 
						} break;
						case InstructionCode.LDARG : 
						{
							stack.Push(method.GetArgType((Int32)i.Param));
						} break;
						case InstructionCode.LDARGA : 
						{
							TypeEx t = method.GetArgType((Int32)i.Param).BuildRefType();
							stack.Push(t);
						} break;
						case InstructionCode.LDLOCA : 
						{
							TypeEx t = new TypeEx(TypeEx.BuildRefType(methodRepr.Locals[(Int32)i.Param]));
							stack.Push(t);
						} break;
						case InstructionCode.LDLOC : 
						{
							stack.Push(new TypeEx(methodRepr.Locals[(Int32)i.Param]));
						} break;
						case InstructionCode.LDIND :
						{
							ProcessLdInd(i.TypeBySuffixOrParam(), stack);
						} break;
						case InstructionCode.LDC:
						{
							stack.Push(new TypeEx(i.TypeBySuffixOrParam()));
						} break;
						case InstructionCode.LDNULL:
						{
							stack.Push(new TypeEx(null));//see `Attention` at the top of the method.
						} break;
						case InstructionCode.LDFLD:
						{
							ProcessLdFld(stack, i.Param as FieldInfo,false);
						} break;
						case InstructionCode.LDFLDA:
						{
							ProcessLdFld(stack, i.Param as FieldInfo,true);
						} break;
						case InstructionCode.LDSFLD:
						{
							stack.Push(new TypeEx((i.Param as FieldInfo).FieldType)); 
						} break;
						case InstructionCode.LDSFLDA:
						{
							stack.Push(TypeEx.BuildRefType((i.Param as FieldInfo).FieldType)); 
						} break;
						case InstructionCode.LDELEM:
						{
							ProcessLdElem(stack, new TypeEx(i.TypeBySuffixOrParam()), false);
						} break;
						case InstructionCode.LDELEMA:
						{
							ProcessLdElem(stack, new TypeEx(i.TypeBySuffixOrParam()), true);
						} break;
						case InstructionCode.LDLEN :
						{
							ProcessLdLen(stack);
						} break;
						case InstructionCode.LDOBJ :
						{
							ProcessLdObj(stack, i.Param as Type);
						} break;
						case InstructionCode.LDSTR:
						{
							if(!(i.Param is string)) 
								throw new VerifierException();
							stack.Push(typeof(string));
						} break;
						case InstructionCode.LDFTN:
						{
							stack.Push(new TypeEx(typeof(IntPtr))); 
						} break;
						case InstructionCode.LDVIRTFTN:
						{
							TypeEx obj = stack.Pop();
							MethodInfo methodInfo = i.Param as MethodInfo;
							if(!methodInfo.IsVirtual)
								throw new VerifierException();
							TypeChecker.CheckAssignment(new TypeEx(methodInfo.DeclaringType , true), obj);
							stack.Push(typeof(IntPtr));
						} break;
						case InstructionCode.LDTOKEN:
						{
							if(i.Param is Type)
								stack.Push(typeof(System.RuntimeTypeHandle));
							else if(i.Param is MethodBase)
								stack.Push(typeof(System.RuntimeMethodHandle));
							else if(i.Param is FieldInfo)
								stack.Push(typeof(System.RuntimeFieldHandle));
							else
								throw new VerifierException();
						} break;
						case InstructionCode.SIZEOF :
						{
							ProcessSizeOf(stack,i.Param as Type);
						} break;

						case InstructionCode.CLT: 
						case InstructionCode.CGT:
						case InstructionCode.CEQ:
						{
              ProcessBinOp(OpType.Compare,stack);
						} break;

						case InstructionCode.BLE:
						case InstructionCode.BLT: 
						case InstructionCode.BGE:
						case InstructionCode.BGT:
						case InstructionCode.BEQ:
						case InstructionCode.BNE:
						{
							TypeEx t1 = stack.Pop();
							TypeEx t2 = stack.Pop();
							Arithmetics.CheckTypes(t1,t2);
							ProcessBr(iNum,methodRepr,stack);
							stack = stack.Clone() as StackTypes; 
							//Andrew: mb wrong, we may let equal stacks to be the same object
						} break;
						case InstructionCode.BRTRUE:
						case InstructionCode.BRFALSE:
						{
							ProcessBrTrueFalse(stack);
							ProcessBr(iNum,methodRepr,stack);
							stack = stack.Clone() as StackTypes; 
							//Andrew: mb wrong, we may let equal stacks to be the same object
						} break;
						case InstructionCode.BR : 
						{
							ProcessBr(iNum,methodRepr,stack);
							stack = null; 
						} break;

						case InstructionCode.SWITCH:
						{
							ProcessSwitch(stack);
							ProcessSwitch(iNum,methodRepr,stack);
							stack = stack.Clone() as StackTypes; 
						} break;
            
						case InstructionCode.THROW :
						{
							ProcessThrow(stack);
							stack = null;
						}break;
            
						case InstructionCode.RETHROW :
						{
							if(GetNearestBlock(methodRepr.EHClauses,iNum).type != BlockType.Catch)
								throw new VerifierException();
							stack = null;
						}break;

						case InstructionCode.LEAVE : 
						{
							BlockType blockType = GetNearestBlock(methodRepr.EHClauses,iNum).type;
							if(blockType != BlockType.Catch && blockType != BlockType.Try)
								throw new VerifierException();
							ProcessLeave(iNum,methodRepr,stack);
							stack = null; 
						} break;

						case InstructionCode.ENDFINALLY : 
						{ 
							if(GetNearestBlock(methodRepr.EHClauses,iNum).type != BlockType.Finally)
								throw new VerifierException();
							ProcessLeave(stack);
							stack = null; 
						} break;

						case InstructionCode.ENDFILTER : 
						{ 
							if(GetNearestBlock(methodRepr.EHClauses,iNum).type != BlockType.Filter)
								throw new VerifierException();
							ProcessEndFilter(stack);
							stack = null; 
						} break;

						case InstructionCode.NOT:
						{
							ProcessNot(stack);
						} break;

						case InstructionCode.NEG:
						{
							ProcessNeg(stack);
						} break;
              
						case InstructionCode.CKFINITE :
						{ 
							ProcessCkFinite(stack);
						} break;

						case InstructionCode.CONV:
						{
							ProcessConv(i.TypeBySuffixOrParam(), stack);
						} break;

						case InstructionCode.SUB: 
						case InstructionCode.ADD: 
						case InstructionCode.MUL: 
						case InstructionCode.DIV: 
						case InstructionCode.REM: 
						case InstructionCode.XOR:
						case InstructionCode.OR:
						case InstructionCode.AND:
						{
							ProcessBinOp(IsFloatOperation(i) ? OpType.FloatOrInt : OpType.Int , stack);
						} break;

						case InstructionCode.SHL:
						case InstructionCode.SHR:
						{
							ProcessBinOp(OpType.Shift , stack);
						} break;

						case InstructionCode.CPOBJ :
						{
							ProcessCpObj(stack, i.Param as Type);
						} break;

						case InstructionCode.STARG : 
						{
							ProcessSt(method.GetArgType((Int32)i.Param) , stack);
						} break;
						case InstructionCode.STLOC : 
						{
							ProcessSt(new TypeEx(methodRepr.Locals[(Int32)i.Param]) , stack);
						} break;
						case InstructionCode.STIND :
						{
							ProcessStInd(i.TypeBySuffixOrParam() , stack);
						} break;
						case InstructionCode.STFLD:
						{
							ProcessStFld(stack, i.Param as FieldInfo);
						} break;
						case InstructionCode.STSFLD:
						{
							ProcessStSFld(stack, i.Param as FieldInfo);
						} break;
						case InstructionCode.STELEM:
						{
							ProcessStElem(stack, new TypeEx(i.TypeBySuffixOrParam()));
						} break;
						case InstructionCode.STOBJ :
						{
							ProcessStObj(stack, i.Param as Type);
						} break;

						case InstructionCode.RET : 
						{
							if(GetNearestBlock(methodRepr.EHClauses,iNum).type != BlockType.Global)
								throw new VerifierException();
							ProcessRet(method.GetReturnType(), stack);
							stack = null;  
						} break;
						case InstructionCode.CALL : 
						case InstructionCode.CALLVIRT :
						case InstructionCode.NEWOBJ :
						{
							//constructor may be invoked using either CALL or NEWOBJ instructions
							MethodBase callee = i.Param as MethodBase; 
							if(i.Code == InstructionCode.NEWOBJ && callee.IsConstructor && IsDelegate(callee.DeclaringType))
								ProcessDelegateConstruction(methodRepr,iNum,stack);
							else
							  ProcessCallOrNewobj(new MethodInfoExtention(callee,i.Code == InstructionCode.CALLVIRT), stack, i.Code == InstructionCode.NEWOBJ);
                            if(i.HasTail && methodRepr[iNum+1].Code != InstructionCode.RET)
								throw new VerifierException();
						} break;
						case InstructionCode.INITOBJ :
						{
							ProcessInitObj(stack, i.Param as Type);
						} break;
						case InstructionCode.NEWARR :
						{
							ProcessNewArr(stack, i.Param as Type);
						} break;
						case InstructionCode.ISINST :
						case InstructionCode.CASTCLASS :
						{
							ProcessCastClass(stack, new TypeEx(i.Param as Type , true));
						} break;

						case InstructionCode.BOX :
						{
							ProcessBox(stack, i.Param as Type);
						} break;

						case InstructionCode.UNBOX :
						{
							ProcessUnBox(stack, i.Param as Type);
						} break;

						case InstructionCode.POP :
						{
							stack.Pop(); 
						} break;

						case InstructionCode.NOP :
						case InstructionCode.BREAK :
							break;

						default: 
						{
							throw new VerifierException();
							//Instruction is not supported yet...
						}
					}  
				}
				return(true);
			}
			catch(VerifierException )
			{ 
				FreeStacks(methodRepr);
				return(false);
			}
			//catch(NullReferenceException ) //Andrew: ZaLyaPa :( waiting for Sergey to patch NEWOBJ (delegate construction)
			//{
			//	FreeStacks(methodRepr);
			//	return(false);
			//}
		}
	}
}

