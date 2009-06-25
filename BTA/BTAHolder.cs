// =============================================================================
// CILPE - Partial Evaluator for Common Intermediate Language
// =============================================================================
// File:
//     BTAHolder.cs
//
// Description:
//     Holder for annotated programs
//
// Authors:
//     Sergei Skorobogatov (Sergei.Skorobogatov@supercompilers.com)
//     Yuri Klimov (yuri.klimov@cilpe.net)
// =============================================================================

using System;

namespace CILPE.BTA
{
    using System.Reflection;
    using System.Collections;
    using CILPE.ReflectionEx;
    using CILPE.CFG;
    using CILPE.DataModel;
    using CILPE.Config;


    public class AnnotatedMethod : IFormattable
    {
        #region Internal static members

        internal static bool EqualMethods (AnnotatedMethod method1, AnnotatedMethod method2)
        {
            if (! Equals(method1.ParamVals.Method, method2.ParamVals.Method))
                return false;

            for (int i = 0; i < method1.ParamVals.Count; i++)
                if (! BTValue.Equals(method1.ParamVals[i].Val as ReferenceBTValue, method2.ParamVals[i].Val as ReferenceBTValue))
                    return false;

            if (! BTValue.Equals(method1.ReturnValue, method2.ReturnValue))
                return false;

            return true;
        }

        internal static Creators PseudoMergeMethods (AnnotatedMethod method1, AnnotatedMethod method2)
        {
            Creators crtrs = new Creators();
            for (int i = 0; i < method1.ParamVals.Count; i++)
                crtrs.AddCreators(BTValue.PseudoMerge(method1.ParamVals[i].Val as ReferenceBTValue, method2.ParamVals[i].Val as ReferenceBTValue));
            crtrs.AddCreators(BTValue.PseudoMerge(method1.ReturnValue, method2.ReturnValue));

            return crtrs;
        }

        internal static Creators MergeMethods (AnnotatedMethod method1, AnnotatedMethod method2)
        {
            Creators crtrs = new Creators();
            for (int i = 0; i < method1.ParamVals.Count; i++)
                crtrs.AddCreators(BTValue.Merge(method1.ParamVals[i].Val as ReferenceBTValue, method2.ParamVals[i].Val as ReferenceBTValue));
            crtrs.AddCreators(BTValue.Merge(method1.ReturnValue, method2.ReturnValue));

            return crtrs;
        }

        #endregion

        #region Internal members

        internal readonly ReferenceBTValue ReturnValue;

        internal ControllingVisitor ControllingVisitor;

        internal AnnotatedMethod (ParameterValues paramVals, ReferenceBTValue ret)
        {
            this.ParamVals = paramVals;
            this.ReturnValue = ret;
            this.ControllingVisitor = null;
        }

        #endregion

        public readonly ParameterValues ParamVals;

        public MethodBase SourceMethod
        {
            get
            {
                return this.ParamVals.Method;
            }
        }

        public override string ToString ()
        {
            return this.ToString("CSharp", ReflectionFormatter.formatter);
        }

        public string ToString (string format, IFormatProvider formatProvider)
        {
            string result = String.Format(formatProvider, "{0:"+format+"}", this.ParamVals.Method);

            for (int i = 0; i < this.ParamVals.Count; i++)
                result += "\n    $Arg: " + this.ParamVals[i].Val;

            if (this.ReturnValue != null)
                result += "\n    $Ret: " + this.ReturnValue;

            return result;
        }
    }


    public class AnnotatedAssemblyHolder : ModifiedAssemblyHolder
    {
        #region Private static members

        private static readonly int NUMBER_FOR_MERGE = 20;

        private static readonly int NUMBER_FOR_LIFT = 200;

        #endregion

        #region Private members

        private readonly Hashtable aMethods;

        #endregion

        #region Internal members

        internal readonly GraphProcessor GraphProcessor;

        internal readonly WhiteList WhiteList;

        internal AnnotatedMethod AnnotateMethod (AnnotatedMethod method)
        {
            int count = 0;
            foreach (AnnotatedMethod key in this.getMethods())
            {
                if (method.SourceMethod == key.SourceMethod)
                {
                    count++;
                    if (AnnotatedMethod.EqualMethods(method, key))
                        return key;
                }
            }

            if (count > AnnotatedAssemblyHolder.NUMBER_FOR_MERGE)
            {
                AnnotatedMethod keyMethod = null;
                int keyMethodCreators = 0;

                foreach (AnnotatedMethod key in this.getMethods())
                    if (method.SourceMethod == key.SourceMethod)
                    {
                        int keyCreators = AnnotatedMethod.PseudoMergeMethods(method, key).Count;
                        if (keyMethod == null || keyMethodCreators > keyCreators)
                        {
                            keyMethod = key;
                            keyMethodCreators = keyCreators;
                        }
                    }

                if (keyMethod != null && (keyMethodCreators == 0 || count > AnnotatedAssemblyHolder.NUMBER_FOR_LIFT))
                {
                    Creators crtrs = AnnotatedMethod.MergeMethods(method, keyMethod);
                    if (! crtrs.IsEmpty)
                        throw new AnnotatingVisitor.LiftException(crtrs);

                    return keyMethod;
                }
            }

            MethodBodyBlock mbbUp = Annotation.AnnotateMethod(this, method);
            this.addMethodBody(method, mbbUp);
            return method;
        }

        internal void RemoveMethod (AnnotatedMethod method)
        {
            this.removeMethodBody(method);
        }

        #endregion

        public AnnotatedAssemblyHolder (AssemblyHolder sourceHolder, WhiteList whiteList) : base(sourceHolder)
        {
            this.aMethods = new Hashtable();
            this.GraphProcessor = new GraphProcessor();
            this.WhiteList = whiteList;

            foreach (MethodBase method in this.SourceHolder.getMethods())
                if (method.IsDefined(typeof(SpecializeAttribute), false))
                    ControllingVisitor.AddAnnotatedMethodUser(this.AnnotateMethod(this.GetAnnotatedMethod(method)));

            this.GraphProcessor.Process();
        }

        public AnnotatedMethod GetAnnotatedMethod (MethodBase sMethod)
        {
            AnnotatedMethod aMethod = this.aMethods[sMethod] as AnnotatedMethod;
            if (aMethod == null)
            {
                ParameterInfo[] parms = sMethod.GetParameters();
                EvaluationStack stack = new EvaluationStack();
                if (! sMethod.IsStatic)
                    stack.Push(new ReferenceBTValue(BTType.Dynamic));
                for (int i = 0; i < parms.Length; i++)
                    stack.Push(new ReferenceBTValue(BTType.Dynamic));
                ParameterValues paramVals = stack.Perform_CallMethod(sMethod, false);
                ReferenceBTValue ret = ReferenceBTValue.NewReferenceBTValue(Annotation.GetReturnType(sMethod), BTType.Dynamic);
                this.aMethods[sMethod] = aMethod = new AnnotatedMethod(paramVals, ret);
            }

            return aMethod;
        }
    }
}
