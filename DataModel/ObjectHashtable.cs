using System;
using System.Collections;

namespace CILPE.DataModel
{
	/// <summary>
	/// Summary description for ObjectHashtable.
	/// </summary>
	 
	public class ObjectHashtable : Hashtable
	{
		#region private members


		private class HashCodeProvider : IHashCodeProvider
		{
			public int GetHashCode(object obj){ return(DataModelUtils.GetObjectHashCode(obj)); }
		}

		private class Comparer : IComparer
		{
			public int Compare(object x, object y){ return(x == y ? 0 : 1); }
		}

		object nullValue;

		#endregion

		public ObjectHashtable()
		: base(new HashCodeProvider(),new Comparer())
		{}

		public override object this[object key]
		{
			get
			{
				if(key == null) 
					return(nullValue);
				return(base[key]);
			}
			set
			{
				if(key == null) 
					nullValue = value;
				else
				    base[key] = value;
			}
		}

	};
}
