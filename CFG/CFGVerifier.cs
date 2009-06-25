
// ===========================================================================
// CILPE - Partial Evaluator for Common Intermediate Language
// ===========================================================================
// File: 
//     CFGVerifier.cs
//
// Description:
//     The verifier for the control flow graph
//
// Author: 
//     Andrei Mishchenko
// ===========================================================================

using System;
using CILPE.ReflectionEx;
using System.Collections;

namespace CILPE.CFG
{
	/// <summary>
	/// Summary description for CFGVerifier.
	/// </summary>
	/// 
	public delegate void ForEachCallback(Node node);
	public class ForEachVisitor : QueueVisitor
	{
		private Hashtable alreadyVisited; 
		private ForEachCallback handler;

		public ForEachVisitor(GraphProcessor processor, ForEachCallback handler) : base(processor)
		{
			alreadyVisited = new Hashtable();
			this.handler = handler;
		}

		private bool IsAlreadyVisited(Node node)
		{
			return(alreadyVisited.ContainsKey(node));
		}

		private void AddAlreadyVisited(Node node)
		{
			alreadyVisited.Add(node,null);
		}

		override protected void DispatchNode(Node node, object data)
		{
			if(IsAlreadyVisited(node))
				return;
			AddAlreadyVisited(node);
			handler(node);
			for(int i=0; i<node.NextArray.Count; i++)
				AddTask(node.NextArray[i], null);
      UserFilteredBlock userFilteredBlock = node as UserFilteredBlock;
			if(userFilteredBlock != null)
				AddTask(userFilteredBlock.Filter, null);
			ProtectedBlock protectedBlock = node as ProtectedBlock;
			if(protectedBlock != null)
				for(int i=0; i<protectedBlock.Count; i++)
 				  AddTask(protectedBlock[i], null); //Handlers
		}

		public static void ForEach(MethodBodyBlock method, ForEachCallback handler)
		{
			GraphProcessor graphProcessor = new GraphProcessor();
			Visitor visitor = new ForEachVisitor(graphProcessor,handler);
			visitor.AddTask(method,null);
			graphProcessor.Process();
		}
	}

	/*public class RefsAndArraysBuilder
	{
		public RefsAndArraysBuilder(){}

		public virtual Type BuildRefType(Type type)
		{
			return(TypeEx.BuildRefType(type)); 
		}

		public virtual Type BuildArrayType(Type type)
		{
			return(TypeEx.BuildArrayType(type)); 
		}
	}*/

	public class CFGVerifier
	{
		private class VerifierVisitor : Visitor
		{
			public VerifierVisitor(GraphProcessor graphProcessor) : base(graphProcessor,new VisitorTaskQueue())
			{}

			private StackTypes GetNodeStack(Node node)
			{
				if(node.Options.ContainsOption("StackTypes"))
					return(node.Options["StackTypes"] as StackTypes);
				return(null);
			}

			static private void SetNodeStack(Node node, StackTypes stack)
			{
				node.Options["StackTypes"] = stack;
			}

			//Common verifier behaviour for all node types
			override protected void DispatchNode(Node node, object data)
			{
				StackTypes stack = data as StackTypes;
				if(stack == null)
					throw new VerifierException();
				StackTypes oldStack = GetNodeStack(node);
				if(oldStack == null)
				{
					SetNodeStack(node,stack);
				}
				else
				{
					if(Verifier.IsStackMoreGeneral(oldStack,stack))
						return;
					stack = Verifier.DoMergeStacks(oldStack,stack); //throws VerifierException
					SetNodeStack(node,stack);
				}
				CallVisitorMethod(node,stack.Clone());
			}

			/* Visiting methods */
			protected internal override void VisitServiceNode(ServiceNode node, object data)
			{
				StackTypes stack = data as StackTypes;
				AddTask(node.Next, stack);
				for(int i=1; i<node.NextArray.Count; i++)
					AddTask(node.NextArray[i], stack.Clone());
				//Service nodes do not change the stack
			}

			protected internal override void VisitMethodBodyBlock(MethodBodyBlock node, object data)
			{
				StackTypes stack = data as StackTypes;
				if(stack.Count != 0)
					throw new VerifierException();

				AddTask(node.Next,stack);
			}

