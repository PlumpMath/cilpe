
// ===========================================================================
// CILPE - Partial Evaluator for Common Intermediate Language
// ===========================================================================
// File: 
//     BasicBlocks.cs
//
// Description:
//     Basic blocks, their calculation and linearization
//
// Author: 
//     Sergei Skorobogatov (Sergei.Skorobogatov@supercompilers.com)
// ===========================================================================


using System;
using System.Collections;
using CILPE.ReflectionEx;

namespace CILPE.CFG
{
    public class BasicBlockArray: IEnumerable
    {
        #region Private and internal members

        internal ArrayList blocks;

        internal BasicBlockArray() { blocks = new ArrayList(); }

        #endregion

        public BasicBlock this [int index]
        {
            get { return blocks[index] as BasicBlock; }
        }

        public int Count { get { return blocks.Count; } }

        public void Remove(BasicBlock block) { blocks.Remove(block); }

        /* Returns an enumerator that can iterate through nodes */
        public IEnumerator GetEnumerator() { return blocks.GetEnumerator(); }
    }

    public class BasicBlock: IFormattable
    {
        #region Private and internal members

        private BasicBlockArray prev, next, links;
        private NodeArray body;
        internal int index = 0;

        private void AddPrevBasicBlock(BasicBlock prevBasicBlock)
        {
            if (prev.blocks.IndexOf(prevBasicBlock) == -1)
                prev.blocks.Add(prevBasicBlock);
        }

        internal void AddNextBasicBlock(BasicBlock nextBasicBlock)
        {
            if (next.blocks.IndexOf(nextBasicBlock) == -1)
            {
                next.blocks.Add(nextBasicBlock);
                nextBasicBlock.AddPrevBasicBlock(this);
            }
        }

        internal void AddLink(BasicBlock link)
        {
            if (links.blocks.IndexOf(link) == -1)
                links.blocks.Add(link);
        }

        internal void AddNode(Node node)
        {
            body.Add(node);
            node.Options[BASIC_BLOCK_OPTION] = this;
        }

        #endregion

        public const string BASIC_BLOCK_OPTION = "Basic block";

        public BasicBlock()
        {
            prev = new BasicBlockArray();
            next = new BasicBlockArray();
            links = new BasicBlockArray();
            body = new NodeArray();
        }

        public BasicBlockArray Prev { get { return prev; } }

        public BasicBlockArray Next { get { return next; } }

        public BasicBlockArray Links { get { return links; } }

        public NodeArray Body { get { return body; } }

        public int Index { get { return index; } }

		public virtual string ToString(string format, IFormatProvider formatProvider,string[] options)
		{
			string result = "@"+Index+":\n";

			foreach (Node node in Body)
				result += "    " + node.ToString(format,formatProvider,options) + "\n";

			if(Body.Count>0) //Andrew
			{
				Node lastNode = Body[Body.Count-1];
				if (! lastNode.IsLeaf)
					result += "    (next " + Node.FormatBranchTarget(lastNode.Next) + ")\n";
			}

			return result;
		}

        public virtual string ToString(string format, IFormatProvider formatProvider)
        {
            return ToString(format,formatProvider,null);
        }

        public override string ToString()
        {
            return ToString("CSharp",ReflectionFormatter.formatter);
        }
    }

    internal class BasicBlocksBuilder: StackVisitor
    {
        #region Private and internal members

        private BasicBlockArray blockList;

        internal BasicBlock createBasicBlock()
        {
            BasicBlock result = new BasicBlock();
            result.index = blockList.Count;
            blockList.blocks.Add(result);
            return result;
        }

        #endregion

        protected override void DispatchNode(Node node, object data)
        {
            SiftNodes(node,ref data);

            if (node is Block)
                SiftBlocks(node as Block,data);
            else if (node is Branch || node is Switch)
                CallVisitorMethod(node,data);
            else
                SiftSerials(node,data);
        }

        protected void SiftNodes(Node node, ref object data)
        {
            BasicBlock basicBlock = data as BasicBlock;

            if (basicBlock.Body.Count > 0 && (node.PrevArray.Count > 1 || node is Block))
            {
                BasicBlock newBlock = createBasicBlock();
                basicBlock.AddNextBasicBlock(newBlock);
                data = newBlock;
            }
        }

