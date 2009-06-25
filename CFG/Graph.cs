
// ===========================================================================
// CILPE - Partial Evaluator for Common Intermediate Language
// ===========================================================================
// File: 
//     Graph.cs
//
// Description:
//     Control-flow graph
//
// Author: 
//     Sergei Skorobogatov (Sergei.Skorobogatov@supercompilers.com)
// ===========================================================================


using System;

using CILPE.ReflectionEx; //Andrew: MethodInfoExtention

namespace CILPE.Exceptions
{
    using CILPE.CFG;

    public abstract class CfgException: ApplicationException
    {
        public CfgException(string msg): base("CFG: " + msg)
        {  }
    }

    public abstract class NodeException: CfgException
    {
        #region Private and internal members

        private Node node;

        #endregion

        public NodeException(Node node, string msg): base(msg)
        {
            this.node = node;
        }

        public Node Node { get { return node; } }
    }

    public abstract class LinkException: CfgException
    {
        #region Private and internal members

        private Node source, target;

        #endregion

        public LinkException(Node source, Node target, string msg): base(msg)
        {
            this.source = source;
            this.target = target;
        }

        public Node SourceNode { get { return source; } }
        public Node TargetNode { get { return target; } }
    }

    public class VarRemovalProhibitedException: CfgException
    {
        public VarRemovalProhibitedException(): 
            base("Removal of variable is prohibited")
        { }
    }

    public class NodeReplacementProhibitedException: NodeException
    {
        public NodeReplacementProhibitedException(Node node):
            base(node,"Node replacement is prohibited")
        {  }
    }

    public class SwitchIndexOutOfRangeException: NodeException
    {
        public SwitchIndexOutOfRangeException(Node node):
            base(node,"Switch index is out of range")
        {  }
    }

    public class MbbNotAvailableException: NodeException
    {
        public MbbNotAvailableException(Node node):
            base(node,"MethodBodyBlock is not available")
        {  }
    }

    public class VariableNotInMbbException: NodeException
    {
        public VariableNotInMbbException(Node node):
            base(node,"Variable is not in MethodBodyBlock")
        {  }
    }

    public class InvalidBranchTargetException: LinkException
    {
        public InvalidBranchTargetException(Node source, Node target):
            base(source,target,"Invalid branch target")
        {  }
    }

    public class LinkAdditionProhibitedException: LinkException
    {
        public LinkAdditionProhibitedException(Node source, Node target):
            base(source,target,"Link addition is prohibited")
        {  }
    }

    public class HandlerAdditionProhibitedException: LinkException
    {
        public HandlerAdditionProhibitedException(Node source, Node target):
            base(source,target,"Handler addition is prohibited")
        {  }
    }

    public class FilterAdditionProhibitedException: LinkException
    {
        public FilterAdditionProhibitedException(Node source, Node target):
            base(source,target,"Filter addition is prohibited")
        {  }
    }
}

namespace CILPE.CFG
{
    using System.Collections;
    using System.Reflection;
    using CILPE.ReflectionEx;
    using CILPE.Exceptions;

    // ========================================================================
    // Auxiliary types
    // ------------------------------------------------------------------------

    public interface IReadonlyNodeArray: IEnumerable
    {
        Node this [int index] { get; }
        int Count { get; }
        int IndexOf(Node node);
    }

    public interface INodeArray: IReadonlyNodeArray
    {
        new Node this [int index] { get; set; }
    }

    public interface INodeList: IEnumerable
    {
        void Add(Node node);
        void Concat(INodeList nodes);
        void Insert(int index, Node node);
        void Remove(Node node);
        void RemoveAt(int index);
        int IndexOf(Node node);
    }

    public class NodeArray: IReadonlyNodeArray, INodeArray, INodeList
    {
        #region Private and internal members

        private ArrayList nodes;
        private int linkCount;

        #endregion

        #region Protected members

        protected virtual Node GetNode(int index)
        {
            return nodes[index] as Node;
        }

        protected virtual void SetNode(int index, Node node)
        {
            Node oldValue = nodes[index] as Node;
            nodes[index] = node;

            if (oldValue == null && node != null)
                linkCount++;
            else if (oldValue != null && node == null)
                linkCount--;
        }

        #endregion

        public NodeArray() { nodes = new ArrayList(); }

        public NodeArray(int arrayLength) 
        { 
            nodes = new ArrayList(arrayLength);
            linkCount = 0;

            for (int i = 0; i < arrayLength; i++)
                Add(null);
        }

        public int LinkCount { get { return linkCount; } }

        Node IReadonlyNodeArray.this [int index] 
        { 
            get { return nodes[index] as Node; } 
        }

        public int Count { get { return nodes.Count; } }

        public Node this [int index]
        {
            get { return GetNode(index); }
            set { SetNode(index,value); }
        }

        public int IndexOf(Node node) { return nodes.IndexOf(node); }

        public void Add(Node node) 
        {
            nodes.Add(node); 

            if (node != null)
                linkCount++;
        }

        public void Concat(INodeList nodes)
        {
            foreach (Node n in nodes)
                Add(n);
        }

        public void Insert(int index, Node node) 
        { 
            nodes.Insert(index,node);

            if (node != null)
                linkCount++;
        }

        public void Remove(Node node) 
        { 
            nodes.Remove(node);

            if (node != null)
                linkCount--;
        }

        public void RemoveAt(int index)
        {
            if (nodes[index] != null)
                linkCount--;

            nodes.RemoveAt(index);
        }

        /* Returns an enumerator that can iterate through nodes */
        public IEnumerator GetEnumerator() { return nodes.GetEnumerator(); }
    }

    public class OptionsHash
    {
        #region Private and internal members

        private Hashtable options;

        #endregion

        public OptionsHash() { options = new Hashtable(); }

        public Object this [string optionName]
        {
            get { return options[optionName]; }
            set { options[optionName] = value; }
        }

        public void Clear() { options.Clear(); }

        public void Remove(string optionName)
        {
            options.Remove(optionName);
        }

        public bool ContainsOption(string optionName)
        {
            return options.ContainsKey(optionName);
        }

		public void CopyFrom(OptionsHash from)
		{
			options = from.options.Clone() as Hashtable;
		}
    }

    public enum VariableKind
    {
        Local = 0,
        Parameter = 1,
        ArgList = 2
    }

    public class Variable
    {
        #region Private and internal members

        private string name;
        private Type type;
        private VariableKind kind;
        private OptionsHash options;
        private int index;

        private NodeArray usersArray;

