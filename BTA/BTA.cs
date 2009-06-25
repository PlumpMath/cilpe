// =============================================================================
// CILPE - Partial Evaluator for Common Intermediate Language
// =============================================================================
// File:
//     BTA.cs
//
// Description:
//     Binding time analysis
//
// Author:
//     Yuri Klimov (yuri.klimov@cilpe.net)
// =============================================================================

using System;

namespace CILPE.Exceptions
{
    using System.Reflection;

    internal abstract class BTAException : ApplicationException
    {
        protected BTAException (string msg) : base("BTA: " + msg)
        {
        }
    }


    internal class NotSupportedOperationException : BTAException
    {
        internal NotSupportedOperationException () : base("Not supported operation")
        {
        }
    }


    internal class InternalException : BTAException
    {
        internal InternalException () : base("Internal error")
        {
        }
    }
}


namespace CILPE.BTA
{
    using System.Reflection;
    using System.Collections;
    using CILPE.Exceptions;
    using CILPE.ReflectionEx;
    using CILPE.CFG;
    using CILPE.DataModel;
    using CILPE.Config;


    public abstract class LiftTask : IFormattable
    {
        public override string ToString ()
        {
            return this.ToString("CSharp", ReflectionFormatter.formatter);
        }

        public abstract string ToString (string format, IFormatProvider formatProvider);
    }


    public class StackLiftTask : LiftTask
    {
        #region Internal members

        internal StackLiftTask (int depth)
        {
            this.Depth = depth;
        }

        #endregion

        public readonly int Depth;

        public override string ToString (string format, IFormatProvider formatProvider)
        {
            return String.Format(formatProvider,"{0:"+format+"}", this.Depth);
        }
    }


    public class VariableLiftTask : LiftTask
    {
        #region Internal members

        internal VariableLiftTask (Variable var)
        {
            this.Variable = var;
        }

        #endregion

        public readonly Variable Variable;

        public override string ToString (string format, IFormatProvider formatProvider)
        {
            return String.Format(formatProvider,"{0:"+format+"}", this.Variable);
        }
    }


    public class Lift : ServiceNode
    {
        #region Internal members

        internal Lift (LiftTask task) : base()
        {
            this.Task = task;
        }

        #endregion

        #region Protected members

        protected override void CallVisitorMethod (Visitor visitor, object o)
        {
            if (visitor is VisitorEX)
                (visitor as VisitorEX).VisitLift(this, o);
            else
                base.CallVisitorMethod(visitor, o);
        }

        #endregion

        public readonly LiftTask Task;

        public override Node Clone ()
        {
            return new Lift(this.Task);
        }

        public override string ToString (string format, IFormatProvider formatProvider)
        {
            return FormatNodeName("Lift") + this.Task.ToString(format, formatProvider);
        }
    }


    public abstract class VisitorEX : Visitor
    {
        #region Protected members

        protected internal virtual void VisitLift (Lift node, object o)
        {
            throw new NodeNotSupportedException(node);
        }

        protected VisitorEX (GraphProcessor graphProcessor, VisitorTaskCollection tasks) : base(graphProcessor, tasks)
        {
        }

        protected VisitorEX (GraphProcessor graphProcessor, int priority, VisitorTaskCollection tasks) : base(graphProcessor, priority, tasks)
        {
        }

        #endregion
    }


    public abstract class StackVisitorEX : VisitorEX
    {
        #region Protected members

        protected StackVisitorEX (GraphProcessor graphProcessor) : base(graphProcessor, new VisitorTaskStack())
        {
        }

        protected StackVisitorEX (GraphProcessor graphProcessor, int priority) : base(graphProcessor, priority, new VisitorTaskStack())
        {
        }

        #endregion
    }



    internal class UpAndDownNodes
    {
        #region Private static members

        private static readonly int NUMBER_FOR_MERGE = 20;

        private static readonly int NUMBER_FOR_LIFT = 200;

        private static bool equalPrimitiveBTValueStates (State state1, State state2)
        {
            if (state1.Stack.Count != state2.Stack.Count)
                throw new InternalException();

            for (int i = 0; i < state1.Stack.Count; i++)
                if (! BTValue.Equals(state1.Stack[i] as PrimitiveBTValue, state2.Stack[i] as PrimitiveBTValue))
                    return false;

            foreach (Variable v in state1.Pool.GetVariables())
                if (! BTValue.Equals(state1.Pool[v].Val as PrimitiveBTValue, state2.Pool[v].Val as PrimitiveBTValue))
                    return false;

            return true;
        }

        private static bool equalStates (State state1, State state2)
        {
            if (state1.Stack.Count != state2.Stack.Count)
                throw new InternalException();

            for (int i = 0; i < state1.Stack.Count; i++)
                if (! BTValue.Equals(state1.Stack[i] as BTValue, state2.Stack[i] as BTValue))
                    return false;

            foreach (Variable v in state1.Pool.GetVariables())
                if (! BTValue.Equals(state1.Pool[v].Val as BTValue, state2.Pool[v].Val as BTValue))
                    return false;

            return true;
        }

        private static Creators pseudoMergeStates (State state1, State state2)
        {
            Creators crtrs = new Creators();
            for (int i = 0; i < state1.Stack.Count; i++)
                crtrs.AddCreators(BTValue.PseudoMerge(state1.Stack[i] as BTValue, state2.Stack[i] as BTValue));

            foreach (Variable v in state1.Pool.GetVariables())
                crtrs.AddCreators(BTValue.PseudoMerge(state1.Pool[v].Val as BTValue, state2.Pool[v].Val as BTValue));

            return crtrs;
        }

