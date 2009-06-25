
// ===========================================================================
// CILPE - Partial Evaluator for Common Intermediate Language
// ===========================================================================
// File: 
//     Emitter.cs
//
// Description:
//     Control flow graph emission. 
//
// Author: 
//     Andrei Mishchenko
// ===========================================================================



using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;

using CILPE.ReflectionEx;


namespace CILPE.CFG
{
	/// <summary>
	/// Summary description for Emitter.
	/// </summary>
	/// 
	public class EmissionException : Exception
	{
		public EmissionException()
		{} //Place a breakpoint here
	}

	public class Emitter
	{

		private class Labeler
		{
			private ILGenerator generator;
			private Hashtable labels; //Node -> Label   mapping

			private Labeler(ILGenerator generator)
			{
				this.generator = generator;
				labels = new Hashtable();
			}
			
			private void Label(Node node)
			{
				labels[node] = generator.DefineLabel();
			}

			public static Hashtable LabelNodes(MethodBodyBlock graph, ILGenerator generator)
			{
				Labeler labeler = new Labeler(generator);
				ForEachVisitor.ForEach(graph, new ForEachCallback(labeler.Label) );
				return(labeler.labels);
			}
		}

		private class EmitterVisitor : Visitor
		{
			private class Tasks : VisitorTaskCollection
			{
				private Stack stacks;
        private EmitterVisitor visitor;
				
				private class Data 
				{
					public Node node;
					public object data;
					public Data(Node node, object data)
					{
						this.node = node;
						this.data = data;
					}
				}

				public Tasks() 
				{
					stacks = new Stack();
					stacks.Push(new Stack());  
				}

				public void SetVisitor(EmitterVisitor visitor)
				{
					this.visitor = visitor;
				}

				public override void Add(Node node, object data)
				{
					(stacks.Peek() as Stack).Push(new Data(node,data));
				}

				//public void Insert(Node node, object data); if it were Deck ...
				public void Suspend()
				{
					stacks.Push(new Stack());
				}

				public void Burrow(Node node, object data)
				{
					object stack = stacks.Pop();
          Add(node,data);
					stacks.Push(stack);
				}

				public override void Get(out Node node, out object data)
				{
					Stack stack = stacks.Peek() as Stack;
					Data x = stack.Pop() as Data;
					node = x.node;
					data = x.data;
				}

				public void TryResume()
				{
					Stack stack = stacks.Peek() as Stack;
					if(stack.Count == 0)
					{
						stacks.Pop();
						if(stacks.Count != 0)
							visitor.OnResume();
					}
				}

				public override bool IsEmpty 
				{
					get
					{  return(stacks.Count == 0);	}
				}

				public override void Remove(Node node)
				{
					throw new EmissionException();
				}

			}

			private Hashtable labels; //Node -> Label   mapping
			private ILGenerator generator;
			private Hashtable alreadyVisited;
			private Hashtable locals; //Index -> LocalBuilder    mapping
			private ParameterMapper paramMapper;
			private new Tasks tasks;
			private Block currentBlock;
			private Hashtable extraVars; //Type -> ArrayList mapping, ArrayList contains LocalBuilder objects
			private LocalBuilder boolVar;
			private bool wasDumpedFlag;
			private Hashtable wasDumped; //Node -> bool mapping
			private bool prevHasNext;

			public EmitterVisitor(GraphProcessor processor, ILGenerator generator, MethodBodyBlock method) 
				: base(processor, new Tasks())
			{
				this.tasks = base.tasks as Tasks;
				tasks.SetVisitor(this);
				paramMapper = method.Variables.ParameterMapper;
				this.labels = Labeler.LabelNodes(method,generator);
				this.generator = generator;
				alreadyVisited = new Hashtable();
				locals = new Hashtable();
				AddTask(method,null);
				foreach(Variable var in method.Variables)
				{
					if(var.Kind == VariableKind.Local)
						locals[var] = generator.DeclareLocal(var.Type);
				}
				extraVars = new Hashtable();
				boolVar = null;
				wasDumped = new Hashtable();;
				wasDumpedFlag = false;
				prevHasNext = true;
			}

			private LocalBuilder GetExtraVar(Type type, int index)
			{
				if(extraVars[type] == null)
					extraVars[type] = new ArrayList();
				ArrayList list = extraVars[type] as ArrayList;
				if(index >= list.Count)
					for(int i=list.Count;i<=index;i++)
						list.Add(generator.DeclareLocal(type));
				LocalBuilder var = list[index] as LocalBuilder;
				if(var == null)
					throw new EmissionException();
				return(var);
			}

