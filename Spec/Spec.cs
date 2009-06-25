// =============================================================================
// CILPE - Partial Evaluator for Common Intermediate Language
// =============================================================================
// File:
//     Spec.cs
//
// Description:
//     IL specializer
//
// Author:
//     Yuri Klimov (yuri.klimov@cilpe.net)
// =============================================================================

using System;

namespace CILPE.Exceptions
{
    internal abstract class SpecException : ApplicationException
    {
        protected SpecException (string msg) : base("Specializer: " + msg)
        {
        }
    }


    internal class IncorrectBTAnnotationException : SpecException
    {
        internal IncorrectBTAnnotationException () : base("Incorrect BT-annotaion")
        {
        }
    }


    internal class InternalException : SpecException
    {
        internal InternalException () : base("Internal error")
        {
        }
    }
}


namespace CILPE.Spec
{
    using System.Runtime.Serialization;
    using System.Reflection;
    using System.Collections;
    using CILPE.Exceptions;
    using CILPE.CFG;
    using CILPE.DataModel;
    using CILPE.BTA;
    using CILPE.Config;


    internal class PointerToNode
    {
        #region Private members

        private readonly Node upNode;

        private readonly int brIndex;

        #endregion

        internal PointerToNode (Node upNode)
        {
            this.upNode = upNode;
            this.brIndex = 0;
        }

        internal PointerToNode (Node upNode, int brIndex)
        {
            this.upNode = upNode;
            this.brIndex = brIndex;
        }

        internal Node Node
        {
            get
            {
                return this.upNode.NextArray[this.brIndex];
            }

            set
            {
                this.upNode.NextArray[this.brIndex] = value;
            }
        }
    }


    internal class VariablesHashtable
    {
        #region Private classes

        private class PtrComparer : IComparer
        {
            #region Private members

            private readonly ObjectHashtable objHash;

            #endregion

            internal PtrComparer (ObjectHashtable objHash)
            {
                this.objHash = objHash;
            }

            public int Compare (object o1, object o2)
            {
                if (o1 == o2)
                    return 0;
                else if (! (o1 is PointerValue) || ! (o2 is PointerValue))
                    return 1;
                else
                {
                    PointerValue ptr1 = o1 as PointerValue;
                    PointerValue ptr2 = o2 as PointerValue;
                    int res = (int) this.objHash[ptr1.GetHeapObject()] - (int) this.objHash[ptr2.GetHeapObject()];
                    if (res == 0)
                        res = ptr1.GetQuasiOffset() - ptr2.GetQuasiOffset();
                    return res;
                }
            }
        }


        #endregion

        #region Private members

        private readonly Hashtable hash;

        #endregion

        internal VariablesHashtable ()
        {
            this.hash = new Hashtable();
        }

        internal ICollection Pointers
        {
            get
            {
                return this.hash.Keys;
            }
        }

        internal Variable this [PointerValue ptr]
        {
            get
            {
                return this.hash[ptr] as Variable;
            }

            set
            {
                this.hash[ptr] = value;
            }
        }

        internal PointerValue[] GetPointers (ObjectHashtable objHash)
        {
            ArrayList ptrs = new ArrayList();
            foreach (PointerValue ptr in this.hash.Keys)
                if (objHash.ContainsKey(ptr.GetHeapObject()))
                    ptrs.Add(ptr);
            ptrs.Sort(new PtrComparer(objHash));

            return ptrs.ToArray(typeof(PointerValue)) as PointerValue[];
        }
    }


    internal class SpecState
    {
        #region Private members

        private readonly State state;

        #endregion

        internal SpecState (int n)
        {
            this.state = new State(n);
        }

        internal EvaluationStack Stack
        {
            get
            {
                return this.state.Stack;
            }
        }

        internal VariablesPool Pool
        {
            get
            {
                return this.state.Pool;
            }
        }