        private static Creators mergeStates (State state1, State state2)
        {
            Creators crtrs = new Creators();
            for (int i = 0; i < state1.Stack.Count; i++)
                crtrs.AddCreators(BTValue.Merge(state1.Stack[i] as BTValue, state2.Stack[i] as BTValue));

            foreach (Variable v in state1.Pool.GetVariables())
                crtrs.AddCreators(BTValue.Merge(state1.Pool[v].Val as BTValue, state2.Pool[v].Val as BTValue));

            return crtrs;
        }

        #endregion

        #region Internal static members

        internal static State CloneState (State state)
        {
            State newState = new State(state.Pool.GetVariables().Count);

            for (int i = state.Stack.Count - 1; i >= 0; i--)
                newState.Stack.Push(state.Stack[i].MakeCopy());

            foreach (Variable var in state.Pool.GetVariables())
            {
                newState.Pool[var] = new Location(var.Type);
                newState.Pool[var].Val = state.Pool[var].Val.MakeCopy();
            }

            return newState;
        }

        internal static void UpdateCreators (State state, AnnotatingVisitor visitor, Node upNode)
        {
            for (int i = 0; i < state.Stack.Count; i++)
            {
                BTValue val = state.Stack[i] as BTValue;
                if (val is PrimitiveBTValue)
                    val.Creators[visitor].AddCreator(upNode, i);
            }
            foreach (Variable var in state.Pool.GetVariables())
            {
                BTValue val = state.Pool[var].Val as BTValue;
                if (val is PrimitiveBTValue)
                    val.Creators[visitor].AddCreator(upNode, var);
            }
        }

        #endregion

        #region Private members

        private readonly Hashtable downToUp;

        private readonly Hashtable upToDown;

        private readonly Hashtable upToState;

        private Hashtable getUpNodes (Node downNode)
        {
            Hashtable upNodes = this.downToUp[downNode] as Hashtable;
            if (upNodes == null)
                this.downToUp[downNode] = upNodes = new Hashtable();

            return upNodes;
        }

        #endregion

        internal UpAndDownNodes ()
        {
            this.downToUp = new Hashtable();
            this.upToDown = new Hashtable();
            this.upToState = new Hashtable();
        }

        internal void SetUpAndDownNode (Node downNode, State state, Node upNode)
        {
            this.upToDown[upNode] = downNode;
            state = UpAndDownNodes.CloneState(state);
            this.upToState[upNode] = state;
            this.getUpNodes(downNode)[state] = upNode;
        }

        internal Node this [Node upNode]
        {
            get
            {
                return this.upToDown[upNode] as Node;
            }
        }

        internal Node this [Node downNode, State state]
        {
            get
            {
                Hashtable upNodes = this.getUpNodes(downNode);

                int count = 0;
                foreach (State key in upNodes.Keys)
                {
                    if (UpAndDownNodes.equalPrimitiveBTValueStates(state, key))
                    {
                        count++;
                        if (UpAndDownNodes.equalStates(state, key))
                            return upNodes[key] as Node;
                    }
                }

                if (count > UpAndDownNodes.NUMBER_FOR_MERGE)
                {
                    State keyState = null;
                    int keyStateCreators = 0;

                    foreach (State key in upNodes.Keys)
                    {
                        int keyCreators = UpAndDownNodes.pseudoMergeStates(state, key).Count;
                        if (keyState == null || keyStateCreators > keyCreators)
                        {
                            keyState = key;
                            keyStateCreators = keyCreators;
                        }
                    }

                    if (keyState != null && (keyStateCreators == 0 || count > UpAndDownNodes.NUMBER_FOR_LIFT))
                    {
                        Creators crtrs = UpAndDownNodes.mergeStates(state, keyState);
                        if (! crtrs.IsEmpty)
                            throw new AnnotatingVisitor.LiftException(crtrs);

                        return upNodes[keyState] as Node;
                    }
                }

                return null;
            }
        }

        internal State GetState (Node upNode)
        {
            return UpAndDownNodes.CloneState(this.upToState[upNode] as State);
        }

        internal void RemoveUpNode (Node upNode)
        {
            Node downNode = this.upToDown[upNode] as Node;
            State state = this.upToState[upNode] as State;
            this.upToDown.Remove(upNode);
            this.upToState.Remove(upNode);
            if (downNode != null && state != null)
                this.getUpNodes(downNode).Remove(state);
        }
    }


    internal class ControllingVisitor : StackVisitorEX
    {
        #region Private static members

        private static void cleanNextArray (Node upNode)
        {
            for (int i = 0; i < upNode.NextArray.Count; i++)
                upNode.NextArray[i] = null;
        }

        #endregion

        #region Internal static members

        internal static void AddAnnotatedMethodUser (AnnotatedMethod method)
        {
            method.ControllingVisitor.users++;
        }

        #endregion

        #region Private members

        private readonly AnnotatedAssemblyHolder holder;

        private readonly AnnotatedMethod method;

        private readonly AnnotatingVisitor aVisitor;

        private readonly LiftingVisitor lVisitor;

        private readonly UpAndDownNodes upDownNodes;

        private readonly MethodBodyBlock mbbUp;

        private int users;

        private void removeAnnotatedMethodUser (Node upNode)
        {
            Hashtable hash = Annotation.GetAnnotatedMethodHashtable(upNode);
            foreach (AnnotatedMethod method in hash.Values)
            {
                ControllingVisitor visitor = method.ControllingVisitor;
                if (--visitor.users == 0)
                    visitor.AddTask(null, null);
                else if (visitor.users < 0)
                    throw new InternalException();
            }
            hash.Clear();
        }

        #endregion

        #region Protected members