			private LocalBuilder GetBoolVar()
			{
				if(boolVar == null)
					boolVar = generator.DeclareLocal(typeof(int));
				return(boolVar);
			}

			private Label GetLabel(Node node)
			{
				return((Label)labels[node]);
			}

			private LocalBuilder GetLocal(Variable var)
			{
				return((LocalBuilder)locals[var]);
			}

			private int GetArgIndex(Variable var)
			{
				for(int i=0;i<paramMapper.Count;i++)
					if(var == paramMapper[i])
						return(i);
				//Andrew: Great idea...  :(
				throw new EmissionException();
			}

			private StackTypes Stack(Node node)
			{
				return((StackTypes)(node.Options["StackTypes"]));
			}

			private bool IsAlreadyVisited(Node node)
			{
				return(alreadyVisited.ContainsKey(node));
			}

			private void AddAlreadyVisited(Node node)
			{
				alreadyVisited.Add(node,null);
			}

			private Type VarType(TypeEx typeEx)
			{
				return(typeEx.boxed ? typeof(object) : typeEx.type);
			}

			private void RestoreStack(StackTypes stack)
			{
				for(int i=0; i<stack.Count; i++)
				{
					TypeEx typeEx = stack[i];
					Type type = VarType(typeEx);
					int index = 0;
					for(int j=0; j<i; j++)
						if(VarType(stack[j]) == type)
							index++;
					generator.Emit(OpCodes.Ldloc, GetExtraVar(type,index));
				}
			}

			private void RestoreStack(Node node)
			{
				object obj = wasDumped[node];
				if(obj == null)
					return; //MethodBodyBlock etc.
				if((bool)obj)
				  RestoreStack(Stack(node));
			}

			private void DumpStack(StackTypes stack)
			{
				for(int i=stack.Count-1; i>=0; i--)
				{
					TypeEx typeEx = stack[i];
					Type type = VarType(typeEx);
							
					int index = 0;
					for(int j=0; j<i; j++)
						if(VarType(stack[j]) == type)
							index++;
					generator.Emit(OpCodes.Stloc, GetExtraVar(type,index));
				}
			}
      
			private bool DumpStack(Node node)
			{
				if(node == null  ||  node.PrevArray.Count <= 1  ||  Stack(node).Count == 0)
					return(false);
				DumpStack(Stack(node));
				return(true);
			}
 
			//Common emitter behaviour for all node types
			override protected void DispatchNode(Node node, object data)
			{
				if(IsAlreadyVisited(node))
				{
					if(prevHasNext)
						generator.Emit(OpCodes.Br, GetLabel(node));
					prevHasNext = false;
				}
				else
				{
					prevHasNext = true;
					AddAlreadyVisited(node);
					generator.MarkLabel(GetLabel(node));
					RestoreStack(node); 
					CallVisitorMethod(node,null);
					if(wasDumpedFlag)
						wasDumpedFlag = false;
					else if(node.Next != null)
						wasDumped[node.Next] = DumpStack(node.Next);
				}
				tasks.TryResume();
			}

			/* Visiting methods */
			protected internal override void VisitServiceNode(ServiceNode node, object data)
			{
				for(int i=node.NextArray.Count - 1; i>=0; i--)
					AddTask(node.NextArray[i], null);
			}

			protected internal override void VisitMethodBodyBlock(MethodBodyBlock node, object data)
			{
				currentBlock = node;
				AddTask(node.Next,null);
			}

			protected internal override void VisitProtectedBlock(ProtectedBlock node, object data)
			{
				generator.BeginExceptionBlock();
				currentBlock = node;
				tasks.Suspend();
				AddTask(node.Next,null);
			}

			protected internal override void VisitCatchBlock(CatchBlock node, object data)
			{
				generator.BeginCatchBlock(node.Type);
				currentBlock = node;
				tasks.Suspend();
				AddTask(node.Next,null);
			}

			protected internal override void VisitFinallyBlock(FinallyBlock node, object data)
			{
				generator.BeginFinallyBlock();
				currentBlock = node;
				tasks.Suspend();
				AddTask(node.Next,null);
			}

			internal void OnResume()
			{
				if(currentBlock is ProtectedBlock)
				{
					ProtectedBlock node = currentBlock as ProtectedBlock;
					for(int i = node.Count - 1; i>=0; i--)
						AddTask(node[i],null);
				}
				else if(currentBlock is EHBlock)
				{
					EHBlock block = currentBlock as EHBlock;
					if(block.TryBlock[block.TryBlock.Count - 1] == block)
					{ // The last handler
						generator.EndExceptionBlock();
            tasks.TryResume();
					}
				}
				else
					throw new EmissionException();
				currentBlock = currentBlock.Parent;
			}

