
// ===========================================================================
// CILPE - Partial Evaluator for Common Intermediate Language
// ===========================================================================
// File: 
//     Visitor.cs
//
// Description:
//     Abstract processing of control-flow graph
//
// Author: 
//     Sergei Skorobogatov (Sergei.Skorobogatov@supercompilers.com)
// ===========================================================================


using System;
using System.Collections;

namespace CILPE.Exceptions
{
    using CILPE.CFG;

    public abstract class VisitorException: ApplicationException
    {
        #region Private and internal members

        private Node node;

        #endregion

        public VisitorException(Node node, string msg): base("Specializer: " + msg)
        {
            this.node = node;
        }

        public Node Node { get { return node; } }
    }

    public class NodeNotSupportedException: VisitorException
    {
        public NodeNotSupportedException(Node node): 
            base(node,"Node currently not supported")
        {  }
    }
}

namespace CILPE.CFG
{
    using CILPE.Exceptions;

    internal class NodeWrapper
    {
        #region Private and internal members

        private Node node;
        private object data;
        private NodeWrapper next;

        #endregion

        public NodeWrapper(Node node, object data)
        {
            this.node = node;
            this.data = data;
            next = null;
        }

        public Node Node { get { return node; } }

        public object Data { get { return data; } }

        public NodeWrapper Next
        {
            get { return next; }
            set { next = value; }
        }
    }

    public abstract class VisitorTaskCollection
    {
        public abstract void Add(Node node, object data);
        public abstract void Get(out Node node, out object data);
        public abstract bool IsEmpty { get; }
        public abstract void Remove(Node node);
    }

    public class VisitorTaskStack: VisitorTaskCollection
    {
        #region Private and internal members

        private NodeWrapper stack = null;

        #endregion

        public override void Add(Node node, object data)
        {
            NodeWrapper head = new NodeWrapper(node,data);
            head.Next = stack;
            stack = head;
        }

        public override void Get(out Node node, out object data)
        {
            node = stack.Node;
            data = stack.Data;
            stack = stack.Next;
        }

        public override bool IsEmpty { get { return stack == null; } }

        public override void Remove(Node node)
        {
            if (stack != null)
            {
                NodeWrapper item = stack;
                while (item.Next != null)
                {
                    if (item.Next.Node == node)
                        item.Next = item.Next.Next;
                    else
                        item = item.Next;
                }

                if (stack.Node == node)
                    stack = stack.Next;
            }
        }
    }

    public class VisitorTaskQueue: VisitorTaskCollection
    {
        #region Private and internal members
        private NodeWrapper first = null, last = null;
        #endregion

        public override void Add(Node node, object data)
        {
            NodeWrapper tail = new NodeWrapper(node,data);
            
            if (last == null)
                first = last = tail;
            else
                last = last.Next = tail;
        }

        public override void Get(out Node node, out object data)
        {
            node = first.Node;
            data = first.Data;

            first = first.Next;
            if (first == null)
                last = null;
        }

        public override bool IsEmpty { get { return first == null; } }

        public override void Remove(Node node)
        {
            if (first != null)
            {
                NodeWrapper item = first;
                while (item.Next != null)
                {
                    if (item.Next.Node == node)
                        item.Next = item.Next.Next;
                    else
                        item = item.Next;
                }

                last = item;

                if (first.Node == node)
                {
                    first = first.Next;
                    if (first == null)
                        last = null;
                }
            }
        }
    }

    /* Abstract visitor is the base class for custom visitors that implement
     * different metacomputational algorithms on control-flow graph */
    public abstract class Visitor
    {
        #region Private and internal members

        /* Reference to GraphProcessor object that controls the visitor */
        private GraphProcessor graphProcessor;

        internal int priority;

        protected VisitorTaskCollection tasks = null;

        internal bool isEmpty { get { return tasks.IsEmpty; } }

        /* Processes next node */
        internal void process()
        {
            Node node;
            object data;
            tasks.Get(out node, out data);

            DispatchNode(node,data);
        }

        #endregion

        #region Protected members

        protected Visitor(GraphProcessor graphProcessor, VisitorTaskCollection tasks)
        {
            this.graphProcessor = graphProcessor;
            this.priority = 0;
            this.tasks = tasks;

            if (graphProcessor != null)
                graphProcessor.addVisitor(this);
        }