        protected override void DispatchNode (Node n, object o)
        {
            if (o is BTValueCreators)
                foreach (BTValueCreator crtr in o as BTValueCreators)
                {
                    Node upNode = crtr.UpNode;
                    if (upNode.Parent != null)
                    {
                        ControllingVisitor.cleanNextArray(upNode);
                        this.removeAnnotatedMethodUser(upNode);

                        if (crtr is PrimitiveCreator)
                            this.lVisitor.AddTask(upNode, (crtr as PrimitiveCreator).GetLiftTask());
                        else if (crtr is ReferenceCreator)
                            this.aVisitor.AddTask(upNode, this.upDownNodes.GetState(upNode));
                        else if (crtr is ToReferenceCreator)
                        {
                            State state = this.upDownNodes.GetState(upNode);
                            PrimitiveCreator primCrtr = (crtr as ToReferenceCreator).PrimitiveCreator;
                            if (primCrtr is StackPrimitiveCreator)
                            {
                                int depth = (primCrtr as StackPrimitiveCreator).Depth;
                                state.Stack[depth] = (state.Stack[depth] as PrimitiveBTValue).FromStack() as ReferenceBTValue;
                            }
                            else if (primCrtr is VariablePrimitiveCreator)
                            {
                                Variable var = (primCrtr as VariablePrimitiveCreator).Variable;
                                state.Pool[var].Val = (state.Pool[var].Val as PrimitiveBTValue).FromStack() as ReferenceBTValue;
                            }
                            else
                                throw new InternalException();
                            this.aVisitor.AddTask(upNode, state);
                        }
                        else
                            throw new InternalException();
                    }
                }
            else
            {
                ControllingVisitor.cleanNextArray(this.mbbUp);
                this.lVisitor.RemoveTask(this.mbbUp);
                this.aVisitor.RemoveTask(this.mbbUp);
                this.upDownNodes.RemoveUpNode(this.mbbUp);
                this.holder.RemoveMethod(this.method);
            }

            this.CleanGraph();
        }

        #endregion

        internal ControllingVisitor (AnnotatedAssemblyHolder holder, AnnotatedMethod method, UpAndDownNodes upDownNodes, MethodBodyBlock mbbUp, out AnnotatingVisitor aVisitor, out LiftingVisitor lVisitor) : base(holder.GraphProcessor, 2)
        {
            this.holder = holder;
            this.method = method;
            this.method.ControllingVisitor = this;
            this.aVisitor = aVisitor = new AnnotatingVisitor(holder, method, this, upDownNodes);
            this.lVisitor = lVisitor = new LiftingVisitor(holder, this, aVisitor, upDownNodes);
            this.upDownNodes = upDownNodes;
            this.mbbUp = mbbUp;
            this.users = 0;
        }

        internal void CleanGraph ()
        {
            foreach (Node upNode in this.mbbUp.CollectGarbage())
            {
                this.lVisitor.RemoveTask(upNode);
                this.aVisitor.RemoveTask(upNode);
                this.upDownNodes.RemoveUpNode(upNode);
                this.removeAnnotatedMethodUser(upNode);
                upNode.RemoveFromGraph();
            }
        }
    }


    internal class LiftingVisitor : StackVisitorEX
    {
        #region Private classes

        private class DepthContainer
        {
            public int Depth;

            public DepthContainer (int depth)
            {
                this.Depth = depth;
            }
        }


        #endregion

        #region Private members

        private readonly ControllingVisitor cVisitor;

        private readonly AnnotatingVisitor aVisitor;

        private readonly UpAndDownNodes upDownNodes;

        #endregion

        #region Protected members

        protected override void DispatchNode (Node upNode, object o)
        {
            State state = this.upDownNodes.GetState(upNode);
            Node downNode = this.upDownNodes[upNode];
            LiftTask task = o as LiftTask;

            if (task is StackLiftTask)
            {
                int depth = (task as StackLiftTask).Depth;
                for (int i = 0; i < depth; i++)
                    if ((state.Stack[i] as BTValue).BTType == BTType.Dynamic)
                    {
                        foreach (Node prevUp in upNode.PrevArray)
                        {
                            DepthContainer cnt = new DepthContainer(depth);
                            this.CallVisitorMethod(prevUp, cnt);
                            this.AddTask(prevUp, new StackLiftTask(cnt.Depth));
                        }
                        upNode.ReplaceByNode(null);
                        goto CleanGraph;
                    }
            }

            Node liftUp = new Lift(task);
            this.upDownNodes.SetUpAndDownNode(downNode, state, liftUp);
            this.aVisitor.AddTask(liftUp, state);
            upNode.ReplaceByNode(liftUp);

            CleanGraph:
                this.cVisitor.CleanGraph();
        }

        protected override void VisitMethodBodyBlock (MethodBodyBlock upNode, object o)
        {
            throw new InternalException();
        }

        protected override void VisitLeave (Leave upNode, object o)
        {
            throw new InternalException();
        }

        protected override void VisitDuplicateStackTop (DuplicateStackTop upNode, object o)
        {
            DepthContainer cnt = o as DepthContainer;
            cnt.Depth -= 1;
        }

        protected override void VisitRemoveStackTop (RemoveStackTop upNode, object o)
        {
            DepthContainer cnt = o as DepthContainer;
            cnt.Depth += 1;
        }

        protected override void VisitLoadConst (LoadConst upNode, object o)
        {
            DepthContainer cnt = o as DepthContainer;
            cnt.Depth -= 1;
        }

        protected override void VisitUnaryOp (UnaryOp upNode, object o)
        {
        }

        protected override void VisitConvertValue (ConvertValue upNode, object o)
        {
        }

        protected override void VisitCheckFinite (CheckFinite upNode, object o)
        {
        }

        protected override void VisitBinaryOp (BinaryOp upNode, object o)
        {
            DepthContainer cnt = o as DepthContainer;
            cnt.Depth += 1;
        }

        protected override void VisitBranch (Branch upNode, object o)
        {
            DepthContainer cnt = o as DepthContainer;
            cnt.Depth += 1;
        }

