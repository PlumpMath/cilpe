
// ===========================================================================
// CILPE - Partial Evaluator for Common Intermediate Language
// ===========================================================================
// File:
//     Values.cs
//
// Description:
//     Classes representing data values
//
// Author:
//     Sergei Skorobogatov (Sergei.Skorobogatov@supercompilers.com)
//     Yuri Klimov (yuri.klimov@supercompilers.ru)
// ===========================================================================


using System;


namespace CILPE.Exceptions
{
    public abstract class DmValueException : ApplicationException
    {
        public DmValueException (string msg) : base("DataModel value: " + msg)
        {
        }
    }

    public class CloningNotSupportedException : DmValueException
    {
        public CloningNotSupportedException () : base("cloning of such a value is currently not supported")
        {
        }
    }

    public class EqualsNotSupportedException : DmValueException
    {
        public EqualsNotSupportedException () : base("equals of such a value is currently not supported")
        {
        }
    }

    public class GetHashCodeNotSupportedException : DmValueException
    {
        public GetHashCodeNotSupportedException () : base("get hash code of such a value is currently not supported")
        {
        }
    }
}


namespace CILPE.DataModel
{
    using System.Reflection;
    using System.Runtime.Serialization;
    using CILPE.Exceptions;
    using CILPE.ReflectionEx;

    public abstract class Value: IFormattable
    {
        public abstract Type Type { get; }

        public abstract Value MakeCopy();

        public virtual Value ToStack() { return this; }

        public virtual Value FromStack(Type targetType) { return this; }

        public abstract object ToParameter();

        public override string ToString()
        {
            return ToString("CSharp",ReflectionFormatter.formatter);
        }

        public abstract string ToString(string format, IFormatProvider formatProvider);
    }

    public class StructValue : Value
    {
        #region Private and internal members

        private Type type;
        private bool isPrimitive;
        private object obj;

        internal enum TypeIndex
        {
            INVALID    = -1,
            INT32      = 0,
            INT64      = 1,
            NATIVEINT  = 2,
            FLOAT64    = 3,
            INT8       = 4,
            UINT8      = 5,
            INT16      = 6,
            UINT16     = 7,
            UINT32     = 8,
            UINT64     = 9,
            UNATIVEINT = 10,
            FLOAT32    = 11,
            BOOL       = 12,
            CHAR       = 13
        };

        internal TypeIndex getTypeIndex ()
        {
            return getTypeIndex(obj.GetType());
        }

        internal static TypeIndex getTypeIndex (Type type)
        {
            TypeIndex index;
            if (type == typeof(Int32))
                index = TypeIndex.INT32;
            else if (type == typeof(Int64))
                index = TypeIndex.INT64;
            else if (type == typeof(IntPtr))
                index = TypeIndex.NATIVEINT;
            else if (type == typeof(Double))
                index = TypeIndex.FLOAT64;
            else if (type == typeof(SByte))
                index = TypeIndex.INT8;
            else if (type == typeof(Byte))
                index = TypeIndex.UINT8;
            else if (type == typeof(Int16))
                index = TypeIndex.INT16;
            else if (type == typeof(UInt16))
                index = TypeIndex.UINT16;
            else if (type == typeof(UInt32))
                index = TypeIndex.UINT32;
            else if (type == typeof(UInt64))
                index = TypeIndex.UINT64;
            else if (type == typeof(UIntPtr))
                index = TypeIndex.UNATIVEINT;
            else if (type == typeof(Single))
                index = TypeIndex.FLOAT32;
            else if (type == typeof(Boolean))
                index = TypeIndex.BOOL;
            else if (type == typeof(Char))
                index = TypeIndex.CHAR;
            else
                index = TypeIndex.INVALID;

            return index;
        }

        internal static Type getEnumType(Type type)
        {
            FieldInfo[] fields = type.GetFields(
                (BindingFlags)
                (BindingFlags.Instance | BindingFlags.Public | 
                BindingFlags.NonPublic)
                );

            return fields[0].FieldType;            
        }

        private static ValueType getEnumValue(object obj)
        {
            FieldInfo[] fields = obj.GetType().GetFields(
                (BindingFlags)
                (BindingFlags.Instance | BindingFlags.Public | 
                BindingFlags.NonPublic)
                );

            return fields[0].GetValue(obj) as ValueType;
        }

        private static object newEnum(Type type, object val)
        {
            FieldInfo[] fields = type.GetFields(
                (BindingFlags)
                (BindingFlags.Instance | BindingFlags.Public | 
                BindingFlags.NonPublic)
                );

            object result = FormatterServices.GetUninitializedObject(type);
            fields[0].SetValue(result,val);
            return result;
        }

        #endregion