        private static int freeIndex = 0;

        private Variable(int index, Type type, VariableKind kind, string name)
        {
            this.index = index;

            this.type = type;
            this.kind = kind;
            this.name = name;
            options = new OptionsHash();
            usersArray = new NodeArray();
        }

        internal int Index { get { return index; } }

        internal void addUser(ManageVar node) { usersArray.Add(node); }

        internal void removeUser(ManageVar node) { usersArray.Remove(node); }

        internal Variable(Type type, VariableKind kind)
        {
            index = freeIndex++;

            this.type = type;
            this.kind = kind;
            options = new OptionsHash();
            usersArray = new NodeArray();

            switch (kind)
            {
                case VariableKind.Local: 
                    name = "Loc" + index; 
                    break;

                case VariableKind.Parameter: 
                    name = "Arg" + index; 
                    break;

                case VariableKind.ArgList: 
                    name = "ArgList";
                    break;
            }
        }

        internal Variable Clone()
        {
            return new Variable(index,type,kind,name);
        }

        #endregion

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public Type Type 
        {
            get { return type; }  
            set { type = value; }
        }

        public VariableKind Kind { get { return kind; } }

        public OptionsHash Options { get { return options; } }

        public IReadonlyNodeArray UsersArray { get { return usersArray; } }

        public override string ToString() { return Type+" "+Name; }
    }

    public class VariablesList: IEnumerable
    {
        #region Private and internal members

        private Hashtable vars;
        private ParameterMapper mapper;

        internal VariablesList() 
        { 
            vars = new Hashtable();
            mapper = new ParameterMapper();
        }

        private VariablesList(Hashtable vars, ParameterMapper mapper)
        {
            this.vars = vars;
            this.mapper = mapper;
        }

        internal Variable getVarByIndex(int index)
        {
            return vars[index] as Variable;
        }

        internal VariablesList Clone()
        {
            Hashtable newVars = new Hashtable(vars.Count);
            ParameterMapper newMapper = new ParameterMapper();

            foreach (Variable v in mapper)
            {
                Variable w = v.Clone();
                newVars.Add(w.Index,w);
                newMapper.Add(w);
            }

            foreach (Variable v in vars.Values)
                if (v.Kind == VariableKind.Local)
                {
                    Variable w = v.Clone();
                    newVars.Add(w.Index,w);
                }

            return new VariablesList(newVars,newMapper);
        }

        #endregion

        public int Count { get { return vars.Count; } }

        public ParameterMapper ParameterMapper { get { return mapper; } }

        public Variable CreateVar(Type type, VariableKind kind) 
        { 
            Variable var = new Variable(type,kind);
            vars.Add(var.Index,var);

            if (var.Kind != VariableKind.Local)
                mapper.Add(var);

            return var;
        }

        public void Remove(Variable var) 
        { 
            if (var.UsersArray.Count > 0)
                throw new VarRemovalProhibitedException();

            vars.Remove(var.Index);

            if (var.Kind != VariableKind.Local)
                mapper.Remove(var);
        }

        /* Returns an enumerator that can iterate through variables */
        public IEnumerator GetEnumerator() { return vars.Values.GetEnumerator(); }
    }

    public class ParameterMapper: IEnumerable
    {
        #region Private and internal members

        private ArrayList map;

        internal ParameterMapper() { map = new ArrayList(); }

        internal void Add(Variable var) { map.Add(var); }

        internal void Remove(Variable var) { map.Remove(var); }

        #endregion

        public Variable this [int index] 
        { 
            get { return map[index] as Variable; } 
        }

        public int Count { get { return map.Count; } }

        /* Returns an enumerator that can iterate through parameters */
        public IEnumerator GetEnumerator() { return map.GetEnumerator(); }
    }
  
    // ========================================================================
    // Abstract base classes
    // ------------------------------------------------------------------------

    public abstract class Node: IFormattable
    {
        #region Private and internal members
        
		private const int NODE_NAME_WIDTH = 20;

        private Block parent;
        private OptionsHash options;
        private NodeArray prevArray;
        private ArrayList prevIndexes;
        private NextNodeArray nextArray;
        internal bool needed;

        private class NextNodeArray: NodeArray
        {
            #region Private and protected members

            private Node owner;

            protected override void SetNode(int index, Node node)
            {
                Node linkedNode = GetNode(index);

                if (linkedNode != node)
                {
                    /* Clearing old link */
                    if (linkedNode != null)
                    {
                        base.SetNode(index,null);
                        linkedNode.removePrevNode(owner);
                    }

                    /* Adding new link */
                    if (node != null)
                    {
                        if (! node.CanBeBranchTarget)
                            throw new InvalidBranchTargetException(owner,node);

                        if (owner.NextParent == null)
                            throw new LinkAdditionProhibitedException(owner,node);

                        node.addPrevNode(owner,index);
                        base.SetNode(index,node);
                    }
                }
            }

            #endregion

            public NextNodeArray(Node owner, int arrayLength): base(arrayLength)
            {
                this.owner = owner;
            }
        }

        internal void addPrevNode(Node node, int indexInNextArray) 
        {
            if (parent == null)
                setParent(node.NextParent);
            else if (parent != node.NextParent)
                throw new InvalidBranchTargetException(node,this);

            prevArray.Add(node);
            prevIndexes.Add(indexInNextArray);
        }

        internal void removePrevNode(Node node) 
        {
            int index = prevArray.IndexOf(node);
            prevArray.RemoveAt(index);
            prevIndexes.RemoveAt(index);
        }

        #endregion

        protected Node(int nextCount)
        {
            parent = null;
            options = new OptionsHash();

            prevArray = new NodeArray();
            prevIndexes = new ArrayList();
            nextArray = new NextNodeArray(this,nextCount);
        }

        public Block Parent { get { return parent; } }

        public IReadonlyNodeArray PrevArray { get { return prevArray; } }

        public Node Next 
        { 
            get { return (nextArray.Count > 0) ? nextArray[0] : null; } 
            set { nextArray[0] = value; }
        }

        public INodeArray NextArray { get { return nextArray; } }

        public virtual void RemoveFromGraph()
        {
            for (int i = 0; i < NextArray.Count; i++)
                NextArray[i] = null;

            ReplaceByNode(null);
            setParent(null);
        }