        protected override void VisitSwitch (Switch upNode, object o)
        {
            DepthContainer cnt = o as DepthContainer;
            cnt.Depth += 1;
        }

        protected override void VisitLoadVar (LoadVar upNode, object o)
        {
            DepthContainer cnt = o as DepthContainer;
            cnt.Depth -= 1;
        }

        protected override void VisitStoreVar (StoreVar upNode, object o)
        {
            DepthContainer cnt = o as DepthContainer;
            cnt.Depth += 1;
        }

        protected override void VisitCallMethod (CallMethod upNode, object o)
        {
            DepthContainer cnt = o as DepthContainer;
            cnt.Depth += ParameterValues.GetParametersNumber(upNode.Method) - (Annotation.GetReturnType(upNode.Method) != typeof(void) ? 1 : 0);
        }

        protected override void VisitNewObject (NewObject upNode, object o)
        {
            DepthContainer cnt = o as DepthContainer;
            cnt.Depth += ParameterValues.GetParametersNumber(upNode.Constructor) - 2;
        }

        protected override void VisitLoadField (LoadField upNode, object o)
        {
        }

        protected override void VisitStoreField (StoreField upNode, object o)
        {
            DepthContainer cnt = o as DepthContainer;
            cnt.Depth += 2;
        }

        protected override void VisitCastClass (CastClass upNode, object o)
        {
        }

        protected override void VisitBoxValue (BoxValue upNode, object o)
        {
        }

        protected override void VisitNewArray (NewArray upNode, object o)
        {
        }

        protected override void VisitLoadLength (LoadLength upNode, object o)
        {
        }

        protected override void VisitLoadElement (LoadElement upNode, object o)
        {
            DepthContainer cnt = o as DepthContainer;
            cnt.Depth += 1;
        }

        protected override void VisitStoreElement (StoreElement upNode, object o)
        {
            DepthContainer cnt = o as DepthContainer;
            cnt.Depth += 3;
        }

        protected internal override void VisitLift (Lift upNode, object o)
        {
        }

        protected override void VisitThrowException (ThrowException upNode, object o)
        {
        }

        #endregion

        internal LiftingVisitor (AnnotatedAssemblyHolder holder, ControllingVisitor cVisitor, AnnotatingVisitor aVisitor, UpAndDownNodes upDownNodes) : base(holder.GraphProcessor, 1)
        {
            this.cVisitor = cVisitor;
            this.aVisitor = aVisitor;
            this.upDownNodes = upDownNodes;
        }
    }


    internal class AnnotatingVisitor : StackVisitorEX
    {
        #region Internal classes

        internal class LiftException : Exception
        {
            public readonly Creators Creators;

            public LiftException (BTValue val)
            {
                this.Creators = val.Lift();
            }

            public LiftException (Creators crtrs)
            {
                this.Creators = crtrs;
            }
        }


        #endregion

        #region Private static members

        private static Type makeArrayType (Type type)
        {
            return type.Module.GetType(type.ToString()+"[]");
        }

        private static void setNewBTValue (Node upNode, ReferenceBTValue val)
        {
            upNode.Options["NewBTValue"] = val;
        }

        private static ReferenceBTValue getReturnValue (Node upNode)
        {
            return upNode.Options["ReturnValue"] as ReferenceBTValue;
        }

        private static void setReturnValue (Node upNode, ReferenceBTValue val)
        {
            upNode.Options["ReturnValue"] = val;
        }

        private static void getAllBTValue (BTValue val, ObjectHashtable hash)
        {
            if (! hash.Contains(val))
            {
                hash[val] = true;
                if (val is ReferenceBTValue)
                    foreach (BTValue v in (val as ReferenceBTValue).GetAllNotNullFieldBTValues())
                        AnnotatingVisitor.getAllBTValue(v, hash);
            }
        }

        private static void getAllFieldBTValue (BTValue val, int depth, ObjectHashtable hash)
        {
            if (! hash.Contains(val) && depth > 0)
            {
                hash[val] = true;
                if (val is ReferenceBTValue)
                    foreach (BTValue v in (val as ReferenceBTValue).GetAllFieldBTValues())
                        AnnotatingVisitor.getAllFieldBTValue(v, depth-1, hash);
            }
        }

        #endregion

        #region Internal static members

        internal static ReferenceBTValue GetNewBTValue (Node upNode)
        {
            return upNode.Options["NewBTValue"] as ReferenceBTValue;
        }

        #endregion

        #region Private members

        private readonly AnnotatedAssemblyHolder holder;

        private readonly ControllingVisitor cVisitor;

        private readonly UpAndDownNodes upDownNodes;

        private readonly ReferenceBTValue ret;

        private void addTask (Node upNode, int brIndex, Node downNext, State state)
        {
            Node upNext = this.upDownNodes[downNext, state];
            if (upNext != null)
                upNode.NextArray[brIndex] = upNext;
            else
            {
                upNext = downNext.Clone();
                upNode.NextArray[brIndex] = upNext;

                this.upDownNodes.SetUpAndDownNode(downNext, state, upNext);
                this.AddTask(upNext, UpAndDownNodes.CloneState(state));
            }
        }

        private void addCreator (BTValue val, ReferenceCreator crtr)
        {
            ObjectHashtable hash = new ObjectHashtable();
            AnnotatingVisitor.getAllBTValue(val, hash);
            foreach (BTValue v in hash.Keys)
                v.Creators[this].AddCreator(crtr);
        }