        protected Visitor(GraphProcessor graphProcessor, int priority, VisitorTaskCollection tasks)
        {
            this.graphProcessor = graphProcessor;
            this.priority = priority;
            this.tasks = tasks;

            if (graphProcessor != null)
                graphProcessor.addVisitor(this);
        }

        protected void CallVisitorMethod(Node node, object data)
        {
            node.CallVisitorMethod(this,data);
        }

        protected void AddNextNodesToTasks(Node node, object data)
        {
            foreach (Node n in node.NextArray)
                AddTask(n,data);
        }

        protected virtual void DispatchNode(Node node, object data)
        {
            CallVisitorMethod(node,data);
        }

        /* Visiting methods */
        protected internal virtual void VisitServiceNode(ServiceNode node, object data)
        {
            throw new NodeNotSupportedException(node);
        }

        protected internal virtual void VisitMethodBodyBlock(MethodBodyBlock node, object data)
        {
            throw new NodeNotSupportedException(node);
        }

        protected internal virtual void VisitProtectedBlock(ProtectedBlock node, object data)
        {
            throw new NodeNotSupportedException(node);
        }

        protected internal virtual void VisitCatchBlock(CatchBlock node, object data)
        {
            throw new NodeNotSupportedException(node);
        }

        protected internal virtual void VisitFinallyBlock(FinallyBlock node, object data)
        {
            throw new NodeNotSupportedException(node);
        }

        protected internal virtual void VisitUserFilteredBlock(UserFilteredBlock node, object data)
        {
            throw new NodeNotSupportedException(node);
        }

        protected internal virtual void VisitFilterBlock(FilterBlock node, object data)
        {
            throw new NodeNotSupportedException(node);
        }

        protected internal virtual void VisitLeave(Leave node, object data)
        {
            throw new NodeNotSupportedException(node);
        }

        protected internal virtual void VisitUnaryOp(UnaryOp node, object data)
        {
            throw new NodeNotSupportedException(node);
        }

        protected internal virtual void VisitBinaryOp(BinaryOp node, object data)
        {
            throw new NodeNotSupportedException(node);
        }

        protected internal virtual void VisitConvertValue(ConvertValue node, object data)
        {
            throw new NodeNotSupportedException(node);
        }

        protected internal virtual void VisitCheckFinite(CheckFinite node, object data)
        {
            throw new NodeNotSupportedException(node);
        }

        protected internal virtual void VisitBranch(Branch node, object data)
        {
            throw new NodeNotSupportedException(node);
        }

        protected internal virtual void VisitSwitch(Switch node, object data)
        {
            throw new NodeNotSupportedException(node);
        }

        protected internal virtual void VisitLoadConst(LoadConst node, object data)
        {
            throw new NodeNotSupportedException(node);
        }

        protected internal virtual void VisitLoadVar(LoadVar node, object data)
        {
            throw new NodeNotSupportedException(node);
        }

        protected internal virtual void VisitLoadVarAddr(LoadVarAddr node, object data)
        {
            throw new NodeNotSupportedException(node);
        }

        protected internal virtual void VisitStoreVar(StoreVar node, object data)
        {
            throw new NodeNotSupportedException(node);
        }

        protected internal virtual void VisitLoadIndirect(LoadIndirect node, object data)
        {
            throw new NodeNotSupportedException(node);
        }

        protected internal virtual void VisitStoreIndirect(StoreIndirect node, object data)
        {
            throw new NodeNotSupportedException(node);
        }

        protected internal virtual void VisitDuplicateStackTop(DuplicateStackTop node, object data)
        {
            throw new NodeNotSupportedException(node);
        }

        protected internal virtual void VisitRemoveStackTop(RemoveStackTop node, object data)
        {
            throw new NodeNotSupportedException(node);
        }

        protected internal virtual void VisitCastClass(CastClass node, object data)
        {
            throw new NodeNotSupportedException(node);
        }

        protected internal virtual void VisitCallMethod(CallMethod node, object data)
        {
            throw new NodeNotSupportedException(node);
        }