			protected internal override void VisitUserFilteredBlock(UserFilteredBlock node, object data)
			{
				throw new EmissionException(); //Not supported yet
			}

			protected internal override void VisitFilterBlock(FilterBlock node, object data)
			{
				throw new EmissionException(); //Not supported yet
			}

			protected internal override void VisitLeave(Leave node, object data)
			{
				prevHasNext = false;

				if(node.Parent is MethodBodyBlock)
					generator.Emit(OpCodes.Ret);
				else if(node.Parent is ProtectedBlock || node.Parent is CatchBlock)
				{
					generator.Emit(OpCodes.Leave, GetLabel(node.Next) );
					tasks.Burrow(node.Next,null);
				}
				else if(node.Parent is FinallyBlock)
					generator.Emit(OpCodes.Endfinally);
				else
					throw new EmissionException();
			}

			protected internal override void VisitUnaryOp(UnaryOp node, object data)
			{
				switch(node.Op)
				{
					case UnaryOp.ArithOp.NEG:
					{
						generator.Emit(OpCodes.Neg);
					} break;
					case UnaryOp.ArithOp.NOT:
					{
						generator.Emit(OpCodes.Not);
					} break;
					default: throw new EmissionException();
				}
				AddTask(node.Next,null);
			}

			protected internal override void VisitBinaryOp(BinaryOp node, object data)
			{
				switch(node.Op)
				{
					case BinaryOp.ArithOp.ADD:
					{
						if(node.Overflow)
							if(node.Unsigned)
								generator.Emit(OpCodes.Add_Ovf_Un);
							else
								generator.Emit(OpCodes.Add_Ovf);
						else
							generator.Emit(OpCodes.Add);
					} break;
					case BinaryOp.ArithOp.AND:
					{
						generator.Emit(OpCodes.And);
					} break;
					case BinaryOp.ArithOp.CEQ:
					{
						if(node.Next is Branch && node.Next.PrevArray.Count == 1)
						{
							Branch br = node.Next as Branch;
							generator.Emit(OpCodes.Beq, GetLabel(br.Alt));
							AddTask(br.Alt,null);
							AddTask(br.Next,null);
							return;
						}
						generator.Emit(OpCodes.Ceq);
					} break;
					case BinaryOp.ArithOp.CGT:
					{
						if(node.Next is Branch && node.Next.PrevArray.Count == 1)
						{
							Branch br = node.Next as Branch;
							if(node.Unsigned)
								generator.Emit(OpCodes.Bgt_Un, GetLabel(br.Alt));
							else
								generator.Emit(OpCodes.Bgt, GetLabel(br.Alt));
							AddTask(br.Alt,null);
							AddTask(br.Next,null);
							return;
						}
						if(node.Unsigned)
							generator.Emit(OpCodes.Cgt_Un);
						else
							generator.Emit(OpCodes.Cgt);
					} break;
					case BinaryOp.ArithOp.CLT:
					{
						if(node.Next is Branch && node.Next.PrevArray.Count == 1)
						{
							Branch br = node.Next as Branch;
							if(node.Unsigned)
								generator.Emit(OpCodes.Blt_Un, GetLabel(br.Alt));
							else
								generator.Emit(OpCodes.Blt, GetLabel(br.Alt));
							AddTask(br.Alt,null);
							AddTask(br.Next,null);
							return;
						}
						if(node.Unsigned)
							generator.Emit(OpCodes.Clt_Un);
						else
							generator.Emit(OpCodes.Clt);
					} break;
					case BinaryOp.ArithOp.DIV:
					{
						if(node.Unsigned)
							generator.Emit(OpCodes.Div_Un);
						else
							generator.Emit(OpCodes.Div);
					} break;
					case BinaryOp.ArithOp.MUL:
					{
						if(node.Overflow)
							if(node.Unsigned)
								generator.Emit(OpCodes.Mul_Ovf_Un);
							else
								generator.Emit(OpCodes.Mul_Ovf);
						else
							generator.Emit(OpCodes.Mul);
					} break;
					case BinaryOp.ArithOp.OR:
					{
						generator.Emit(OpCodes.Or); 
					} break;
					case BinaryOp.ArithOp.REM:
					{
						if(node.Unsigned)
							generator.Emit(OpCodes.Rem_Un);
						else
							generator.Emit(OpCodes.Rem);
					} break;
					case BinaryOp.ArithOp.SHL:
					{
						generator.Emit(OpCodes.Shl);
					} break;
					case BinaryOp.ArithOp.SHR:
					{
						generator.Emit(OpCodes.Shr);
					} break;
					case BinaryOp.ArithOp.SUB:
					{
						if(node.Overflow)
							if(node.Unsigned)
								generator.Emit(OpCodes.Sub_Ovf_Un);
							else
								generator.Emit(OpCodes.Sub_Ovf);
						else
							generator.Emit(OpCodes.Sub);
					} break;
					case BinaryOp.ArithOp.XOR:
					{
						generator.Emit(OpCodes.Xor);
					} break;
					default: throw new EmissionException();
				}
				AddTask(node.Next,null);
			}