        internal Node InterpretNode (Node downNode)
        {
            Node nextDown;
            Exception exc;
            this.state.InterpretNode(downNode, out nextDown, out exc);

            if (nextDown != null && exc == null)
                return nextDown;
            else if (downNode is CallMethod)
            {
                ParameterValues paramVals = this.state.Stack.Perform_CallMethod((downNode as CallMethod).Method, (downNode as CallMethod).IsVirtCall);
                Value retVal = paramVals.Invoke(out exc);
                if (exc != null)
                    throw new InternalException();
                if (retVal != null)
                    state.Stack.Push(retVal);
                return downNode.Next;
            }
            else if (downNode is NewObject)
            {
                ParameterValues paramVals = state.Stack.Perform_CreateObject((downNode as NewObject).Constructor);
                paramVals.Invoke(out exc);
                if (exc != null)
                    throw new InternalException();
                return downNode.Next;
            }
            else
                throw new IncorrectBTAnnotationException();
        }

        internal MemoSpecState Memorize (VariablesHashtable varsHash, out ObjectHashtable objHash)
        {
            MemoState memo = this.state.Memorize(out objHash);
            PointerValue[] ptrs = varsHash.GetPointers(objHash);

            Variable[] vars = new Variable[ptrs.Length];
            for (int i=0; i < vars.Length; i++)
                vars[i] = varsHash[ptrs[i] as PointerValue] as Variable;

            return new MemoSpecState(memo, vars);
        }

        internal void Recall (MemoSpecState memo, ObjectHashtable objHash)
        {
            this.state.Recall(memo.MemoState, objHash);
        }
    }


    internal class MemoSpecState
    {
        internal readonly MemoState MemoState;

        internal readonly Variable[] Variables;

        internal MemoSpecState (MemoState memo, Variable[] vars)
        {
            this.MemoState = memo;
            this.Variables = vars;
        }
    }


    internal class SpecializingVisitor : StackVisitorEX
    {
        #region Private classes

        private class NodeAndVariables
        {
            internal readonly Node Node;

            internal readonly Variable[] Variables;

            internal NodeAndVariables (Node upNode, Variable[] vars)
            {
                this.Node = upNode;
                this.Variables = vars;
            }
        }


        private class Data
        {
            internal readonly MemoSpecState MemoSpecState;

            internal readonly ObjectHashtable ObjectHashtable;

            internal readonly PointerToNode PointerToNode;

            internal Data (MemoSpecState memo, ObjectHashtable objHash, PointerToNode ptrUpNode)
            {
                this.MemoSpecState = memo;
                this.ObjectHashtable = objHash;
                this.PointerToNode = ptrUpNode;
            }
        }


        #endregion

        #region Private static members

        private static int toInt (StructValue val)
        {
            if (val.Type == typeof(int))
                return (int) val.Obj;
            else if (val.Type == typeof(IntPtr))
                return (int) (IntPtr) val.Obj;
            else
                throw new InternalException();
        }

        private static Node liftValue (Value val)
        {
            if (val is StructValue)
                return new LoadConst((val as StructValue).Obj);
            else if (val is NullValue)
                return new LoadConst(null);
            else if (val is ObjectReferenceValue && val.Type == typeof(string))
                return new LoadConst((val as ObjectReferenceValue).Obj);
            else
                throw new IncorrectBTAnnotationException();
        }

        private static PointerToNode initVariable (Variable varUp, PointerToNode ptrUp)
        {
			// Postprocessor dows not support LoadVarAddr
			return ptrUp;
            if (varUp.Type.IsValueType)
            {
                ptrUp = new PointerToNode(ptrUp.Node = new LoadVarAddr(varUp));
                return new PointerToNode(ptrUp.Node = new InitValue(varUp.Type));
            }
            else
            {
                ptrUp = new PointerToNode(ptrUp.Node = new LoadConst(null));
                return new PointerToNode(ptrUp.Node = new StoreVar(varUp));
            }
        }

