// =============================================================================
// CILPE - Partial Evaluator for Common Intermediate Language
// =============================================================================
// File:
//     SpecHolder.cs
//
// Description:
//     Holder for residual programs
//
// Authors:
//     Sergei Skorobogatov (Sergei.Skorobogatov@supercompilers.com)
//     Yuri Klimov (yuri.klimov@cilpe.net)
// =============================================================================

using System;

namespace CILPE.Spec
{
    using System.Reflection;
    using CILPE.Exceptions;
    using CILPE.ReflectionEx;
    using CILPE.CFG;
    using CILPE.DataModel;
    using CILPE.BTA;
    using CILPE.Config;


    public class ResidualMethod : IFormattable
    {
        #region Private members

        private readonly MemoState memo;

        #endregion

        #region Internal members

        internal readonly AnnotatedMethod AnnotatedMethod;

        internal readonly Value[] Arguments;

        internal readonly PointerValue[] Pointers;

        internal ResidualMethod (AnnotatedMethod method, MemoState memo, Value[] args, PointerValue[] ptrs)
        {
            this.AnnotatedMethod = method;
            this.memo = memo;
            this.Arguments = args;
            this.Pointers = ptrs;
        }

        #endregion

        public MethodBase SourceMethod
        {
            get
            {
                return this.AnnotatedMethod.ParamVals.Method;
            }
        }

        public bool IsConstructor
        {
            get
            {
                return this.AnnotatedMethod.ParamVals.Method.IsConstructor && Annotation.GetValueBTType(this.AnnotatedMethod.ParamVals[0].Val) == BTType.Dynamic;
            }
        }

        public override bool Equals (object obj)
        {
            if (obj is ResidualMethod)
            {
                ResidualMethod method = obj as ResidualMethod;
                return Equals(this.AnnotatedMethod, method.AnnotatedMethod) && Equals(this.memo, method.memo);
            }
            else
                return false;
        }

        public override int GetHashCode ()
        {
            return this.AnnotatedMethod.GetHashCode() ^ this.memo.GetHashCode();
        }

        public override string ToString ()
        {
            return this.ToString("CSharp", ReflectionFormatter.formatter);
        }

        public string ToString (string format, IFormatProvider formatProvider)
        {
            return this.AnnotatedMethod.ToString(format, formatProvider);
        }
    }


    public class ResidualAssemblyHolder : ModifiedAssemblyHolder
    {
        #region Internal members

        internal readonly AnnotatedAssemblyHolder AnnotatedHolder;

        internal void AddMethod (ResidualMethod method, MethodBodyBlock mbbUp)
        {
            this.addMethodBody(method, mbbUp);
        }

        internal void SpecializeMethod (ResidualMethod method)
        {
            if (! this.ContainsMethodBody(method))
                Specialization.SpecializeMethod(this, method);
        }

        #endregion

        public ResidualAssemblyHolder (AnnotatedAssemblyHolder annotatedHolder) : base(annotatedHolder.SourceHolder)
        {
            this.AnnotatedHolder = annotatedHolder;

            foreach (MethodBase method in this.SourceHolder.getMethods())
                if (method.IsDefined(typeof(SpecializeAttribute), false))
                    this.SpecializeMethod(this.GetResidualMethod(method));
        }

        public ResidualMethod GetResidualMethod (MethodBase method)
        {
            return new ResidualMethod(this.AnnotatedHolder.GetAnnotatedMethod(method), new MemoState(new Value[0]), new Value[0], new PointerValue[0]);
        }

        public MethodBodyBlock this [MethodBase method]
        {
            get
            {
                return this[this.GetResidualMethod(method)];
            }
        }
    }
}
