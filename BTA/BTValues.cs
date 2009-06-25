// =============================================================================
// CILPE - Partial Evaluator for Common Intermediate Language
// =============================================================================
// File:
//     BTValues.cs
//
// Description:
//     Classes representing BT-values
//
// Author:
//     Yuri Klimov (yuri.klimov@cilpe.net)
// =============================================================================

using System;

namespace CILPE.BTA
{
    using System.Reflection;
    using System.Collections;
    using CILPE.Exceptions;
    using CILPE.ReflectionEx;
    using CILPE.CFG;
    using CILPE.DataModel;


    internal abstract class BTValueCreator
    {
        internal readonly Node UpNode;

        internal BTValueCreator (Node upNode)
        {
            this.UpNode = upNode;
        }

        public override bool Equals (object o)
        {
            if (o is BTValueCreator)
                return this.UpNode == (o as BTValueCreator).UpNode;
            else
                return false;
        }

        public override int GetHashCode ()
        {
            return this.UpNode.GetHashCode();
        }
    }


    internal abstract class PrimitiveCreator : BTValueCreator
    {
        #region Protected members

        protected PrimitiveCreator (Node upNode) : base(upNode)
        {
        }

        #endregion

        internal abstract LiftTask GetLiftTask ();
    }


    internal class StackPrimitiveCreator : PrimitiveCreator
    {
        internal readonly int Depth;

        internal StackPrimitiveCreator (Node upNode, int depth) : base(upNode)
        {
            this.Depth = depth;
        }

        internal override LiftTask GetLiftTask ()
        {
            return new StackLiftTask(this.Depth);
        }

        public override bool Equals (object o)
        {
            if (o is StackPrimitiveCreator)
                return base.Equals(o) && this.Depth == (o as StackPrimitiveCreator).Depth;
            else
                return false;

        }

        public override int GetHashCode ()
        {
            return base.GetHashCode() ^ this.Depth.GetHashCode();
        }
    }


    internal class VariablePrimitiveCreator : PrimitiveCreator
    {
        internal readonly Variable Variable;

        internal VariablePrimitiveCreator (Node upNode, Variable var) : base(upNode)
        {
            this.Variable = var;
        }

        internal override LiftTask GetLiftTask ()
        {
            return new VariableLiftTask(this.Variable);
        }

        public override bool Equals (object o)
        {
            if (o is VariablePrimitiveCreator)
                return base.Equals(o) && this.Variable == (o as VariablePrimitiveCreator).Variable;
            else
                return false;

        }

        public override int GetHashCode ()
        {
            return base.GetHashCode() ^ this.Variable.GetHashCode();
        }
    }


    internal class ToReferenceCreator : BTValueCreator
    {
        internal readonly PrimitiveCreator PrimitiveCreator;

        internal ToReferenceCreator (PrimitiveCreator crtr) : base(crtr.UpNode)
        {
            this.PrimitiveCreator = crtr;
        }

        public override bool Equals (object o)
        {
            if (o is ToReferenceCreator)
                return base.Equals(o) && this.PrimitiveCreator == (o as ToReferenceCreator).PrimitiveCreator;
            else
                return false;

        }

        public override int GetHashCode ()
        {
            return base.GetHashCode() ^ this.PrimitiveCreator.GetHashCode();
        }
    }


    internal class ReferenceCreator : BTValueCreator
    {
        internal ReferenceCreator (Node upNode) : base(upNode)
        {
        }
    }


    internal class BTValueCreators : IEnumerable
    {
        #region Private members

        private readonly Hashtable crtrs;

        #endregion

        internal BTValueCreators ()
        {
            this.crtrs = new Hashtable();
        }

        internal void AddCreator (BTValueCreator crtr)
        {
            this.crtrs[crtr] = true;
        }

        internal void AddCreators (BTValueCreators crtrs)
        {
            foreach (BTValueCreator crtr in crtrs)
                this.crtrs[crtr] = true;
        }

        internal void AddCreator (Node upNode)
        {
            this.crtrs[new ReferenceCreator(upNode)] = true;
        }