        private static void createSubstitution (Variable[] vars1, Variable[] vars2, PointerToNode ptrUpNode, Node upNode)
        {
            if (vars1.Length != vars2.Length)
                throw new InternalException();
            int length = vars1.Length;
            Hashtable hash12 = new Hashtable();
            Hashtable hash21 = new Hashtable();
            Hashtable processed = new Hashtable();

            for (int i=0; i<length; i++)
            {
                hash12[vars1[i]] = vars2[i];
                hash21[vars2[i]] = vars1[i];
            }

            for (int i=0; i<length; i++)
            {
                Variable var1 = vars1[i];
                Variable var2 = hash12[var1] as Variable;
                if (var2 == var1)
                    continue;
                if (processed.ContainsKey(var1))
                    continue;

                Variable Var = var1;
                bool isLoop = false;
                //Searching for the beginning of the chain
                for (;;)
                {
                    if (hash21[var1] == null)
                        break; //the beginning of the chain;
                    var2 = var1;
                    var1 = hash21[var2] as Variable;
                    if(var1 == Var)//the loop
                    {
                        isLoop = true;
                        ptrUpNode = new PointerToNode(ptrUpNode.Node = new LoadVar(Var));
                        Var = hash21[Var] as Variable;
                        hash12.Remove(Var);
                        //we break the loop at 'Var' to have a chain
                        break;
                    }
                }

                //processing the chain, loops are already broken
                for ( ;var2 != null; )
                {
                    ptrUpNode = new PointerToNode(ptrUpNode.Node = new LoadVar(var2));
                    ptrUpNode = new PointerToNode(ptrUpNode.Node = new StoreVar(var1));
                    processed[var1] = null; //mark var1 as 'processed'
                    var1 = var2;
                    var2 = hash12[var1] as Variable;
                }

                if (isLoop)
                {
                    //restore the loop
                    ptrUpNode = new PointerToNode(ptrUpNode.Node = new StoreVar(Var));
                    processed[Var] = null; //mark Var as 'processed'
                }
            }

            ptrUpNode.Node = upNode;
        }

        #endregion

        #region Private members

        private readonly ResidualAssemblyHolder holder;

        private readonly MethodBodyBlock mbbUp;

        private readonly SpecState state;

        private readonly VariablesHashtable varsHash;

        private readonly Hashtable upDownNodes;

        private readonly ArrayList exitData;

        private Hashtable getUpNodes (Node downNode)
        {
            Hashtable upNodes = this.upDownNodes[downNode] as Hashtable;
            if (upNodes == null)
                this.upDownNodes[downNode] = upNodes = new Hashtable();

            return upNodes;
        }

        private void setUpAndDownNode (Node downNode, MemoSpecState memo, Node upNode)
        {
            this.getUpNodes(downNode)[memo.MemoState] = new NodeAndVariables(upNode, memo.Variables);
        }

        private void getUpNode (Node downNode, MemoSpecState memo, out Variable[] vars1, out Node upNode, out Variable[] vars2)
        {
            NodeAndVariables nodeVars = this.getUpNodes(downNode)[memo.MemoState] as NodeAndVariables;
            if (nodeVars == null)
            {
                vars1 = null;
                upNode = null;
                vars2 = null;
            }
            else
            {
                vars1 = memo.Variables;
                upNode = nodeVars.Node;
                vars2 = nodeVars.Variables;
            }
        }

        private void loadVar (Node downNode, PointerValue ptr, object o)
        {
            PointerToNode ptrUpNode = (o as Data).PointerToNode;
            ptrUpNode = new PointerToNode(ptrUpNode.Node = new LoadVar(this.varsHash[ptr]));
            this.AddTask(downNode.Next, ptrUpNode);
        }

        private void storeVar (Node downNode, PointerValue ptr, object o)
        {
            PointerToNode ptrUpNode = (o as Data).PointerToNode;
            ptrUpNode = new PointerToNode(ptrUpNode.Node = new StoreVar(this.varsHash[ptr]));
            ptr.SetZeroValue();
            this.AddTask(downNode.Next, ptrUpNode);
        }