        public virtual void ReplaceByNode(Node node)
        {
			//Hy Cepera!!
			int prevCount = this.prevArray.Count;
			Node[] prevArray = new Node[prevCount];
			int[] prevIndexes = new int[prevCount];
			for(int i=0; i<prevCount; i++)
			{
				prevArray[i] = this.prevArray[i];
				prevIndexes[i] = (int)this.prevIndexes[i];
			}


            for (int i = 0; i < prevCount; i++)
            {
                Node prevNode = prevArray[i];
                int index = prevIndexes[i];
                prevNode.NextArray[index] = node;
            }
        }

        public bool IsLeaf { get { return nextArray.LinkCount == 0; } }

        public bool IsValid { get { return nextArray.LinkCount == nextArray.Count; } }

        public OptionsHash Options { get { return options; } }

        public abstract Node Clone();

		public virtual string ToString(string format, IFormatProvider formatProvider, string[] options)
		{
			string result = ToString(format,formatProvider);

            if (options != null)
			    for (int i = options.Length-1; i >= 0; i--)
			    {
				    if (Options.ContainsOption(options[i]))
					    result = "["+Options[options[i]]+"] " + result;
			    }

			return result;
		}

		public virtual string ToString(string format, IFormatProvider formatProvider)
		{
			return "";
		}

        public override string ToString()
        {
            return ToString("CSharp",ReflectionFormatter.formatter);
        }

		protected internal static string FormatNodeName(string name)
		{
			return String.Format("{0,-"+NODE_NAME_WIDTH+"}",name);
		}

		protected internal static string FormatBranchTarget(Node target)
		{
			string result = "@";

			if (target != null && target.Options.ContainsOption(BasicBlock.BASIC_BLOCK_OPTION))
			{
				BasicBlock basicBlock = target.Options[BasicBlock.BASIC_BLOCK_OPTION] as BasicBlock;
				result += basicBlock.Index;
			}
			else
				result += "unknown";

			return result;
		}

        protected void ResetNextArray(int nextCount)
        {
            nextArray = new NextNodeArray(this,nextCount);
        }
        
        protected internal virtual bool CanBeBranchTarget { get { return true; } }

        protected internal virtual Block NextParent { get { return Parent; } }

        protected internal virtual void setParent(Block parent) 
        {
            if (this.parent != null)
                this.parent.removeChild(this);

            this.parent = parent;

            if (parent != null)
                parent.addChild(this);
        }

        protected internal virtual void CallVisitorMethod(Visitor visitor, object data) {  }
    }

    public abstract class ServiceNode: Node
    {
        protected ServiceNode(): base(1) {  }

        protected internal override void CallVisitorMethod(Visitor visitor, object data)
        {
            visitor.VisitServiceNode(this,data);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return "ServiceNode";
        }
    }

    public interface ITypedNode
    {
        Type Type { get; set; }
    }

    public abstract class TypedNode: Node, ITypedNode
    {
        #region Private and internal members

        private Type type;

        #endregion

        protected TypedNode(Type type): base(1)
        {
            this.type = type;
        }

        public Type Type
        {
            get { return type; }
            set { type = value; }
        }
    }

    public abstract class Block: Node
    {
        #region Private and internal members

        private NodeArray childArray;

        internal void addChild(Node child) { childArray.Add(child); }

        internal void removeChild(Node child) { childArray.Remove(child); }

        private class GCVisitor: StackVisitor
        {
            protected override void DispatchNode(Node node, object data)
            {
                if (node != null && node.needed == false)
                {
                    node.needed = true;
                    if (node is Block)
                    {
                        foreach (Node n in (node as Block).childArray)
                            if (n is Leave)
                                AddNextNodesToTasks(n, null);
                    }
                    else if (!(node is Leave))
                        AddNextNodesToTasks(node, null);
                }
            }

            public GCVisitor (GraphProcessor graphProcessor): base(graphProcessor)
            {
            }
        }

        #endregion

        protected Block(): base(1) 
        {
            childArray = new NodeArray();
        }

        public IReadonlyNodeArray ChildArray { get { return childArray; } }

        public bool IsChildTo(Block block)
        {
            return (Parent == block) || (Parent != null ? Parent.IsChildTo(block) : false);
        }

        public static Block FindCommonParent(Block a, Block b)
        {
            Block result = a;

            while (! (result == null || result == b || b.IsChildTo(result)))
                result = result.Parent;

            return result;
        }

        public IReadonlyNodeArray CollectGarbage()
        {
            foreach (Node n in ChildArray)
                n.needed = false;

            GraphProcessor graphProcessor = new GraphProcessor();
            GCVisitor gcVisitor = new GCVisitor(graphProcessor);
            foreach (Node n in NextArray)
                gcVisitor.AddTask(n, null);
            graphProcessor.Process();

            NodeArray arr = new NodeArray();
            foreach (Node n in ChildArray)
                if (! n.needed)
                    arr.Add(n);
            return arr;
        }

        public void RemoveOption(string option)
        {
            foreach (Node n in childArray)
            {
                n.Options.Remove(option);

                if (n is Block)
                    (n as Block).RemoveOption(option);
            }
        }

        public override void RemoveFromGraph()
        {
            foreach (Node n in childArray)
                n.RemoveFromGraph();

            base.RemoveFromGraph();
        }

        protected internal override Block NextParent 
        { 
            get { return (Parent == null) ? null : this; } 
        }
    }

    public abstract class EHBlock: Block
    {
        #region Private and internal members

        internal ProtectedBlock tryBlock;

        #endregion

        protected EHBlock() { tryBlock = null; }

        public ProtectedBlock TryBlock { get { return tryBlock; } }

        public override void RemoveFromGraph()
        {
            if (tryBlock != null)
                tryBlock.RemoveHandler(this);

            base.RemoveFromGraph();
        }

        public override void ReplaceByNode(Node node)
        {
            throw new NodeReplacementProhibitedException(this);
        }

        protected internal override bool CanBeBranchTarget { get { return false; } }
    }

    // ========================================================================
    // Blocks
    // ------------------------------------------------------------------------

    public class MethodBodyBlock: Block
    {
        #region Private and internal members

        private VariablesList variables;
        private Type returnType;

        private MethodBodyBlock(VariablesList variables, Type returnType)
        {
            this.variables = variables;
            this.returnType = returnType;
        }

        #endregion

        public MethodBodyBlock(Type returnType)
        {
            variables = new VariablesList();
            this.returnType = returnType;
        }

        public VariablesList Variables { get { return variables; } }

        public Type ReturnType 
        {
            get { return returnType; } 
            set { returnType = value; }
        }

        public override Node Clone()
        {
            return new MethodBodyBlock(variables.Clone(), returnType);
        }