        private AnnotatedMethod checkAnnotatedMethodForCall (AnnotatedMethod method)
        {
            if (! method.SourceMethod.IsDefined(typeof(InlineAttribute), false) || (method.SourceMethod.IsConstructor && (method.ParamVals[0].Val as ReferenceBTValue).BTType == BTType.Dynamic))
            {
                Creators crtrs = new Creators();
                for (int i = 0; i < method.ParamVals.Count; i++)
                {
                    ReferenceBTValue val = method.ParamVals[i].Val as ReferenceBTValue;
                    if (val.BTType == BTType.Static)
                        crtrs.AddCreators(val.LiftAllFields());
                }
                ReferenceBTValue ret = method.ReturnValue;
                if (ret != null)
                    crtrs.AddCreators(ret.Lift());

                if (! crtrs.IsEmpty)
                    throw new LiftException(crtrs);
            }

            return this.holder.AnnotateMethod(method);
        }

        private BTType checkAnnotatedMethodForInvoke (AnnotatedMethod aMethod)
        {
            MethodBase sMethod = aMethod.SourceMethod;
            if (sMethod.DeclaringType.ToString() == "System.Object" &&
                sMethod.Name == ".ctor" &&
                sMethod.GetParameters().Length == 0)
            {
                BTValue obj = aMethod.ParamVals[0].Val as BTValue;
                return obj.BTType;
            }
            else if (this.holder.WhiteList.Contains(sMethod))
            {
                ObjectHashtable hash = new ObjectHashtable();
                for (int i = 0; i < aMethod.ParamVals.Count; i++)
                    getAllFieldBTValue(aMethod.ParamVals[i].Val as ReferenceBTValue, 5, hash);
                if (aMethod.ReturnValue != null)
                    getAllFieldBTValue(aMethod.ReturnValue, 5, hash);

                foreach (ReferenceBTValue val in hash.Keys)
                    if (val.BTType == BTType.Dynamic)
                        goto P;

                return BTType.Static;
            }

          P:

            Creators crtrs = new Creators();
            for (int i = 0; i < aMethod.ParamVals.Count; i++)
                crtrs.AddCreators((aMethod.ParamVals[i].Val as ReferenceBTValue).Lift());
            ReferenceBTValue ret = aMethod.ReturnValue;
            if (ret != null)
                crtrs.AddCreators(ret.Lift());

            if (! crtrs.IsEmpty)
                throw new LiftException(crtrs);

            return BTType.Dynamic;
        }

        private void callMethod (Node upNode, ParameterValues paramVals, ReferenceBTValue ret, bool isVirtCall)
        {
            bool flag;
            if (isVirtCall)
            {
                ReferenceBTValue thisObj = paramVals[0].Val as ReferenceBTValue;
                flag = thisObj.BTType == BTType.Dynamic;

                for (int i = 0; (! flag) && i < thisObj.Types.Length; i++)
                    if (paramVals.Method.DeclaringType.IsAssignableFrom(thisObj.Types[i]) && thisObj.Types[i] != PrimitiveBTValue.PrimitiveType())
                    {

                        ParameterValues exactParamVals = paramVals.Clone();
                        exactParamVals.ChooseVirtualMethod(thisObj.Types[i]);
                        flag = ! this.holder.SourceHolder.ContainsMethodBody(exactParamVals.Method);
                    }
            }
            else
                flag = ! this.holder.SourceHolder.ContainsMethodBody(paramVals.Method);

            if (flag)
                Annotation.SetNodeBTType(upNode, this.checkAnnotatedMethodForInvoke(new AnnotatedMethod(paramVals, ret)));
            else
            {
                if (isVirtCall)
                {
                    foreach (Type type in (paramVals[0].Val as ReferenceBTValue).Types)
                        if (paramVals.Method.DeclaringType.IsAssignableFrom(type) && type != PrimitiveBTValue.PrimitiveType())
                        {
                            ParameterValues exactParamVals = paramVals.Clone();
                            exactParamVals.ChooseVirtualMethod(type);
                            AnnotatedMethod method = new AnnotatedMethod(exactParamVals, ret);
                            Annotation.SetAnnotatedMethod(upNode, type, this.checkAnnotatedMethodForCall(method));
                        }
                }
                else
                    Annotation.SetAnnotatedMethod(upNode, this.checkAnnotatedMethodForCall(new AnnotatedMethod(paramVals, ret)));
                Annotation.SetNodeBTType(upNode, BTType.eXclusive);
            }
        }

        private static void patchUpParamVals (ParameterValues paramVals)
        {
            for (int i = 0; i < paramVals.Count; i++)
                paramVals[i].Val = (paramVals[i].Val as BTValue).FromStack();
        }

        #endregion

        #region Protected members

        protected override void DispatchNode (Node upNode, object o)
        {
            this.RemoveTask(upNode);
            try
            {
                State state = o as State;
                UpAndDownNodes.UpdateCreators(state, this, upNode);
                this.CallVisitorMethod(upNode, state);
                Node downNode = this.upDownNodes[upNode];
                if (upNode is Lift)
                    this.addTask(upNode, 0, downNode, state);
                else
                    for (int i = 0; i < downNode.NextArray.Count; i++)
                        this.addTask(upNode, i, downNode.NextArray[i], state);
            }
            catch (LiftException e)
            {
                for (int i = 0; i < upNode.NextArray.Count; i++)
                    upNode.NextArray[i] = null;
                this.AddTask(upNode, this.upDownNodes.GetState(upNode));

                foreach (AnnotatingVisitor aVisitor in e.Creators)
                    aVisitor.cVisitor.AddTask(null, e.Creators[aVisitor]);
            }
        }

        protected override void VisitMethodBodyBlock (MethodBodyBlock upNode, object o)
        {
            State state = o as State;
            foreach (Variable var in upNode.Variables)
                if (! state.Pool.ContainsVar(var))
                {
                    state.Pool[var] = new Location(var.Type);
                    state.Pool[var].Val = (new ReferenceBTValue(var.Type, BTType.Static)).ToStack(var.Type);
                }
            Annotation.SetNodeBTType(upNode, BTType.eXclusive);
        }