        private void callMethod (Node downNode, AnnotatedMethod method, PointerToNode ptrUpNode, Node upNode, Value[] args)
        {
            if (method.SourceMethod.IsDefined(typeof(InlineAttribute), false) && ! (upNode is NewObject))
            {
                MethodBodyBlock mbbDown = this.holder.AnnotatedHolder[method];
                SpecState state = new SpecState(mbbDown.Variables.Count);

                int varCount = 0;
                int argCount = 0;
                foreach (Variable varDown in mbbDown.Variables.ParameterMapper)
                {
                    state.Pool[varDown] = new Location(varDown.Type);
                    Variable varUp = this.mbbUp.Variables.CreateVar(varDown.Type, VariableKind.Local);
                    this.varsHash[new PointerToLocationValue(state.Pool[varDown])] = varUp;

                    if (Annotation.GetValueBTType(method.ParamVals[varCount++].Val) == BTType.Static)
                    {
                        state.Pool[varDown].Val = args[argCount];
                        state.Stack.Push(args[argCount++]);
                    }
                    else
                    {
                        Node upNext = new StoreVar(varUp);
                        Node upPrevNext = ptrUpNode.Node;
                        ptrUpNode.Node = upNext;
                        upNext.Next = upPrevNext;
                    }
                }
                while (ptrUpNode.Node != null)
                    ptrUpNode = new PointerToNode(ptrUpNode.Node);
                foreach (Variable varDown in mbbDown.Variables)
                    if (! state.Pool.ContainsVar(varDown))
                    {
                        state.Pool[varDown] = new Location(varDown.Type);
                        Variable varUp = this.mbbUp.Variables.CreateVar(varDown.Type, VariableKind.Local);
                        this.varsHash[new PointerToLocationValue(state.Pool[varDown])] = varUp;
                        ptrUpNode = SpecializingVisitor.initVariable(varUp, ptrUpNode);
                    }

                int depth = state.Stack.Count + 1;

                GraphProcessor graphProc = new GraphProcessor();
                SpecializingVisitor visitor = new SpecializingVisitor(graphProc, this.holder, this.mbbUp, state, this.varsHash);
                visitor.AddTask(mbbDown.Next, ptrUpNode);
                graphProc.Process();

                foreach (Data newData in visitor.exitData)
                {
                    state.Recall(newData.MemoSpecState, newData.ObjectHashtable);
                    if (state.Stack.Count == depth)
                        this.state.Stack.Push(state.Stack.Pop());
                    this.AddTask(downNode.Next, newData.PointerToNode);
                }
            }
            else
            {
                ObjectHashtable objHash = new ObjectHashtable();
                MemoState memoArgs = new MemoState(args, objHash);
                PointerValue[] ptrs = this.varsHash.GetPointers(objHash);

                for (int i = 0; i < ptrs.Length; i++)
                    ptrUpNode = new PointerToNode(ptrUpNode.Node = new LoadVarAddr(this.varsHash[ptrs[i]]));
                ptrUpNode = new PointerToNode(ptrUpNode.Node = upNode);

                ResidualMethod callMethod = new ResidualMethod(method, memoArgs, args, ptrs);
                Specialization.SetResidualMethod(upNode, callMethod);
                this.holder.SpecializeMethod(callMethod);

                this.AddTask(downNode.Next, ptrUpNode);
            }
        }

        #endregion

        #region Protected members

        protected override void DispatchNode (Node downNode, object o)
        {
            Data data = o as Data;
            MemoSpecState memo = data.MemoSpecState;
            PointerToNode ptrUpNode = data.PointerToNode;
            this.state.Recall(memo, data.ObjectHashtable);

            BTType btType = Annotation.GetNodeBTType(downNode);
            if (btType == BTType.Static)
            {
                Node nextDownNode = this.state.InterpretNode(downNode);
                this.AddTask(nextDownNode, ptrUpNode);
            }
            else
            {
                Variable[] vars1;
                Node upNode;
                Variable[] vars2;
                this.getUpNode(downNode, memo, out vars1, out upNode, out vars2);
                if (upNode != null)
                    SpecializingVisitor.createSubstitution(vars2, vars1, ptrUpNode, upNode);
                else if (btType == BTType.eXclusive)
                    this.CallVisitorMethod(downNode, data);
                else
                {
                    upNode = ptrUpNode.Node = downNode.Clone();
                    for (int i = 0; i < downNode.NextArray.Count; i++)
                        this.AddTask(downNode.NextArray[i], new PointerToNode(upNode, i));
                }
            }

            if (ptrUpNode.Node != null)
                this.setUpAndDownNode(downNode, memo, ptrUpNode.Node);
        }