        public override void ReplaceByNode(Node node)
        {
            throw new NodeReplacementProhibitedException(this);
        }

        protected internal override bool CanBeBranchTarget { get { return false; } }

        protected internal override Block NextParent { get { return this; } }

        protected internal override void CallVisitorMethod(Visitor visitor, object data)
        {
            visitor.VisitMethodBodyBlock(this,data);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return "MethodBodyBlock";
        }
    }

    public class ProtectedBlock: Block, IEnumerable
    {
        #region Private and internal members

        private ArrayList handlers;

        private void setHandler(int index, EHBlock handler)
        {
            if ((handler.Parent != null && handler.Parent != Parent) || 
                (handler.tryBlock != null))
                throw new HandlerAdditionProhibitedException(this,handler);

                EHBlock oldHandler = handlers[index] as EHBlock;

                if (oldHandler != null)
                oldHandler.tryBlock = null;

            handlers[index] = handler;

            handler.tryBlock = this;
            handler.setParent(Parent);
        }

        #endregion

        public ProtectedBlock()
        { 
            handlers = new ArrayList();
        }

        public EHBlock this [int index]
        {
            get { return handlers[index] as EHBlock; }
            set { setHandler(index,value); }
        }

        public int Count { get { return handlers.Count; } }

        public void AddHandler(EHBlock handler) 
        { 
            handlers.Add(null);
            setHandler(handlers.Count-1,handler);
        }

        public void InsertHandler(int index,EHBlock handler)
        {
            handlers.Insert(index,null);
            setHandler(index,handler);
        }

        public void RemoveHandler(EHBlock handler) 
        { 
            handlers.Remove(handler); 
            handler.tryBlock = null;
        }

        public override Node Clone() { return new ProtectedBlock(); }

        /* Returns an enumerator that can iterate through exception handlers */
        public IEnumerator GetEnumerator() { return handlers.GetEnumerator(); }

        public override void RemoveFromGraph()
        {
            foreach (EHBlock block in handlers)
            {
                block.tryBlock = null;
                block.RemoveFromGraph();
            }

            handlers.Clear();
            base.RemoveFromGraph();
        }

        protected internal override void CallVisitorMethod(Visitor visitor, object data)
        {
            visitor.VisitProtectedBlock(this,data);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
                        string result = FormatNodeName("ProtectedBlock")+"(handlers) ";

                        foreach (EHBlock block in handlers)
                                result += FormatBranchTarget(block)+" ";

            return result;
        }
    }

    public class CatchBlock: EHBlock, ITypedNode
    {
        #region Private and internal members

        private Type type;

        #endregion

        public CatchBlock(Type type)
        {
            this.type = type;
        }

        public Type Type
        {
            get { return type; }
            set { type = value; }
        }

        public override Node Clone() { return new CatchBlock(type); }

        protected internal override void CallVisitorMethod(Visitor visitor, object data)
        {
            visitor.VisitCatchBlock(this,data);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
			return FormatNodeName("CatchBlock")+
				String.Format(formatProvider,"(Type) {0:"+format+"} (TryBlock) ",Type)+
                FormatBranchTarget(TryBlock);
        }
    }

    public class FinallyBlock: EHBlock
    {
        #region Private and internal members

        private bool isFault;

        #endregion

        public FinallyBlock(bool isFault)
        {
            this.isFault = isFault;
        }

        public bool IsFault 
        { 
            get { return isFault; } 
            set { isFault = value; }
        }

        public override Node Clone() { return new FinallyBlock(isFault); }

        protected internal override void CallVisitorMethod(Visitor visitor, object data)
        {
            visitor.VisitFinallyBlock(this,data);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
			return FormatNodeName("FinallyBlock")+
				"(TryBlock) "+FormatBranchTarget(TryBlock);
        }
    }

    public class UserFilteredBlock: EHBlock
    {
        #region Private and internal members

        FilterBlock filter;

        private void setFilter(FilterBlock newFilter)
        {
            if (filter != null)
                filter.block = null;

            filter = newFilter;

            if (newFilter != null)
            {
                if (newFilter.Parent == null)
                    newFilter.setParent(Parent);
                else if (newFilter.Parent != Parent)
                    throw new FilterAdditionProhibitedException(this,newFilter);

                newFilter.block = this;
            }
        }

        #endregion

        public UserFilteredBlock()
        {  
            filter = null;
        }

        public FilterBlock Filter 
        { 
            get { return filter; } 
            set { setFilter(value); } 
        }

        public override Node Clone() { return new UserFilteredBlock(); }

        public override void RemoveFromGraph()
        {
            if (filter != null)
                filter.RemoveFromGraph();

            base.RemoveFromGraph();
        }

        protected internal override void CallVisitorMethod(Visitor visitor, object data)
        {
            visitor.VisitUserFilteredBlock(this,data);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
			return FormatNodeName("UserFilteredBlock")+
				"(TryBlock) "+FormatBranchTarget(TryBlock)+
				"(Filter) "+FormatBranchTarget(Filter);
        }
    }

    public class FilterBlock: Block
    {
        #region Private and internal members

        internal UserFilteredBlock block;

        #endregion

        public FilterBlock()
        {
            this.block = null;
        }

        public override Node Clone()
        {
            return new FilterBlock();
        }

        public override void RemoveFromGraph()
        {
            if (block != null)
                block.Filter = null;

            base.RemoveFromGraph();
        }

        public override void ReplaceByNode(Node node)
        {
            throw new NodeReplacementProhibitedException(this);
        }

        protected internal override bool CanBeBranchTarget { get { return false; } }

        protected internal override void CallVisitorMethod(Visitor visitor, object data)
        {
            visitor.VisitFilterBlock(this,data);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
			return FormatNodeName("FilterBlock")+
				"(handler) "+FormatBranchTarget(block);
        }
    }

    // ========================================================================
    // Instructions
    // ------------------------------------------------------------------------

    public class Leave: Node
    {
        public Leave(): base(0) {  }

        public override Node Clone() { return new Leave(); }

        protected internal override void CallVisitorMethod(Visitor visitor, object data)
        {
            visitor.VisitLeave(this,data);
        }

        protected internal override Block NextParent 
        { 
            get { return Parent != null ? Parent.Parent : null; } 
        }

        protected internal override void setParent(Block parent) 
        {
            if (parent == null)
                ResetNextArray(0);
            else if (! (parent is MethodBodyBlock) && ! (parent is FinallyBlock) && 
                ! (parent is FilterBlock))
                ResetNextArray(1);

            base.setParent(parent);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            string result = FormatNodeName("Leave");

            if (Next != null)
				result += FormatBranchTarget(Next);

            return result;
        }
    }