			protected internal override void VisitConvertValue(ConvertValue node, object data)
			{
				if(node.Type.Equals(typeof(IntPtr))) 
					if(node.Overflow)
						if(node.Unsigned)
              generator.Emit(OpCodes.Conv_Ovf_I_Un);
						else
							generator.Emit(OpCodes.Conv_Ovf_I);
					else
            generator.Emit(OpCodes.Conv_I);
				else if(node.Type.Equals(typeof(sbyte)))
					if(node.Overflow)
						if(node.Unsigned)
							generator.Emit(OpCodes.Conv_Ovf_I1_Un);
						else
							generator.Emit(OpCodes.Conv_Ovf_I1);
					else
						generator.Emit(OpCodes.Conv_I1);
				else if(node.Type.Equals(typeof(short)))
					if(node.Overflow)
						if(node.Unsigned)
							generator.Emit(OpCodes.Conv_Ovf_I2_Un);
						else
							generator.Emit(OpCodes.Conv_Ovf_I2);
					else
						generator.Emit(OpCodes.Conv_I2);
				else if(node.Type.Equals(typeof(int)))
					if(node.Overflow)
						if(node.Unsigned)
							generator.Emit(OpCodes.Conv_Ovf_I4_Un);
						else
							generator.Emit(OpCodes.Conv_Ovf_I4);
					else
						generator.Emit(OpCodes.Conv_I4);
				else if(node.Type.Equals(typeof(long)))
					if(node.Overflow)
						if(node.Unsigned)
							generator.Emit(OpCodes.Conv_Ovf_I8_Un);
						else
							generator.Emit(OpCodes.Conv_Ovf_I8);
					else
						generator.Emit(OpCodes.Conv_I8);
				else if(node.Type.Equals(typeof(UIntPtr)))
					if(node.Overflow)
						if(node.Unsigned)
							generator.Emit(OpCodes.Conv_Ovf_U_Un);
						else
							generator.Emit(OpCodes.Conv_Ovf_U);
					else
						generator.Emit(OpCodes.Conv_U);
				else if(node.Type.Equals(typeof(byte)))
					if(node.Overflow)
						if(node.Unsigned)
							generator.Emit(OpCodes.Conv_Ovf_U1_Un);
						else
							generator.Emit(OpCodes.Conv_Ovf_U1);
					else
						generator.Emit(OpCodes.Conv_U1);
				else if(node.Type.Equals(typeof(ushort)))
					if(node.Overflow)
						if(node.Unsigned)
							generator.Emit(OpCodes.Conv_Ovf_U2_Un);
						else
							generator.Emit(OpCodes.Conv_Ovf_U2);
					else
						generator.Emit(OpCodes.Conv_U2);
				else if(node.Type.Equals(typeof(uint)))
					if(node.Overflow)
						if(node.Unsigned)
							generator.Emit(OpCodes.Conv_Ovf_U4_Un);
						else
							generator.Emit(OpCodes.Conv_Ovf_U4);
					else
						generator.Emit(OpCodes.Conv_U4);
				else if(node.Type.Equals(typeof(ulong)))
					if(node.Overflow)
						if(node.Unsigned)
							generator.Emit(OpCodes.Conv_Ovf_U8_Un);
						else
							generator.Emit(OpCodes.Conv_Ovf_U8);
					else
						generator.Emit(OpCodes.Conv_U8);
				else if(node.Type.Equals(typeof(float)))
					if(node.Unsigned)
            generator.Emit(OpCodes.Conv_R_Un);
  				else
	  				generator.Emit(OpCodes.Conv_R4);
				else if(node.Type.Equals(typeof(double)))
					generator.Emit(OpCodes.Conv_R8); 
				AddTask(node.Next,null);
			}

			protected internal override void VisitCheckFinite(CheckFinite node, object data)
			{
				generator.Emit(OpCodes.Ckfinite);
				AddTask(node.Next,null);
			}