        protected void SiftBlocks(Block node, object data)
        {
            BasicBlock basicBlock = data as BasicBlock;
            basicBlock.AddNode(node);

            if (node is ProtectedBlock)
            {
                foreach (EHBlock block in (node as ProtectedBlock))
                {
                    if (block.Options.ContainsOption(BasicBlock.BASIC_BLOCK_OPTION))
                        basicBlock.AddLink(block.Options[BasicBlock.BASIC_BLOCK_OPTION] as BasicBlock);
                    else
                    {
                        BasicBlock newBlock = createBasicBlock();
                        basicBlock.AddLink(newBlock);
                        AddTask(block,newBlock);
                    }
                }
            }
            else if (node is UserFilteredBlock)
            {
                FilterBlock block = (node as UserFilteredBlock).Filter;

                if (block.Options.ContainsOption(BasicBlock.BASIC_BLOCK_OPTION))
                    basicBlock.AddLink(block.Options[BasicBlock.BASIC_BLOCK_OPTION] as BasicBlock);
                else
                {
                    BasicBlock newBlock = createBasicBlock();
                    basicBlock.AddLink(newBlock);
                    AddTask(block,newBlock);
                }
            }

            if (node.Next.Options.ContainsOption(BasicBlock.BASIC_BLOCK_OPTION))
                basicBlock.AddNextBasicBlock(node.Next.Options[BasicBlock.BASIC_BLOCK_OPTION] as BasicBlock);
            else
            {
                BasicBlock nextBlock = createBasicBlock();
                basicBlock.AddNextBasicBlock(nextBlock);
                AddTask(node.Next,nextBlock);
            }
        }

        protected internal override void VisitBranch(Branch node, object data)
        {
            if (node.Next == node.Alt)
                SiftSerials(node,data);
            else
            {
                BasicBlock basicBlock = data as BasicBlock;
                basicBlock.AddNode(node);

                if (node.Next.Options.ContainsOption(BasicBlock.BASIC_BLOCK_OPTION))
                    basicBlock.AddNextBasicBlock(node.Next.Options[BasicBlock.BASIC_BLOCK_OPTION] as BasicBlock);
                else
                {
                    BasicBlock nextBlock = createBasicBlock();
                    basicBlock.AddNextBasicBlock(nextBlock);
                    AddTask(node.Next,nextBlock);
                }

                if (node.Alt.Options.ContainsOption(BasicBlock.BASIC_BLOCK_OPTION))
                    basicBlock.AddNextBasicBlock(node.Alt.Options[BasicBlock.BASIC_BLOCK_OPTION] as BasicBlock);
                else
                {
                    BasicBlock altBlock = createBasicBlock();
                    basicBlock.AddNextBasicBlock(altBlock);
                    AddTask(node.Alt,altBlock);
                }
            }
        }

        protected internal override void VisitSwitch(Switch node, object data)
        {
            BasicBlock basicBlock = data as BasicBlock;
            basicBlock.AddNode(node);

            if (node.Next.Options.ContainsOption(BasicBlock.BASIC_BLOCK_OPTION))
                basicBlock.AddNextBasicBlock(node.Next.Options[BasicBlock.BASIC_BLOCK_OPTION] as BasicBlock);
            else
            {
                BasicBlock nextBlock = createBasicBlock();
                basicBlock.AddNextBasicBlock(nextBlock);
                AddTask(node.Next,nextBlock);
            }

            foreach (Node alt in node)
            {
                if (alt.Options.ContainsOption(BasicBlock.BASIC_BLOCK_OPTION))
                    basicBlock.AddNextBasicBlock(alt.Options[BasicBlock.BASIC_BLOCK_OPTION] as BasicBlock);
                else
                {
                    BasicBlock altBlock = createBasicBlock();
                    basicBlock.AddNextBasicBlock(altBlock);
                    AddTask(alt,altBlock);
                }
            }
        }

        protected void SiftSerials(Node node, object data)
        {
            BasicBlock basicBlock = data as BasicBlock;
            basicBlock.AddNode(node);

            if (node.Next != null)
            {
                if (node.Next.Options.ContainsOption(BasicBlock.BASIC_BLOCK_OPTION))
                    basicBlock.AddNextBasicBlock(node.Next.Options[BasicBlock.BASIC_BLOCK_OPTION] as BasicBlock);
                else if (node is Leave)
                {
                    BasicBlock nextBlock = createBasicBlock();
                    basicBlock.AddNextBasicBlock(nextBlock);
                    AddTask(node.Next,nextBlock);
                }
                else
                    AddTask(node.Next,basicBlock);
            }
        }

