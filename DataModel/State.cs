
// ===========================================================================
// CILPE - Partial Evaluator for Common Intermediate Language
// ===========================================================================
// File:
//     State.cs
//
// Description:
//     Machine state
//
// Author:
//     Sergei Skorobogatov (Sergei.Skorobogatov@supercompilers.com)
// Corrector:
//     Yuri Klimov (yuri.klimov@cilpe.net)
// ===========================================================================


using System;


namespace CILPE.DataModel
{
    using System.Collections;
    using System.Runtime.Serialization;
    using CILPE.Exceptions;
    using CILPE.ReflectionEx;
    using CILPE.CFG;

    public class Location: IFormattable
    {
        #region Private and internal members

        private Type type;
        private Value val;

        #endregion

        public Location(Value val)
        {
            this.type = val.Type;
            this.val = val;
        }

        public Location(Type type)
        {
            this.type = type;
            val = type.IsValueType ? 
                new StructValue(FormatterServices.GetUninitializedObject(type) as ValueType) as Value : 
                new NullValue() as Value;
        }

        public Type Type { get { return type; } }

        public Value Val
        {
            get { return this.val; }
            set { val = value.FromStack(type); }
        }

        public override string ToString()
        {
            return ToString("CSharp",ReflectionFormatter.formatter);
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            return (val == null) ? "?" : val.ToString(format,formatProvider);
        }
    }


    public class VariablesPool: IFormattable
    {
        #region Private and internal members

        private Hashtable pool;

        #endregion

        public VariablesPool (int number)
        {
            pool = new Hashtable(number);
        }

        public Location this [Variable var]
        {
            get { return pool[var] as Location; }

            set { pool[var] = value; }
        }

        public bool ContainsVar (Variable var)
        {
            return pool.Contains(var);
        }

        public ICollection GetVariables()
        {
            return(pool.Keys);
        }

        public override string ToString()
        {
            return ToString("CSharp",ReflectionFormatter.formatter);
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            string result = "";

            foreach (Variable var in pool.Keys)
                result += ", " + var.ToString() + " - " + 
                    (pool[var] as IFormattable).ToString(format,formatProvider);

            if (result.Length == 0)
                result = "VariablesPool: []";
            else
                result = "VariablesPool: ["+result.Remove(0,2)+"]";

            return result;
        }
    }


    public class State: IFormattable
    {
        #region Private and internal members

        private VariablesPool pool;
        private EvaluationStack stack;
        private IntVisitor visitor;

        private State(VariablesPool pool, EvaluationStack stack)
        {
            this.pool = pool;
            this.stack = stack;
            visitor = new IntVisitor(this);
        }

        private class IntVisitor: StackVisitor
        {
            private State state;

            public bool result;
            public Node nextNode;
            public Exception exc;

            public IntVisitor(State state): base(null)
            {
                this.state = state;
            }

            public void InterpretNode(Node node)
            {
                result = true;
                nextNode = null;
                exc = null;
                DispatchNode(node,null);
            }

            protected override void VisitMethodBodyBlock(MethodBodyBlock node, object data)
            {
                result = false;
            }

            protected override void VisitProtectedBlock(ProtectedBlock node, object data)
            {
                result = false;
            }

            protected override void VisitCatchBlock(CatchBlock node, object data)
            {
                result = false;
            }

            protected override void VisitFinallyBlock(FinallyBlock node, object data)
            {
                result = false;
            }

            protected override void VisitUserFilteredBlock(UserFilteredBlock node, object data)
            {
                result = false;
            }

            protected override void VisitFilterBlock(FilterBlock node, object data)
            {
                result = false;
            }

            protected override void VisitLeave(Leave node, object data)
            {
                result = false;
            }

            protected override void VisitUnaryOp(UnaryOp node, object data)
            {
                state.Stack.Perform_UnaryOp(node.Op);
                nextNode = node.Next;
            }