			protected internal override void VisitBranch(Branch node, object data)
			{
				if(node.Alt.PrevArray.Count > 1  &&  Stack(node.Alt).Count != 0)
				{
          generator.Emit(OpCodes.Stloc, GetBoolVar());
					DumpStack(Stack(node.Alt));
          generator.Emit(OpCodes.Ldloc, GetBoolVar());
					wasDumpedFlag = true;
					wasDumped[node.Alt] = true;
					wasDumped[node.Next] = true;
				}
				//if(node.Condition)
				generator.Emit(OpCodes.Brtrue, GetLabel(node.Alt));
				//else
				//	generator.Emit(OpCodes.Brfalse, GetLabel(node.Alt));
				AddTask(node.Alt,null);
				AddTask(node.Next,null);
			}

			protected internal override void VisitSwitch(Switch node, object data)
			{
				bool shouldDump = false;
				for(int i=0; i<node.Count; i++)
					if(node[i].PrevArray.Count > 1  &&  Stack(node[i]).Count != 0)
					{
						shouldDump = true;
						break;
					}
				if(shouldDump)
				{
					generator.Emit(OpCodes.Stloc, GetBoolVar());
					DumpStack(Stack(node.Next));
					generator.Emit(OpCodes.Ldloc, GetBoolVar());
					wasDumpedFlag = true;
					for(int i=0;i<node.Count;i++)
					  wasDumped[node[i]] = true;
				}
				Label[] labels = new Label[node.Count];
				for(int i=0;i<labels.Length;i++)
					labels[i] = GetLabel(node[i]);
				generator.Emit(OpCodes.Switch,labels);
				for(int i=0;i<node.Count;i++)
					AddTask(node[i],null);
				AddTask(node.Next,null);
			}

			protected internal override void VisitLoadConst(LoadConst node, object data)
			{
				if(node.Constant == null)
          generator.Emit(OpCodes.Ldnull);
				else if(node.Constant is string )
					generator.Emit(OpCodes.Ldstr, node.Constant as string);
				else if(node.Constant is RuntimeTypeHandle)
					generator.Emit(OpCodes.Ldtoken,  Type.GetTypeFromHandle((RuntimeTypeHandle)(node.Constant)));
				else if(node.Constant is RuntimeMethodHandle)
					generator.Emit(OpCodes.Ldtoken, MethodBase.GetMethodFromHandle((RuntimeMethodHandle)(node.Constant)) as MethodInfo);
					//Andrew: Zlp!
				else if(node.Constant is RuntimeFieldHandle)
					generator.Emit(OpCodes.Ldtoken, FieldInfo.GetFieldFromHandle((RuntimeFieldHandle)(node.Constant)));
				else if(node.Constant is IntPtr)
					generator.Emit(OpCodes.Ldc_I4, (int)(IntPtr)(node.Constant)); //Andrew!!
				else if(node.Constant is int)
					generator.Emit(OpCodes.Ldc_I4, (int)(node.Constant));
				else if(node.Constant is long)
					generator.Emit(OpCodes.Ldc_I8, (long)(node.Constant));
				else if(node.Constant is float)
					generator.Emit(OpCodes.Ldc_R4, (float)(node.Constant));
				else if(node.Constant is double)
					generator.Emit(OpCodes.Ldc_R8, (double)(node.Constant));
				else 
					throw new EmissionException();
				AddTask(node.Next,null);
			}

			protected internal override void VisitLoadVar(LoadVar node, object data)
			{
				switch(node.Var.Kind)
				{
					case VariableKind.Local:
					{
						generator.Emit(OpCodes.Ldloc, GetLocal(node.Var));
					} break;
					case VariableKind.Parameter:
					{
						int index = GetArgIndex(node.Var);
						switch(index)
						{
							case 0:
								generator.Emit(OpCodes.Ldarg_0);
								break;
							case 1:
								generator.Emit(OpCodes.Ldarg_1);
								break;
							case 2:
								generator.Emit(OpCodes.Ldarg_2);
								break;
							case 3:
								generator.Emit(OpCodes.Ldarg_3);
								break;
							default:
								if(index <= 255)
									generator.Emit(OpCodes.Ldarg_S, (byte)index);
								else
									generator.Emit(OpCodes.Ldarg, index);
								break; 
						}
					} break;
					case VariableKind.ArgList:
						throw new EmissionException(); //TODO: not supported yet
				}
				AddTask(node.Next,null);
			}

			protected internal override void VisitLoadVarAddr(LoadVarAddr node, object data)
			{
				switch(node.Var.Kind)
				{
					case VariableKind.Local:
					{
						generator.Emit(OpCodes.Ldloca, GetLocal(node.Var));
					} break;
					case VariableKind.Parameter:
					{
						int index = GetArgIndex(node.Var);
						if(index <= 255)
						  generator.Emit(OpCodes.Ldarga_S, (byte)index);
						else
              generator.Emit(OpCodes.Ldarga, index);
					} break;
					case VariableKind.ArgList:
						throw new EmissionException(); //Impossible!
				}
				AddTask(node.Next,null);
			}