			protected internal override void VisitProtectedBlock(ProtectedBlock node, object data)
			{
				StackTypes stack = data as StackTypes;
				if(stack.Count != 0)
					throw new VerifierException();

				foreach (EHBlock block in node)
					AddTask(block,new StackTypes());
				AddTask(node.Next,stack);
			}

			protected internal override void VisitCatchBlock(CatchBlock node, object data)
			{
				StackTypes stack = data as StackTypes;
				if(stack.Count != 0)
					throw new VerifierException();
				
				stack.Push(node.Type);
				AddTask(node.Next,stack);
			}

			protected internal override void VisitFinallyBlock(FinallyBlock node, object data)
			{
				StackTypes stack = data as StackTypes;
				if(stack.Count != 0)
					throw new VerifierException();

				AddTask(node.Next,stack);
			}

			protected internal override void VisitUserFilteredBlock(UserFilteredBlock node, object data)
			{
				StackTypes stack = data as StackTypes;
				if(stack.Count != 0)
					throw new VerifierException();

				stack.Push(typeof(object)); 
				AddTask(node.Next, stack);
				AddTask(node.Filter, new StackTypes());
			}

			protected internal override void VisitFilterBlock(FilterBlock node, object data)
			{
				StackTypes stack = data as StackTypes;
				if(stack.Count != 0)
					throw new VerifierException();

				stack.Push(typeof(object)); 
				AddTask(node.Next,stack);
			}

			protected internal override void VisitLeave(Leave node, object data)
			{
				StackTypes stack = data as StackTypes;
				if(node.Parent is FilterBlock)
					Verifier.ProcessEndFilter(stack);
				else if(node.Parent is MethodBodyBlock)
					Verifier.ProcessRet(new TypeEx((node.Parent as MethodBodyBlock).ReturnType) , stack);
				else //CatchBlock || ProtectedBlock || UserFilteredBlock => stack should be empty
					Verifier.ProcessLeave(stack);
				if(node.Next != null)
					AddTask(node.Next,stack);
			}

			protected internal override void VisitUnaryOp(UnaryOp node, object data)
			{
				StackTypes stack = data as StackTypes;
				switch(node.Op)
				{
					case UnaryOp.ArithOp.NEG:
					{
						Verifier.ProcessNeg(stack);
					} break;
					case UnaryOp.ArithOp.NOT:
					{
						Verifier.ProcessNot(stack);
					} break;
					default: throw new VerifierException();
				}

				AddTask(node.Next,stack);
			}

			private static Verifier.OpType GetOpType(BinaryOp.ArithOp op, bool un, bool ovf)
			{
				if(op == BinaryOp.ArithOp.SHL || op == BinaryOp.ArithOp.SHR)
					return(Verifier.OpType.Shift);
				if(op == BinaryOp.ArithOp.CEQ || op == BinaryOp.ArithOp.CLT || op == BinaryOp.ArithOp.CGT)
					return(Verifier.OpType.Compare);
				if(Verifier.IsFloatOperation(Converter.CodeFromBinaryOp(op), un, ovf))
					return(Verifier.OpType.FloatOrInt);
				return(Verifier.OpType.Int);
			}

			protected internal override void VisitBinaryOp(BinaryOp node, object data)
			{
				StackTypes stack = data as StackTypes;
				Verifier.ProcessBinOp(GetOpType(node.Op, node.Unsigned, node.Overflow ),stack);

				AddTask(node.Next,stack);
			}

			protected internal override void VisitConvertValue(ConvertValue node, object data)
			{
				StackTypes stack = data as StackTypes;
				Verifier.ProcessConv(node.Type, stack);

				AddTask(node.Next,stack);
			}

			protected internal override void VisitCheckFinite(CheckFinite node, object data)
			{
				StackTypes stack = data as StackTypes;
				Verifier.ProcessCkFinite(stack);

				AddTask(node.Next,stack);
			}

			protected internal override void VisitBranch(Branch node, object data)
			{
				StackTypes stack = data as StackTypes;
				Verifier.ProcessBrTrueFalse(stack);
				AddTask(node.Next,stack);
				AddTask(node.Alt, stack.Clone());
			}

			protected internal override void VisitSwitch(Switch node, object data)
			{
				StackTypes stack = data as StackTypes;
				Verifier.ProcessSwitch(stack);
				AddTask(node.Next,stack);
				for(int i=1;i<node.NextArray.Count;i++)
					AddTask(node.NextArray[i],stack.Clone());
			}

