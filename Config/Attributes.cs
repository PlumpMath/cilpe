
// ===========================================================================
// CILPE - Partial Evaluator for Common Intermediate Language
// ===========================================================================
// File: 
//     Attributes.cs
//
// Description:
//     Custom attributes for BTA and specializer 
//
// Author: 
//     Sergei Skorobogatov (Sergei.Skorobogatov@supercompilers.com)
// ===========================================================================


using System;

namespace CILPE.Config
{
    using System.Reflection;

    [AttributeUsage(
         (AttributeTargets)(
             AttributeTargets.Method | 
             AttributeTargets.Constructor
         ),
         AllowMultiple = false
     )]
    public class SpecializeAttribute: Attribute
    {
        public SpecializeAttribute() {  }
    }

	[AttributeUsage(
		 (AttributeTargets)(
		 AttributeTargets.Method | 
		 AttributeTargets.Constructor
		 ),
		 AllowMultiple = false
		 )]
	public class InlineAttribute: Attribute
	{
		public InlineAttribute() {  }
	}
}