			protected internal override void VisitStoreVar(StoreVar node, object data)
			{
				switch(node.Var.Kind)
				{
					case VariableKind.Local:
					{
						generator.Emit(OpCodes.Stloc, GetLocal(node.Var));
					} break;
					case VariableKind.Parameter:
					{
						int index = GetArgIndex(node.Var);
						if(index <= 255)
							generator.Emit(OpCodes.Starg_S, (byte)index);
						else
							generator.Emit(OpCodes.Starg, index);
					} break;
					case VariableKind.ArgList:
						throw new EmissionException(); //Impossible!
				}
				AddTask(node.Next,null);
			}

			protected internal override void VisitLoadIndirect(LoadIndirect node, object data)
			{
				if(node.Type.Equals( typeof(IntPtr) ))
					generator.Emit(OpCodes.Ldind_I);
				else if(node.Type.Equals( typeof(sbyte) ))
					generator.Emit(OpCodes.Ldind_I1);
				else if(node.Type.Equals( typeof(short) ))
					generator.Emit(OpCodes.Ldind_I2);
				else if(node.Type.Equals( typeof(int) ))
					generator.Emit(OpCodes.Ldind_I4);
				else if(node.Type.Equals( typeof(long) ))
					generator.Emit(OpCodes.Ldind_I8);
				else if(node.Type.Equals( typeof(UIntPtr) ))
					generator.Emit(OpCodes.Ldind_I); //missing Ldind_U ... ?...
				else if(node.Type.Equals( typeof(byte) ))
					generator.Emit(OpCodes.Ldind_U1);
				else if(node.Type.Equals( typeof(ushort) ))
					generator.Emit(OpCodes.Ldind_U2);
				else if(node.Type.Equals( typeof(uint) ))
					generator.Emit(OpCodes.Ldind_U4);
				else if(node.Type.Equals( typeof(ulong) ))
					generator.Emit(OpCodes.Ldind_I8); //missing Ldind_U8 ... ?...
				else if(node.Type.Equals( typeof(float) ))
					generator.Emit(OpCodes.Ldind_R4);
				else if(node.Type.Equals( typeof(double) ))
					generator.Emit(OpCodes.Ldind_R8);
				else if(node.Type.IsValueType)
				{
					if(node.Next is StoreIndirect && node.Next.PrevArray.Count == 1)
					{
						generator.Emit(OpCodes.Cpobj, node.Type);
						AddTask(node.Next.Next,null);
						return;
					}
					generator.Emit(OpCodes.Ldobj, node.Type);
				}
				else 
					generator.Emit(OpCodes.Ldind_Ref);
				AddTask(node.Next,null);
			}

			protected internal override void VisitStoreIndirect(StoreIndirect node, object data)
			{
				if(node.Type.Equals( typeof(IntPtr) ))
					generator.Emit(OpCodes.Stind_I);
				else if(node.Type.Equals( typeof(sbyte) ))
					generator.Emit(OpCodes.Stind_I1);
				else if(node.Type.Equals( typeof(short) ))
					generator.Emit(OpCodes.Stind_I2);
				else if(node.Type.Equals( typeof(int) ))
					generator.Emit(OpCodes.Stind_I4);
				else if(node.Type.Equals( typeof(long) ))
					generator.Emit(OpCodes.Stind_I8);
				else if(node.Type.Equals( typeof(UIntPtr) ))
					generator.Emit(OpCodes.Stind_I);
				else if(node.Type.Equals( typeof(byte) ))
					generator.Emit(OpCodes.Stind_I1);
				else if(node.Type.Equals( typeof(ushort) ))
					generator.Emit(OpCodes.Stind_I2);
				else if(node.Type.Equals( typeof(uint) ))
					generator.Emit(OpCodes.Stind_I4);
				else if(node.Type.Equals( typeof(ulong) ))
					generator.Emit(OpCodes.Stind_I8);
				else if(node.Type.Equals( typeof(float) ))
					generator.Emit(OpCodes.Stind_R4);
				else if(node.Type.Equals( typeof(double) ))
					generator.Emit(OpCodes.Stind_R8);
				else if(node.Type.IsValueType)
					generator.Emit(OpCodes.Stobj, node.Type);
				else 
					generator.Emit(OpCodes.Stind_Ref);
				AddTask(node.Next,null);
			}