            protected override void VisitBinaryOp(BinaryOp node, object data)
            {
                state.Stack.Perform_BinaryOp(node.Op,node.Overflow,node.Unsigned,out exc);
                nextNode = node.Next;
            }

            protected override void VisitConvertValue(ConvertValue node, object data)
            {
                state.Stack.Perform_ConvertValue(node.Type,node.Overflow,node.Unsigned,out exc);
                nextNode = node.Next;
            }

            protected override void VisitCheckFinite(CheckFinite node, object data)
            {
                state.Stack.Perform_CheckFinite(out exc);
                nextNode = node.Next;
            }

            protected override void VisitBranch(Branch node, object data)
            {
                nextNode = state.Stack.Perform_Branch() ? node.Alt : node.Next;
            }

            protected override void VisitSwitch(Switch node, object data)
            {
                nextNode = node.NextArray[state.Stack.Perform_Switch(node.Count)+1];
            }

            protected override void VisitLoadConst(LoadConst node, object data)
            {
                state.Stack.Perform_LoadConst(node.Constant);
                nextNode = node.Next;
            }

            protected override void VisitLoadVar(LoadVar node, object data)
            {
                state.Perform_LoadVar(node.Var);
                nextNode = node.Next;
            }

            protected override void VisitLoadVarAddr(LoadVarAddr node, object data)
            {
                state.Perform_LoadVarAddr(node.Var);
                nextNode = node.Next;
            }

            protected override void VisitStoreVar(StoreVar node, object data)
            {
                state.Perform_StoreVar(node.Var);
                nextNode = node.Next;
            }

            protected override void VisitLoadIndirect(LoadIndirect node, object data)
            {
                state.Stack.Perform_LoadIndirect(node.Type,out exc);
                nextNode = node.Next;
            }

            protected override void VisitStoreIndirect(StoreIndirect node, object data)
            {
                state.Stack.Perform_StoreIndirect(node.Type,out exc);
                nextNode = node.Next;
            }

            protected override void VisitDuplicateStackTop(DuplicateStackTop node, object data)
            {
                state.Stack.Perform_DuplicateStackTop();
                nextNode = node.Next;
            }

            protected override void VisitRemoveStackTop(RemoveStackTop node, object data)
            {
                state.Stack.Perform_RemoveStackTop();
                nextNode = node.Next;
            }

            protected override void VisitCastClass(CastClass node, object data)
            {
                state.Stack.Perform_CastClass(node.Type,node.ThrowException,out exc);
                nextNode = node.Next;
            }

            protected override void VisitCallMethod(CallMethod node, object data)
            {
                result = false;
            }

            protected override void VisitCreateDelegate(CreateDelegate node, object data)
            {
                throw new NodeNotSupportedException(node);
            }

            protected override void VisitLoadField(LoadField node, object data)
            {
                state.Stack.Perform_LoadField(node.Field,out exc);
                nextNode = node.Next;
            }

            protected override void VisitLoadFieldAddr(LoadFieldAddr node, object data)
            {
                state.Stack.Perform_LoadFieldAddr(node.Field,out exc);
                nextNode = node.Next;
            }

            protected override void VisitStoreField(StoreField node, object data)
            {
                state.Stack.Perform_StoreField(node.Field,out exc);
                nextNode = node.Next;
            }

            protected override void VisitThrowException(ThrowException node, object data)
            {
                result = false;
            }

            protected override void VisitRethrowException(RethrowException node, object data)
            {
                result = false;
            }

            protected override void VisitNewObject(NewObject node, object data)
            {
                result = false;
            }

            protected override void VisitLoadElement(LoadElement node, object data)
            {
                state.Stack.Perform_LoadElement(node.Type,out exc);
                nextNode = node.Next;
            }

            protected override void VisitLoadElementAddr(LoadElementAddr node, object data)
            {
                state.Stack.Perform_LoadElementAddr(node.Type,out exc);
                nextNode = node.Next;
            }