        public BasicBlocksBuilder(GraphProcessor graphProcessor):
            base(graphProcessor)
        {
            blockList = new BasicBlockArray();
        }

        public BasicBlockArray BlockList { get { return blockList; } }
    }

    public class BasicBlocksGraph: IFormattable
    {
		#region Private classes

		private class BasicBlockStub
		{
			private ArrayList prev, next;
			private bool isEmpty;
			private NodeArray usageArray;

			private void addPrev(BasicBlockStub stub)
			{
				if (! prev.Contains(stub))
					prev.Add(stub);
			}

			private void removePrev(BasicBlockStub stub) { prev.Remove(stub); }
			private void removeNext(BasicBlockStub stub) { next.Remove(stub); }

			public BasicBlockStub(Variable var, BasicBlock block)
			{
				prev = new ArrayList();
				next = new ArrayList();

				isEmpty = true;
				foreach (ManageVar node in var.UsersArray)
					isEmpty &= (node.Options[BasicBlock.BASIC_BLOCK_OPTION] as BasicBlock) != block;

				usageArray = new NodeArray();
				if (! isEmpty)
				{
					foreach (Node node in block.Body)
						if (node is ManageVar)
						{
							ManageVar usage = node as ManageVar;
							if (usage.Var == var)
								usageArray.Add(usage);
						}
				}
			}

			public bool IsEmpty { get { return isEmpty; } }

		    public IReadonlyNodeArray UsageArray { get { return usageArray as IReadonlyNodeArray; } }

			public void AddNext(BasicBlockStub stub)
			{
				if (! next.Contains(stub))
				{
					next.Add(stub);
					stub.addPrev(this);
				}
			}

			public void Reduce()
			{
				if (isEmpty)
				{
					foreach (BasicBlockStub prevStub in prev)
						prevStub.removeNext(this);

					foreach (BasicBlockStub nextStub in next)
						nextStub.removePrev(this);

					foreach (BasicBlockStub prevStub in prev)
						foreach (BasicBlockStub nextStub in next)
							prevStub.AddNext(nextStub);
				}
			}

			public void AddToUsage(VarUsage usage)
			{
				if (! isEmpty)
				{
					for (int i = 0; i < UsageArray.Count-1; i++)
						if (UsageArray[i] is StoreVar)
						{
							StoreVar storer = UsageArray[i] as StoreVar;
							if (UsageArray[i+1] is LoadVar)
								usage.addUsage(storer,UsageArray[i+1] as LoadVar);
						}

					if (UsageArray.Count > 0 && UsageArray[UsageArray.Count-1] is StoreVar)
					{
						StoreVar storer = UsageArray[UsageArray.Count-1] as StoreVar;

						bool flag = true;
						for (int i = 0; i < next.Count && flag; i++)
						{
							BasicBlockStub nextStub = next[i] as BasicBlockStub;
							if (nextStub.UsageArray[0] is LoadVar)
							{
								usage.addUsage(storer,nextStub.UsageArray[0] as LoadVar);
								flag = false;
							}
						}
					}
				}
			}
		}

		private class VarUsage
		{
			private Hashtable usageHash;

			internal void addUsage(StoreVar storer, LoadVar loader)
			{
				usageHash.Add(storer,loader);
			}

			public VarUsage() { usageHash = new Hashtable(); }

			public bool IsUsed(StoreVar storer)
			{
				return usageHash.ContainsKey(storer);
			}

			public LoadVar this [StoreVar storer]
			{
				get { return usageHash[storer] as LoadVar; }
			}
		}

		#endregion

        #region Private and internal members

        private const string VAR_CATEGORY_OPTION = "Category of variable";

        MethodBodyBlock mbb;
        BasicBlock entry;
        BasicBlockArray blockList;

        private void categorizeVariables()
        {
            foreach (Variable var in mbb.Variables)
            {
                bool notReferencedFlag = true;
                for (int i = 0; i < var.UsersArray.Count && notReferencedFlag; i++)
                    if (var.UsersArray[i] is LoadVarAddr)
                        notReferencedFlag = false;

                var.Options[VAR_CATEGORY_OPTION] = notReferencedFlag;
            }
        }

        private static bool varIsNotReferenced(Variable var)
        {
            return (bool)(var.Options[VAR_CATEGORY_OPTION]);
        }

        private DuplicateStackTop newDuplicateStackTop(BasicBlock block)
        {
            DuplicateStackTop result = new DuplicateStackTop();
            result.Options[BasicBlock.BASIC_BLOCK_OPTION] = block;
            return result;
        }

