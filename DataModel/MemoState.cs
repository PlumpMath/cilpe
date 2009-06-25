using System;
using System.Collections;
using System.Reflection;
using System.Runtime.Serialization; //GetUninitializedObject

using CILPE.CFG;


namespace CILPE.DataModel
{
    
    public class ReflectionUtils
    {
        static public FieldInfo[] GetAllFields(Type type)
        {
            ArrayList fields = new ArrayList();
            for(int i=0;i<1000;i++)
            {
                if(type == null)
                    break;
                FieldInfo[] flds = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                fields.AddRange(flds);
                type = type.BaseType;
            }
            if(type != null)
                throw new Exception();
            FieldInfo[] result = new FieldInfo[fields.Count];
            fields.CopyTo(result);
            return(result);
        }
    }


	public sealed class MemoState
	{
		public class MemoException : Exception
		{
			public MemoException()
			{
				//breakpoint
			}
		};

		private struct MemoObject
		{
			public Type type; // == null for "null"
			public int[] fields; // == null for primitive types; == new int[0] for types without fields
			public long primitiveIntValue; // == 0 if not used
			public double primitiveDoubleValue; // == 0 if not used
			public int hashCode;

			private int Length(Array array)
			{
				return(array == null ? 0 : array.Length);
			}

			private int FieldsLength()
			{
				return(Length(fields));
			}

			private void CalculateHashCode()
			{
				hashCode = type == null ? 0 : type.GetHashCode();
				for(int i=0; i<FieldsLength(); i++)
				{
					hashCode^= fields[i];
					hashCode = (hashCode >> 1) + (hashCode << 31);
				}
				hashCode ^= primitiveIntValue.GetHashCode();
				hashCode ^= primitiveDoubleValue.GetHashCode();
			}


			public MemoObject(Type type, int[] fields)
			{
				this.type = type;
				this.fields = fields;
				primitiveIntValue = 0;
				primitiveDoubleValue = 0.0;
				hashCode = 0;
				CalculateHashCode();
			}

			public MemoObject(Type type, long x)
			{
				this.type = type;
				this.fields = null;
				primitiveIntValue = x;
				primitiveDoubleValue = 0.0;
				hashCode = 0;
				CalculateHashCode();
			}

			public MemoObject(Type type, double x)
			{
				this.type = type;
				this.fields = null;
				primitiveIntValue = 0;
				primitiveDoubleValue = x;
				hashCode = 0;
				CalculateHashCode();
			}
  
			public override int GetHashCode()
			{
				return(hashCode); 
			}

			public override bool Equals(object obj)
			{
				return(obj is MemoObject ? Equals((MemoObject)obj) : false);
			}

			public bool Equals(MemoObject obj)
			{
				if(hashCode != obj.hashCode)
					return(false);
				if(type != obj.type)
					return(false);
				if(FieldsLength() != obj.FieldsLength())
					return(false);
				int length = FieldsLength();
				for(int i=0; i<length; i++)
				{
					if(fields[i] != obj.fields[i])
						return(false);
				}
				if(primitiveIntValue != obj.primitiveIntValue)
					return(false);
				if(primitiveDoubleValue != obj.primitiveDoubleValue)
					return(false);
				return(true);
			}

		}

		private MemoObject[] memo;
		private int hashCode;

		private static void SetElement(ArrayList list, int i, object obj)
		{
			for( ;list.Count <= i; )
				list.Add(null);
			list[i] = obj;
		}