        internal void AddCreator (Node upNode, int depth)
        {
            this.crtrs[new StackPrimitiveCreator(upNode, depth)] = true;
        }

        internal void AddCreator (Node upNode, Variable var)
        {
            this.crtrs[new VariablePrimitiveCreator(upNode, var)] = true;
        }

        internal int Count
        {
            get
            {
                return this.crtrs.Keys.Count;
            }
        }

        internal bool IsEmpty
        {
            get
            {
                return this.crtrs.Keys.Count == 0;
            }
        }

        public IEnumerator GetEnumerator ()
        {
            return this.crtrs.Keys.GetEnumerator();
        }
    }


    internal class Creators : IEnumerable
    {
        #region Private members

        private readonly Hashtable hash;

        #endregion

        internal Creators ()
        {
            this.hash = new Hashtable();
        }

        internal BTValueCreators this [AnnotatingVisitor visitor]
        {
            get
            {
                BTValueCreators creators = this.hash[visitor] as BTValueCreators;
                if (creators == null)
                    this.hash[visitor] = creators = new BTValueCreators();

                return creators;
            }
        }

        internal void AddCreators (Creators crtrs)
        {
            foreach (AnnotatingVisitor visitor in crtrs.hash.Keys)
                this[visitor].AddCreators(crtrs[visitor]);
        }

        internal int Count
        {
            get
            {
                int count = 0;
                foreach (AnnotatingVisitor visitor in this.hash.Keys)
                    count += (this.hash[visitor] as BTValueCreators).Count;

                return count;
            }
        }

        internal bool IsEmpty
        {
            get
            {
                foreach (AnnotatingVisitor visitor in this.hash.Keys)
                    if (! (this.hash[visitor] as BTValueCreators).IsEmpty)
                        return false;

                return true;
            }
        }

        internal Creators Clone ()
        {
            Creators crtrs = new Creators();
            crtrs.AddCreators(this);
            return crtrs;
        }

        public IEnumerator GetEnumerator ()
        {
            return this.hash.Keys.GetEnumerator();
        }
    }



    public struct BTType
    {
        #region Private Enums

        private enum BTConst
        {
            S, D, X
        }


        #endregion

        #region Public static members

        public static BTType Static
        {
            get
            {
                return new BTType(BTConst.S);
            }
        }

        public static BTType Dynamic
        {
            get
            {
                return new BTType(BTConst.D);
            }
        }

        public static BTType eXclusive
        {
            get
            {
                return new BTType(BTConst.X);
            }
        }

        public static bool operator == (BTType btType1, BTType btType2)
        {
            return btType1.btConst == btType2.btConst;
        }

        public static bool operator != (BTType btType1, BTType btType2)
        {
            return btType1.btConst != btType2.btConst;
        }

        #endregion

        #region Private members

        private readonly BTConst btConst;

        private BTType (BTConst btConst)
        {
            this.btConst = btConst;
        }

        #endregion

        public override bool Equals (object obj)
        {
            if (obj is BTType)
                return this.btConst == ((BTType) obj).btConst;

            return false;
        }

        public override int GetHashCode ()
        {
            return this.btConst.GetHashCode();
        }

        public override string ToString ()
        {
            return this.btConst.ToString();
        }
    }


    internal abstract class BTValue : Value
    {
        #region Internal static members

        internal static Creators PseudoMerge (BTValue val1, BTValue val2)
        {
            if (val1 == null || val2 == null)
                return new Creators();
            else if (val1 is PrimitiveBTValue && val2 is PrimitiveBTValue)
                return PrimitiveBTValue.PseudoMerge(val1 as PrimitiveBTValue, val2 as PrimitiveBTValue);
            else if (val1 is ReferenceBTValue && val2 is ReferenceBTValue)
                return ReferenceBTValue.PseudoMerge(val1 as ReferenceBTValue, val2 as ReferenceBTValue);
            else if (val1 is PrimitiveBTValue)
                return (val1 as PrimitiveBTValue).ToReferenceBTValueCreators();
            else if (val2 is PrimitiveBTValue)
                return (val2 as PrimitiveBTValue).ToReferenceBTValueCreators();
            else
                throw new InternalException();
        }