        public StructValue(ValueType obj)
        {
            type = obj.GetType();
            isPrimitive = getTypeIndex(type) != TypeIndex.INVALID;
            this.obj = obj;
        }

        public override Type Type { get { return type; } }

        public bool IsPrimitive { get { return isPrimitive; } }

        public ValueType Obj 
        { 
            get { return obj as ValueType; }
            set { obj = value; }
        }

        public override Value ToStack()
        {
            Value result = null;

            if (isPrimitive)
            {
                object res = null;
                DataModelUtils.ToStack(obj,(int)getTypeIndex(),out res);
                result = new StructValue(res as ValueType);
            }
            else
            {
                if (obj.GetType().IsEnum)
                    result = new StructValue(getEnumValue(obj));
                else
                    result = MakeCopy();
            }

            return result;
        }

        public override Value FromStack(Type targetType)
        {
            object result = obj;

            if (isPrimitive)
            {
                TypeIndex resIndex = 
                    getTypeIndex((targetType.IsEnum) ? getEnumType(targetType) : targetType);

                DataModelUtils.FromStack(obj,(int)getTypeIndex(),(int)resIndex,out result);

                if (targetType.IsEnum)
                    result = newEnum(targetType,result);
            }

            return new StructValue(result as ValueType);
        }

        public override Value MakeCopy()
        {
            Array arr = Array.CreateInstance(type, 1);
            arr.SetValue(obj,0);
            return new StructValue(arr.GetValue(0) as ValueType);
        }

        public override object ToParameter () { return obj; }

        public override bool Equals(object obj)
        {
            bool flag = false;
            if (obj is StructValue)
                flag = Equals(this.obj, (obj as StructValue).obj);

            return flag;
        }

        public override int GetHashCode()
        {
            return this.obj.GetHashCode();
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return String.Format(formatProvider,"{0:"+format+"}",Obj.GetType())
                + (IsPrimitive ? " ("+obj+")" : "");
        }
    }

    public abstract class ReferenceValue : Value
    {
    }

    public class NullValue : ReferenceValue
    {
        public override Type Type { get { return null; } }

        public override object ToParameter ()
        {
            return null;
        }

        public override Value MakeCopy() { return new NullValue(); }

        public override bool Equals (object obj)
        {
            return obj is NullValue;
        }

        public override int GetHashCode ()
        {
            return "NullValue".GetHashCode();
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return "NULL";
        }
    }

    public class ObjectReferenceValue : ReferenceValue
    {
        #region Private and internal members

        private object obj;

        #endregion

        public ObjectReferenceValue (object obj)
        {
            this.obj = obj;
        }

        public override Type Type { get { return obj.GetType(); } }

        public object Obj { get { return obj; } }

        public override object ToParameter () { return obj; }

        public override Value MakeCopy() { return new ObjectReferenceValue(obj); }

        public override bool Equals (object obj)
        {
            bool flag = false;
            if (obj is ObjectReferenceValue)
                flag = this.obj == (obj as ObjectReferenceValue).obj;

            return flag;
        }

        public override int GetHashCode ()
        {
            return DataModelUtils.GetObjectHashCode(this.obj);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            string result = String.Format(formatProvider,"{0:"+format+"}",Type);
            
            if (Type.IsValueType)
            {
                result = "boxed "+result;

                if (Type.IsPrimitive)
                    result += " "+Obj;
            }

            if (Type == typeof(string))
                result += " (\"" + Obj + "\")";

            return result;
        }
    }

	public abstract class PointerValue : ReferenceValue
	{
		protected Type makePointerType(Type type)
		{
			return type.Module.GetType(type.ToString()+"&");
		}

		public abstract object GetReferencedObject();

		public abstract void SetReferencedValue(Value val);

		public void SetZeroValue()
		{
			Type type = Type.GetElementType();

            SetReferencedValue(
				type.IsValueType ?
				new StructValue(FormatterServices.GetUninitializedObject(type) as ValueType) as Value :
				new NullValue() as Value
				);
	    }

        public abstract object GetHeapObject();

        public abstract int GetQuasiOffset();

        public virtual bool IsGreaterThan(ReferenceValue obj)
        {
            return obj is NullValue;
        }

        public virtual bool IsLessThan (ReferenceValue obj)
        {
            return false;
        }

        protected static int QuasiSize(Type type)
        {
            if(type.IsPrimitive)
                return(1);
            if(! type.IsValueType)
                return(1);
            //UD value-type
            FieldInfo[] fields = ReflectionUtils.GetAllFields(type);
            int s=0;
            for(int i=0; i<fields.Length; i++)
                s+= QuasiSize(fields[i].FieldType); 
            return(s+1); //+1 in order to distinguish between a pointer to a structure and a pointer to its first field
        }

