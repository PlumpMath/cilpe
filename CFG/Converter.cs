
// ===========================================================================
// CILPE - Partial Evaluator for Common Intermediate Language
// ===========================================================================
// File: 
//     Converter.cs
//
// Description:
//     The converter from simple code model to the control flow graph
//
// Author: 
//     Andrei Mishchenko
// ===========================================================================


using System;
using System.Collections;
using System.Reflection;
using CILPE.ReflectionEx;

namespace CILPE.CFG
{
	public class ConvertionException : Exception
	{
		public ConvertionException()
		{}//place breakpoint here
	}


	public class Converter
	{

		static private UnaryOp.ArithOp UnaryOpFromCode(InstructionCode code)
		{
			switch(code)
			{
				case InstructionCode.NEG: return UnaryOp.ArithOp.NEG;    
				case InstructionCode.NOT: return UnaryOp.ArithOp.NOT;    
			}
			throw new ConvertionException();
		}

		static public InstructionCode CodeFromBinaryOp(BinaryOp.ArithOp op)
		{
			switch(op)
			{
				case BinaryOp.ArithOp.ADD: return InstructionCode.ADD;    
				case BinaryOp.ArithOp.AND: return InstructionCode.AND;    
				case BinaryOp.ArithOp.CEQ: return InstructionCode.CEQ;    
				case BinaryOp.ArithOp.CGT: return InstructionCode.CGT;    
				case BinaryOp.ArithOp.CLT: return InstructionCode.CLT;    
				case BinaryOp.ArithOp.DIV: return InstructionCode.DIV;    
				case BinaryOp.ArithOp.MUL: return InstructionCode.MUL;    
				case BinaryOp.ArithOp.OR:  return InstructionCode.OR;    
				case BinaryOp.ArithOp.REM: return InstructionCode.REM;    
				case BinaryOp.ArithOp.SHL: return InstructionCode.SHL;    
				case BinaryOp.ArithOp.SHR: return InstructionCode.SHR;    
				case BinaryOp.ArithOp.SUB: return InstructionCode.SUB;    
				case BinaryOp.ArithOp.XOR: return InstructionCode.XOR;    
			}
			throw new ConvertionException();	
		}

		static private BinaryOp.ArithOp BinaryOpFromCode(InstructionCode code)
		{
			switch(code)
			{
				case InstructionCode.ADD: return BinaryOp.ArithOp.ADD;    
				case InstructionCode.AND: return BinaryOp.ArithOp.AND;    
				case InstructionCode.CEQ: return BinaryOp.ArithOp.CEQ;    
				case InstructionCode.CGT: return BinaryOp.ArithOp.CGT;    
				case InstructionCode.CLT: return BinaryOp.ArithOp.CLT;    
				case InstructionCode.DIV: return BinaryOp.ArithOp.DIV;    
				case InstructionCode.MUL: return BinaryOp.ArithOp.MUL;    
				case InstructionCode.OR:  return BinaryOp.ArithOp.OR;    
				case InstructionCode.REM: return BinaryOp.ArithOp.REM;    
				case InstructionCode.SHL: return BinaryOp.ArithOp.SHL;    
				case InstructionCode.SHR: return BinaryOp.ArithOp.SHR;    
				case InstructionCode.SUB: return BinaryOp.ArithOp.SUB;    
				case InstructionCode.XOR: return BinaryOp.ArithOp.XOR;    
			}
			throw new ConvertionException();	
		}

		/*private static int FindFirstNonNull(Object[] array,int I)
		{
			for(int i=I; i<array.Length; i++)
			{
				if(array[i] != null)
					return(i);
			}
			return(array.Length);
		}*/

		private class DummyNode : Node
		{
			public DummyNode() : base(1) {}
			public override Node Clone() { throw new ConvertionException(); } 
		}

		private enum BlockType
		{
			Try,Catch,Finally,Filter,FilteredCatch,Global,None
		};

		private static bool IsBlockLater(int b1Start,int b1End, int b2Start, int b2End)
		{
			if(b2Start > b1Start)
				return(true);
			if(b2Start < b1Start)
				return(false);
			return(b2End < b1End); 
		}