    public class UnaryOp: Node
    {
        #region Private and internal members

        private ArithOp op;

        #endregion

        public enum ArithOp
        {
            NEG  = 0,   /* Negate value */  
            NOT  = 1,   /* Bitwise complement */
        }

        public UnaryOp(ArithOp op): base(1)
        {
            this.op = op;
        }

        public ArithOp Op 
        { 
            get { return op; } 
            set { op = value; }
        }

        public override Node Clone() { return new UnaryOp(op); }

        protected internal override void CallVisitorMethod(Visitor visitor, object data)
        {
            visitor.VisitUnaryOp(this,data);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return FormatNodeName("UnaryOp")+Op;
        }
    }

    public class BinaryOp: Node
    {
        #region Private and internal members

        private ArithOp op;
        private bool overflow, unsigned;

        #endregion

        public enum ArithOp
        {
            ADD  = 0,   /* Add two values, returning a new value */
            AND  = 1,   /* Bitwise AND of two integral values, returns an integral value */
            CEQ  = 2,   /* Push 1 (int32) if equal, else 0 */
            CGT  = 3,   /* Push 1 (int32) if greater, else 0 */
            CLT  = 4,   /* Push 1 (int32) if less, else 0 */
            DIV  = 5,   /* Divide two values to return a quatient or floating-point result */
            MUL  = 6,   /* Multiply values */
            OR   = 7,   /* Bitwise OR of two integer values, returns as integer */
            REM  = 8,   /* Remainder of dividing */
            SHL  = 9,   /* Shift an integer to the left */
            SHR  = 10,  /* Shift an integer to the right */
            SUB  = 11,  /* Substract values */
            XOR  = 12,  /* Bitwise XOR of integer values, returns an integer */
        }

        public BinaryOp(ArithOp op, bool overflow, bool unsigned): base(1)
        {
            this.op = op;
            this.overflow = overflow;
            this.unsigned = unsigned;
        }

        public ArithOp Op 
        { 
            get { return op; } 
            set { op = value; }
        }

        public bool Overflow
        {
            get { return overflow; }
            set { overflow = value; }
        }

        public bool Unsigned
        {
            get { return unsigned; }
            set { unsigned = value; }
        }

        public override Node Clone()
        {
            return new BinaryOp(op,overflow,unsigned);
        }

        protected internal override void CallVisitorMethod(Visitor visitor, object data)
        {
            visitor.VisitBinaryOp(this,data);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return FormatNodeName("BinaryOp") + Op +
				(overflow ? " [overflow]" : "") + (unsigned ? " [unsigned]" : "");
        }
    }

    public class ConvertValue: TypedNode
    {
        #region Private and internal members
        private bool overflow, unsigned;
        #endregion

        public ConvertValue(Type type, bool overflow, bool unsigned): base(type)
        {  
            this.overflow = overflow;
            this.unsigned = unsigned;
        }

        public bool Overflow
        {
            get { return overflow; }
            set { overflow = value; }
        }

        public bool Unsigned
        {
            get { return unsigned; }
            set { unsigned = value; }
        }

        public override Node Clone()
        {
            return new ConvertValue(Type,overflow,unsigned);
        }

        protected internal override void CallVisitorMethod(Visitor visitor, object data)
        {
            visitor.VisitConvertValue(this,data);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
			return FormatNodeName("ConvertValue") +
                String.Format(formatProvider,"{0:"+format+"}",Type) +
				(overflow ? " [overflow]" : "") + (unsigned ? " [unsigned]" : "");
        }
    }

    public class CheckFinite: Node
    {
        public CheckFinite(): base(1) {  }

        public override Node Clone() { return new CheckFinite(); }

        protected internal override void CallVisitorMethod(Visitor visitor, object data)
        {
            visitor.VisitCheckFinite(this,data);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return "CheckFinite";
        }
    }

    public class Branch: Node
    {
        public Branch(): base(2) {  }

        public Node Alt
        { 
            get { return NextArray[1]; } 
            set { NextArray[1] = value; }
        }

        public override Node Clone() { return new Branch(); }

        protected internal override void CallVisitorMethod(Visitor visitor, object data)
        {
            visitor.VisitBranch(this,data);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return FormatNodeName("Branch")+FormatBranchTarget(Alt);
        }
    }

    public class Switch: Node, IEnumerable
    {
        #region Private and internal members

        private void checkRange(int index)
        {
            if (index < 0 || index >= Count)
                throw new SwitchIndexOutOfRangeException(this);
        }

        private int curIndex = 0, changeIndex = 0;

        private class Enumerator: IEnumerator
        {
            private Switch sw;
            private INodeArray nextArray;
            private int index, position;

            public Enumerator(Switch sw)
            {
                this.sw = sw;
                nextArray = sw.NextArray;

                index = sw.curIndex;
                sw.curIndex++;

                position = 0;
            }

            public object Current 
            { 
                get 
                {
                    if (index < sw.changeIndex ||
                        position == 0 || position == nextArray.Count)
                        throw new InvalidOperationException();

                    return nextArray[position];
                } 
            }

            public bool MoveNext() 
            {
                if (index < sw.changeIndex)
                    throw new InvalidOperationException();

                if (position < nextArray.Count)
                    position++;

                return position < nextArray.Count;
            }

            public void Reset() 
            {
                if (index < sw.changeIndex)
                    throw new InvalidOperationException();

                position = 0;
            }
        }

        #endregion

        public Switch(int count): base(count+1) {  }

        public Node this [int index]
        { 
            get 
            {
                checkRange(index);
                return NextArray[index+1]; 
            } 

            set 
            {
                checkRange(index);
                changeIndex = curIndex;
                NextArray[index+1] = value; 
            }
        }

        public int Count { get { return NextArray.Count-1; } }

        public override Node Clone() { return new Switch(Count); }

        /* Returns an enumerator that can iterate through switch cases */
        public IEnumerator GetEnumerator() { return new Enumerator(this); }

        protected internal override void CallVisitorMethod(Visitor visitor, object data)
        {
            visitor.VisitSwitch(this,data);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            string result = FormatNodeName("Switch");

            int count = 0;
            foreach (Node alt in this)
            {
                result += count + ":" + FormatBranchTarget(alt) + " ";
                count++;
            }

            return result;
        }
    }

    public class LoadConst: Node
    {
        #region Private and internal members