			protected internal override void VisitLoadConst(LoadConst node, object data)
			{
				StackTypes stack = data as StackTypes;
				if(node.Constant == null)
          stack.Push(null);
				else
				  stack.Push(node.Constant.GetType());
				AddTask(node.Next,stack);
			}

			protected internal override void VisitLoadVar(LoadVar node, object data)
			{
				StackTypes stack = data as StackTypes;
				stack.Push(node.Var.Type);
				AddTask(node.Next,stack);
			}

			protected internal override void VisitLoadVarAddr(LoadVarAddr node, object data)
			{
				StackTypes stack = data as StackTypes;
				stack.Push(TypeEx.BuildRefType(node.Var.Type));
				AddTask(node.Next,stack);
			}

			protected internal override void VisitStoreVar(StoreVar node, object data)
			{
				StackTypes stack = data as StackTypes;
				Verifier.ProcessSt(new TypeEx(node.Var.Type), stack);
				AddTask(node.Next,stack);
			}

			protected internal override void VisitLoadIndirect(LoadIndirect node, object data)
			{
				StackTypes stack = data as StackTypes;
				Verifier.ProcessLdInd(node.Type, stack); 
				//looks like ProcessLdInd method functions correctly in case of LDOBJ
				AddTask(node.Next,stack);
			}

			protected internal override void VisitStoreIndirect(StoreIndirect node, object data)
			{
				StackTypes stack = data as StackTypes;
				Verifier.ProcessStInd(node.Type, stack);
				//looks like ProcessStInd method functions correctly in case of STOBJ 
				AddTask(node.Next,stack);
			}

			protected internal override void VisitDuplicateStackTop(DuplicateStackTop node, object data)
			{
				StackTypes stack = data as StackTypes;
				stack.Push(stack.Peek());
				AddTask(node.Next,stack);
			}

			protected internal override void VisitRemoveStackTop(RemoveStackTop node, object data)
			{
				StackTypes stack = data as StackTypes;
				stack.Pop();
				AddTask(node.Next,stack);
			}

			protected internal override void VisitCastClass(CastClass node, object data)
			{
				StackTypes stack = data as StackTypes;
				Verifier.ProcessCastClass(stack, new TypeEx(node.Type,true) );
				AddTask(node.Next,stack);
			}

			private static bool IsRet(Node node)
			{
				return(node is Leave && node.Parent is MethodBodyBlock);
			}

			protected internal override void VisitCallMethod(CallMethod node, object data)
			{
				StackTypes stack = data as StackTypes;
				Verifier.ProcessCallOrNewobj(node.MethodWithParams, stack, false);
				AddTask(node.Next,stack);
				if(node.IsTailCall && !IsRet(node.Next))
					throw new VerifierException();
			}

			protected internal override void VisitCreateDelegate(CreateDelegate node, object data)
			{
				StackTypes stack = data as StackTypes;
				Verifier.ProcessDelegateConstruction(stack,node.Method,node.DelegateCtor);
				AddTask(node.Next,stack);
			}

			protected internal override void VisitLoadField(LoadField node, object data)
			{
				StackTypes stack = data as StackTypes;
				if(node.Field.IsStatic)
				  stack.Push(node.Field.FieldType); 
				else
				  Verifier.ProcessLdFld(stack, node.Field , false);
				AddTask(node.Next,stack);
			}

			protected internal override void VisitLoadFieldAddr(LoadFieldAddr node, object data)
			{
				StackTypes stack = data as StackTypes;
				if(node.Field.IsStatic)
					stack.Push(TypeEx.BuildRefType(node.Field.FieldType)); 
				else
					Verifier.ProcessLdFld(stack, node.Field , true);
				AddTask(node.Next,stack);
			}

			protected internal override void VisitStoreField(StoreField node, object data)
			{
				StackTypes stack = data as StackTypes;
				if(node.Field.IsStatic)
					Verifier.ProcessStSFld(stack, node.Field);
				else
					Verifier.ProcessStFld(stack, node.Field);
				AddTask(node.Next,stack);
			}

			protected internal override void VisitThrowException(ThrowException node, object data)
			{
				StackTypes stack = data as StackTypes;
				Verifier.ProcessThrow(stack);
			}

			protected internal override void VisitRethrowException(RethrowException node, object data)
			{
				if(! (node.Parent is CatchBlock || node.Parent is UserFilteredBlock) )
					throw new VerifierException();
			}