        protected override void VisitLeave (Leave downNode, object o)
        {
            this.exitData.Add(o);
        }

        protected override void VisitLoadVar (LoadVar downNode, object o)
        {
            PointerValue ptr = new PointerToLocationValue(this.state.Pool[downNode.Var]);
            this.loadVar(downNode, ptr, o);
        }

        protected override void VisitStoreVar (StoreVar downNode, object o)
        {
            PointerValue ptr = new PointerToLocationValue(this.state.Pool[downNode.Var]);
            this.storeVar(downNode, ptr, o);
        }

        protected override void VisitLoadField (LoadField downNode, object o)
        {
            ObjectReferenceValue obj = this.state.Stack.Pop() as ObjectReferenceValue;
            PointerValue ptr = new PointerToObjectFieldValue(obj.Obj, downNode.Field);
            this.loadVar(downNode, ptr, o);
        }

        protected override void VisitStoreField (StoreField downNode, object o)
        {
            ObjectReferenceValue obj = this.state.Stack.Pop() as ObjectReferenceValue;
            PointerValue ptr = new PointerToObjectFieldValue(obj.Obj, downNode.Field);
            this.storeVar(downNode, ptr, o);
        }

        protected override void VisitLoadElement (LoadElement downNode, object o)
        {
            StructValue idx = this.state.Stack.Pop() as StructValue;
            ObjectReferenceValue arr = this.state.Stack.Pop() as ObjectReferenceValue;
            PointerValue ptr = new PointerToElementValue(arr.Obj as Array, SpecializingVisitor.toInt(idx));
            this.loadVar(downNode, ptr, o);
        }

        protected override void VisitStoreElement (StoreElement downNode, object o)
        {
            StructValue idx = this.state.Stack.Pop() as StructValue;
            ObjectReferenceValue arr = this.state.Stack.Pop() as ObjectReferenceValue;
            PointerValue ptr = new PointerToElementValue(arr.Obj as Array, SpecializingVisitor.toInt(idx));
            this.storeVar(downNode, ptr, o);
        }

        protected override void VisitLoadIndirect (LoadIndirect downNode, object o)
        {
            PointerValue ptr = this.state.Stack.Pop() as PointerValue;
            this.loadVar(downNode, ptr, o);
        }

        protected override void VisitStoreIndirect (StoreIndirect downNode, object o)
        {
            PointerValue ptr = this.state.Stack.Pop() as PointerValue;
            this.storeVar(downNode, ptr, o);
        }

        protected override void VisitCallMethod (CallMethod downNode, object o)
        {
            PointerToNode ptrUpNode = (o as Data).PointerToNode;
            bool isVirtCall = downNode.IsVirtCall && downNode.Method.IsVirtual;

            AnnotatedMethod aMethod;
            if (isVirtCall)
                aMethod = Annotation.GetAnyAnnotatedMethod(downNode);
            else
                aMethod = Annotation.GetAnnotatedMethod(downNode);

            ArrayList list = new ArrayList();
            for (int i = aMethod.ParamVals.Count - 1; i >= 0; i--)
                if (Annotation.GetValueBTType(aMethod.ParamVals[i].Val) == BTType.Static)
                    list.Add(this.state.Stack.Pop());
            list.Reverse();
            Value[] args = list.ToArray(typeof(Value)) as Value[];

            MethodBase sMethod;
            if (isVirtCall)
                sMethod = (aMethod = Annotation.GetAnnotatedMethod(downNode, args[0].Type)).SourceMethod;
            else
                sMethod = aMethod.SourceMethod;

            this.callMethod(downNode, aMethod, ptrUpNode, new CallMethod(sMethod, false, downNode.IsTailCall), args);
        }

