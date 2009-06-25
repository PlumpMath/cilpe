
// ===========================================================================
// CILPE - Partial Evaluator for Common Intermediate Language
// ===========================================================================
// File: 
//     Methods.cs
//
// Description:
//     Reflection extensions for methods
//
// Author: 
//     Sergei Skorobogatov (Sergei.Skorobogatov@supercompilers.com)
// ===========================================================================

using System;
using System.Collections;
using System.Reflection;
using CILPE.MdDecoder;

namespace CILPE.ReflectionEx
{
    /* Types of method's locals */
    public class LocalVariables: IEnumerable
    {
        #region Private and internal members

        private Type[] types;

        internal LocalVariables(Type[] types)
        {  
            this.types = types;
        }

        #endregion

        /* Returns type of local var with specified index */
        public Type this [int index]
        {
            get { return types[index]; }
        }

        /* Returns the number of local vars */
        public int Count { get { return types.Length; } }

        /* Returns an enumerator that can iterate through types of local vars */
        public IEnumerator GetEnumerator() { return types.GetEnumerator(); }
    }

    /* Kinds of exception handling clauses */
    public enum EHClauseKind
    {
        FinallyHandler = 0,       /* Finally clause */
        FaultHandler = 1,         /* Fault clause (finally that is called on exception only) */
        TypeFilteredHandler = 2,  /* Typed exception clause */
        UserFilteredHandler = 3   /* Exception filter and handler clause */
    }

    /* Exception handling clause */
    public class EHClause
    {
        #region Private and internal members

        private EHClauseKind kind;
        private int tryStart;
        private int tryLength;
        private int handlerStart;
        private int handlerLength;
        private Type classObject;
        private int filterStart;

        internal EHClause(EHDecoder ehDecoder, int index, int[] offsetsMap)
        {
            kind = (EHClauseKind)(ehDecoder.GetKind(index));
            tryStart = offsetsMap[ehDecoder.GetTryOfs(index)];
            tryLength = offsetsMap[ehDecoder.GetTryOfs(index)+ehDecoder.GetTryLen(index)]-tryStart;
            handlerStart = offsetsMap[ehDecoder.GetHOfs(index)];
            handlerLength = offsetsMap[ehDecoder.GetHOfs(index)+ehDecoder.GetHLen(index)]-handlerStart;

            classObject = (kind == EHClauseKind.TypeFilteredHandler) ? ehDecoder.GetClass(index) as Type : null;
            filterStart = (kind == EHClauseKind.UserFilteredHandler) ? ehDecoder.GetFOfs(index) : -1;
        }

        #endregion

        /* Kind of exception handler */
        public EHClauseKind Kind { get { return kind; } }

        /* Number of first instruction of try block */
        public int TryStart { get { return tryStart; } }

        /* Length of try block in instructions */
        public int TryLength { get { return tryLength; } }

        /* Number of the very next instruction after the try block*/
        public int TryEnd { get { return tryStart + tryLength; } }
            
        /* Number of first instruction of handler */
        public int HandlerStart { get { return handlerStart; } }

        /* Length of handler in instructions */
        public int HandlerLength { get { return handlerLength; } }

        /* Number of the very next instruction after the handler */
        public int HandlerEnd { get { return handlerStart + handlerLength; } }
            
        /* Reflection object for a type-based exception handler */
        public Type ClassObject { get { return classObject; } }

        /* Number of first instruction of filter-based exception handler */
        public int FilterStart { get { return filterStart; } }

        /* The length of the filter in instructions */
        public int FilterLength { get { return handlerStart - filterStart; } }

        /* The number of the very next instruction after the filter */
        public int FilterEnd { get { return handlerStart; } }
    }

    /* Array of EH-clauses */
    public class EHClausesArray: IEnumerable
    {
        #region Private and internal members

        private EHClause[] clauses;

        internal EHClausesArray(EHDecoder ehDecoder, int[] offsetsMap)
        {  
            clauses = new EHClause [ehDecoder.GetCount()];

            for (int i = 0; i < ehDecoder.GetCount(); i++)
                clauses[i] = new EHClause(ehDecoder,i,offsetsMap);
        }
        
        #endregion

        /* Returns EH-clause by index */
        public EHClause this [int index]
        {
            get { return clauses[index]; }
        }

        /* Returns total number of EH-clauses */
        public int Count { get { return clauses.Length; } }

        /* Returns an enumerator that can iterate through EH-clauses */
        public IEnumerator GetEnumerator() { return clauses.GetEnumerator(); }
    }

    /* Class, that extends functionality of System.Reflection.MethodBase class,
     * allowing to read method bodies, local variables and exception handling
     * clauses.
     */
    public class MethodEx: IEnumerable
    {
        #region Private and internal members
    
        private MethodBase method;
        private Instruction[] body;
        private LocalVariables locals;
        private EHClausesArray ehClauses;
        private bool verified;

        internal MethodEx(MethodBase method, ILMethodDecoder decoder)
        {
            this.method = method;
            ArrayList list = new ArrayList();

            locals = new LocalVariables(decoder.GetLocalVarTypes());

            while (! decoder.EndOfCode())
                list.Add(new Instruction(decoder));

            body = new Instruction [list.Count];
            list.CopyTo(body);

            int[] offsetsMap = new int [decoder.CodeSize];
            for (int i = 0; i < body.Length; i++)
                offsetsMap[body[i].startOffset] = i;

            foreach (Instruction instr in body)
                instr.FixOffset(offsetsMap);

            ehClauses = new EHClausesArray(decoder.GetEHDecoder(),offsetsMap);

            verified = Verifier.Check(this);
        }

        #endregion

        /* Reflection object for represented method */
        public MethodBase Method { get { return method; } }

        /* Indexer through array of instructions */
        public Instruction this [int index]
        {
            get { return body[index]; }
        }

        /* Total number of instructions in method body */
        public int Count { get { return body.Length; } }

        /* Exception handling clauses */
        public EHClausesArray EHClauses { get { return ehClauses; } }

        /* Local variables */
        public LocalVariables Locals { get { return locals; } }

        /* Is method verified */
        public bool IsVerified { get { return verified; } }

        //Andrew: Debug
        public void Verify()
        {
            Verifier.Check(this);
        }

        /* Returns an enumerator that can iterate through instructions */
        public IEnumerator GetEnumerator() { return body.GetEnumerator(); }
    }
}