        protected internal virtual void VisitCreateDelegate(CreateDelegate node, object data)
        {
            throw new NodeNotSupportedException(node);
        }

        protected internal virtual void VisitLoadField(LoadField node, object data)
        {
            throw new NodeNotSupportedException(node);
        }

        protected internal virtual void VisitLoadFieldAddr(LoadFieldAddr node, object data)
        {
            throw new NodeNotSupportedException(node);
        }

        protected internal virtual void VisitStoreField(StoreField node, object data)
        {
            throw new NodeNotSupportedException(node);
        }

        protected internal virtual void VisitThrowException(ThrowException node, object data)
        {
            throw new NodeNotSupportedException(node);
        }

        protected internal virtual void VisitRethrowException(RethrowException node, object data)
        {
            throw new NodeNotSupportedException(node);
        }

        protected internal virtual void VisitNewObject(NewObject node, object data)
        {
            throw new NodeNotSupportedException(node);
        }

        protected internal virtual void VisitLoadElement(LoadElement node, object data)
        {
            throw new NodeNotSupportedException(node);
        }

        protected internal virtual void VisitLoadElementAddr(LoadElementAddr node, object data)
        {
            throw new NodeNotSupportedException(node);
        }

        protected internal virtual void VisitStoreElement(StoreElement node, object data)
        {
            throw new NodeNotSupportedException(node);
        }

        protected internal virtual void VisitLoadLength(LoadLength node, object data)
        {
            throw new NodeNotSupportedException(node);
        }

        protected internal virtual void VisitNewArray(NewArray node, object data)
        {
            throw new NodeNotSupportedException(node);
        }

        protected internal virtual void VisitBoxValue(BoxValue node, object data)
        {
            throw new NodeNotSupportedException(node);
        }

        protected internal virtual void VisitUnboxValue(UnboxValue node, object data)
        {
            throw new NodeNotSupportedException(node);
        }

        protected internal virtual void VisitInitValue(InitValue node, object data)
        {
            throw new NodeNotSupportedException(node);
        }

        protected internal virtual void VisitLoadSizeOfValue(LoadSizeOfValue node, object data)
        {
            throw new NodeNotSupportedException(node);
        }

        protected internal virtual void VisitMakeTypedRef(MakeTypedRef node, object data)
        {
            throw new NodeNotSupportedException(node);
        }

        protected internal virtual void VisitRetrieveType(RetrieveType node, object data)
        {
            throw new NodeNotSupportedException(node);
        }

        protected internal virtual void VisitRetrieveValue(RetrieveValue node, object data)
        {
            throw new NodeNotSupportedException(node);
        }
            
        #endregion

        public void AddTask(Node node, object data)
        {
            tasks.Add(node,data);
        }

        public void RemoveTask(Node node)
        {
            tasks.Remove(node);
        }
    }

    public abstract class StackVisitor: Visitor
    {
        protected StackVisitor(GraphProcessor graphProcessor): base(graphProcessor, new VisitorTaskStack())
        {  }

        protected StackVisitor(GraphProcessor graphProcessor, int priority): base(graphProcessor, priority, new VisitorTaskStack())
        {  }
    }

    public abstract class QueueVisitor: Visitor
    {
        protected QueueVisitor(GraphProcessor graphProcessor): base(graphProcessor, new VisitorTaskQueue())
        {  }

        protected QueueVisitor(GraphProcessor graphProcessor, int priority): base(graphProcessor, priority, new VisitorTaskQueue())
        {  }
    }

    public class GraphProcessor
    {
        #region Private and internal members

        private ArrayList visitors;

        internal void addVisitor (Visitor visitor)
        {
            int i;
            for(i = 0; i < visitors.Count && (visitors[i] as Visitor).priority >= visitor.priority; i++);
            visitors.Insert(i, visitor);
        }

        #endregion

        public GraphProcessor()
        {
            visitors = new ArrayList(5);
        }

        public void Process()
        {
            bool flag;
            
            do
            {
                flag = false;

                for (int i = 0; i < visitors.Count && ! flag; i++)
                {
                    Visitor visitor = visitors[i] as Visitor;

                    flag = ! visitor.isEmpty;
                    if (flag)
                        visitor.process();
                }
            }
            while (flag);
        }
    }
}