        protected override void VisitNewObject (NewObject downNode, object o)
        {
            PointerToNode ptrUpNode = (o as Data).PointerToNode;
            AnnotatedMethod method = Annotation.GetAnnotatedMethod(downNode);

            Node upNode;
            ArrayList list = new ArrayList();
            for (int i = method.ParamVals.Count - 1; i > 0; i--)
                if (Annotation.GetValueBTType(method.ParamVals[i].Val) == BTType.Static)
                    list.Add(this.state.Stack.Pop());
            if (Annotation.GetValueBTType(method.ParamVals[0].Val) == BTType.Static)
            {
                Type objtype = downNode.Constructor.DeclaringType;
                ObjectReferenceValue obj = new ObjectReferenceValue(FormatterServices.GetUninitializedObject(objtype));
                FieldInfo[] fldInfos = ReflectionUtils.GetAllFields(objtype);
                BTType[] btTypes = Annotation.GetObjectFieldBTTypes(downNode);
                for (int i = 0; i < fldInfos.Length; i++)
                    if (btTypes[i] == BTType.Dynamic)
                    {
                        Variable varUp = this.mbbUp.Variables.CreateVar(fldInfos[i].FieldType, VariableKind.Local);
                        this.varsHash[new PointerToObjectFieldValue(obj.Obj, fldInfos[i])] = varUp;
                        ptrUpNode = SpecializingVisitor.initVariable(varUp, ptrUpNode);
                    }
                list.Add(obj);
                this.state.Stack.Push(obj);
                upNode = new CallMethod(downNode.Constructor, false, false);
            }
            else
                upNode = new NewObject(downNode.Constructor);
            list.Reverse();
            Value[] args = list.ToArray(typeof(Value)) as Value[];

            this.callMethod(downNode, method, ptrUpNode, upNode, args);
        }

        protected override void VisitNewArray (NewArray downNode, object o)
        {
            PointerToNode ptrUpNode = (o as Data).PointerToNode;
            StructValue idx = this.state.Stack.Pop() as StructValue;
            Array arr = Array.CreateInstance(downNode.Type, SpecializingVisitor.toInt(idx));
            ObjectReferenceValue obj = new ObjectReferenceValue(arr);

            if (Annotation.GetArrayElementsBTType(downNode) == BTType.Dynamic)
                for (int i = 0; i < SpecializingVisitor.toInt(idx); i++)
                {
                    Variable varUp = this.mbbUp.Variables.CreateVar(downNode.Type, VariableKind.Local);
                    this.varsHash[new PointerToElementValue(arr, i)] = varUp;
                    ptrUpNode = SpecializingVisitor.initVariable(varUp, ptrUpNode);
                }

            this.state.Stack.Push(obj);
            this.AddTask(downNode.Next, ptrUpNode);
        }

        protected override void VisitLift (Lift downNode, object o)
        {
            PointerToNode ptrUpNode = (o as Data).PointerToNode;
            LiftTask task = downNode.Task;
            if (task is StackLiftTask)
            {
                int depth = (task as StackLiftTask).Depth;
                ptrUpNode = new PointerToNode(ptrUpNode.Node = SpecializingVisitor.liftValue(this.state.Stack[depth]));
                this.state.Stack.RemoveAt(depth);
            }
            else
            {
                Variable var = (task as VariableLiftTask).Variable;
                ptrUpNode = new PointerToNode(ptrUpNode.Node = SpecializingVisitor.liftValue(this.state.Pool[var].Val));
                PointerValue ptr = new PointerToLocationValue(this.state.Pool[var]);
                ptrUpNode = new PointerToNode(ptrUpNode.Node = new StoreVar(this.varsHash[ptr]));
                ptr.SetZeroValue();
            }
            this.AddTask(downNode.Next, ptrUpNode);
        }

        #endregion

        internal SpecializingVisitor (GraphProcessor graphProcessor, ResidualAssemblyHolder holder, MethodBodyBlock mbbUp, SpecState state, VariablesHashtable varsHash) : base(graphProcessor)
        {
            this.holder = holder;
            this.mbbUp = mbbUp;
            this.state = state;
            this.varsHash = varsHash;
            this.upDownNodes = new Hashtable();
            this.exitData = new ArrayList();
        }

        internal void AddTask (Node downNode, PointerToNode ptrUpNode)
        {
            ObjectHashtable objHash;
            MemoSpecState memo = this.state.Memorize(this.varsHash, out objHash);
            this.AddTask(downNode, new Data(memo, objHash, ptrUpNode));
        }