        private object constant;

        #endregion

        public LoadConst(object constant): base(1)
        {
            this.constant = constant;
        }

        public object Constant
        { 
            get { return constant; } 
            set { constant = value; }
        }

        public override Node Clone() { return new LoadConst(constant); }

        protected internal override void CallVisitorMethod(Visitor visitor, object data)
        {
            visitor.VisitLoadConst(this,data);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            string result = FormatNodeName("LoadConst");

            if (Constant == null)
                result += "null";
            else
                result +=
                    "(" + String.Format(formatProvider,"{0:"+format+"}",Constant.GetType()) + ") " +
                    (Constant is string ? "\"" : "") +
                    String.Format(formatProvider,"{0:"+format+"}",Constant) +
                    (Constant is string ? "\"" : "");

            return result;
        }
    }

    public abstract class ManageVar: Node
    {
        #region Private and internal members

        private int varIndex;
        private Variable var;

        private void setVarByIndex()
        {
            Block block = Parent;

            if (var != null)
                var.removeUser(this);

            if (block == null)
                var = null;
            else
            {
            while (! (block is MethodBodyBlock))
                if (block != null)
                    block = block.Parent;
                else
                    throw new MbbNotAvailableException(this);

                var = (block as MethodBodyBlock).Variables.getVarByIndex(varIndex);

                if (var == null)
                    throw new VariableNotInMbbException(this);
                
                var.addUser(this);
            }
        }

        protected ManageVar(int varIndex): base(1)
        {
            this.varIndex = varIndex;
            var = null;
        }

        #endregion

        public ManageVar(Variable var): base(1)
        {
            this.varIndex = var.Index;
            this.var = null;
        }

        public Variable Var
        {
            get 
            {
                if (Parent == null)
                    throw new MbbNotAvailableException(this);

                return var;
            }
            
            set 
            {
                varIndex = value.Index;
                setVarByIndex();
            }
        }

        protected internal override void setParent(Block parent) 
        {
            base.setParent(parent);
            setVarByIndex();
        }
    }

    public class LoadVar: ManageVar
    {
        protected LoadVar(int varIndex): base(varIndex) {  }

        public LoadVar(Variable var): base(var) {  }

        public override Node Clone()
        {
            return new LoadVar(Var.Index);
        }

        protected internal override void CallVisitorMethod(Visitor visitor, object data)
        {
            visitor.VisitLoadVar(this,data);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return FormatNodeName("LoadVar") + Var.Name;
        }
    }

    public class LoadVarAddr: ManageVar
    {
        protected LoadVarAddr(int varIndex): base(varIndex) {  }

        public LoadVarAddr(Variable var): base(var) {  }

        public override Node Clone()
        {
            return new LoadVarAddr(Var.Index);
        }

        protected internal override void CallVisitorMethod(Visitor visitor, object data)
        {
            visitor.VisitLoadVarAddr(this,data);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return FormatNodeName("LoadVarAddr") + Var.Name;
        }
    }

    public class StoreVar: ManageVar
    {
        protected StoreVar(int varIndex): base(varIndex) {  }

        public StoreVar(Variable var): base(var) {  }

        public override Node Clone()
        {
            return new StoreVar(Var.Index);
        }

        protected internal override void CallVisitorMethod(Visitor visitor, object data)
        {
            visitor.VisitStoreVar(this,data);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return FormatNodeName("StoreVar") + Var.Name;
        }
    }

    public class LoadIndirect: TypedNode
    {
        public LoadIndirect(Type type): base(type) {  }

        public override Node Clone() { return new LoadIndirect(Type); }

        protected internal override void CallVisitorMethod(Visitor visitor, object data)
        {
            visitor.VisitLoadIndirect(this,data);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return FormatNodeName("LoadIndirect")+
                String.Format(formatProvider,"{0:"+format+"}",Type);
        }
    }

    public class StoreIndirect: TypedNode
    {
        public StoreIndirect(Type type): base(type) {  }

        public override Node Clone() { return new StoreIndirect(Type); }

        protected internal override void CallVisitorMethod(Visitor visitor, object data)
        {
            visitor.VisitStoreIndirect(this,data);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return FormatNodeName("StoreIndirect")+
                String.Format(formatProvider,"{0:"+format+"}",Type);
        }
    }

    public class DuplicateStackTop: Node
    {
        public DuplicateStackTop(): base(1) {  }

        public override Node Clone() { return new DuplicateStackTop(); }

        protected internal override void CallVisitorMethod(Visitor visitor, object data)
        {
            visitor.VisitDuplicateStackTop(this,data);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return "DuplicateStackTop";
        }
    }

    public class RemoveStackTop: Node
    {
        public RemoveStackTop(): base(1) {  }

        public override Node Clone() { return new RemoveStackTop(); }

        protected internal override void CallVisitorMethod(Visitor visitor, object data)
        {
            visitor.VisitRemoveStackTop(this,data);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return "RemoveStackTop";
        }
    }

    public class CastClass: TypedNode
    {
        #region Private and internal members

        private bool throwException;

        #endregion

        public CastClass(Type type, bool throwException): base(type)
        {
            this.throwException = throwException;
        }

        public bool ThrowException
        {
            get { return throwException; }
            set { throwException = value; }
        }

        public override Node Clone() { return new CastClass(Type,throwException); }

        protected internal override void CallVisitorMethod(Visitor visitor, object data)
        {
            visitor.VisitCastClass(this,data);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return FormatNodeName("CastClass")+
                String.Format(formatProvider,"{0:"+format+"}",Type)+
                (ThrowException ? " [throws exception]" : "");
        }
    }

	public class CallMethod: Node
	{
        #region Private and internal members

		//Andrew: if `method` is a MethodBuilder of a TypeBuilder that is not created yet
		//the call to GetParameters throws an exception.
		private MethodInfoExtention method;
		//private MethodBase method
		
		private bool isVirtCall;
		private bool isTailCall;

        #endregion

		public CallMethod(MethodBase method, bool isVirtCall, bool isTailCall): base(1)
		{
			this.method = new MethodInfoExtention(method, isVirtCall);
			this.isVirtCall = isVirtCall;
			this.isTailCall = isTailCall;
		}

		public MethodBase Method
		{
			get { return method.Method; }
			//set { method.Assign(value, isVirtCall); }
		}

		public MethodInfoExtention MethodWithParams
		{
			get{ return(method); }
			set{ method = value; }
		}

        public bool IsVirtCall
        {
            get { return isVirtCall; }
            set { isVirtCall = value; }
        }