        protected override void VisitLeave (Leave upNode, object o)
        {
            State state = o as State;
            MethodBodyBlock mbbUpNode = upNode.Parent as MethodBodyBlock;
            if (mbbUpNode.ReturnType != typeof(void))
            {
                Creators crtrs = BTValue.Merge((state.Stack.Pop() as BTValue).FromStack(), this.ret);
                if (! crtrs.IsEmpty)
                    throw new LiftException(crtrs);
            }
            Annotation.SetNodeBTType(upNode, BTType.eXclusive);
        }

        protected override void VisitDuplicateStackTop (DuplicateStackTop upNode, object o)
        {
            State state = o as State;
            BTValue val = state.Stack[0] as BTValue;
            state.Stack.Push(val);
            Annotation.SetNodeBTType(upNode, val.BTType);
        }

        protected override void VisitRemoveStackTop (RemoveStackTop upNode, object o)
        {
            State state = o as State;
            BTValue val = state.Stack.Pop() as BTValue;
            Annotation.SetNodeBTType(upNode, val.BTType);
        }

        protected override void VisitLoadConst (LoadConst upNode, object o)
        {
            State state = o as State;
            BTValue val = new PrimitiveBTValue(BTType.Static);
            state.Stack.Push(val);
            Annotation.SetNodeBTType(upNode, BTType.Static);
        }

        protected override void VisitUnaryOp (UnaryOp upNode, object o)
        {
            State state = o as State;
            BTValue val = state.Stack[0] as PrimitiveBTValue;
            Annotation.SetNodeBTType(upNode, val.BTType);
        }

        protected override void VisitConvertValue (ConvertValue upNode, object o)
        {
            State state = o as State;
            BTValue val = state.Stack[0] as PrimitiveBTValue;
            Annotation.SetNodeBTType(upNode, val.BTType);
        }

        protected override void VisitCheckFinite (CheckFinite upNode, object o)
        {
            State state = o as State;
            BTValue val = state.Stack[0] as PrimitiveBTValue;
            Annotation.SetNodeBTType(upNode, val.BTType);
        }

        protected override void VisitBinaryOp (BinaryOp upNode, object o)
        {
            State state = o as State;
            BTValue val2 = state.Stack.Pop() as BTValue;
            BTValue val1 = state.Stack.Pop() as BTValue;

            if (val1.BTType == val2.BTType)
            {
                BTValue val3 = new PrimitiveBTValue(val1.BTType);
                state.Stack.Push(val3);
                Annotation.SetNodeBTType(upNode, val3.BTType);
            }
            else if (val1.BTType == BTType.Static)
                throw new LiftException(val1);
            else if (val2.BTType == BTType.Static)
                throw new LiftException(val2);
            else
                throw new InternalException();
        }


        protected override void VisitBranch (Branch upNode, object o)
        {
            State state = o as State;
            BTValue val = state.Stack.Pop() as BTValue;
            Annotation.SetNodeBTType(upNode, val.BTType);
        }

        protected override void VisitSwitch (Switch upNode, object o)
        {
            State state = o as State;
            BTValue val = state.Stack.Pop() as PrimitiveBTValue;
            Annotation.SetNodeBTType(upNode, val.BTType);
        }

        protected override void VisitLoadVar (LoadVar upNode, object o)
        {
            State state = o as State;
            BTValue val = state.Pool[upNode.Var].Val as BTValue;
            state.Stack.Push(val);

            BTType btType;
            if (val.BTType == BTType.Dynamic)
                btType = BTType.eXclusive;
            else
                btType = BTType.Static;
            Annotation.SetNodeBTType(upNode, btType);
        }

        protected override void VisitStoreVar (StoreVar upNode, object o)
        {
            State state = o as State;
            BTValue val = state.Stack.Pop() as BTValue;
            state.Pool[upNode.Var].Val = val;

            BTType btType;
            if (val.BTType == BTType.Dynamic)
                btType = BTType.eXclusive;
            else
                btType = BTType.Static;
            Annotation.SetNodeBTType(upNode, btType);
        }

        protected override void VisitCallMethod (CallMethod upNode, object o)
        {
            State state = o as State;
            ParameterValues paramVals = state.Stack.Perform_CallMethod(upNode.Method, upNode.IsVirtCall);
            AnnotatingVisitor.patchUpParamVals(paramVals);
            Type retType = Annotation.GetReturnType(paramVals.Method);
            ReferenceBTValue ret = AnnotatingVisitor.getReturnValue(upNode);
            if (ret == null)
            {
                ret = ReferenceBTValue.NewReferenceBTValue(retType, BTType.Static);
                AnnotatingVisitor.setReturnValue(upNode, ret);
            }

            this.callMethod(upNode, paramVals, ret, upNode.IsVirtCall && upNode.Method.IsVirtual);
            if (ret != null)
            {
                ret.Creators[this].AddCreator(upNode);
                state.Stack.Push(ret.ToStack(retType));
            }
        }

        protected override void VisitNewObject (NewObject upNode, object o)
        {
            State state = o as State;
            ReferenceBTValue obj = AnnotatingVisitor.GetNewBTValue(upNode);
            if (obj == null)
            {
                obj = new ReferenceBTValue(upNode.Constructor.DeclaringType, BTType.Static);
                AnnotatingVisitor.setNewBTValue(upNode, obj);
                obj.Creators[this].AddCreator(upNode);
            }
            ParameterValues paramVals = state.Stack.Perform_CreateObject(upNode.Constructor, obj);
            AnnotatingVisitor.patchUpParamVals(paramVals);

            this.callMethod(upNode, paramVals, null, false);
            state.Stack.Push(obj);
        }