		private static void FindNextBlockStart(EHClausesArray clauses,out BlockType blockType,ref int blockStart,ref int blockEnd,out Type catchBlockType, out int index)
		{
			//given a block [blockStart,blockEnd) we search for the block [newBlockStart,newBlockEnd) such that:
			//1) IsBlockLater([blockStart,blockEnd) , [newBlockStart,newBlockEnd))
			//2) if   IsBlockLater([blockStart,blockEnd) , [newBlockStart',newBlockEnd')) 
			//   then IsBlockLater([newBlockStart,newBlockEnd) , [newBlockStart',newBlockEnd'))
			int newBlockStart = 1<<30;
			int newBlockEnd = 1<<30;
			blockType = BlockType.None;
			catchBlockType = null;
			index = -1;
			for(int i=0 ; i<clauses.Count ; i++)
			{
				EHClause c = clauses[i];
				if(IsBlockLater(blockStart,blockEnd,c.TryStart,c.TryEnd) && 
					IsBlockLater(c.TryStart,c.TryEnd,newBlockStart,newBlockEnd))
				{
					blockType = BlockType.Try;
					newBlockStart = c.TryStart;
					newBlockEnd = c.TryEnd;
					index = i;
				}
				if(IsBlockLater(blockStart,blockEnd,c.HandlerStart,c.HandlerEnd) && 
					IsBlockLater(c.HandlerStart,c.HandlerEnd,newBlockStart,newBlockEnd))
				{ //Andrew: mb treat filters another way...
					newBlockStart = c.HandlerStart;
					newBlockEnd = c.HandlerEnd;
					index = i;
					switch(c.Kind)
					{
						case EHClauseKind.FinallyHandler:
							blockType = BlockType.Finally;
							break;
						case EHClauseKind.TypeFilteredHandler:
							blockType = BlockType.Catch;
							catchBlockType = c.ClassObject;
							break;
						case EHClauseKind.FaultHandler: 
							blockType = BlockType.Catch;
              break;
						case EHClauseKind.UserFilteredHandler:
 						  blockType = BlockType.FilteredCatch;
							break;
					}
				}
				if(c.Kind == EHClauseKind.UserFilteredHandler)
					if(IsBlockLater(blockStart,blockEnd,c.FilterStart,c.FilterEnd) && 
						IsBlockLater(c.FilterStart,c.FilterEnd,newBlockStart,newBlockEnd))
					{
						newBlockStart = c.FilterStart;
						newBlockEnd = c.FilterEnd;
						blockType = BlockType.Filter;
						index = i;
					}
			}
			blockStart = newBlockStart;
			blockEnd = newBlockEnd;
		}

		private struct Segment
		{
			public int start;
			public int end;
			
			public Segment(int start,int end)
			{
				this.start = start;
				this.end = end;
			}

			public override int GetHashCode()
			{
				return(start<<16 + end); 
			}
		}

		static Segment FindProtectedBlock(EHClausesArray clauses, int index)
		{
			return(new Segment(clauses[index].TryStart,clauses[index].TryEnd));
		}

		static Segment FindFilterBlock(EHClausesArray clauses, int index)
		{
			if(clauses[index].Kind != EHClauseKind.UserFilteredHandler)
				throw new ConvertionException();
			return(new Segment(clauses[index].FilterStart,clauses[index].FilterEnd));
		}