        private RemoveStackTop newRemoveStackTop(BasicBlock block)
        {
            RemoveStackTop result = new RemoveStackTop();
            result.Options[BasicBlock.BASIC_BLOCK_OPTION] = block;
            return result;
        }

        private bool constantsAreEqual(object a, object b)
        {
            bool result = false;

            if (a == null || b == null)
                result = a == b;
            else
                result = a.Equals(b);

            return result;
        }

        private bool detectedDoubleLoading(Node n1, Node n2)
        {
            bool result = n1 is LoadSizeOfValue && n2 is LoadSizeOfValue &&
                (n1 as LoadSizeOfValue).Type == (n2 as LoadSizeOfValue).Type;

            result |= (n1 is LoadVar && n2 is LoadVar || n1 is LoadVarAddr && n2 is LoadVarAddr) &&
                (n1 as ManageVar).Var == (n2 as ManageVar).Var;

            return result;
        }

        private bool detectedDoubleCasting(Node n1, Node n2)
        {
            bool result = n1 is CastClass && n2 is CastClass &&
                (n1 as CastClass).Type == (n2 as CastClass).Type;

            result |= n1 is CheckFinite && n2 is CheckFinite;

            return result;
        }

        private bool detectedReplacementByPop(Node n1, Node n2)
        {
            bool result = n2 is RemoveStackTop;

            if (result)
            {
                result = 
                    n1 is UnaryOp || 
                    n1 is ConvertValue && ! (n1 as ConvertValue).Overflow ||
                    n1 is LoadIndirect ||
                    n1 is LoadField && ! (n1 as LoadField).Field.IsStatic ||
                    n1 is LoadFieldAddr && ! (n1 as LoadFieldAddr).Field.IsStatic ||
                    n1 is CastClass && ! (n1 as CastClass).ThrowException ||
                    n1 is LoadLength || n1 is NewArray || n1 is BoxValue ||
                    n1 is UnboxValue || n1 is LoadSizeOfValue || n1 is MakeTypedRef ||
                    n1 is RetrieveType || n1 is RetrieveValue;
            }

            return result;
        }

        private bool detectedReplacementByDoublePop(Node n1, Node n2)
        {
            bool result = n2 is RemoveStackTop;

            if (result)
            {
                result = 
                    n1 is BinaryOp && ! (n1 as BinaryOp).Overflow ||
                    n1 is LoadElement || n1 is LoadElementAddr;
            }

            return result;
        }

        private static bool typeIsInteger(Type type)
        {
            return
                type == typeof(SByte) || type == typeof(short) || type == typeof(int) ||
                type == typeof(long) || type == typeof(IntPtr) ||
                type == typeof(Byte) || type == typeof(ushort) || type == typeof(uint) ||
                type == typeof(ulong) || type == typeof(UIntPtr) ||
                type == typeof(bool) || type == typeof(char) || type.IsEnum;
        }

        private bool detectedRemoval(Node n1, Node n2)
        {
            bool result = false;

            if (n2 is RemoveStackTop)
            {
                result = 
                    n1 is LoadConst || n1 is LoadVar ||
                    n1 is LoadVarAddr || n1 is DuplicateStackTop ||
                    n1 is LoadField && (n1 as LoadField).Field.IsStatic ||
                    n1 is LoadFieldAddr && (n1 as LoadFieldAddr).Field.IsStatic ||
                    n1 is LoadVar && n2 is StoreVar &&
                    (n1 as LoadVar).Var == (n2 as LoadVar).Var;
            }
            else if (n1 is LoadConst && n2 is BinaryOp)
            {
                object constantObj = (n1 as LoadConst).Constant;
                BinaryOp.ArithOp op = (n2 as BinaryOp).Op;

				if (constantObj != null &&
					(constantObj.GetType() == typeof(Int32) ||
					 constantObj.GetType() == typeof(Int64) ||
					 constantObj.GetType() == typeof(Double)))
                {
                    double constant;

                    if (constantObj.GetType() == typeof(Int32))
                        constant = (Int32)constantObj;
                    else if (constantObj.GetType() == typeof(Int64))
                        constant = (Int64)constantObj;
                    else
                        constant = (Double)constantObj;

                    result =
                        constant == 0 && (op == BinaryOp.ArithOp.ADD || op == BinaryOp.ArithOp.SUB) ||
                        constant == 1.0 && (op == BinaryOp.ArithOp.MUL || op == BinaryOp.ArithOp.DIV);
                }
            }

            return result;
        }