        internal static Creators Merge (BTValue val1, BTValue val2)
        {
            if (val1 == null || val2 == null)
                return new Creators();
            else if (val1 is PrimitiveBTValue && val2 is PrimitiveBTValue)
                return PrimitiveBTValue.Merge(val1 as PrimitiveBTValue, val2 as PrimitiveBTValue);
            else if (val1 is ReferenceBTValue && val2 is ReferenceBTValue)
                return ReferenceBTValue.Merge(val1 as ReferenceBTValue, val2 as ReferenceBTValue);
            else if (val1 is PrimitiveBTValue)
                return (val1 as PrimitiveBTValue).ToReferenceBTValueCreators();
            else if (val2 is PrimitiveBTValue)
                return (val2 as PrimitiveBTValue).ToReferenceBTValueCreators();
            else
                throw new InternalException();
        }

        #endregion

        internal abstract BTType BTType
        {
            get;
        }

        internal abstract ReferenceBTValue FromStack ();

        internal abstract Creators Creators
        {
            get;
        }

        internal abstract Creators Lift ();

        public override Type Type
        {
            get
            {
                throw new InternalException();
            }
        }

        public override object ToParameter ()
        {
            throw new NotSupportedOperationException();
        }
    }


    internal class PrimitiveBTValue : BTValue
    {
        #region Internal static members

        internal static Creators PseudoMerge (PrimitiveBTValue val1, PrimitiveBTValue val2)
        {
            if (val1.btType == val2.btType)
                return new Creators();
            else if (val1.btType == BTType.Static)
                return val1.crtrs;
            else if (val2.btType == BTType.Static)
                return val2.crtrs;
            else
                throw new InternalException();
        }

        internal static Creators Merge (PrimitiveBTValue val1, PrimitiveBTValue val2)
        {
            if (val1.btType == val2.btType)
                return new Creators();
            else if (val1.btType == BTType.Static)
                return val1.Lift();
            else if (val2.btType == BTType.Static)
                return val2.Lift();
            else
                throw new InternalException();
        }

        internal static bool CheckType (Type type)
        {
            return type == null || type == typeof(string) || type.IsPrimitive;
        }

        internal static Type PrimitiveType ()
        {
            return typeof(ValueType);
        }

        #endregion

        #region Private members

        private BTType btType;

        private readonly Creators crtrs;

        #endregion

        internal PrimitiveBTValue (BTType btType)
        {
            this.btType = btType;
            this.crtrs = new Creators();
        }

        internal override BTType BTType
        {
            get
            {
                return this.btType;
            }
        }

        internal override ReferenceBTValue FromStack ()
        {
            ReferenceBTValue val = new ReferenceBTValue(PrimitiveBTValue.PrimitiveType(), this.btType);
            val.Creators.AddCreators(this.crtrs);
            return val;
        }

        internal override Creators Creators
        {
            get
            {
                return this.crtrs;
            }
        }

        internal override Creators Lift ()
        {
            if (this.btType == BTType.Static)
            {
                this.btType = BTType.Dynamic;
                return this.crtrs;
            }
            else
                return new Creators();
        }

        internal Creators ToReferenceBTValueCreators ()
        {
            Creators crtrs = new Creators();
            foreach (AnnotatingVisitor visitor in this.crtrs)
                foreach (PrimitiveCreator crtr in this.crtrs[visitor])
                    crtrs[visitor].AddCreator(new ToReferenceCreator(crtr as PrimitiveCreator));

            return crtrs;
        }

        public override Value MakeCopy()
        {
            return new PrimitiveBTValue(this.BTType);
        }

        public override bool Equals (object o)
        {
            if (o is PrimitiveBTValue)
                return this.btType == (o as PrimitiveBTValue).btType;
            else
                return false;
        }

        public override int GetHashCode ()
        {
            return 0;
        }

