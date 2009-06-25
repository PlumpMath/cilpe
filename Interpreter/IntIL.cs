
// ===========================================================================
// CILPE - Partial Evaluator for Common Intermediate Language
// ===========================================================================
// File: 
//     IntIL.cs
//
// Description:
//     IL interpreter
//
// Author: 
//     Sergei Skorobogatov (Sergei.Skorobogatov@supercompilers.com)
// ===========================================================================


using System;

namespace CILPE.Exceptions
{
    using CILPE.CFG;

    public abstract class InterpreterException: ApplicationException
    {
        #region Private and internal members

        private Node node;

        #endregion

        public InterpreterException(Node node, string msg): base("Interpreter: " + msg)
        {
            this.node = node;
        }

        public Node Node { get { return node; } }
    }
}

namespace CILPE.Interpreter
{
    using System.Collections;
    using System.Reflection;
    using CILPE.DataModel;
    using CILPE.CFG;
    using CILPE.Exceptions;

    public class IntVisitor: StackVisitor
    {
        private MethodBodyHolder holder;
        private State state;
        private Exception unhandledException = null;

        private string indent;

        protected override void DispatchNode(Node node, object data)
        {
//            Console.WriteLine(indent + state.ToString());
//            Console.WriteLine();
//
//            Console.WriteLine(indent + node.ToString());
//            Console.WriteLine();

            Exception exc;
            Node nextNode;
            bool result = state.InterpretNode(node,out nextNode,out exc);

            if (exc != null)
                HandleException(node,exc);
            else if (nextNode != null)
                AddTask(nextNode);
            else if (! result)
                base.DispatchNode(node,data);
        }

        private void AddTask(Node node) { AddTask(node,null); }

        private void HandleException(Node node, Exception exc)
        {
            // Searching appropriate handler
            Type excType = exc.GetType();
            EHBlock handler = null;
            Block parent = node.Parent;
            while (! (parent is MethodBodyBlock))
            {
                if (parent is ProtectedBlock)
                {
                    ProtectedBlock tryBlock = parent as ProtectedBlock;

                    for (int count = 0; count < tryBlock.Count && handler == null; count++)
                    {
                        if (tryBlock[count] is CatchBlock)
                        {
                            CatchBlock catchBlock = tryBlock[count] as CatchBlock;

                            if (catchBlock.Type.IsAssignableFrom(excType))
                                handler = catchBlock;
                        }
                    }
                }
                
                parent = parent.Parent;
            }

            if (handler == null)
                unhandledException = exc;
            else
            {
                state.Stack.Clear();
                state.Stack.Push(new ObjectReferenceValue(exc));
                AddTask(handler);
            }
        }

        internal IntVisitor(GraphProcessor graphProcessor, MethodBodyHolder holder, string indent):
            base(graphProcessor)
        {
            this.holder = holder;
            this.indent = indent;
        }

        protected override void VisitMethodBodyBlock(MethodBodyBlock node, object data)
        {
            foreach (Variable var in node.Variables)
                if (! state.Pool.ContainsVar(var))
                    state.Pool[var] = new Location(var.Type);

            AddTask(node.Next);
        }

        protected override void VisitProtectedBlock(ProtectedBlock node, object data)
        {
            AddTask(node.Next);
        }

        protected override void VisitCatchBlock(CatchBlock node, object data)
        {
            AddTask(node.Next);
        }

        protected override void VisitFinallyBlock(FinallyBlock node, object data)
        {
            throw new NodeNotSupportedException(node);
        }

        protected override void VisitUserFilteredBlock(UserFilteredBlock node, object data)
        {
            throw new NodeNotSupportedException(node);
        }

        protected override void VisitFilterBlock(FilterBlock node, object data)
        {
            throw new NodeNotSupportedException(node);
        }

        protected override void VisitLeave(Leave node, object data)
        {
            if (node.Parent is ProtectedBlock)
                AddTask(node.Next);
            else if (node.Parent is CatchBlock)
                AddTask(node.Next);
            else if (! (node.Parent is MethodBodyBlock))
                throw new NodeNotSupportedException(node);
        }

        protected override void VisitCallMethod(CallMethod node, object data)
        {
            ParameterValues paramVals =
                state.Stack.Perform_CallMethod(node.Method,node.IsVirtCall);

			if (node.IsVirtCall)
				paramVals.ChooseVirtualMethod();

            Exception exc = null;
            Value retVal = null;

            if (holder.ContainsMethodBody(paramVals.Method))
                retVal = InterpretMethod(holder,holder[paramVals.Method],paramVals,out exc,indent+"    ");
            else
                retVal = paramVals.Invoke(out exc);

            if (exc == null)
            {
                if (retVal != null)
                    state.Stack.Push(retVal);
                AddTask(node.Next);
            }
            else
                HandleException(node,exc);
        }

        protected override void VisitThrowException(ThrowException node, object data)
        {
            Exception obj, exc;
            state.Stack.Perform_Throw(out obj,out exc);
            HandleException(node,(exc == null) ? obj : exc);
        }

        protected override void VisitRethrowException(RethrowException node, object data)
        {
            throw new NodeNotSupportedException(node);
        }

        protected override void VisitNewObject(NewObject node, object data)
        {
            ParameterValues paramVals =
                state.Stack.Perform_CreateObject(node.Constructor);

            Exception exc = null;

            if (holder.ContainsMethodBody(paramVals.Method))
                InterpretMethod(holder,holder[paramVals.Method],paramVals,out exc,indent+"    ");
            else
                paramVals.Invoke(out exc);

            if (exc == null)
                AddTask(node.Next);
            else
                HandleException(node,exc);
        }

        public static Value InterpretMethod(MethodBodyHolder holder, 
            MethodBodyBlock body, ParameterValues paramVals, out Exception exc, 
            string indent)
        {
            exc = null;

            GraphProcessor graphProcessor = new GraphProcessor();
            IntVisitor visitor = new IntVisitor(graphProcessor,holder,indent);
            visitor.state = new State(body.Variables.Count);

            int paramCount = 0;
            foreach (Variable var in body.Variables.ParameterMapper)
                visitor.state.Pool[var] = paramVals[paramCount++];
            
            visitor.AddTask(body);
            graphProcessor.Process();

            Value result = null;
            if (visitor.unhandledException != null)
                exc = visitor.unhandledException;
            else if (body.ReturnType != typeof(void))
                result = visitor.state.Stack.Pop().FromStack(body.ReturnType);

            return result;
        }
    }
}