		private bool detectedInitValueReplacement(Node n1, Node n2)
		{
			bool result = n1 is LoadVarAddr && n2 is InitValue;

			if (result)
			{
				InitValue initNode = n2 as InitValue;
				result = initNode.Type.IsPrimitive;
			}
		
			return result;
		}

        private bool detectedOperandExchange(Node n1, Node n2, Node n3, Node n4)
        {
            bool result = n1 is StoreVar && (n2 is LoadVar || n2 is LoadConst) &&
                n3 is LoadVar && n4 is BinaryOp;

            if (result)
            {
                result = (n1 as StoreVar).Var == (n3 as LoadVar).Var;

                if (result)
                {
                    BinaryOp.ArithOp op = (n4 as BinaryOp).Op;

                    result = op == BinaryOp.ArithOp.ADD || op == BinaryOp.ArithOp.AND ||
                        op == BinaryOp.ArithOp.CEQ || op == BinaryOp.ArithOp.MUL ||
                        op == BinaryOp.ArithOp.OR || op == BinaryOp.ArithOp.XOR;
                }
            }

            return result;
        }

        private bool detectedQuadruple1(Node n1, Node n2, Node n3, Node n4)
        {
            bool result = n1 is DuplicateStackTop && n2 is StoreVar &&
                n3 is StoreVar && n4 is LoadVar;

            if (result)
                result = (n2 as StoreVar).Var == (n4 as LoadVar).Var;

            return result;
        }