        protected override void VisitLoadField (LoadField upNode, object o)
        {
            State state = o as State;
            ReferenceBTValue obj = state.Stack.Pop() as ReferenceBTValue;
            ReferenceBTValue fld = obj[upNode.Field];
            state.Stack.Push(fld.ToStack(upNode.Field.FieldType));

            BTType btType;
            if (obj.BTType == BTType.Dynamic)
                btType = BTType.Dynamic;
            else if (fld.BTType == BTType.Dynamic)
                btType = BTType.eXclusive;
            else
                btType = BTType.Static;
            Annotation.SetNodeBTType(upNode, btType);

            fld.Creators[this].AddCreator(upNode);
        }

        protected override void VisitStoreField (StoreField upNode, object o)
        {
            State state = o as State;
            BTValue val = state.Stack.Pop() as BTValue;
            ReferenceBTValue obj = state.Stack.Pop() as ReferenceBTValue;
            ReferenceBTValue fld = obj[upNode.Field];

            Creators crtrs = BTValue.Merge(val.FromStack(), fld);
            if (crtrs.IsEmpty)
            {
                BTType btType;
                if (obj.BTType == BTType.Dynamic)
                    btType = BTType.Dynamic;
                else if (fld.BTType == BTType.Dynamic)
                    btType = BTType.eXclusive;
                else
                    btType = BTType.Static;
                Annotation.SetNodeBTType(upNode, btType);

                fld.Creators[this].AddCreator(upNode);
            }
            else
                throw new LiftException(crtrs);
        }

        protected override void VisitCastClass (CastClass upNode, object o)
        {
            State state = o as State;
            BTValue val1 = state.Stack.Pop() as BTValue;
            BTValue val2;
            if (val1 is ReferenceBTValue)
                val2 = (val1 as ReferenceBTValue).ToStack(upNode.Type);
            else
                val2 = val1;
            state.Stack.Push(val2);
            Annotation.SetNodeBTType(upNode, val2.BTType);
        }

        protected override void VisitBoxValue (BoxValue upNode, object o)
        {
            State state = o as State;
            PrimitiveBTValue val1 = state.Stack.Pop() as PrimitiveBTValue;
            ReferenceBTValue val2 = val1.FromStack();
            state.Stack.Push(val2);
            Annotation.SetNodeBTType(upNode, val2.BTType);
        }

        protected override void VisitNewArray (NewArray upNode, object o)
        {
            State state = o as State;
            PrimitiveBTValue length = state.Stack.Pop() as PrimitiveBTValue;
            ReferenceBTValue arr = AnnotatingVisitor.GetNewBTValue(upNode);
            if (arr == null)
            {
                arr = new ReferenceBTValue(AnnotatingVisitor.makeArrayType(upNode.Type), BTType.Static);
                AnnotatingVisitor.setNewBTValue(upNode, arr);
                arr.Creators[this].AddCreator(upNode);
            }

            if (length.BTType == arr.BTType)
            {
                state.Stack.Push(arr);

                BTType btType;
                if (arr.BTType == BTType.Dynamic)
                    btType = BTType.Dynamic;
                else
                    btType = BTType.eXclusive;
                Annotation.SetNodeBTType(upNode, btType);
            }
            else if (length.BTType == BTType.Static)
                throw new LiftException(length);
            else if (arr.BTType == BTType.Static)
                throw new LiftException(arr);
            else
                throw new InternalException();
        }

        protected override void VisitLoadLength (LoadLength upNode, object o)
        {
            State state = o as State;
            ReferenceBTValue arr = state.Stack.Pop() as ReferenceBTValue;
            BTType btType = arr.BTType;
            PrimitiveBTValue length = new PrimitiveBTValue(btType);
            state.Stack.Push(length);
            Annotation.SetNodeBTType(upNode, btType);
        }

        protected override void VisitLoadElement (LoadElement upNode, object o)
        {
            State state = o as State;
            PrimitiveBTValue idx = state.Stack.Pop() as PrimitiveBTValue;
            ReferenceBTValue arr = state.Stack.Pop() as ReferenceBTValue;

            if (idx.BTType == arr.BTType)
            {
                ReferenceBTValue elm = arr.ArrayElements;
                state.Stack.Push(elm.ToStack(upNode.Type));

                BTType btType;
                if (arr.BTType == BTType.Dynamic)
                    btType = BTType.Dynamic;
                else if (elm.BTType == BTType.Dynamic)
                    btType = BTType.eXclusive;
                else
                    btType = BTType.Static;
                Annotation.SetNodeBTType(upNode, btType);

                elm.Creators[this].AddCreator(upNode);
            }
            else if (idx.BTType == BTType.Static)
                throw new LiftException(idx);
            else if (arr.BTType == BTType.Static)
                throw new LiftException(arr);
            else
                throw new InternalException();
        }

        protected override void VisitStoreElement (StoreElement upNode, object o)
        {
            State state = o as State;
            BTValue val = state.Stack.Pop() as BTValue;
            PrimitiveBTValue idx = state.Stack.Pop() as PrimitiveBTValue;
            ReferenceBTValue arr = state.Stack.Pop() as ReferenceBTValue;

            if (idx.BTType == arr.BTType)
            {
                ReferenceBTValue elm = arr.ArrayElements;

                Creators crtrs = BTValue.Merge(val.FromStack(), elm);
                if (crtrs.IsEmpty)
                {
                    BTType btType;
                    if (arr.BTType == BTType.Dynamic)
                        btType = BTType.Dynamic;
                    else if (elm.BTType == BTType.Dynamic)
                        btType = BTType.eXclusive;
                    else
                        btType = BTType.Static;
                    Annotation.SetNodeBTType(upNode, btType);

                    elm.Creators[this].AddCreator(upNode);
                }
                else
                    throw new LiftException(crtrs);
            }
            else if (idx.BTType == BTType.Static)
                throw new LiftException(idx);
            else if (arr.BTType == BTType.Static)
                throw new LiftException(arr);
            else
                throw new InternalException();
        }