            protected override void VisitStoreElement(StoreElement node, object data)
            {
                state.Stack.Perform_StoreElement(node.Type,out exc);
                nextNode = node.Next;
            }

            protected override void VisitLoadLength(LoadLength node, object data)
            {
                state.Stack.Perform_LoadLength(out exc);
                nextNode = node.Next;
            }

            protected override void VisitNewArray(NewArray node, object data)
            {
                state.Stack.Perform_NewArray(node.Type,out exc);
                nextNode = node.Next;
            }

            protected override void VisitBoxValue(BoxValue node, object data)
            {
                state.Stack.Perform_BoxValue(node.Type);
                nextNode = node.Next;
            }

            protected override void VisitUnboxValue(UnboxValue node, object data)
            {
                state.Stack.Perform_UnboxValue(node.Type,out exc);
                nextNode = node.Next;
            }

            protected override void VisitInitValue(InitValue node, object data)
            {
                state.Stack.Perform_InitValue(node.Type);
				nextNode = node.Next;
            }

            protected override void VisitLoadSizeOfValue(LoadSizeOfValue node, object data)
            {
                throw new NodeNotSupportedException(node);
            }

            protected override void VisitMakeTypedRef(MakeTypedRef node, object data)
            {
                throw new NodeNotSupportedException(node);
            }

            protected override void VisitRetrieveType(RetrieveType node, object data)
            {
                throw new NodeNotSupportedException(node);
            }

            protected override void VisitRetrieveValue(RetrieveValue node, object data)
            {
                throw new NodeNotSupportedException(node);
            }
        }

        private class StateDecomposition
        {
            public readonly Value[] Stack;

            public readonly Location[] Pool;

            public StateDecomposition (Value[] stack, Location[] pool)
            {
                this.Stack = stack;
                this.Pool = pool;
            }
        }

        #endregion

        public State (int n)
        {
            pool = new VariablesPool(n);
            stack = new EvaluationStack();
            visitor = new IntVisitor(this);
        }

        public VariablesPool Pool { get { return pool; } }

        public EvaluationStack Stack { get { return stack; } }

        public MemoState Memorize (out ObjectHashtable hash)
        {
            hash = new ObjectHashtable();

            Value[] stack = new Value[this.stack.Count];
            for (int i = 0; i < this.stack.Count; i++)
                stack[i] = this.stack[i];

            int j = 0;
            Location[] pool = new Location[this.pool.GetVariables().Count];
            foreach(Variable v in this.pool.GetVariables())
                pool[j++] = this.pool[v];

            return new MemoState(new StateDecomposition(stack, pool), hash);
        }

        public void Recall (MemoState memo, ObjectHashtable hash)
        {
            StateDecomposition dec = memo.Recall(hash) as StateDecomposition;

            this.stack.Clear();
            for (int i = dec.Stack.Length - 1; i >= 0; i--)
                this.stack.Push(dec.Stack[i]);

            int j = 0;
            foreach(Variable v in this.pool.GetVariables())
                this.pool[v].Val = dec.Pool[j++].Val;
        }

        public void Perform_LoadVar(Variable var)
        {
            Stack.Push(Pool[var].Val.MakeCopy());
        }

        public void Perform_LoadVarAddr(Variable var)
        {
            Location loc = Pool[var];
            Stack.Push(new PointerToLocationValue(loc));
        }

        public void Perform_StoreVar(Variable var)
        {
            Pool[var].Val = Stack.Pop().MakeCopy();
        }

        public override string ToString()
        {
            return ToString("CSharp",ReflectionFormatter.formatter);
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            return "State: ["+Pool.ToString(format,formatProvider)+", "+
                Stack.ToString(format,formatProvider)+"]";
        }

        public bool InterpretNode(Node node, out Node nextNode, out Exception exc)
        {
            visitor.InterpretNode(node);
            nextNode = visitor.nextNode;
            exc = visitor.exc;
            return visitor.result;
        }
    }
}