        public override string ToString (string format, IFormatProvider formatProvider)
        {
            return "[" + this.btType.ToString() + "]";
        }
    }


    internal class ReferenceBTValue : BTValue
    {
        #region Private classes

        private class BTValuePairs
        {
            #region Private classes

            private class BTValuePair
            {
                #region private members

                private readonly BTValue val1;

                private readonly BTValue val2;

                #endregion

                internal BTValuePair (BTValue val1, BTValue val2)
                {
                    this.val1 = val1;
                    this.val2 = val2;
                }

                public override bool Equals (object o)
                {
                    if (o is BTValuePair)
                    {
                        BTValuePair pair = o as BTValuePair;
                        return (this.val1 == pair.val1 && this.val2 == pair.val2) || (this.val1 == pair.val2 && this.val2 == pair.val1);
                    }
                
                    return false;
                }

                public override int GetHashCode ()
                {
                    return DataModelUtils.GetObjectHashCode(val1) ^ DataModelUtils.GetObjectHashCode(val2);
                }
            }


            #endregion

            #region private members

            private readonly Hashtable hash;

            #endregion

            internal BTValuePairs ()
            {
                this.hash = new Hashtable();
            }

            internal bool this [BTValue val1, BTValue val2]
            {
                get
                {
                    BTValuePair pair = new BTValuePair(val1, val2);
                    bool flag = this.hash.Contains(pair);
                    this.hash[pair] = true;

                    return flag;
                }
            }
        }


        #endregion

        #region Private static members

        private static Creators pseudoMerge (ReferenceBTValue val1, ReferenceBTValue val2, BTValuePairs pairs)
        {
            val1 = val1.findLeaf();
            val2 = val2.findLeaf();
            bool flag = pairs[val1, val2];

            if (! flag && val1 != val2 && val1.btType == BTType.Static && val2.btType == BTType.Static)
            {
                Creators crtrs = new Creators();

                int count = 0;
                for (int i = 0; i < val1.types.Count; i++)
                    if (val2.types.Contains(val1.types[i]))
                        count++;

                if (val1.types.Count > count)
                    crtrs.AddCreators(val2.crtrs);
                if (val2.types.Count > count)
                    crtrs.AddCreators(val1.crtrs);

                foreach (object key in val2.flds.Keys)
                {
                    ReferenceBTValue fld1 = val1.flds[key] as ReferenceBTValue;
                    ReferenceBTValue fld2 = val2.flds[key] as ReferenceBTValue;
                    if (fld1 != null)
                        crtrs.AddCreators(ReferenceBTValue.pseudoMerge(fld1, fld2, pairs));
                }

                return crtrs;
            }
            else if (flag || val1.btType == val2.btType)
                return new Creators();
            else if (val1.btType == BTType.Static)
                return val1.crtrs;
            else if (val2.btType == BTType.Static)
                return val2.crtrs;
            else
                throw new InternalException();
        }

        #endregion

        #region Internal static members

        internal static Creators PseudoMerge (ReferenceBTValue val1, ReferenceBTValue val2)
        {
            return ReferenceBTValue.pseudoMerge(val1, val2, new BTValuePairs());
        }

        internal static Creators Merge (ReferenceBTValue val1, ReferenceBTValue val2)
        {
            val1 = val1.findLeaf();
            val2 = val2.findLeaf();

            if (val1 != val2 && val1.btType  == BTType.Static && val2.btType == BTType.Static)
            {
                Creators crtrs = new Creators();

                int count = 0;
                for (int i = 0; i < val1.types.Count; i++)
                    if (val2.types.Contains(val1.types[i]))
                        count++;

                if (val1.types.Count > count)
                    crtrs.AddCreators(val2.crtrs);
                if (val2.types.Count > count)
                    crtrs.AddCreators(val1.crtrs);

                val2.setNext(val1);

                foreach (object key in val2.flds.Keys)
                {
                    ReferenceBTValue fld1 = val1.flds[key] as ReferenceBTValue;
                    ReferenceBTValue fld2 = val2.flds[key] as ReferenceBTValue;
                    if (fld1 == null)
                        val1.flds[key] = fld2;
                    else
                        crtrs.AddCreators(ReferenceBTValue.Merge(fld1, fld2));
                }

                return crtrs;
            }
            else if (val1.btType == val2.btType)
                return new Creators();
            else if (val1.btType == BTType.Static)
            {
                val1.setNext(val2);
                return val1.crtrs;
            }
            else if (val2.btType == BTType.Static)
            {
                val2.setNext(val1);
                return val2.crtrs;
            }
            else
                throw new InternalException();
        }