        protected internal override void VisitLift (Lift upNode, object o)
        {
            State state = o as State;
            LiftTask task = upNode.Task;
            if (task is StackLiftTask)
                (state.Stack[(task as StackLiftTask).Depth] as PrimitiveBTValue).Lift();
            else if (task is VariableLiftTask)
                (state.Pool[(task as VariableLiftTask).Variable].Val as PrimitiveBTValue).Lift();
            else
                throw new InternalException();
            Annotation.SetNodeBTType(upNode, BTType.eXclusive);
        }

        protected override void VisitThrowException (ThrowException upNode, object o)
        {
            State state = o as State;
            BTValue obj = state.Stack.Pop() as BTValue; // !!!!!

            Creators crtrs = obj.Lift();
            if (! crtrs.IsEmpty)
                throw new LiftException(crtrs);

            Annotation.SetNodeBTType(upNode, BTType.Dynamic);
        }

        #endregion

        internal AnnotatingVisitor (AnnotatedAssemblyHolder holder, AnnotatedMethod method, ControllingVisitor cVisitor, UpAndDownNodes upDownNodes) : base(holder.GraphProcessor, 0)
        {
            this.holder = holder;
            this.cVisitor = cVisitor;
            this.upDownNodes = upDownNodes;
            this.ret = method.ReturnValue;
        }
    }


    public class Annotation
    {
        #region Internal static members

        internal static Type GetReturnType (MethodBase method)
        {
            if (method is MethodInfo)
                return (method as MethodInfo).ReturnType;
            else
                return typeof(void);
        }

        internal static void SetNodeBTType (Node upNode, BTType btType)
        {
            upNode.Options["BTType"] = btType;
        }

        internal static Hashtable GetAnnotatedMethodHashtable (Node upNode)
        {
            Hashtable hash = upNode.Options["AnnotatedMethodHashtable"] as Hashtable;
            if (hash == null)
                upNode.Options["AnnotatedMethodHashtable"] = hash = new Hashtable();

            return hash;
        }

        internal static void SetAnnotatedMethod (Node upNode, AnnotatedMethod method)
        {
            Annotation.GetAnnotatedMethodHashtable(upNode)["AnnotatedMethod"] = method;
            upNode.Options[Annotation.MethodBTTypeOption] = method;
            ControllingVisitor.AddAnnotatedMethodUser(method);
        }

        internal static void SetAnnotatedMethod (Node upNode, Type type, AnnotatedMethod method)
        {
            Annotation.GetAnnotatedMethodHashtable(upNode)[type] = method;
            upNode.Options[Annotation.MethodBTTypeOption] = method;
            ControllingVisitor.AddAnnotatedMethodUser(method);
        }

        internal static MethodBodyBlock AnnotateMethod (AnnotatedAssemblyHolder holder, AnnotatedMethod method)
        {
            MethodBodyBlock mbbDown = holder.SourceHolder[method.SourceMethod];
            MethodBodyBlock mbbUp = mbbDown.Clone() as MethodBodyBlock;

            UpAndDownNodes upDownNodes = new UpAndDownNodes();
            State state = new State(mbbUp.Variables.Count);
            for(int i = 0; i < mbbUp.Variables.ParameterMapper.Count; i++)
            {
                Variable var = mbbUp.Variables.ParameterMapper[i];
                state.Pool[var] = new Location(var.Type);
                state.Pool[var].Val = (method.ParamVals[i].Val as ReferenceBTValue).ToStack(var.Type);
            }
            upDownNodes.SetUpAndDownNode(mbbDown, state, mbbUp);

            AnnotatingVisitor aVisitor;
            LiftingVisitor lVisitor;
            ControllingVisitor cVisitor = new ControllingVisitor(holder, method, upDownNodes, mbbUp, out aVisitor, out lVisitor);
            aVisitor.AddTask(mbbUp, state);

            return mbbUp;
        }

        #endregion

        public static string MethodBTTypeOption
        {
            get
            {
                return "MethodBTType";
            }
        }

        public static string BTTypeOption
        {
            get
            {
                return "BTType";
            }
        }

        public static BTType GetNodeBTType (Node upNode)
        {
            return (BTType) upNode.Options["BTType"];
        }

        public static BTType GetValueBTType (Value val)
        {
            return (val as BTValue).BTType;
        }

        public static AnnotatedMethod GetAnnotatedMethod (Node upNode)
        {
            return Annotation.GetAnnotatedMethodHashtable(upNode)["AnnotatedMethod"] as AnnotatedMethod;
        }

        public static AnnotatedMethod GetAnyAnnotatedMethod (Node upNode)
        {
            IEnumerator vals = Annotation.GetAnnotatedMethodHashtable(upNode).Values.GetEnumerator();
            vals.MoveNext();
            return vals.Current as AnnotatedMethod;
        }

        public static AnnotatedMethod GetAnnotatedMethod (Node upNode, Type type)
        {
            return Annotation.GetAnnotatedMethodHashtable(upNode)[type] as AnnotatedMethod;
        }

        public static BTType[] GetObjectFieldBTTypes (NewObject upNode)
        {
            ReferenceBTValue obj = AnnotatingVisitor.GetNewBTValue(upNode);
            FieldInfo[] fldInfos = ReflectionUtils.GetAllFields(upNode.Constructor.DeclaringType);
            BTType[] btTypes = new BTType[fldInfos.Length];
            for (int i = 0; i < btTypes.Length; i++)
                btTypes[i] = obj[fldInfos[i]].BTType;

            return btTypes;
        }

        public static BTType GetArrayElementsBTType (NewArray upNode)
        {
            ReferenceBTValue arr = AnnotatingVisitor.GetNewBTValue(upNode);
            return arr.ArrayElements.BTType;
        }
    }
}