        protected static int QuasiOffset(Type type, FieldInfo field) //the field should directly belong to the type
        {
            FieldInfo[] fields = ReflectionUtils.GetAllFields(type);
            int s=0;
            int i=0;
            for(; fields[i]!=field; i++) //may throw OutOfRangeException if the field is not found
                s+= QuasiSize(fields[i].FieldType); 
            return(s+1); //+1 in order to distinguish between a pointer to a structure and a pointer to its first field
        }
    }

    public class PointerToLocationValue : PointerValue
    {
        #region Private and internal members

        private Location loc;

        #endregion

        public PointerToLocationValue (Location loc)
        {
            this.loc = loc;
        }

        public override Type Type { get { return makePointerType(loc.Type); } }

        public override object GetReferencedObject()
        {
            object result = null;
            Value val = loc.Val;

            if (val is StructValue)
                result = (val as StructValue).Obj;
            else if (val is ObjectReferenceValue)
                result = (val as ObjectReferenceValue).Obj;
            else if (val is NullValue)
                result = null;

            return result;
        }

        public override void SetReferencedValue(Value val) { loc.Val = val; }

        public override object GetHeapObject()
        {
            return this.loc;
        }

        public override int GetQuasiOffset()
        {
            return(0);
        }

        public override object ToParameter ()
        {
            return loc.Val.ToParameter();
        }

        public override Value MakeCopy() 
        { 
            return new PointerToLocationValue(loc); 
        }

        public override bool Equals (object obj)
        {
            bool flag = false;
            if (obj is PointerToLocationValue)
                flag = this.loc == (obj as PointerToLocationValue).loc;

            return flag;
        }

        public override int GetHashCode ()
        {
            return DataModelUtils.GetObjectHashCode(this.loc);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return "Pointer to location";
        }
    }

    public class PointerToObjectFieldValue: PointerValue
    {
        #region Private and internal members

        private object obj;
        private FieldInfo field;

        #endregion

        public PointerToObjectFieldValue(object obj, FieldInfo field)
        {
            this.obj = obj;
            this.field = field;
        }

        public PointerToObjectFieldValue(FieldInfo field)
        {
            obj = null;
            this.field = field;
        }

        public override Value MakeCopy() 
        { 
            return new PointerToObjectFieldValue(obj,field); 
        }

        public override int GetHashCode()
        {
            return(DataModelUtils.GetObjectHashCode(obj) ^ field.GetHashCode());
        }

        public override bool Equals(object o)
        {
            if(GetType() != o.GetType())
                return(false);
            PointerToObjectFieldValue ptr = o as PointerToObjectFieldValue;
            return(obj == ptr.obj  &&  field.Equals(ptr.field));
        }

        public override Type Type { get { return makePointerType(field.FieldType); } }

        public override object GetReferencedObject()
        {
            return field.GetValue(obj);
        }

        public override void SetReferencedValue(Value val)
        {
            val = val.FromStack(field.FieldType);

            object valObj = null;
            if (val is StructValue)
                valObj = (val as StructValue).Obj;
            else if (val is ObjectReferenceValue)
                valObj = (val as ObjectReferenceValue).Obj;
            else if (val is NullValue)
                valObj = null;

            field.SetValue(obj,valObj);
        }

        public override object GetHeapObject()
        {
            return(obj);
        }

        public override int GetQuasiOffset()
        {
            return( QuasiOffset(obj.GetType(),field) );
        }

        public override object ToParameter ()
        {
            return field.GetValue(obj);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return String.Format(formatProvider,"Pointer to field {0:"+format+"}",
                obj.GetType());
        }
    }

    public class PointerToStructFieldValue: PointerValue
    {
        #region Private and internal members

        private PointerValue ptr;
        private FieldInfo field;

        #endregion

        public PointerToStructFieldValue(PointerValue ptr, FieldInfo field)
        {
            this.ptr = ptr;
            this.field = field;
        }

        public override Value MakeCopy()
        { 
            return new PointerToStructFieldValue(ptr.MakeCopy() as PointerValue,field); 
        }

        public override int GetHashCode()
        {
            return ptr.GetHashCode() ^ field.GetHashCode();
        }

        public override bool Equals(object o)
        {
            bool result = GetType() == o.GetType();
            
            if (result)
            {
                PointerToStructFieldValue p = o as PointerToStructFieldValue;
                result = ptr.Equals(p.ptr) && field.Equals(p.field);
            }
            
            return result;
        }

        public override Type Type { get { return makePointerType(field.FieldType); } }

        public override object GetReferencedObject()
        {
            return field.GetValue(ptr.GetReferencedObject());
        }