        internal static ReferenceBTValue NewReferenceBTValue (Type type, BTType btType)
        {
            return type == typeof(void) ? null : new ReferenceBTValue(btType);
        }

        #endregion

        #region Private members

        private ReferenceBTValue next;

        private readonly ArrayList types;

        private BTType btType;

        private readonly Hashtable flds;

        private readonly Creators crtrs;

        private ReferenceBTValue findLeaf ()
        {
            if (this.next == null)
                return this;
            else
                return this.next = this.next.findLeaf();
        }

        private void addType (Type type)
        {
            if (type != null && ! this.types.Contains(type))
                this.types.Add(type);
        }

        private void setNext (ReferenceBTValue val)
        {
            this.next = val;
            val.crtrs.AddCreators(this.crtrs);
            foreach (Type type in this.types)
                val.addType(type);
        }

        private Creators lift ()
        {
            if (this.btType == BTType.Static)
            {
                this.btType = BTType.Dynamic;
                this.flds.Clear();
                return this.crtrs;
            }
            else
                return new Creators();
        }

        private ReferenceBTValue getField (object key)
        {
            ReferenceBTValue fld = this.flds[key] as ReferenceBTValue;
            if (fld == null)
                this.flds[key] = fld = new ReferenceBTValue(this.btType);

            return fld;
        }

        #endregion

        internal ReferenceBTValue (BTType btType)
        {
            this.next = null;
            this.types = new ArrayList();
            this.btType = btType;
            this.flds = new Hashtable();
            this.crtrs = new Creators();
        }

        internal ReferenceBTValue (Type type, BTType btType)
        {
            this.next = null;
            this.types = new ArrayList();
            this.addType(type);
            this.btType = btType;
            this.flds = new Hashtable();
            this.crtrs = new Creators();
        }

        internal Type[] Types
        {
            get
            {
                return this.findLeaf().types.ToArray(typeof(Type)) as Type[];
            }
        }

        internal BTValue ToStack (Type type)
        {
            ReferenceBTValue val = this.findLeaf();
            if (PrimitiveBTValue.CheckType(type))
            {
                val.addType(PrimitiveBTValue.PrimitiveType()); //!!!!!!!!!!!!
                return new PrimitiveBTValue(val.btType);
            }
            else
                return val;
        }

        internal ReferenceBTValue this [FieldInfo fldInfo]
        {
            get
            {
                return this.findLeaf().getField(fldInfo);
            }
        }

        internal ReferenceBTValue ArrayElements
        {
            get
            {
                return this.findLeaf().getField("ArrayElements");
            }
        }

        internal ReferenceBTValue[] GetAllNotNullFieldBTValues ()
        {
            ICollection vals = this.findLeaf().flds.Values;
            ReferenceBTValue[] arr = new ReferenceBTValue[vals.Count];
            vals.CopyTo(arr, 0);
            
            return arr;
        }

        internal ReferenceBTValue[] GetAllFieldBTValues ()
        {
            ArrayList list = new ArrayList();

            foreach (Type type in this.types)
                if (type.IsArray)
                    list.Add(this.ArrayElements);
                else
                    foreach (FieldInfo fldInfo in ReflectionUtils.GetAllFields(type))
                        list.Add(this[fldInfo]);

            return list.ToArray(typeof(ReferenceBTValue)) as ReferenceBTValue[];
        }