        internal void SetLastNode (Node upNode)
        {
            foreach (Data data in this.exitData)
                data.PointerToNode.Node = upNode;
        }
    }


    public class Specialization
    {
        #region Private classes

        private class DummyNode : ServiceNode
        {
            public DummyNode (Block parent) : base()
            {
                this.setParent(parent);
            }

            public override Node Clone ()
            {
                return new DummyNode(this.Parent);
            }
        }


        #endregion

        #region Internal static members

        internal static void SetResidualMethod (Node upNode, ResidualMethod method)
        {
            upNode.Options["ResidualMethod"] = method;
        }

        internal static void SpecializeMethod (ResidualAssemblyHolder holder, ResidualMethod method)
        {
            Value[] args = method.Arguments;
            PointerValue[] ptrs = method.Pointers;
            MethodBodyBlock mbbDown = holder.AnnotatedHolder[method.AnnotatedMethod];
            MethodBodyBlock mbbUp = new MethodBodyBlock(mbbDown.ReturnType);
            holder.AddMethod(method, mbbUp);

            SpecState state = new SpecState(mbbDown.Variables.Count);
            VariablesHashtable varsHash = new VariablesHashtable();

            int varCount = 0;
            int argCount = 0;
            foreach (Variable varDown in mbbDown.Variables.ParameterMapper)
            {
                state.Pool[varDown] = new Location(varDown.Type);
                Variable varUp;
                if (Annotation.GetValueBTType(method.AnnotatedMethod.ParamVals[varCount++].Val) == BTType.Static)
                {
                    state.Pool[varDown].Val = args[argCount++];
                    varUp = mbbUp.Variables.CreateVar(varDown.Type, VariableKind.Local);
                }
                else
                    varUp = mbbUp.Variables.CreateVar(varDown.Type, VariableKind.Parameter);
                varsHash[new PointerToLocationValue(state.Pool[varDown])] = varUp;
            }
            foreach (Variable varDown in mbbDown.Variables)
                if (! state.Pool.ContainsVar(varDown))
                {
                    state.Pool[varDown] = new Location(varDown.Type);
                    Variable varUp = mbbUp.Variables.CreateVar(varDown.Type, VariableKind.Local);
                    varsHash[new PointerToLocationValue(state.Pool[varDown])] = varUp;
                }

            PointerToNode ptrUpNode = new PointerToNode(mbbUp);
            Node dummyUp = new DummyNode(mbbUp);
            Node upNode = dummyUp;
            for (int i = 0; i < ptrs.Length; i++)
            {
                Type ptrType = ptrs[i].Type;
                Type type = ptrType.GetElementType();
                Variable newVar1 = mbbUp.Variables.CreateVar(ptrType, VariableKind.Parameter);
                Variable newVar2 = mbbUp.Variables.CreateVar(type, VariableKind.Local);
                varsHash[ptrs[i]] = newVar2;

                ptrUpNode = new PointerToNode(ptrUpNode.Node = new LoadVar(newVar1));
                ptrUpNode = new PointerToNode(ptrUpNode.Node = new LoadIndirect(type));
                ptrUpNode = new PointerToNode(ptrUpNode.Node = new StoreVar(newVar2));

                upNode = upNode.Next = new LoadVar(newVar1);
                upNode = upNode.Next = new LoadVar(newVar2);
                upNode = upNode.Next = new StoreIndirect(type);
            }
            upNode.Next = new Leave();
            upNode = dummyUp.Next;
            dummyUp.RemoveFromGraph();

            GraphProcessor graphProc = new GraphProcessor();
            SpecializingVisitor visitor = new SpecializingVisitor(graphProc, holder, mbbUp, state, varsHash);
            visitor.AddTask(mbbDown.Next, ptrUpNode);
            graphProc.Process();
            visitor.SetLastNode(upNode);
        }

        #endregion

        public static ResidualMethod GetResidualMethod (Node upNode)
        {
            return upNode.Options["ResidualMethod"] as ResidualMethod;
        }
    }
}