        private bool performPatternReplacing()
        {
            bool result = false;

            foreach (BasicBlock block in blockList)
            {
                NodeArray body = block.Body;

                bool flag = true;
                while (flag)
                {
                    flag = false;
                    for (int i = 0; i < body.Count-1 && ! flag; i++)
                    {
                        Node n1 = body[i];

                        if (n1 is Branch && (n1 as Branch).Next == (n1 as Branch).Alt)
                        {
                            result = flag = true;
                            Node n = newRemoveStackTop(block);
                            body[i] = n;
                            n1.ReplaceByNode(n);
                            n.Next = n1.Next;
                            n1.RemoveFromGraph();
                        }
                        else
                        {
                            Node n2 = body[i+1];

                            if (n1 is StoreVar && n2 is LoadVar &&
                                (n1 as ManageVar).Var == (n2 as ManageVar).Var)
                            {
                                result = flag = true;
                                Node n = newDuplicateStackTop(block);
                                body[i] = n;
                                body[i+1] = n1;
                                n1.ReplaceByNode(n);
                                n.Next = n1;
                                n1.Next = n2.Next;
                                n2.RemoveFromGraph();
                            }
                            else if (detectedDoubleLoading(n1,n2))
                            {
                                result = flag = true;
                                Node n = newDuplicateStackTop(block);
                                body[i+1] = n;
                                n2.ReplaceByNode(n);
                                n.Next = n2.Next;
                                n2.RemoveFromGraph();
                            }
                            else if (detectedDoubleCasting(n1,n2))
                            {
                                result = flag = true;
                                body.RemoveAt(i+1);
                                n1.Next = n2.Next;
                                n2.RemoveFromGraph();
                            }
                            else if (n1 is StoreVar && n2 is Leave &&
                                n2.Parent is MethodBodyBlock)
                            {
                                result = flag = true;
                                Node n = newRemoveStackTop(block);
                                body[i] = n;
                                n1.ReplaceByNode(n);
                                n1.RemoveFromGraph();
                                n.Next = n2;
                            }
                            else if (detectedReplacementByPop(n1,n2))
                            {
                                result = flag = true;
                                Node n = newRemoveStackTop(block);
                                body.RemoveAt(i+1);
                                body[i] = n;
                                n1.ReplaceByNode(n);
                                n1.RemoveFromGraph();
                                n.Next = n2.Next;
                                n2.RemoveFromGraph();
                            }
                            else if (detectedReplacementByDoublePop(n1,n2))
                            {
                                result = flag = true;
                                Node new1 = newRemoveStackTop(block),
                                    new2 = newRemoveStackTop(block);
                                body[i] = new1;
                                body[i+1] = new2;
                                n1.ReplaceByNode(new1);
                                n1.RemoveFromGraph();
                                new1.Next = new2;
                                new2.Next = n2.Next;
                                n2.RemoveFromGraph();
                            }
                            else if (detectedRemoval(n1,n2))
                            {
                                result = flag = true;
                                body.RemoveAt(i+1);
                                body.RemoveAt(i);
                                n1.ReplaceByNode(n2.Next);
                                n1.RemoveFromGraph();
                                n2.RemoveFromGraph();
                            }
                            else if (detectedInitValueReplacement(n1,n2))
                            {
                                result = flag = true;
                                Variable var = (n1 as LoadVarAddr).Var;
                                Type type = (n2 as InitValue).Type;
							
                                object constant = null;
                                if (type == typeof(Int64))
                                    constant = (Int64)0;
                                else if (type == typeof(Single) || type == typeof(Double))
                                    constant = (Double)0;
                                else
                                    constant = (Int32)0;

                                Node new1 = new LoadConst(constant),
                                    new2 = new StoreVar(var);
                                new1.Options[BasicBlock.BASIC_BLOCK_OPTION] = block;
                                new2.Options[BasicBlock.BASIC_BLOCK_OPTION] = block;

                                body[i] = new1;
                                body[i+1] = new2;
                                n1.ReplaceByNode(new1);
                                n1.RemoveFromGraph();
                                new1.Next = new2;
                                new2.Next = n2.Next;
                                n2.RemoveFromGraph();
                            }
                            else if (i < body.Count-2)
                            {
                                Node n3 = body[i+2];

                                if (i < body.Count-3)
                                {
                                    Node n4 = body[i+3];

                                    if (detectedOperandExchange(n1,n2,n3,n4))
                                    {
                                        result = flag = true;
                                        Node n = newDuplicateStackTop(block);
                                        body[i] = n;
                                        body[i+1] = n1;
                                        body[i+2] = n2;
                                        body[i+3] = n4;
                                        n3.RemoveFromGraph();
                                        n1.ReplaceByNode(n);
                                        n.Next = n1;
                                        n2.Next = n4;
                                    }
                                    else if (detectedQuadruple1(n1,n2,n3,n4))
                                    {
                                        result = flag = true;
                                        Node n = newDuplicateStackTop(block);
                                        body[i] = n;
                                        body[i+1] = n1;
                                        body[i+2] = n2;
                                        body[i+3] = n3;
                                        n1.ReplaceByNode(n);
                                        n.Next = n1;
                                        n3.Next = n4.Next;
                                        n4.RemoveFromGraph();
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        private void replaceNodeByPop(Node node)
        {
            BasicBlock block = node.Options[BasicBlock.BASIC_BLOCK_OPTION] as BasicBlock;
            Node n = newRemoveStackTop(block);
            block.Body[block.Body.IndexOf(node)] = n;
            node.ReplaceByNode(n);
            n.Next = node.Next;
        }

		private VarUsage analyseVariable(Variable var)
		{
			BasicBlockStub[] stubArray = new BasicBlockStub[BlockList.Count];
			Hashtable hash = new Hashtable();
			for (int i = 0; i < BlockList.Count; i++)
			{
				stubArray[i] = new BasicBlockStub(var,BlockList[i]);
				hash.Add(BlockList[i],stubArray[i]);
			}

			for (int i = 0; i < BlockList.Count; i++)
				foreach (BasicBlock block in BlockList[i].Next)
					stubArray[i].AddNext(hash[block] as BasicBlockStub);

			foreach (BasicBlockStub stub in stubArray)
				stub.Reduce();

			VarUsage result = new VarUsage();
			foreach (BasicBlockStub stub in stubArray)
				stub.AddToUsage(result);

			return result;
		}

        private bool performUnnecessaryStoringRemoval()
        {
            bool result = false;

			bool containsProtectedBlock = false;
			MethodBodyBlock mbb = Entry.Body[0] as MethodBodyBlock;
			foreach (Node node in mbb.ChildArray)
				containsProtectedBlock |= node is ProtectedBlock;

			if (containsProtectedBlock)
			{
				Hashtable varFlags = new Hashtable();
				foreach (BasicBlock block in blockList)
				{
					int i;
					NodeArray body = block.Body;

					if (body.Count > 0)
					{
						Node lastNode = body[body.Count-1];
						bool initialFlag = 
							lastNode is Leave && lastNode.Parent is MethodBodyBlock;
                
						foreach (Variable var in mbb.Variables)
							varFlags[var] = initialFlag;

						for (i = body.Count-1; i >= 0; i--)
						{
							Node node = body[i];

							if (node is LoadVar || node is StoreVar)   
							{
								Variable var = (node as ManageVar).Var;
								if (varIsNotReferenced(var))
								{
									bool flag = (bool)(varFlags[var]);

									if (node is LoadVar && flag)
										varFlags[var] = false;
									else if (node is StoreVar && ! flag)
										varFlags[var] = true;
									else if (node is StoreVar && flag)
									{
										result = true;
										replaceNodeByPop(node);
										node.RemoveFromGraph();
									}
								}
							}
						}
					}
				}
			}
			else
			{
				foreach (Variable var in mbb.Variables)
					if (varIsNotReferenced(var))
					{
						VarUsage usage = analyseVariable(var);
						NodeArray nodesToRemove = new NodeArray();

						foreach (ManageVar node in var.UsersArray)
							if (node is StoreVar)
							{
								StoreVar storer = node as StoreVar;
								if (! usage.IsUsed(storer))
									nodesToRemove.Add(storer);
							}

						foreach (StoreVar storer in nodesToRemove)
						{
							result = true;
							replaceNodeByPop(storer);
							storer.RemoveFromGraph();
						}
					}
			}

            return result;
        }

		private bool performConstantAliasesRemoval()
		{
			bool result = false;

			foreach (Variable v in mbb.Variables)
				if (varIsNotReferenced(v) && v.Kind == VariableKind.Local)
				{
					Node varUseNode = null;
					int count = 0;
					foreach (Node node in v.UsersArray)
						if (node is StoreVar)
						{
							varUseNode = node;
							count++;
						}

					if (count == 1)
					{
						BasicBlock block = varUseNode.Options[BasicBlock.BASIC_BLOCK_OPTION] as BasicBlock;
						Node prevNode = varUseNode.PrevArray[0];
						while (prevNode is DuplicateStackTop &&
							prevNode.Options[BasicBlock.BASIC_BLOCK_OPTION] == block)
							prevNode = prevNode.PrevArray[0];

						if (prevNode is LoadConst &&
							prevNode.Options[BasicBlock.BASIC_BLOCK_OPTION] == block)
						{
							result = true;

							StoreVar stNode = varUseNode as StoreVar;
							LoadConst ldNode = prevNode as LoadConst;

							NodeArray aliasUsageList = new NodeArray();
							foreach (Node node in stNode.Var.UsersArray)
								if (node is LoadVar)
									aliasUsageList.Add(node);

							replaceNodeByPop(stNode);
							stNode.RemoveFromGraph();

							foreach (LoadVar node in aliasUsageList)
							{
								Node n = ldNode.Clone();
								BasicBlock blk = node.Options[BasicBlock.BASIC_BLOCK_OPTION] as BasicBlock;
								n.Options[BasicBlock.BASIC_BLOCK_OPTION] = blk;
								blk.Body[blk.Body.IndexOf(node)] = n;
								node.ReplaceByNode(n);
								n.Next = node.Next;
								node.RemoveFromGraph();
							}
						}
					}
				}

            return result;
		}

		private bool performVariableAliasesRemoval()
		{
			bool result = false;

			foreach (Variable v in mbb.Variables)
			{
				if (v.UsersArray.Count == 1)
				{
					Node varUseNode = v.UsersArray[0];
					if (varUseNode is LoadVar)
					{
						BasicBlock block = varUseNode.Options[BasicBlock.BASIC_BLOCK_OPTION] as BasicBlock;
						Node nextNode = varUseNode.Next;
						while (nextNode is DuplicateStackTop &&
							nextNode.Options[BasicBlock.BASIC_BLOCK_OPTION] == block)
							nextNode = nextNode.Next;

						if (nextNode is StoreVar &&
							nextNode.Options[BasicBlock.BASIC_BLOCK_OPTION] == block)
						{
							LoadVar ldNode = varUseNode as LoadVar;
							StoreVar stNode = nextNode as StoreVar;
							Variable var = ldNode.Var, alias = stNode.Var;

							if (var != alias && var.Type.Equals(alias.Type))
							{
								result = true;
								replaceNodeByPop(stNode);
								stNode.RemoveFromGraph();

								NodeArray aliasUsageList = new NodeArray();
								foreach (Node node in alias.UsersArray)
									aliasUsageList.Add(node);

								foreach (ManageVar node in aliasUsageList)
									node.Var = var;
							}
						}
					}
				}
			}

			return result;
		}

        private bool performUnusedVariablesRemoval()
        {
            bool result = false;

            ArrayList unusedVars = new ArrayList();
            foreach (Variable var in mbb.Variables)
            {
                bool unused = true;
                foreach (ManageVar node in var.UsersArray)
                    unused &= node is StoreVar;

                if (unused)
                {
                    if (var.Kind == VariableKind.Local)
                        unusedVars.Add(var);

                    ArrayList removedNodes = new ArrayList();
                    foreach (StoreVar node in var.UsersArray)
                    {
                        result = true;
                        replaceNodeByPop(node);
                        removedNodes.Add(node);
                    }

                    foreach (StoreVar node in removedNodes)
                    {
                        result = true;
                        node.RemoveFromGraph();
                    }
                }
            }

            foreach (Variable var in unusedVars)
                mbb.Variables.Remove(var);

            return result;
        }

        private bool performLeaveReproduction()
        {
            bool result = false;

            ArrayList leaveBlocks = new ArrayList();
            foreach (BasicBlock block in blockList)
            {
                int count = block.Body.Count;

                if (count > 0 && count <= 2)
                {
                    Node lastNode = block.Body[count-1];
                    if (lastNode is Leave && lastNode.Parent is MethodBodyBlock)
                        leaveBlocks.Add(block);
                }
            }

            ArrayList leaveBlocksToRemove = new ArrayList();
            foreach (BasicBlock block in leaveBlocks)
            {
                Node opt = (block.Body.Count == 2) ? block.Body[0] : null;

                ArrayList prevBlocksToRemove = new ArrayList();
                foreach (BasicBlock b in block.Prev)
                    if (b.Next.Count == 1 && b.Body.Count > 0)
                    {
                        NodeArray body = b.Body;
                        Node lastNode = body[body.Count-1];

                        if (! (lastNode is Branch))
                        {
                            result = true;
                            prevBlocksToRemove.Add(b);

                            if (opt != null)
                            {
                                Node n = opt.Clone();
                                n.Options[BasicBlock.BASIC_BLOCK_OPTION] = b;
                                body.Add(n);
                                lastNode = lastNode.Next = n;
                            }

                            Node l = new Leave();
                            l.Options[BasicBlock.BASIC_BLOCK_OPTION] = b;
                            body.Add(l);
                            lastNode.Next = l;
                        }
                    }

                foreach (BasicBlock b in prevBlocksToRemove)
                {
                    b.Next.Remove(block);
                    block.Prev.Remove(b);
                }

                if (block.Prev.Count == 0)
                    leaveBlocksToRemove.Add(block);
            }

            foreach (BasicBlock block in leaveBlocksToRemove)
                blockList.Remove(block);

            return result;
        }

        #endregion

        public BasicBlocksGraph(MethodBodyBlock methodBodyBlock)
        {
            mbb = methodBodyBlock;
            mbb.RemoveOption(BasicBlock.BASIC_BLOCK_OPTION);
            GraphProcessor processor = new GraphProcessor();
            BasicBlocksBuilder builder = new BasicBlocksBuilder(processor);
            
            entry = builder.createBasicBlock();
            builder.AddTask(methodBodyBlock,entry);
            processor.Process();
            blockList = builder.BlockList;
        }

		private void ReConstruct() //Andrew
		{
			mbb.RemoveOption(BasicBlock.BASIC_BLOCK_OPTION);
			GraphProcessor processor = new GraphProcessor();
			BasicBlocksBuilder builder = new BasicBlocksBuilder(processor);
            
			entry = builder.createBasicBlock();
			builder.AddTask(mbb,entry);
			processor.Process();
			blockList = builder.BlockList;
		}

        public BasicBlock Entry { get { return entry; } }

        public BasicBlockArray BlockList { get { return blockList; } }

        public void Optimize()
        {
            bool flag;
            do
            {
                categorizeVariables();
				flag = performPatternReplacing();
				flag |= performUnnecessaryStoringRemoval();
				flag |= performVariableAliasesRemoval();
				flag |= performConstantAliasesRemoval();
				flag |= performUnusedVariablesRemoval();
				flag |= performLeaveReproduction();
            }
            while (flag);
        }

		public virtual string ToString(string format, IFormatProvider formatProvider, string[] options)
		{
			string result = "";

            for (int i = 0; i < BlockList.Count; i++)
                result += BlockList[i].ToString(format,formatProvider,options) +
                    (i < BlockList.Count-1 ? "\n" : "");

			return result;
		}

        public virtual string ToString(string format, IFormatProvider formatProvider)
        {
            return ToString(format,formatProvider,null);
        }

        public override string ToString()
        {
            return ToString("CSharp",ReflectionFormatter.formatter);
        }
    }
}