        public override void SetReferencedValue(Value val)
        {
            object obj = ptr.GetReferencedObject();
            val = val.FromStack(field.FieldType);

            object valObj = null;
            if (val is StructValue)
                valObj = (val as StructValue).Obj;
            else if (val is ObjectReferenceValue)
                valObj = (val as ObjectReferenceValue).Obj;
            else if (val is NullValue)
                valObj = null;

            field.SetValue(obj,valObj);
            ptr.SetReferencedValue(new StructValue(obj as ValueType));
        }

        public override object GetHeapObject()
        {
            return(ptr.GetHeapObject());
        }
        
        public override int GetQuasiOffset()
        {
            return(ptr.GetQuasiOffset() + QuasiOffset(ptr.Type.GetElementType() ,field));
        }

        public override object ToParameter ()
        {
            return field.GetValue(ptr.GetReferencedObject());
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return String.Format(formatProvider,"Pointer to field of struct {0:"+format+"}",
                ptr.GetReferencedObject().GetType());
        }
    }

    public class PointerToElementValue: PointerValue
    {
        #region Private and internal members

        private Array arr;
        private Type type;
        private int index;

        #endregion

        public PointerToElementValue (Array arr, int index)
        {
            this.arr = arr;
            type = arr.GetType().GetElementType();
            this.index = index;
        }

        public override Value MakeCopy()
        { 
            return new PointerToElementValue(arr,index); 
        }

        public override int GetHashCode()
        {
            return(DataModelUtils.GetObjectHashCode(arr) ^ index);
        }

        public override bool Equals(object o)
        {
            if(GetType() != o.GetType())
                return(false);
            PointerToElementValue ptr = o as PointerToElementValue;
            return(arr == ptr.arr  &&  index == ptr.index);
        }

        public override Type Type { get { return makePointerType(type); } }

        public override object GetReferencedObject ()
        {
            return arr.GetValue(index);
        }

        public override void SetReferencedValue(Value val)
        {
            val = val.FromStack(type);

            object valObj = null;
            if (val is StructValue)
                valObj = (val as StructValue).Obj;
            else if (val is ObjectReferenceValue)
                valObj = (val as ObjectReferenceValue).Obj;
            else if (val is NullValue)
                valObj = null;

            arr.SetValue(valObj,index);
        }

        public override object GetHeapObject()
        {
            return(arr);
        }

        public override int GetQuasiOffset()
        {
            return( QuasiSize(arr.GetType().GetElementType()) * index );
        }

        public override bool IsGreaterThan (ReferenceValue obj)
        {
            bool result;
            if (obj is PointerToElementValue)
            {
                PointerToElementValue ptr = obj as PointerToElementValue;
                result = arr == ptr.arr && index > ptr.index;
            }
            else
                result = obj is NullValue;

            return result;
        }

        public override bool IsLessThan (ReferenceValue obj)
        {
            bool result;
            if (obj is PointerToElementValue)
            {
                PointerToElementValue ptr = obj as PointerToElementValue;
                result = arr == ptr.arr && index < ptr.index;
            }
            else
                result = false;

            return result;
        }

        public override object ToParameter ()
        {
            return arr.GetValue(index);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return "Pointer to element " + index;
        }
    }

    public class PointerToUnboxedValue: PointerValue
    {
        #region Private and internal members

        private object obj;

        #endregion

        public PointerToUnboxedValue(object obj) { this.obj = obj; }

        public override Value MakeCopy()
        { 
            return new PointerToUnboxedValue(obj); 
        }

        public override int GetHashCode()
        {
            return(DataModelUtils.GetObjectHashCode(obj));
        }

        public override bool Equals(object o)
        {
            if(GetType() != o.GetType())
                return(false);
            PointerToUnboxedValue ptr = o as PointerToUnboxedValue;
            return(obj == ptr.obj);
        }

        public override Type Type { get { return makePointerType(obj.GetType()); } }

        public override object GetReferencedObject()
        {
            return obj;
        }

        public override void SetReferencedValue(Value val)
        {
            Type t = obj.GetType();
            StructValue structVal = val.FromStack(t) as StructValue;

            if (structVal.IsPrimitive)
                DataModelUtils.StoreToBox(obj,(int)(StructValue.getTypeIndex(t)),structVal.Obj);
            else
            {
                FieldInfo[] fields = obj.GetType().GetFields((BindingFlags)
                    (BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    );

                foreach (FieldInfo field in fields)
                    field.SetValue(obj,field.GetValue(structVal.Obj));
            }
        }

        public override object GetHeapObject()
        {
            return(obj);
        }

        public override int GetQuasiOffset()
        {
            return(0);
        }

        public override object ToParameter ()
        {
            return obj;
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return "Pointer to unboxed value";
        }
    }
}