		public static MethodBodyBlock Convert(MethodEx method)
		{
			if(!method.IsVerified)
				throw new ConvertionException();
			MethodInfoExtention _method_ = new MethodInfoExtention(method.Method);
			MethodBodyBlock mainBlock = new MethodBodyBlock(_method_.GetReturnType().type);
			mainBlock.Options["StackTypes"] = new StackTypes();
			Block currentBlock = mainBlock;
			Node[] heads = new Node[method.Count];
			Node[] tails = new Node[method.Count];
			Node head = null;
			Node tail = null;
			Node firstBlock = null;
			Node lastBlock = null;
			int iNum,iNumNext;
			Variable[] locals = new Variable[method.Locals.Count];
			Variable[] args = new Variable[_method_.ArgCount];
			VariablesList methodVarList = mainBlock.Variables;
			for(int i=0;i<args.Length;i++)
			{
				args[i] = methodVarList.CreateVar(_method_.GetArgType(i).type, VariableKind.Parameter);
				args[i].Name = "Arg" + i;
//				methodVarList.Add(args[i]);
			}
			for(int i=0;i<locals.Length;i++)
			{
				locals[i] = methodVarList.CreateVar(method.Locals[i], VariableKind.Local);
				locals[i].Name = "Loc" + i;
//				methodVarList.Add(locals[i]);
			}

			BlockType nextBlockType;
			int nextBlockStart = -1;
			int nextBlockEnd = 1<<30;
			int nextBlockIndex;
			Type nextCatchBlockType;
			Hashtable tryBlocks = new Hashtable();
			Hashtable filterBlocks = new Hashtable();
			Stack blockEnds = new Stack(); 
			blockEnds.Push(method.Count);
			FindNextBlockStart(method.EHClauses,out nextBlockType,ref nextBlockStart, ref nextBlockEnd, out nextCatchBlockType, out nextBlockIndex);

			//Nodes and blocks creation, blocks linkage
			for(iNum = 0; iNum < method.Count ; iNum ++)
			{
				while(iNum == (int)blockEnds.Peek())
				{
					currentBlock = currentBlock.Parent;
					blockEnds.Pop();
				}
        
				firstBlock = null;
				lastBlock = null;
				Node thisBlock = null;
				while(iNum == nextBlockStart)
				{
					Block currentBlockOld = currentBlock;

					switch(nextBlockType)
					{
						case BlockType.Try:
							currentBlock = new ProtectedBlock();
							break;
						case BlockType.Catch:
							currentBlock = new CatchBlock(nextCatchBlockType);
							break;
						case BlockType.Finally:
							currentBlock = new FinallyBlock(false);
							break;
						case BlockType.Filter:
							currentBlock = new FilterBlock();
							break;
            case BlockType.FilteredCatch:
							currentBlock = new UserFilteredBlock();
							break;
					}

					currentBlock.setParent(currentBlockOld);

					blockEnds.Push(nextBlockEnd); 
					if(thisBlock == null)
						thisBlock = firstBlock = currentBlock;
					else
					{
						thisBlock.Next = currentBlock;
						thisBlock = currentBlock;
					}
					switch(nextBlockType)
					{
						case BlockType.Try: 
							tryBlocks.Add(new Segment(nextBlockStart,nextBlockEnd),thisBlock);
							break;
						case BlockType.Filter:
							filterBlocks.Add(new Segment(nextBlockStart,nextBlockEnd),thisBlock);
							break;
						case BlockType.Finally:
						case BlockType.Catch:
						{ 
							Segment tryBlockKey = FindProtectedBlock(method.EHClauses,nextBlockIndex);
							ProtectedBlock tryBlock = tryBlocks[tryBlockKey] as ProtectedBlock;
							tryBlock.AddHandler(thisBlock as EHBlock);
						}	break;
						case BlockType.FilteredCatch:
						{
							Segment tryBlockKey = FindProtectedBlock(method.EHClauses,nextBlockIndex);
							ProtectedBlock tryBlock = tryBlocks[tryBlockKey] as ProtectedBlock;
							tryBlock.AddHandler(thisBlock as EHBlock);
							
							Segment filterKey = FindFilterBlock(method.EHClauses,nextBlockIndex);
							FilterBlock filterBlock = filterBlocks[filterKey] as FilterBlock;
							(thisBlock as UserFilteredBlock).Filter = filterBlock;
						}	break;
					}
					FindNextBlockStart(method.EHClauses,out nextBlockType,ref nextBlockStart, ref nextBlockEnd,out nextCatchBlockType, out nextBlockIndex);
				}
				lastBlock = thisBlock;

				Instruction i = method[iNum];
				switch(i.Code)
				{
					case InstructionCode.NEG:
					case InstructionCode.NOT:
					{
						head = tail = new UnaryOp(UnaryOpFromCode(i.Code));
						/*!*/head.setParent(currentBlock);
					} break;

					case InstructionCode.ADD:
					case InstructionCode.AND:
					case InstructionCode.CEQ:
					case InstructionCode.CGT:
					case InstructionCode.CLT:
					case InstructionCode.DIV:
					case InstructionCode.MUL:
					case InstructionCode.OR:
					case InstructionCode.REM:
					case InstructionCode.SHL:
					case InstructionCode.SHR:
					case InstructionCode.SUB:
					case InstructionCode.XOR:
					{
						head = tail = new BinaryOp(BinaryOpFromCode(i.Code),i.OverflowFlag,i.UnsignedFlag);
						/*!*/head.setParent(currentBlock);
					} break;

					case InstructionCode.LDC:
						head = tail = new LoadConst(i.Param);
						/*!*/head.setParent(currentBlock);
						break;

					case InstructionCode.LDARG:
						head = tail = new LoadVar(args[(int)(i.Param)]);
						/*!*/head.setParent(currentBlock);
						break;
					case InstructionCode.LDLOC:
						head = tail = new LoadVar(locals[(int)(i.Param)]);
						/*!*/head.setParent(currentBlock);
						break;
					case InstructionCode.LDARGA:
						head = tail = new LoadVarAddr(args[(int)(i.Param)]);
						/*!*/head.setParent(currentBlock);
						break;
					case InstructionCode.LDLOCA:
						head = tail = new LoadVarAddr(locals[(int)(i.Param)]);
						/*!*/head.setParent(currentBlock);
						break;
					case InstructionCode.LDIND:
						head = tail = new LoadIndirect(i.TypeBySuffixOrParam());
						/*!*/head.setParent(currentBlock);
						break;
					case InstructionCode.LDFLD:
					{
						FieldInfo field = i.Param as FieldInfo;
						if(field.IsStatic)
						{
							head = new RemoveStackTop();
							/*!*/head.setParent(currentBlock);
							//remove the object instance when accessing the static field with LDFLD
							tail = new LoadField(field);
							/*!*/tail.setParent(currentBlock);
							head.Next = tail;
						}
						else
						{
							head = tail = new LoadField(field);
							/*!*/head.setParent(currentBlock);
						}
					}	break;
					case InstructionCode.LDFLDA:
					{
						FieldInfo field = i.Param as FieldInfo;
						if(field.IsStatic)
						{
							head = new RemoveStackTop();
							/*!*/head.setParent(currentBlock);
							tail = new LoadFieldAddr(field);
							/*!*/tail.setParent(currentBlock);
							head.Next = tail;
						}
						else
						{
							head = tail = new LoadFieldAddr(field);
							/*!*/head.setParent(currentBlock);
						}
					}	break;
					case InstructionCode.LDSFLD:
						head = tail = new LoadField(i.Param as FieldInfo);
						/*!*/head.setParent(currentBlock);
						break;
					case InstructionCode.LDSFLDA:
						head = tail = new LoadFieldAddr(i.Param as FieldInfo);
						/*!*/head.setParent(currentBlock);
						break;
					case InstructionCode.LDELEM:
						head = tail = new LoadElement(i.TypeBySuffixOrParam());
						/*!*/head.setParent(currentBlock);
						break;
					case InstructionCode.LDELEMA:
						head = tail = new LoadElementAddr(i.TypeBySuffixOrParam());
						/*!*/head.setParent(currentBlock);
						break;
					case InstructionCode.LDOBJ:
						head = tail = new LoadIndirect(i.Param as Type);
						/*!*/head.setParent(currentBlock);
						break;
					case InstructionCode.SIZEOF:
						head = tail = new LoadSizeOfValue(i.Param as Type);
						/*!*/head.setParent(currentBlock);
						break;
					case InstructionCode.LDLEN:
						head = tail = new LoadLength();
						/*!*/head.setParent(currentBlock);
						break;
					case InstructionCode.LDTOKEN:
						if(i.Param is Type)
							head = tail = new LoadConst((i.Param as Type).TypeHandle);
						else if(i.Param is MethodBase)
							head = tail = new LoadConst((i.Param as MethodBase).MethodHandle);
						else if(i.Param is FieldInfo)
							head = tail = new LoadConst((i.Param as FieldInfo).FieldHandle);
						else 
							throw new ConvertionException();
						/*!*/head.setParent(currentBlock);
						break;
					case InstructionCode.LDNULL:
						head = tail = new LoadConst(null);
						/*!*/head.setParent(currentBlock);
						break; 
					case InstructionCode.LDSTR:
						head = tail = new LoadConst(i.Param);
						/*!*/head.setParent(currentBlock);
						break; 

					case InstructionCode.STARG:
						head = tail = new StoreVar(args[(int)(i.Param)]);
						/*!*/head.setParent(currentBlock);
						break;
					case InstructionCode.STLOC:
						head = tail = new StoreVar(locals[(int)(i.Param)]);
						/*!*/head.setParent(currentBlock);
						break;
					case InstructionCode.STIND:
						head = tail = new StoreIndirect(i.TypeBySuffixOrParam());
						/*!*/head.setParent(currentBlock);
						break;
					case InstructionCode.STFLD:
					{
						FieldInfo field = i.Param as FieldInfo;
						if(field.IsStatic)
						{
							head = new StoreField(field);
							/*!*/head.setParent(currentBlock);
							tail = new RemoveStackTop();
							/*!*/tail.setParent(currentBlock);
							head.Next = tail;
						}
						else
						{
							head = tail = new StoreField(i.Param as FieldInfo);
							/*!*/head.setParent(currentBlock);
						}
					}	break;
					case InstructionCode.STSFLD:
						head = tail = new StoreField(i.Param as FieldInfo);
						/*!*/head.setParent(currentBlock);
						break;
					case InstructionCode.STELEM:
						head = tail = new StoreElement(i.TypeBySuffixOrParam());
						/*!*/head.setParent(currentBlock);
						break;
					case InstructionCode.STOBJ:
						head = tail = new StoreIndirect(i.Param as Type);
						/*!*/head.setParent(currentBlock);
						break;
          case InstructionCode.CPOBJ:
						head = new LoadIndirect(i.Param as Type);
						/*!*/head.setParent(currentBlock); 
						tail = new StoreIndirect(i.Param as Type);
						/*!*/tail.setParent(currentBlock);
						head.Next = tail;
						break;
					case InstructionCode.DUP:
						head = tail = new DuplicateStackTop();
						/*!*/head.setParent(currentBlock);
						break;

					case InstructionCode.CALL:
						head = tail = new CallMethod(i.Param as MethodBase,false,i.HasTail);
						/*!*/head.setParent(currentBlock);
						break;
					case InstructionCode.CALLVIRT:
						MethodInfo callee = i.Param as MethodInfo;
						head = tail = new CallMethod(callee, callee.IsVirtual, i.HasTail);
						/*!*/head.setParent(currentBlock);
						break;
					case InstructionCode.NEWOBJ:
					{ ConstructorInfo ctor = i.Param as ConstructorInfo; 
						if(Verifier.IsDelegate(ctor.DeclaringType))
						{
							if(Verifier.IsInstanceDispatch(method,iNum))
							{
								heads[iNum-1] = tails[iNum-1] = null;
								head = tail = new CreateDelegate(ctor, method[iNum-1].Param as MethodInfo, false);
							}
							else if(Verifier.IsVirtualDispatch(method,iNum))
							{
								heads[iNum-2] = tails[iNum-2] = null;
								heads[iNum-1] = tails[iNum-1] = null;
								head = tail = new CreateDelegate(ctor, method[iNum-1].Param as MethodInfo, true);
							}
						}
						else
						  head = tail = new NewObject(ctor);
						/*!*/head.setParent(currentBlock);
					}	break;
					case InstructionCode.NEWARR:
						head = tail = new NewArray(i.Param as Type); 
						/*!*/head.setParent(currentBlock);
						break;
					case InstructionCode.INITOBJ:
						head = tail = new InitValue(i.Param as Type); 
						/*!*/head.setParent(currentBlock);
						break;

					case InstructionCode.ISINST:
						head = tail = new CastClass(i.Param as Type,false);
						/*!*/head.setParent(currentBlock);
						break; 
					case InstructionCode.CASTCLASS: 
						head = tail = new CastClass(i.Param as Type,true);
						/*!*/head.setParent(currentBlock);
						break; 


					case InstructionCode.BOX:
						head = tail = new BoxValue(i.Param as Type);
						/*!*/head.setParent(currentBlock);
						break;
					case InstructionCode.UNBOX:
						head = tail = new UnboxValue(i.Param as Type);
						/*!*/head.setParent(currentBlock);
						break;

					case InstructionCode.CONV:
						head = tail = new ConvertValue(i.TypeBySuffixOrParam(),i.OverflowFlag,i.UnsignedFlag);
						/*!*/head.setParent(currentBlock);
						break;

					case InstructionCode.POP:
						head = tail = new RemoveStackTop();
						/*!*/head.setParent(currentBlock);
						break;

					case InstructionCode.BEQ:
						head = new BinaryOp(BinaryOp.ArithOp.CEQ,false,false);
						/*!*/head.setParent(currentBlock);
						tail = new Branch();
						/*!*/tail.setParent(currentBlock);
						head.Next = tail;
						break;
					case InstructionCode.BNE:
						head = new BinaryOp(BinaryOp.ArithOp.CEQ,false,false);
						/*!*/head.setParent(currentBlock);
						tail = new Branch();
						/*!*/tail.setParent(currentBlock);
						head.Next = tail;
						break;
					case InstructionCode.BGE:
                        if(TypeFixer.IsFloatOrCompatible(i.Stack.Top()))
                            head = new BinaryOp(BinaryOp.ArithOp.CLT,false,! i.UnsignedFlag);
                        else
                            head = new BinaryOp(BinaryOp.ArithOp.CLT,false,i.UnsignedFlag);
                        tail = new Branch();
						/*!*/head.setParent(currentBlock);
						/*!*/tail.setParent(currentBlock);
						head.Next = tail;
						break;
					case InstructionCode.BGT:
						head = new BinaryOp(BinaryOp.ArithOp.CGT,false,i.UnsignedFlag);
						tail = new Branch();
						/*!*/head.setParent(currentBlock);
						/*!*/tail.setParent(currentBlock);
						head.Next = tail;
						break;
					case InstructionCode.BLE:
						if(TypeFixer.IsFloatOrCompatible(i.Stack.Top()))
							head = new BinaryOp(BinaryOp.ArithOp.CGT,false,! i.UnsignedFlag);
						else
							head = new BinaryOp(BinaryOp.ArithOp.CGT,false,i.UnsignedFlag);
                        tail = new Branch();
                        /*!*/head.setParent(currentBlock);
						/*!*/tail.setParent(currentBlock);
						head.Next = tail;
						break;
					case InstructionCode.BLT:
						head = new BinaryOp(BinaryOp.ArithOp.CLT,false,i.UnsignedFlag);
						tail = new Branch();
						/*!*/head.setParent(currentBlock);
						/*!*/tail.setParent(currentBlock);
						head.Next = tail;
						break;

					case InstructionCode.BRTRUE:
						head = tail = new Branch();
						/*!*/head.setParent(currentBlock);
						break;
					case InstructionCode.BRFALSE:
						head = tail = new Branch();
						/*!*/head.setParent(currentBlock);
						break;
					case InstructionCode.SWITCH:
						head = tail = new Switch((i.Param as int[]).Length);
						/*!*/head.setParent(currentBlock);
						break;

					case InstructionCode.BR:
					case InstructionCode.NOP:
					case InstructionCode.BREAK:
					case InstructionCode.LDFTN:     // Expecting further delegate construction...
					case InstructionCode.LDVIRTFTN: //
						head = tail = new DummyNode();
                      /*!*/head.setParent(currentBlock);
						break;
					case InstructionCode.THROW:
						head = tail = new ThrowException();
						/*!*/head.setParent(currentBlock);
						break;

 
					case InstructionCode.RET:
					case InstructionCode.ENDFINALLY:
					case InstructionCode.ENDFILTER:
					case InstructionCode.LEAVE:
						head = tail = new Leave();
						/*!*/head.setParent(currentBlock);
						break;

					default: 
						throw new ConvertionException();
				}
				if(head != null)
					head.Options["StackTypes"] = i.Stack.Clone() as StackTypes;
				if(head != tail) //=>   head :: BinaryOp, tail :: Branch
					             //||   head :: LoadIndirect, tail :: StoreIndirect
				{
					if(head is BinaryOp && tail is Branch)
					{
						StackTypes stack = i.Stack.Clone() as StackTypes;
						stack.Pop();
						stack.Pop();
						stack.Push(typeof(int));
						tail.Options["StackTypes"] = stack;
					}
					else if(head is LoadIndirect && tail is StoreIndirect)
					{
						StackTypes stack = i.Stack.Clone() as StackTypes;
						TypeEx type = stack.Pop(); //type == S&
						stack.Push(type.type.GetElementType());
						tail.Options["StackTypes"] = stack;
					}
				}
				if(firstBlock != null)
				{
					lastBlock.Next = head;
					for(Node n = firstBlock  ;  n!=head  ;  n = n.Next)
						n.Options["StackTypes"] = i.Stack.Clone() as StackTypes;
					head = firstBlock;
					if(tail == null)
						tail = lastBlock; //This may occure what the NOP instruction starts some block
				}
				heads[iNum] = head;
				tails[iNum] = tail;
			}//for
			mainBlock.Next = heads[0]; 			
			//Control flow linkage
			for(iNum=0; iNum<method.Count; iNum++)
			{
				if(heads[iNum] == null)
					throw new ConvertionException(); //impossible :)

				Instruction i = method[iNum];
				
				switch(i.Code)
				{

					case InstructionCode.BR:
					case InstructionCode.LEAVE:
						tails[iNum].Next = heads[(int)i.Param];
						break;

					case InstructionCode.RET:
					case InstructionCode.ENDFINALLY:
					case InstructionCode.ENDFILTER:
					case InstructionCode.THROW:
					case InstructionCode.RETHROW:
						break; 

					case InstructionCode.BRFALSE: //false
					case InstructionCode.BGE: //false
					case InstructionCode.BLE: //false
					case InstructionCode.BNE: //false
						tails[iNum].Next = heads[(int)i.Param];
						(tails[iNum] as Branch).Alt = heads[iNum+1];      
						break;

					case InstructionCode.BRTRUE: //true
					case InstructionCode.BEQ: //true
					case InstructionCode.BGT: //true
					case InstructionCode.BLT: //true
						tails[iNum].Next = heads[iNum+1];
						(tails[iNum] as Branch).Alt = heads[(int)i.Param];      
						break;

					case InstructionCode.SWITCH:
						tails[iNum].Next = heads[iNum+1];
						Switch node = tails[iNum] as Switch;
						int[] alt = i.Param as int[];
							for(int j=0;j<node.Count;j++)
								node[j] = heads[alt[j]];
						break;
					default:
						tails[iNum].Next = heads[iNum+1];
						break;
				}
			}

			//Removing DummyNodes
			for(iNum=0; iNum<method.Count; iNum++)
			{
				if(heads[iNum] is DummyNode)
				{
					Node dummy = heads[iNum];
					Node[] prev = new Node[dummy.PrevArray.Count];
					for(int j=0; j<prev.Length; j++)
						prev[j] = dummy.PrevArray[j];
					for(int j=0; j<prev.Length; j++)
						prev[j].NextArray[ prev[j].NextArray.IndexOf(dummy) ] = dummy.Next; 
					dummy.RemoveFromGraph();
				}
			}


			return(mainBlock);
		}

	}
}