        internal Creators LiftAllFields ()
        {
            Creators crtrs = new Creators();
            foreach (Type type in this.types)
                if (type.IsArray)
                    crtrs.AddCreators(this.ArrayElements.Lift());
                else
                    foreach (FieldInfo fldInfo in ReflectionUtils.GetAllFields(type))
                        crtrs.AddCreators(this[fldInfo].Lift());

            return crtrs;
        }

        internal override BTType BTType
        {
            get
            {
                return this.findLeaf().btType;
            }
        }

        internal override ReferenceBTValue FromStack ()
        {
            return this.findLeaf();
        }

        internal override Creators Creators
        {
            get
            {
                return this.findLeaf().crtrs;
            }
        }

        internal override Creators Lift ()
        {
            return this.findLeaf().lift();
        }

        public override Value MakeCopy()
        {
            return this.findLeaf();
        }

        public override bool Equals (object o)
        {
            if (o is ReferenceBTValue)
            {
                ReferenceBTValue val1 = this.findLeaf();
                ReferenceBTValue val2 = (o as ReferenceBTValue).findLeaf();
                if (val1.btType == BTType.Dynamic && val2.btType == BTType.Dynamic)
                    return true;
                else if (val1.types.Count == 1 && val1.types[0] == PrimitiveBTValue.PrimitiveType() && val2.types.Count == 1 && val2.types[0] == PrimitiveBTValue.PrimitiveType())
                    return val1.btType == val2.btType;
                else
                    return val1 == val2;
            }
            else
                return false;
        }

        public override int GetHashCode ()
        {
            return 0;
        }

        public override string ToString (string format, IFormatProvider formatProvider)
        {
            ReferenceBTValue val = this.findLeaf();
            string s = "[" + val.btType.ToString() + "] {";
            int i = 0;
            if (i < val.types.Count)
                s += String.Format(formatProvider, "{0:"+format+"}", val.types[i]);
            for (i++; i < val.types.Count; i++)
                s += ", " + String.Format(formatProvider, "{0:"+format+"}", val.types[i]);
            s += "} [" ;
            foreach (object key in val.flds.Keys)
                s += String.Format(formatProvider, "{0:"+format+"}", key) + " - " +
                     (val.flds[key] as ReferenceBTValue).ToString(format, formatProvider, 5) + ", ";

            return s + "]";
        }

        private string ToString (string format, IFormatProvider formatProvider, int depth)
        {
            if (depth > 0)
            {
                ReferenceBTValue val = this.findLeaf();
                string s = "[" + val.btType.ToString() + "] {";
                int i = 0;
                if (i < val.types.Count)
                    s += String.Format(formatProvider, "{0:"+format+"}", val.types[i]);
                for (i++; i < val.types.Count; i++)
                    s += ", " + String.Format(formatProvider, "{0:"+format+"}", val.types[i]);
                s += "} [" ;
                foreach (object key in val.flds.Keys)
                    s += String.Format(formatProvider, "{0:"+format+"}", key) + " - " +
                        (val.flds[key] as ReferenceBTValue).ToString(format, formatProvider, depth-1) + ", ";

                return s + "]";
            }
            else
                return "";
        }
    }


    internal class NBTValue : Value
    {
        #region Private members

        private NBTValue next;

        private ArrayList types;

        private BTType btType;

        private Hashtable flds;

        private Creators crtrs;

        private void setNext (NBTValue val)
        {
            this.next = val;
            this.types = null;
            this.flds = null;
            this.crtrs = null;
        }


        #endregion

        internal NBTValue (BTType btType)
        {
            this.next = null;
            this.types = new ArrayList();
            this.btType = btType;
            this.flds = new Hashtable();
            this.crtrs = new Creators();
        }

        public override Type Type
        {
            get
            {
                throw new NotSupportedOperationException();
            }
        }

        public override Value MakeCopy()
        {
            return this;
        }

        public override object ToParameter ()
        {
            throw new NotSupportedOperationException();
        }

        public override string ToString (string format, IFormatProvider formatProvider)
        {
            return "???";
        }
    }
}