			protected internal override void VisitDuplicateStackTop(DuplicateStackTop node, object data)
			{
				generator.Emit(OpCodes.Dup);
				AddTask(node.Next,null);
			}

			protected internal override void VisitRemoveStackTop(RemoveStackTop node, object data)
			{
				generator.Emit(OpCodes.Pop);
				AddTask(node.Next,null);
			}

			protected internal override void VisitCastClass(CastClass node, object data)
			{
				if(node.ThrowException)
					generator.Emit(OpCodes.Castclass, node.Type);
				else
					generator.Emit(OpCodes.Isinst, node.Type);
				AddTask(node.Next,null);
			}

			protected internal override void VisitCallMethod(CallMethod node, object data)
			{
				if(HasPseudoParameter(node))
					generator.Emit(OpCodes.Ldnull);

				OpCode code = node.IsVirtCall ?  OpCodes.Callvirt : OpCodes.Call;
				if(node.Method is MethodInfo)
					generator.Emit(code,node.Method as MethodInfo);
				else
					generator.Emit(code,node.Method as ConstructorInfo);
				AddTask(node.Next,null);
			}

			protected internal override void VisitCreateDelegate(CreateDelegate node, object data)
			{
				if(node.IsVirtual)
				{
					generator.Emit(OpCodes.Dup);
          generator.Emit(OpCodes.Ldvirtftn, node.Method);
					generator.Emit(OpCodes.Newobj, node.DelegateCtor);
				}
				else
				{
					generator.Emit(OpCodes.Ldftn, node.Method);
					generator.Emit(OpCodes.Newobj, node.DelegateCtor);
				}
				AddTask(node.Next,null);
			}

			protected internal override void VisitLoadField(LoadField node, object data)
			{
				if(node.Field.IsStatic)
					generator.Emit(OpCodes.Ldsfld, node.Field);
				else
					generator.Emit(OpCodes.Ldfld, node.Field);
				AddTask(node.Next,null);
			}

			protected internal override void VisitLoadFieldAddr(LoadFieldAddr node, object data)
			{
				if(node.Field.IsStatic)
					generator.Emit(OpCodes.Ldsflda, node.Field);
			    else
					generator.Emit(OpCodes.Ldflda, node.Field);
				AddTask(node.Next,null);
			}

			protected internal override void VisitStoreField(StoreField node, object data)
			{
				if(node.Field.IsStatic)
				    generator.Emit(OpCodes.Stsfld, node.Field);
				else
					generator.Emit(OpCodes.Stfld, node.Field);
				AddTask(node.Next,null);
			}

			protected internal override void VisitThrowException(ThrowException node, object data)
			{
				prevHasNext = false;
				generator.Emit(OpCodes.Throw);
			}

			protected internal override void VisitRethrowException(RethrowException node, object data)
			{
				prevHasNext = false;
				generator.Emit(OpCodes.Rethrow);
			}

			private static bool HasPseudoParameter(Node node)
			{
				object opt = node.Options["HasPseudoParameter"];
				if(opt == null)
					return(false);
				return((bool)opt);
			}
	
			protected internal override void VisitNewObject(NewObject node, object data)
			{
				if(HasPseudoParameter(node))
					generator.Emit(OpCodes.Ldnull);
				generator.Emit(OpCodes.Newobj, node.Constructor);
				AddTask(node.Next,null);
			}

			protected internal override void VisitLoadElement(LoadElement node, object data)
			{
				if(node.Type.Equals( typeof(IntPtr) ))
					generator.Emit(OpCodes.Ldelem_I);
				else if(node.Type.Equals( typeof(sbyte) ))
					generator.Emit(OpCodes.Ldelem_I1);
				else if(node.Type.Equals( typeof(short) ))
					generator.Emit(OpCodes.Ldelem_I2);
				else if(node.Type.Equals( typeof(int) ))
					generator.Emit(OpCodes.Ldelem_I4);
				else if(node.Type.Equals( typeof(long) ))
					generator.Emit(OpCodes.Ldelem_I8);
				else if(node.Type.Equals( typeof(UIntPtr) ))
					generator.Emit(OpCodes.Ldelem_I);
				else if(node.Type.Equals( typeof(byte) ))
					generator.Emit(OpCodes.Ldelem_I1);
				else if(node.Type.Equals( typeof(ushort) ))
					generator.Emit(OpCodes.Ldelem_I2);
				else if(node.Type.Equals( typeof(uint) ))
					generator.Emit(OpCodes.Ldelem_I4);
				else if(node.Type.Equals( typeof(ulong) ))
					generator.Emit(OpCodes.Ldelem_I8);
				else if(node.Type.Equals( typeof(float) ))
					generator.Emit(OpCodes.Ldelem_R4);
				else if(node.Type.Equals( typeof(double) ))
					generator.Emit(OpCodes.Ldelem_R8);
				else if(node.Type.Equals( typeof(object) ))
					generator.Emit(OpCodes.Ldelem_Ref);
				else
					throw new EmissionException();
				AddTask(node.Next,null);
			}