			protected internal override void VisitNewObject(NewObject node, object data)
			{
				StackTypes stack = data as StackTypes;
				Verifier.ProcessCallOrNewobj(node.CtorWithParams, stack, true);
				AddTask(node.Next,stack);
			}

			protected internal override void VisitLoadElement(LoadElement node, object data)
			{
				StackTypes stack = data as StackTypes;
				Verifier.ProcessLdElem(stack, new TypeEx(node.Type), false);
				AddTask(node.Next,stack);
			}

			protected internal override void VisitLoadElementAddr(LoadElementAddr node, object data)
			{
				StackTypes stack = data as StackTypes;
				Verifier.ProcessLdElem(stack, new TypeEx(node.Type), true);
				AddTask(node.Next,stack);
			}

			protected internal override void VisitStoreElement(StoreElement node, object data)
			{
				StackTypes stack = data as StackTypes;
				Verifier.ProcessStElem(stack, new TypeEx(node.Type));
				AddTask(node.Next,stack);
			}

			protected internal override void VisitLoadLength(LoadLength node, object data)
			{
				StackTypes stack = data as StackTypes;
				Verifier.ProcessLdLen(stack);
				AddTask(node.Next,stack);
			}

			protected internal override void VisitNewArray(NewArray node, object data)
			{
				StackTypes stack = data as StackTypes;
				Verifier.ProcessNewArr(stack, node.Type);
				AddTask(node.Next,stack);
			}

			protected internal override void VisitBoxValue(BoxValue node, object data)
			{
				StackTypes stack = data as StackTypes;
				Verifier.ProcessBox(stack, node.Type);
				AddTask(node.Next,stack);
			}

			protected internal override void VisitUnboxValue(UnboxValue node, object data)
			{
				StackTypes stack = data as StackTypes;
				Verifier.ProcessUnBox(stack, node.Type);
				AddTask(node.Next,stack);
			}

			//protected internal override void VisitCopyObject(CopyObject node, object data)
			//{
			//	StackTypes stack = data as StackTypes;
			//	Verifier.ProcessCpObj(stack, node.Type);
			//	AddTask(node.Next,stack);
			//}

			protected internal override void VisitInitValue(InitValue node, object data)
			{
				StackTypes stack = data as StackTypes;
				Verifier.ProcessInitObj(stack, node.Type);
				AddTask(node.Next,stack);
			}

			//protected internal override void VisitLoadObject(LoadObject node, object data)
			//{
			//	StackTypes stack = data as StackTypes;
			//	Verifier.ProcessLdObj(stack, node.Type);
			//	AddTask(node.Next,stack);
			//}

			protected internal override void VisitLoadSizeOfValue(LoadSizeOfValue node, object data)
			{
				StackTypes stack = data as StackTypes;
				Verifier.ProcessSizeOf(stack, node.Type);
				AddTask(node.Next,stack);
			}

			//protected internal override void VisitStoreObject(StoreObject node, object data)
			//{
			//	StackTypes stack = data as StackTypes;
			//	Verifier.ProcessStObj(stack, node.Type);
			//	AddTask(node.Next,stack);
			//}

			protected internal override void VisitMakeTypedRef(MakeTypedRef node, object data)
			{
				throw new VerifierException();
				//not supported yet...
			}

			protected internal override void VisitRetrieveType(RetrieveType node, object data)
			{
				throw new VerifierException();
				//not supported yet...
			}

			protected internal override void VisitRetrieveValue(RetrieveValue node, object data)
			{
				throw new VerifierException();
				//not supported yet...
			}
		};

		private static void RemoveStackTypesCallback(Node node)
		{
      node.Options["StackTypes"] = null;  
		}

		private static void RemoveStackTypes(MethodBodyBlock method)
		{
			ForEachVisitor.ForEach(method, new ForEachCallback(RemoveStackTypesCallback) );
		}

		public static bool Check(MethodBodyBlock method)
		{
			//TODO: Will check parents consistency, if give up to do it automatically
			RemoveStackTypes(method);
			GraphProcessor graphProcessor = new GraphProcessor();
			VerifierVisitor verifierVisitor = new VerifierVisitor(graphProcessor);
			verifierVisitor.AddTask(method,new StackTypes());
			try
			{
				graphProcessor.Process(); 
			}
			catch(VerifierException)
			{
				RemoveStackTypes(method);
				return(false);
			}
			return(true);
		}
	}
}