		private static int Memo(object obj, ArrayList heap, ObjectHashtable hash, bool addToHash)
		{
			int myIndex = heap.Count;
			heap.Add(null); //Reserve some space for this object
			if(addToHash) //addToHash == false, when obj is a boxed representation of some non-boxed value 
			    hash[obj] = myIndex;
			Type type = obj.GetType();
			if(typeof(Pointer).IsAssignableFrom(type))
				throw new MemoException(); //some mannaged wrapper for unmannaged data...
			if(type.IsPrimitive)
			{
				switch(Type.GetTypeCode(type))
				{ 
					case TypeCode.Boolean:
					case TypeCode.Byte:
					case TypeCode.SByte:
					case TypeCode.Char:
					case TypeCode.Int16:
					case TypeCode.UInt16:
					case TypeCode.Int32:
					case TypeCode.UInt32:
					case TypeCode.Int64:
					case TypeCode.UInt64:
						SetElement(heap , myIndex , new MemoObject(type, (obj as IConvertible).ToInt64(null)));
						return(myIndex); 
					case TypeCode.Single:
					case TypeCode.Double:
						SetElement(heap , myIndex , new MemoObject(type, (obj as IConvertible).ToDouble(null)));
						return(myIndex);
					default: //IntPtr? UIntPtr?
						if(type.Equals(typeof(IntPtr)))
						{
							SetElement(heap , myIndex , new MemoObject(type, ((IntPtr)obj).ToInt64()));
							return(myIndex);
						}
						else if(type.Equals(typeof(UIntPtr)))
						{
							SetElement(heap , myIndex , new MemoObject(type, (long)((UIntPtr)obj).ToUInt64()));
							return(myIndex);
						}
						else
							throw new MemoException();
				}//switch
			}//if(IsPrimitive)
			int[] fieldIndices;
			if(Equals(type,typeof(string)))
			{
				string str = obj as string;
				fieldIndices = new int[str.Length];
				for(int i=0; i<str.Length; i++)
				{
					char fieldValue = str[i];
					fieldIndices[i] = Memo(fieldValue, heap, hash, false);
				}
			}
			else if(type.IsArray)
			{
				Array array = obj as Array;
				if(array.Rank > 1 || array.GetLowerBound(0) != 0)
					throw new MemoException(); //not supported yet...
				fieldIndices = new int[array.Length];
				bool doAddToHash = ! type.GetElementType().IsValueType;
				for(int i=0; i<array.Length; i++)
				{
					object fieldValue = array.GetValue(i);
					if(doAddToHash)
					{
						object fieldIndexBoxed = hash[fieldValue];
						if(fieldIndexBoxed != null)
						{
							fieldIndices[i] = (int)fieldIndexBoxed;
							continue;
						}
					}
					fieldIndices[i] = Memo(fieldValue, heap, hash, doAddToHash);																				
				}
			}
			else
			{
				//Custom object
				FieldInfo[] fieldInfos = ReflectionUtils.GetAllFields(type);
				fieldIndices = new int[fieldInfos.Length];
				for(int i=0; i<fieldInfos.Length; i++)
				{
					bool doAddToHash = ! fieldInfos[i].FieldType.IsValueType;
					object fieldValue = fieldInfos[i].GetValue(obj);
					if(doAddToHash)
					{
						object fieldIndexBoxed = hash[fieldValue];
						if(fieldIndexBoxed != null)
						{
							fieldIndices[i] = (int)fieldIndexBoxed;
							continue;
						}
					}
					fieldIndices[i] = Memo(fieldValue, heap, hash, doAddToHash);
				}
			}
			SetElement(heap , myIndex , new MemoObject(type, fieldIndices));
			return(myIndex);
		}

		private void RecallObject(object obj, int i, object[] objs)
		{
			FieldInfo[] fieldInfos = ReflectionUtils.GetAllFields(memo[i].type);
			if(fieldInfos.Length != memo[i].fields.Length)
				throw new MemoException();
			for(int f=0; f<fieldInfos.Length; f++)
			{
				int j = memo[i].fields[f];
				fieldInfos[f].SetValue(obj, objs[j] != null ? objs[j] : CreateStruct(j,objs));
			}
		}

		private void RecallArray(Array obj, int i, object[] objs)
		{
			for(int f=0; f<obj.Length; f++)
			{
				int j = memo[i].fields[f];
				obj.SetValue( objs[j] != null ? objs[j] : CreateStruct(j,objs) ,f);  
			}
		}