        public bool IsTailCall
        {
            get { return isTailCall; }
            set { isTailCall = value; }
        }

        public override Node Clone() 
        {
            CallMethod newNode = new CallMethod(method.Method,isVirtCall,isTailCall);
			newNode.Options.CopyFrom(Options);
			return(newNode); //Andrew: MBB!
        }

        protected internal override void CallVisitorMethod(Visitor visitor, object data)
        {
            visitor.VisitCallMethod(this,data);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return FormatNodeName("CallMethod")+
                String.Format(formatProvider,"{0:"+format+"}",Method)+
                (IsVirtCall ? " [virtual]" : "") +
                (IsTailCall ? " [tail]" : "");
        }
    }

    public class CreateDelegate: Node
    {
        #region Private and internal members

        private ConstructorInfo delegateCtor;
        private MethodInfo method;
        private bool isVirtual;

        #endregion

        public CreateDelegate(ConstructorInfo delegateCtor, MethodInfo method, bool isVirtual): base(1)
        {
            this.delegateCtor = delegateCtor;
            this.method = method;
            this.isVirtual = isVirtual;
        }

        public ConstructorInfo DelegateCtor
        {
            get { return delegateCtor; }
            set { delegateCtor = value; }
        }

        public MethodInfo Method
        {
            get { return method; }
            set { method = value; }
        }

        public bool IsVirtual
        {
            get { return isVirtual; }
            set { isVirtual = value; }
        }

        public override Node Clone() 
        {
            return new CreateDelegate(delegateCtor,method,isVirtual);
        }

        protected internal override void CallVisitorMethod(Visitor visitor, object data)
        {
            visitor.VisitCreateDelegate(this,data);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return FormatNodeName("CreateDelegate")+
                String.Format(
                    formatProvider,
                    "(DelegateCtor) {0:"+format+"} (Method) {1:"+format+"}",
                    DelegateCtor,Method
                    )+
                (IsVirtual ? " [virtual]" : "");
        }
    }

    public abstract class ManageField: Node
    {
        #region Private and internal members

        private FieldInfo field;

        #endregion

        public ManageField(FieldInfo field): base(1)
        {  
            this.field = field;
        }

        public FieldInfo Field
        {
            get { return field; } 
            set { field = value; }
        }
    }

    public class LoadField: ManageField
    {
        public LoadField(FieldInfo field): base(field) {  }

        public override Node Clone() { return new LoadField(Field); }

        protected internal override void CallVisitorMethod(Visitor visitor, object data)
        {
            visitor.VisitLoadField(this,data);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return FormatNodeName("LoadField")+
                String.Format(formatProvider,"{0:"+format+"}",Field);
        }
    }

    public class LoadFieldAddr: ManageField
    {
        public LoadFieldAddr(FieldInfo field): base(field) {  }

        public override Node Clone() { return new LoadFieldAddr(Field); }

        protected internal override void CallVisitorMethod(Visitor visitor, object data)
        {
            visitor.VisitLoadFieldAddr(this,data);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return FormatNodeName("LoadFieldAddr")+
                String.Format(formatProvider,"{0:"+format+"}",Field);
        }
    }

    public class StoreField: ManageField
    {
        public StoreField(FieldInfo field): base(field) {  }

        public override Node Clone() { return new StoreField(Field); }

        protected internal override void CallVisitorMethod(Visitor visitor, object data)
        {
            visitor.VisitStoreField(this,data);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return FormatNodeName("StoreField")+
                String.Format(formatProvider,"{0:"+format+"}",Field);
        }
    }

    public class ThrowException: Node
    {
        public ThrowException(): base(0) {  }

        public override Node Clone() { return new ThrowException(); }

        protected internal override void CallVisitorMethod(Visitor visitor, object data)
        {
            visitor.VisitThrowException(this,data);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return "ThrowException";
        }
    }

    public class RethrowException: Node
    {
        public RethrowException(): base(0) {  }

        public override Node Clone() { return new RethrowException(); }

        protected internal override void CallVisitorMethod(Visitor visitor, object data)
        {
            visitor.VisitRethrowException(this,data);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return "RethrowException";
        }
    }

    public class NewObject: Node
    {
        #region Private and internal members

        //private ConstructorInfo ctor;
		private MethodInfoExtention ctor;

        #endregion

        public NewObject(ConstructorInfo ctor): base(1) 
        {
            this.ctor = new MethodInfoExtention(ctor, false);
        }

		public ConstructorInfo Constructor
		{	
			get { return ctor.Method as ConstructorInfo; }
		}

		public MethodInfoExtention CtorWithParams
		{
			get{ return(ctor); }
			set{ ctor = value; }
		}

        public override Node Clone() 
		{
			NewObject clone = new NewObject(Constructor); 
			clone.Options.CopyFrom(Options);
			return(clone);
		}

        protected internal override void CallVisitorMethod(Visitor visitor, object data)
        {
            visitor.VisitNewObject(this,data);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return FormatNodeName("NewObject")+
                String.Format(formatProvider,"{0:"+format+"}",Constructor);
        }
    }

    public class LoadElement: TypedNode
    {
        public LoadElement(Type type): base(type) {  }

        public override Node Clone() { return new LoadElement(Type); }

        protected internal override void CallVisitorMethod(Visitor visitor, object data)
        {
            visitor.VisitLoadElement(this,data);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return FormatNodeName("LoadElement")+
                String.Format(formatProvider,"{0:"+format+"}",Type);
        }
    }

    public class LoadElementAddr: TypedNode
    {
        public LoadElementAddr(Type type): base(type) {  }

        public override Node Clone() { return new LoadElementAddr(Type); }

        protected internal override void CallVisitorMethod(Visitor visitor, object data)
        {
            visitor.VisitLoadElementAddr(this,data);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return FormatNodeName("LoadElementAddr")+
                String.Format(formatProvider,"{0:"+format+"}",Type);
        }
    }

    public class StoreElement: TypedNode
    {
        public StoreElement(Type type): base(type) {  }

        public override Node Clone() { return new StoreElement(Type); }

        protected internal override void CallVisitorMethod(Visitor visitor, object data)
        {
            visitor.VisitStoreElement(this,data);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return FormatNodeName("StoreElement")+
                String.Format(formatProvider,"{0:"+format+"}",Type);
        }
    }

    public class LoadLength: Node
    {
        public LoadLength(): base(1) {  }

        public override Node Clone() { return new LoadLength(); }