			protected internal override void VisitLoadElementAddr(LoadElementAddr node, object data)
			{
				generator.Emit(OpCodes.Ldelema);
				AddTask(node.Next,null);
			}

			protected internal override void VisitStoreElement(StoreElement node, object data)
			{
				if(node.Type.Equals( typeof(IntPtr) ))
					generator.Emit(OpCodes.Stelem_I);
				else if(node.Type.Equals( typeof(sbyte) ))
					generator.Emit(OpCodes.Stelem_I1);
				else if(node.Type.Equals( typeof(short) ))
					generator.Emit(OpCodes.Stelem_I2);
				else if(node.Type.Equals( typeof(int) ))
					generator.Emit(OpCodes.Stelem_I4);
				else if(node.Type.Equals( typeof(long) ))
					generator.Emit(OpCodes.Stelem_I8);
				else if(node.Type.Equals( typeof(UIntPtr) ))
					generator.Emit(OpCodes.Stelem_I);
				else if(node.Type.Equals( typeof(byte) ))
					generator.Emit(OpCodes.Stelem_I1);
				else if(node.Type.Equals( typeof(ushort) ))
					generator.Emit(OpCodes.Stelem_I2);
				else if(node.Type.Equals( typeof(uint) ))
					generator.Emit(OpCodes.Stelem_I4);
				else if(node.Type.Equals( typeof(ulong) ))
					generator.Emit(OpCodes.Stelem_I8);
				else if(node.Type.Equals( typeof(float) ))
					generator.Emit(OpCodes.Stelem_R4);
				else if(node.Type.Equals( typeof(double) ))
					generator.Emit(OpCodes.Stelem_R8);
				else if(node.Type.Equals( typeof(object) ))
					generator.Emit(OpCodes.Stelem_Ref);
				else
					throw new EmissionException();
				AddTask(node.Next,null);
			}

			protected internal override void VisitLoadLength(LoadLength node, object data)
			{
				generator.Emit(OpCodes.Ldlen);
				AddTask(node.Next,null);
			}

			protected internal override void VisitNewArray(NewArray node, object data)
			{
				generator.Emit(OpCodes.Newarr, node.Type);
				AddTask(node.Next,null);
			}

			protected internal override void VisitBoxValue(BoxValue node, object data)
			{
				generator.Emit(OpCodes.Box, node.Type);
				AddTask(node.Next,null);
			}

			protected internal override void VisitUnboxValue(UnboxValue node, object data)
			{
				generator.Emit(OpCodes.Unbox, node.Type);
				AddTask(node.Next,null);
			}

			//protected internal override void VisitCopyObject(CopyObject node, object data)
			//{
			//	generator.Emit(OpCodes.Cpobj, node.Type);
			//	AddTask(node.Next,null);
			//}

			protected internal override void VisitInitValue(InitValue node, object data)
			{
				generator.Emit(OpCodes.Initobj, node.Type);
				AddTask(node.Next,null);
			}

			//protected internal override void VisitLoadObject(LoadObject node, object data)
			//{
			//	generator.Emit(OpCodes.Ldobj, node.Type);
			//	AddTask(node.Next,null);
			//}

			protected internal override void VisitLoadSizeOfValue(LoadSizeOfValue node, object data)
			{
				generator.Emit(OpCodes.Sizeof, node.Type);
				AddTask(node.Next,null);
			}

			//protected internal override void VisitStoreObject(StoreObject node, object data)
			//{
			//	generator.Emit(OpCodes.Stobj, node.Type);
			//	AddTask(node.Next,null);
			//}

			protected internal override void VisitMakeTypedRef(MakeTypedRef node, object data)
			{
				throw new EmissionException();//TODO: Not supported yet
			}

			protected internal override void VisitRetrieveType(RetrieveType node, object data)
			{
				throw new EmissionException();//TODO: Not supported yet
			}

			protected internal override void VisitRetrieveValue(RetrieveValue node, object data)
			{
				throw new EmissionException();//TODO: Not supported yet
			}
		}

		public static void Emit(ILGenerator generator, MethodBodyBlock method)
		{
			GraphProcessor graphProcessor = new GraphProcessor();
			EmitterVisitor visitor = new EmitterVisitor(graphProcessor, generator, method);
			graphProcessor.Process(); 
		}
	}
}