		private object RecallPrimitive(int i)
		{
			Type type = memo[i].type;
			switch(Type.GetTypeCode(type))
			{ 
				case TypeCode.Boolean:
					return(memo[i].primitiveIntValue != 0);
				case TypeCode.Byte:
					return((byte)memo[i].primitiveIntValue);
				case TypeCode.SByte:
					return((sbyte)memo[i].primitiveIntValue);
				case TypeCode.Char:
					return((char)memo[i].primitiveIntValue);
				case TypeCode.Int16:
					return((short)memo[i].primitiveIntValue);
				case TypeCode.UInt16:
					return((ushort)memo[i].primitiveIntValue);
				case TypeCode.Int32:
					return((int)memo[i].primitiveIntValue);
				case TypeCode.UInt32:
					return((uint)memo[i].primitiveIntValue);
				case TypeCode.Int64:
					return((long)memo[i].primitiveIntValue);
				case TypeCode.UInt64:
					return((ulong)memo[i].primitiveIntValue);
				case TypeCode.Single:
					return((float)memo[i].primitiveDoubleValue);
				case TypeCode.Double:
					return((double)memo[i].primitiveDoubleValue);
				default: //IntPtr? UIntPtr?
					if(type.Equals(typeof(IntPtr)))
						return(new IntPtr(memo[i].primitiveIntValue));
					else if(type.Equals(typeof(UIntPtr)))
						return(new UIntPtr((ulong)(memo[i].primitiveIntValue)));
					else
						throw new MemoException();
			}
    	}

		private object CreateStruct(int i, object[] objs)
		{
			if(objs[i] != null)
				throw new MemoException();
			Type type = memo[i].type;
			if(type == null)
				return(null);
			if(type.IsPrimitive) 
				return(RecallPrimitive(i));

			object obj = FormatterServices.GetUninitializedObject(memo[i].type);
			RecallObject(obj, i, objs);
			return(obj);

		}

		private void CalculateHashCode()
		{
			hashCode = 0;
			for(int i=0; i<memo.Length; i++)
			{
				hashCode ^= memo[i].GetHashCode();
				hashCode = (hashCode >> 1) | (hashCode << 31);
			}
		}

		private void Ctor(object obj, ObjectHashtable hash)
		{
			if(hash.Count != 0)
				throw new MemoException(); //XZ??
			ArrayList heap = new ArrayList();
			heap.Add(new MemoObject(null,null));
			hash[null] = 0;
			Memo(obj, heap, hash, true); 
			memo = new MemoObject[heap.Count];
			heap.CopyTo(memo);
			CalculateHashCode();
		}


		public MemoState(object obj)
		{
			Ctor(obj, new ObjectHashtable());
		}

		public MemoState(object obj, ObjectHashtable hash)
		{
			Ctor(obj, hash);
		}

		public override bool Equals(object stateObj)
		{
			MemoState state = stateObj as MemoState;
			if(state == null)
				return(false);
			if(GetHashCode() != state.GetHashCode())
				return(false);
			if(this == state)
				return(true);
			if(memo.Length != state.memo.Length)
				return(false);
			int length = memo.Length;
			for(int i=0; i<length; i++)
			{
				if(!memo[i].Equals(state.memo[i]))
					return(false);
			}
			return(true);
		}

		public override int GetHashCode()
		{
			return(hashCode);
		}

		public object Recall(ObjectHashtable hash)
		{
			//hash is an (object -> int) mapping, the same that was used while memoizing
			object[] objs = new object[memo.Length];
			foreach(object obj in hash.Keys)
				objs[(int)hash[obj]] = obj;
			for(int i=0; i<memo.Length; i++)
			{
			    object obj = objs[i];
				if(obj == null)
					continue;
				Type type = memo[i].type;
				if(!Equals(type, obj.GetType()))
					throw new MemoException();
				if(Equals(type,typeof(string)))
					continue;//strings are immutable...
				if(type.IsPrimitive)
				{
					//DmUtils call?...
				}
				else if(type.IsArray)
					RecallArray(obj as Array, i, objs);
				else 
					RecallObject(obj, i, objs);
			}

            return objs[1];


		}

	}

}