        protected internal override void CallVisitorMethod(Visitor visitor, object data)
        {
            visitor.VisitLoadLength(this,data);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return "LoadLength";
        }
    }

    public class NewArray: TypedNode
    {
        public NewArray(Type type): base(type) {  }

        public override Node Clone() { return new NewArray(Type); }

        protected internal override void CallVisitorMethod(Visitor visitor, object data)
        {
            visitor.VisitNewArray(this,data);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return FormatNodeName("NewArray")+
                String.Format(formatProvider,"{0:"+format+"}",Type);
        }
    }

    public class BoxValue: TypedNode
    {
        public BoxValue(Type type): base(type) {  }

        public override Node Clone() { return new BoxValue(Type); }

        protected internal override void CallVisitorMethod(Visitor visitor, object data)
        {
            visitor.VisitBoxValue(this,data);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return FormatNodeName("BoxValue")+
                String.Format(formatProvider,"{0:"+format+"}",Type);
        }
    }

    public class UnboxValue: TypedNode
    {
        public UnboxValue(Type type): base(type) {  }

        public override Node Clone() { return new UnboxValue(Type); }

        protected internal override void CallVisitorMethod(Visitor visitor, object data)
        {
            visitor.VisitUnboxValue(this,data);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return FormatNodeName("UnboxValue")+
                String.Format(formatProvider,"{0:"+format+"}",Type);
        }
    }

    public class InitValue: TypedNode
    {
        public InitValue(Type type): base(type) {  }

        public override Node Clone() { return new InitValue(Type); }

        protected internal override void CallVisitorMethod(Visitor visitor, object data)
        {
            visitor.VisitInitValue(this,data);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return FormatNodeName("InitValue")+
                String.Format(formatProvider,"{0:"+format+"}",Type);
        }
    }

    public class LoadSizeOfValue: TypedNode
    {
        public LoadSizeOfValue(Type type): base(type) {  }

        public override Node Clone() { return new LoadSizeOfValue(Type); }

        protected internal override void CallVisitorMethod(Visitor visitor, object data)
        {
            visitor.VisitLoadSizeOfValue(this,data);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return FormatNodeName("LoadSizeOfValue")+
                String.Format(formatProvider,"{0:"+format+"}",Type);
        }
    }

    public class MakeTypedRef: TypedNode
    {
        public MakeTypedRef(Type type): base(type) {  }

        public override Node Clone() { return new MakeTypedRef(Type); }

        protected internal override void CallVisitorMethod(Visitor visitor, object data)
        {
            visitor.VisitMakeTypedRef(this,data);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return FormatNodeName("MakeTypedRef")+
                String.Format(formatProvider,"{0:"+format+"}",Type);
        }
    }

    public class RetrieveType: Node
    {
        public RetrieveType(): base(1) {  }

        public override Node Clone() { return new RetrieveType(); }

        protected internal override void CallVisitorMethod(Visitor visitor, object data)
        {
            visitor.VisitRetrieveType(this,data);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return "RetrieveType";
        }
    }

    public class RetrieveValue: TypedNode
    {
        public RetrieveValue(Type type): base(type) {  }

        public override Node Clone() { return new RetrieveValue(Type); }

        protected internal override void CallVisitorMethod(Visitor visitor, object data)
        {
            visitor.VisitRetrieveValue(this,data);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return FormatNodeName("RetrieveValue")+
                String.Format(formatProvider,"{0:"+format+"}",Type);
        }
    }

    // ========================================================================
    // Containers for the program
    // ------------------------------------------------------------------------

    public abstract class MethodBodyHolder: IEnumerable
    {
        #region Private and internal members

        private Hashtable bodies;

        #endregion

        #region Protected members

        protected void addMethodBody(object method, MethodBodyBlock body)
        {
            if (ContainsMethodBody(method))
                removeMethodBody(method);

            bodies.Add(method,body);
        }

        protected void removeMethodBody(object method)
        {
            bodies.Remove(method);
        }

        protected MethodBodyHolder() { bodies = new Hashtable(); }

        #endregion

        public bool ContainsMethodBody(object method)
        {
            return bodies.ContainsKey(method);
        }

        public virtual MethodBodyBlock this [object method]
        {
            get { return bodies[method] as MethodBodyBlock; }
        }

		public virtual void Optimize()
		{
			foreach (MethodBodyBlock mbb in this)
			{
				BasicBlocksGraph graph = new BasicBlocksGraph(mbb);
				graph.Optimize();
			}
		}

		public ICollection getMethods() { return bodies.Keys; }

        /* Returns an enumerator that can iterate through method bodies */
        public IEnumerator GetEnumerator() { return bodies.Values.GetEnumerator(); }

        public virtual string ToString(string format, IFormatProvider formatProvider, string[] options)
        {
            string result = "";

            foreach (object method in getMethods())
            {
                BasicBlocksGraph graph = new BasicBlocksGraph(bodies[method] as MethodBodyBlock);
                result += 
					String.Format(formatProvider,"{0:"+format+"}\n",method)+"{\n"+
					graph.ToString(format,formatProvider,options)+"}\n\n";
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

    public class AssemblyHolder: MethodBodyHolder
    {
        #region Private and internal members

        private Assembly assembly;
        private MethodBodyBlock entryPoint;

        #endregion

        public AssemblyHolder(Assembly assembly)
        {
            this.assembly = assembly;

            Module[] modules = assembly.GetModules();
            foreach (Module module in modules)
            {
                ModuleEx moduleEx = new ModuleEx(module);
                foreach (MethodBase method in moduleEx)
                {
                    MethodEx methodEx = moduleEx.GetMethodEx(method);
                    MethodBodyBlock body = Converter.Convert(methodEx);
                    addMethodBody(method,body);
                }
            }

            entryPoint = null;
            MethodBase entryMethod = assembly.EntryPoint;
            if (entryMethod != null)
                entryPoint = this[entryMethod];
        }

        public Assembly Assembly { get { return assembly; } }

        public MethodBodyBlock EntryPoint { get { return entryPoint; } }
    }

	public abstract class ModifiedAssemblyHolder: MethodBodyHolder
	{
		#region Private and internal members

		private AssemblyHolder sourceHolder;

		#endregion

		protected ModifiedAssemblyHolder(AssemblyHolder sourceHolder)
		{
			this.sourceHolder = sourceHolder;
		}

		public AssemblyHolder SourceHolder { get { return sourceHolder; } }

		public override void Optimize()
		{
			SourceHolder.Optimize();
			base.Optimize();
		}
	}
}
