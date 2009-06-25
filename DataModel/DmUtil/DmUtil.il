
// ===========================================================================
// CILPE - Partial Evaluator for Common Intermediate Language
// ===========================================================================
// File: 
//     DmUtil.il
//
// Description:
//     Utility functions for DataModel API
//
// Author: 
//     Sergei Skorobogatov (Sergei.Skorobogatov@supercompilers.com)
// ===========================================================================


.assembly extern mscorlib { }

.assembly DmUtil { }

.module DmUtil.dll

.namespace CILPE.DataModel
{
    .class public DataModelUtils
    {
        .field private static int32[] resultTable
        
        .method public static hidebysig int32 GetObjectHashCode(object o)
        {
            .maxstack  1
            ldarg.0
            call       instance int32 [mscorlib]System.Object::GetHashCode()
            ret
        }

        .method public static hidebysig void ToStack(object a, int32 typeIndex, [out] object& result)
        {
            .maxstack  2

            // result = null;
            ldarg.2
            ldnull
            stind.ref
            
            // switch (typeIndex)
            ldarg.1
            switch ( 
                CASE_INT32,
                CASE_INT64,
                CASE_NATIVEINT,
                CASE_FLOAT64,
                CASE_INT8,
                CASE_UINT8,
                CASE_INT16,
                CASE_UINT16,
                CASE_UINT32,
                CASE_UINT64,
                CASE_UNATIVEINT,
                CASE_FLOAT32,
                CASE_BOOL,
                CASE_CHAR
                )
            br         EXIT

            // case int32
            CASE_INT32:
            ldarg.2
            ldarg.0
            unbox      int32
            ldind.i4
            box        int32
            stind.ref
            br         EXIT

            // case int64
            CASE_INT64:  
            ldarg.2
            ldarg.0
            unbox      int64
            ldind.i8
            box        int64
            stind.ref
            br         EXIT

            // case native int
            CASE_NATIVEINT:  
            ldarg.2
            ldarg.0
            unbox      native int
            ldind.i
            box        native int
            stind.ref
            br         EXIT

            // case float64
            CASE_FLOAT64:  
            ldarg.2
            ldarg.0
            unbox      float64
            ldind.r8
            box        float64
            stind.ref
            br         EXIT

            // case int8
            CASE_INT8:  
            ldarg.2
            ldarg.0
            unbox      int8
            ldind.i1
            box        int32
            stind.ref
            br         EXIT

            // case unsigned int8
            CASE_UINT8:  
            ldarg.2
            ldarg.0
            unbox      unsigned int8
            ldind.u1
            box        int32
            stind.ref
            br         EXIT

            // case int16
            CASE_INT16:
            ldarg.2
            ldarg.0
            unbox      int16
            ldind.i2
            box        int32
            stind.ref
            br.s       EXIT

            // case unsigned int16
            CASE_UINT16:
            ldarg.2
            ldarg.0
            unbox      unsigned int16
            ldind.u2
            box        int32
            stind.ref
            br.s       EXIT

            // case unsigned int32
            CASE_UINT32:
            ldarg.2
            ldarg.0
            unbox      unsigned int32
            ldind.u4
            box        int32
            stind.ref
            br.s       EXIT

            // case unsigned int64
            CASE_UINT64:
            ldarg.2
            ldarg.0
            unbox      unsigned int64
            ldind.i8
            box        int64
            stind.ref
            br.s       EXIT

            // case native unsigned int
            CASE_UNATIVEINT:
            ldarg.2
            ldarg.0
            unbox      native unsigned int
            ldind.i
            box        native int
            stind.ref
            br.s       EXIT

            // case float32
            CASE_FLOAT32:
            ldarg.2
            ldarg.0
            unbox      float32
            ldind.r4
            box        float64
            stind.ref
            br.s       EXIT
            
            // case bool
            CASE_BOOL:
            ldarg.2
            ldarg.0
            unbox      bool
            ldobj      bool
            box        int32
            stind.ref
            br.s       EXIT
            
            // case char
            CASE_CHAR:
            ldarg.2
            ldarg.0
            unbox      char
            ldobj      char
            box        int32
            stind.ref

            EXIT:
            ret
        } // end of method DataModelUtils::ToStack
        
        .method public hidebysig static void FromStack(object a, int32 typeIndex, int32 resultIndex, [out] object& result)
        {
            .maxstack  2
            
            // result = a;
            ldarg.3
            ldarg.0
            stind.ref
            
            // if (typeIndex == 0 /* int32 */)
            ldarg.1
            brtrue     CHECK_INT64
            {
                // if (resultIndex == 4 /* int8 */)
                ldarg.2
                ldc.i4.4
                bne.un.s   CHECKRES_UINT8
                {
                    ldarg.3
                    ldarg.0
                    unbox      int32
                    ldind.i4
                    box        int8
                    stind.ref
                    br         EXIT
                }
                
                // if (resultIndex == 5 /* unsigned int8 */)
                CHECKRES_UINT8:
                ldarg.2
                ldc.i4.5
                bne.un.s   CHECKRES_INT16
                {
                    ldarg.3
                    ldarg.0
                    unbox      int32
                    ldind.i4
                    box        unsigned int8
                    stind.ref
                    br         EXIT
                }
                
                // if (resultIndex == 6 /* int16 */)
                CHECKRES_INT16:
                ldarg.2
                ldc.i4.6
                bne.un.s   CHECKRES_UINT16
                {
                    ldarg.3
                    ldarg.0
                    unbox      int32
                    ldind.i4
                    box        int16
                    stind.ref
                    br         EXIT
                }
                
                // if (resultIndex == 7 /* unsigned int16 */)
                CHECKRES_UINT16:
                ldarg.2
                ldc.i4.7
                bne.un.s   CHECKRES_UINT32
                {
                    ldarg.3
                    ldarg.0
                    unbox      int32
                    ldind.i4
                    box        unsigned int16
                    stind.ref
                    br         EXIT
                }
                
                // if (resultIndex == 8 /* unsigned int32 */)
                CHECKRES_UINT32:
                ldarg.2
                ldc.i4.8
                bne.un.s   CHECKRES_BOOL
                {
                    ldarg.3
                    ldarg.0
                    unbox      int32
                    ldind.i4
                    box        unsigned int32
                    stind.ref
                    br         EXIT
                }
                
                // if (resultIndex == 12 /* bool */)
                CHECKRES_BOOL:
                ldarg.2
                ldc.i4     12
                bne.un.s   CHECKRES_CHAR
                {
                    ldarg.3
                    ldarg.0
                    unbox      int32
                    ldind.i4
                    box        bool
                    stind.ref
                    br         EXIT
                }
                
                // if (resultIndex == 13 /* char */)
                CHECKRES_CHAR:
                ldarg.2
                ldc.i4     13
                bne.un.s   EXIT
                {
                    ldarg.3
                    ldarg.0
                    unbox      int32
                    ldind.i4
                    box        char
                    stind.ref
                    br         EXIT
                }
            }
            
            // if (typeIndex == 1 /* int64 */ && resultIndex == 9 /* unsigned int64 */)
            CHECK_INT64:
            ldarg.1
            ldc.i4.1
            bne.un.s   CHECK_NATIVEINT
            ldarg.2
            ldc.i4.s   9
            bne.un.s   CHECK_NATIVEINT
            {
                ldarg.3
                ldarg.0
                unbox      int64
                ldind.i8
                box        unsigned int64
                stind.ref
                br         EXIT
            }
            
            // if (typeIndex == 2 /* native int */ && resultIndex == 10 /* native unsigned int */)
            CHECK_NATIVEINT:
            ldarg.1
            ldc.i4.2
            bne.un.s   CHECK_FLOAT64
            ldarg.2
            ldc.i4.s   10
            bne.un.s   CHECK_FLOAT64
            {
                ldarg.3
                ldarg.0
                unbox      native int
                ldind.i
                box        native unsigned int
                stind.ref
                br         EXIT
            }
            
            // if (typeIndex == 3 /* float64 */ && resultIndex == 11 /* float32 */)
            CHECK_FLOAT64:
            ldarg.1
            ldc.i4.3
            bne.un.s   EXIT
            ldarg.2
            ldc.i4.s   11
            bne.un.s   EXIT
            {
                ldarg.3
                ldarg.0
                unbox      float64
                ldind.r8
                box        float32
                stind.ref
            }
            
            EXIT:
            ret
        } // end of method DataModelUtils::FromStack

        .method public hidebysig static bool Unary(
            int32 op, object obj, int32 typeIndex,
            [out] class [mscorlib]System.ValueType& result
            )
        {
            .maxstack  2
            
            // result = null;
            ldarg.3
            ldnull
            stind.ref

            // if (op == NEG)
            ldarg.0
            brtrue.s   PROCESS_NOT

            // switch (typeIndex)
            ldarg.2
            switch ( 
                CASE_INT32,
                CASE_INT64,
                CASE_NATIVEINT,
                CASE_FLOAT64
                )
            br         ERROR

            // case int32
            CASE_INT32:
            ldarg.3
            ldarg.1
            unbox      int32
            ldind.i4
            neg
            box        int32
            stind.ref
            br.s       SUCCESS

            // case int64
            CASE_INT64:
            ldarg.3
            ldarg.1
            unbox      int64
            ldind.i8
            neg
            box        int64
            stind.ref
            br.s       SUCCESS

            // case native int
            CASE_NATIVEINT:
            ldarg.3
            ldarg.1
            unbox      native int
            ldind.i
            neg
            box        native int
            stind.ref
            br.s       SUCCESS

            // case float64
            CASE_FLOAT64:
            ldarg.3
            ldarg.1
            unbox      float64
            ldind.r8
            neg
            box        float64
            stind.ref
            br.s       SUCCESS
        
            // if (op == NOT)
            PROCESS_NOT:
            
            // switch (typeIndex)
            ldarg.2
            switch ( 
                CASE_INT32_2,
                CASE_INT64_2,
                CASE_NATIVEINT_2
                )
            br.s       ERROR

            // case int32
            CASE_INT32_2:
            ldarg.3
            ldarg.1
            unbox      int32
            ldind.i4
            not
            box        int32
            stind.ref
            br.s       SUCCESS

            // case int64
            CASE_INT64_2:
            ldarg.3
            ldarg.1
            unbox      int64
            ldind.i8
            not
            box        int64
            stind.ref
            br.s       SUCCESS

            // case native int
            CASE_NATIVEINT_2:
            ldarg.3
            ldarg.1
            unbox      native int
            ldind.i
            not
            box        native int
            stind.ref
            br.s       SUCCESS
            
            // Return false, if error
            ERROR:  
            ldc.i4.0
            br.s      EXIT
            
            // Return true, if success
            SUCCESS:
            ldc.i4.1
            
            EXIT:
            ret
        } // end of method DataModelUtils::Unary

        .method public hidebysig static bool NumericOp(
            int32 op, object a, object b, int32 operandsKind,
            [out] object& result
            )
        {
            .maxstack  6
            .locals init (
                [0] bool utilResult,
                [1] class [mscorlib]System.Exception e
                )
            
            // utilResult = true;        
            ldc.i4.1
            stloc.0
            
            // result = null;
            ldarg.s    result
            ldnull
            stind.ref
            
            .try
            {
                // switch (operandsKind)
                ldarg.3
                switch ( 
                    CASE_INT32_INT32,
                    CASE_INT64_INT64,
                    CASE_NATIVEINT_NATIVEINT,
                    CASE_FLOAT64_FLOAT64,
                    CASE_INT32_NATIVEINT,
                    CASE_NATIVEINT_INT32
                    )
                br         LEAVE_BLOCK
                
                // case int32, int32
                CASE_INT32_INT32:
                {
                    // switch (op)  
                    ldarg.0
                    ldc.i4.0
                    beq.s      CASE_ADD_0
                    ldarg.0
                    ldc.i4.5
                    sub
                    switch ( 
                        CASE_DIV_0,
                        CASE_MUL_0,
                        LEAVE_BLOCK,
                        CASE_REM_0,
                        LEAVE_BLOCK,
                        LEAVE_BLOCK,
                        CASE_SUB_0
                        )
                    br         LEAVE_BLOCK
                    
                    // case add
                    CASE_ADD_0:
                    ldarg.s    result
                    ldarg.1
                    unbox      int32
                    ldind.i4
                    ldarg.2
                    unbox      int32
                    ldind.i4
                    add
                    box        int32
                    stind.ref
                    br         LEAVE_BLOCK
                    
                    // case div
                    CASE_DIV_0:
                    ldarg.s    result
                    ldarg.1
                    unbox      int32
                    ldind.i4
                    ldarg.2
                    unbox      int32
                    ldind.i4
                    div
                    box        int32
                    stind.ref
                    br         LEAVE_BLOCK
                    
                    // case mul
                    CASE_MUL_0:
                    ldarg.s    result
                    ldarg.1
                    unbox      int32
                    ldind.i4
                    ldarg.2
                    unbox      int32
                    ldind.i4
                    mul
                    box        int32
                    stind.ref
                    br         LEAVE_BLOCK
                    
                    // case rem
                    CASE_REM_0:
                    ldarg.s    result
                    ldarg.1
                    unbox      int32
                    ldind.i4
                    ldarg.2
                    unbox      int32
                    ldind.i4
                    rem
                    box        int32
                    stind.ref
                    br         LEAVE_BLOCK
                    
                    // case sub
                    CASE_SUB_0:
                    ldarg.s    result
                    ldarg.1
                    unbox      int32
                    ldind.i4
                    ldarg.2
                    unbox      int32
                    ldind.i4
                    sub
                    box        int32
                    stind.ref
                    br         LEAVE_BLOCK
                }
                
                // case int64, int64
                CASE_INT64_INT64:
                {
                    // switch (op)  
                    ldarg.0
                    ldc.i4.0
                    beq.s      CASE_ADD_1
                    ldarg.0
                    ldc.i4.5
                    sub
                    switch ( 
                        CASE_DIV_1,
                        CASE_MUL_1,
                        LEAVE_BLOCK,
                        CASE_REM_1,
                        LEAVE_BLOCK,
                        LEAVE_BLOCK,
                        CASE_SUB_1
                        )
                    br         LEAVE_BLOCK
                    
                    // case add
                    CASE_ADD_1:
                    ldarg.s    result
                    ldarg.1
                    unbox      int64
                    ldind.i8
                    ldarg.2
                    unbox      int64
                    ldind.i8
                    add
                    box        int64
                    stind.ref
                    br         LEAVE_BLOCK
                    
                    // case div
                    CASE_DIV_1:
                    ldarg.s    result
                    ldarg.1
                    unbox      int64
                    ldind.i8
                    ldarg.2
                    unbox      int64
                    ldind.i8
                    div
                    box        int64
                    stind.ref
                    br         LEAVE_BLOCK
                    
                    // case mul
                    CASE_MUL_1:
                    ldarg.s    result
                    ldarg.1
                    unbox      int64
                    ldind.i8
                    ldarg.2
                    unbox      int64
                    ldind.i8
                    mul
                    box        int64
                    stind.ref
                    br         LEAVE_BLOCK
                    
                    // case rem
                    CASE_REM_1:
                    ldarg.s    result
                    ldarg.1
                    unbox      int64
                    ldind.i8
                    ldarg.2
                    unbox      int64
                    ldind.i8
                    rem
                    box        int64
                    stind.ref
                    br         LEAVE_BLOCK
                    
                    // case sub
                    CASE_SUB_1:
                    ldarg.s    result
                    ldarg.1
                    unbox      int64
                    ldind.i8
                    ldarg.2
                    unbox      int64
                    ldind.i8
                    sub
                    box        int64
                    stind.ref
                    br         LEAVE_BLOCK
                }
                
                // case native int, native int
                CASE_NATIVEINT_NATIVEINT:
                {
                    // switch (op)  
                    ldarg.0
                    ldc.i4.0
                    beq.s      CASE_ADD_2
                    ldarg.0
                    ldc.i4.5
                    sub
                    switch ( 
                        CASE_DIV_2,
                        CASE_MUL_2,
                        LEAVE_BLOCK,
                        CASE_REM_2,
                        LEAVE_BLOCK,
                        LEAVE_BLOCK,
                        CASE_SUB_2
                        )
                    br         LEAVE_BLOCK
                    
                    // case add
                    CASE_ADD_2:
                    ldarg.s    result
                    ldarg.1
                    unbox      native int
                    ldind.i
                    ldarg.2
                    unbox      native int
                    ldind.i
                    add
                    box        native int
                    stind.ref
                    br         LEAVE_BLOCK
                    
                    // case div
                    CASE_DIV_2:
                    ldarg.s    result
                    ldarg.1
                    unbox      native int
                    ldind.i
                    ldarg.2
                    unbox      native int
                    ldind.i
                    div
                    box        native int
                    stind.ref
                    br         LEAVE_BLOCK
                    
                    // case mul
                    CASE_MUL_2:
                    ldarg.s    result
                    ldarg.1
                    unbox      native int
                    ldind.i
                    ldarg.2
                    unbox      native int
                    ldind.i
                    mul
                    box        native int
                    stind.ref
                    br         LEAVE_BLOCK
                    
                    // case rem
                    CASE_REM_2:
                    ldarg.s    result
                    ldarg.1
                    unbox      native int
                    ldind.i
                    ldarg.2
                    unbox      native int
                    ldind.i
                    rem
                    box        native int
                    stind.ref
                    br         LEAVE_BLOCK
                    
                    // case sub
                    CASE_SUB_2:
                    ldarg.s    result
                    ldarg.1
                    unbox      native int
                    ldind.i
                    ldarg.2
                    unbox      native int
                    ldind.i
                    sub
                    box        native int
                    stind.ref
                    br         LEAVE_BLOCK
                }
                
                // case float64, float64
                CASE_FLOAT64_FLOAT64:
                {
                    // switch (op)  
                    ldarg.0
                    ldc.i4.0
                    beq.s      CASE_ADD_3
                    ldarg.0
                    ldc.i4.5
                    sub
                    switch ( 
                        CASE_DIV_3,
                        CASE_MUL_3,
                        LEAVE_BLOCK,
                        CASE_REM_3,
                        LEAVE_BLOCK,
                        LEAVE_BLOCK,
                        CASE_SUB_3
                        )
                    br         LEAVE_BLOCK
                    
                    // case add
                    CASE_ADD_3:
                    ldarg.s    result
                    ldarg.1
                    unbox      float64
                    ldind.r8
                    ldarg.2
                    unbox      float64
                    ldind.r8
                    add
                    box        float64
                    stind.ref
                    br         LEAVE_BLOCK
                    
                    // case div
                    CASE_DIV_3:
                    ldarg.s    result
                    ldarg.1
                    unbox      float64
                    ldind.r8
                    ldarg.2
                    unbox      float64
                    ldind.r8
                    div
                    box        float64
                    stind.ref
                    br         LEAVE_BLOCK
                    
                    // case mul
                    CASE_MUL_3:
                    ldarg.s    result
                    ldarg.1
                    unbox      float64
                    ldind.r8
                    ldarg.2
                    unbox      float64
                    ldind.r8
                    mul
                    box        float64
                    stind.ref
                    br         LEAVE_BLOCK
                    
                    // case rem
                    CASE_REM_3:
                    ldarg.s    result
                    ldarg.1
                    unbox      float64
                    ldind.r8
                    ldarg.2
                    unbox      float64
                    ldind.r8
                    rem
                    box        float64
                    stind.ref
                    br         LEAVE_BLOCK
                    
                    // case sub
                    CASE_SUB_3:
                    ldarg.s    result
                    ldarg.1
                    unbox      float64
                    ldind.r8
                    ldarg.2
                    unbox      float64
                    ldind.r8
                    sub
                    box        float64
                    stind.ref
                    br         LEAVE_BLOCK
                }
                
                // case int32, native int
                CASE_INT32_NATIVEINT:
                {
                    // switch (op)  
                    ldarg.0
                    ldc.i4.0
                    beq.s      CASE_ADD_4
                    ldarg.0
                    ldc.i4.5
                    sub
                    switch ( 
                        CASE_DIV_4,
                        CASE_MUL_4,
                        LEAVE_BLOCK,
                        CASE_REM_4,
                        LEAVE_BLOCK,
                        LEAVE_BLOCK,
                        CASE_SUB_4
                        )
                    br         LEAVE_BLOCK
                    
                    // case add
                    CASE_ADD_4:
                    ldarg.s    result
                    ldarg.1
                    unbox      int32
                    ldind.i4
                    ldarg.2
                    unbox      native int
                    ldind.i
                    add
                    box        native int
                    stind.ref
                    br         LEAVE_BLOCK
                    
                    // case div
                    CASE_DIV_4:
                    ldarg.s    result
                    ldarg.1
                    unbox      int32
                    ldind.i4
                    ldarg.2
                    unbox      native int
                    ldind.i
                    div
                    box        native int
                    stind.ref
                    br         LEAVE_BLOCK
                    
                    // case mul
                    CASE_MUL_4:
                    ldarg.s    result
                    ldarg.1
                    unbox      int32
                    ldind.i4
                    ldarg.2
                    unbox      native int
                    ldind.i
                    mul
                    box        native int
                    stind.ref
                    br         LEAVE_BLOCK
                    
                    // case rem
                    CASE_REM_4:
                    ldarg.s    result
                    ldarg.1
                    unbox      int32
                    ldind.i4
                    ldarg.2
                    unbox      native int
                    ldind.i
                    rem
                    box        native int
                    stind.ref
                    br         LEAVE_BLOCK
                    
                    // case sub
                    CASE_SUB_4:
                    ldarg.s    result
                    ldarg.1
                    unbox      int32
                    ldind.i4
                    ldarg.2
                    unbox      native int
                    ldind.i
                    sub
                    box        native int
                    stind.ref
                    br         LEAVE_BLOCK
                }
                
                // case native int, int32
                CASE_NATIVEINT_INT32:
                {
                    // switch (op)  
                    ldarg.0
                    ldc.i4.0
                    beq.s      CASE_ADD_5
                    ldarg.0
                    ldc.i4.5
                    sub
                    switch ( 
                        CASE_DIV_5,
                        CASE_MUL_5,
                        LEAVE_BLOCK,
                        CASE_REM_5,
                        LEAVE_BLOCK,
                        LEAVE_BLOCK,
                        CASE_SUB_5
                        )
                    br         LEAVE_BLOCK
                    
                    // case add
                    CASE_ADD_5:
                    ldarg.s    result
                    ldarg.1
                    unbox      native int
                    ldind.i
                    ldarg.2
                    unbox      int32
                    ldind.i4
                    add
                    box        native int
                    stind.ref
                    br         LEAVE_BLOCK
                    
                    // case div
                    CASE_DIV_5:
                    ldarg.s    result
                    ldarg.1
                    unbox      native int
                    ldind.i
                    ldarg.2
                    unbox      int32
                    ldind.i4
                    div
                    box        native int
                    stind.ref
                    br         LEAVE_BLOCK
                    
                    // case mul
                    CASE_MUL_5:
                    ldarg.s    result
                    ldarg.1
                    unbox      native int
                    ldind.i
                    ldarg.2
                    unbox      int32
                    ldind.i4
                    mul
                    box        native int
                    stind.ref
                    br         LEAVE_BLOCK
                    
                    // case rem
                    CASE_REM_5:
                    ldarg.s    result
                    ldarg.1
                    unbox      native int
                    ldind.i
                    ldarg.2
                    unbox      int32
                    ldind.i4
                    rem
                    box        native int
                    stind.ref
                    br         LEAVE_BLOCK
                    
                    // case sub
                    CASE_SUB_5:
                    ldarg.s    result
                    ldarg.1
                    unbox      native int
                    ldind.i
                    ldarg.2
                    unbox      int32
                    ldind.i4
                    sub
                    box        native int
                    stind.ref
                    br         LEAVE_BLOCK
                }
                
                LEAVE_BLOCK:
                leave.s    EXIT
            }  // end .try
            catch [mscorlib]System.Exception 
            {
                // result = e;
                stloc.1
                ldarg.s    result
                ldloc.1
                stind.ref
                
                // utilResult = false;
                ldc.i4.0
                stloc.0
                leave.s    EXIT
            }  // end handler

            // return utilResult;            
            EXIT:
            ldloc.0
            ret
        } // end of method DataModelUtils::NumericOp
        
        .method public hidebysig static void  ComparisonOp(
            int32 op, bool 'unsigned',
            object a, object b, int32 operandsKind,
            [out] object& result
            )
        {
            .maxstack  6
                
            // result = null;
            ldarg.s    result
            ldnull
            stind.ref
            
            // switch (operandsKind)
            ldarg.s    operandsKind
            switch ( 
                CASE_INT32_INT32,
                CASE_INT64_INT64,
                CASE_NATIVEINT_NATIVEINT,
                CASE_FLOAT64_FLOAT64,
                CASE_INT32_NATIVEINT,
                CASE_NATIVEINT_INT32
                )
            br        EXIT
            
            // case int32, int32
            CASE_INT32_INT32:
            {
                // switch (op)
                ldarg.0
                ldc.i4.2
                sub
                switch ( 
                    CASE_CEQ_0,
                    CASE_CGT_0,
                    CASE_CLT_0
                    )
                br         EXIT
                
                // case ceq
                CASE_CEQ_0:
                ldarg.s    result
                ldarg.2
                unbox      int32
                ldind.i4
                ldarg.3
                unbox      int32
                ldind.i4
                ceq
                box        int32
                stind.ref
                br         EXIT
                
                // case cgt
                CASE_CGT_0:
                ldarg.s    result
                ldarg.2
                unbox      int32
                ldind.i4
                ldarg.3
                unbox      int32
                ldind.i4
                ldarg.1
                brtrue.s   UNSIGNED_CGT_0
                cgt
                br.s       SAVE_CGT_0
                UNSIGNED_CGT_0:
                cgt.un
                SAVE_CGT_0:
                box        int32
                stind.ref
                br         EXIT
                
                // case clt
                CASE_CLT_0:
                ldarg.s    result
                ldarg.2
                unbox      int32
                ldind.i4
                ldarg.3
                unbox      int32
                ldind.i4
                ldarg.1
                brtrue.s   UNSIGNED_CLT_0
                clt
                br.s       SAVE_CLT_0
                UNSIGNED_CLT_0:
                clt.un
                SAVE_CLT_0:
                box        int32
                stind.ref
                br         EXIT
            }
            
            // case int64, int64
            CASE_INT64_INT64:  
            {
                // switch (op)
                ldarg.0
                ldc.i4.2
                sub
                switch ( 
                    CASE_CEQ_1,
                    CASE_CGT_1,
                    CASE_CLT_1
                    )
                br         EXIT
                
                // case ceq
                CASE_CEQ_1:
                ldarg.s    result
                ldarg.2
                unbox      int64
                ldind.i8
                ldarg.3
                unbox      int64
                ldind.i8
                ceq
                box        int32
                stind.ref
                br         EXIT
                
                // case cgt
                CASE_CGT_1:
                ldarg.s    result
                ldarg.2
                unbox      int64
                ldind.i8
                ldarg.3
                unbox      int64
                ldind.i8
                ldarg.1
                brtrue.s   UNSIGNED_CGT_1
                cgt
                br.s       SAVE_CGT_1
                UNSIGNED_CGT_1:
                cgt.un
                SAVE_CGT_1:
                box        int32
                stind.ref
                br         EXIT
                
                // case clt
                CASE_CLT_1:
                ldarg.s    result
                ldarg.2
                unbox      int64
                ldind.i8
                ldarg.3
                unbox      int64
                ldind.i8
                ldarg.1
                brtrue.s   UNSIGNED_CLT_1
                clt
                br.s       SAVE_CLT_1
                UNSIGNED_CLT_1:
                clt.un
                SAVE_CLT_1:
                box        int32
                stind.ref
                br         EXIT
            }
            
            // case native int, native int
            CASE_NATIVEINT_NATIVEINT:
            {
                // switch (op)
                ldarg.0
                ldc.i4.2
                sub
                switch ( 
                    CASE_CEQ_2,
                    CASE_CGT_2,
                    CASE_CLT_2
                    )
                br         EXIT
                
                // case ceq
                CASE_CEQ_2:
                ldarg.s    result
                ldarg.2
                unbox      native int
                ldind.i
                ldarg.3
                unbox      native int
                ldind.i
                ceq
                box        int32
                stind.ref
                br         EXIT
                
                // case cgt
                CASE_CGT_2:
                ldarg.s    result
                ldarg.2
                unbox      native int
                ldind.i
                ldarg.3
                unbox      native int
                ldind.i
                ldarg.1
                brtrue.s   UNSIGNED_CGT_2
                cgt
                br.s       SAVE_CGT_2
                UNSIGNED_CGT_2:
                cgt.un
                SAVE_CGT_2:
                box        int32
                stind.ref
                br         EXIT
                
                // case clt
                CASE_CLT_2:
                ldarg.s    result
                ldarg.2
                unbox      native int
                ldind.i
                ldarg.3
                unbox      native int
                ldind.i
                ldarg.1
                brtrue.s   UNSIGNED_CLT_2
                clt
                br.s       SAVE_CLT_2
                UNSIGNED_CLT_2:
                clt.un
                SAVE_CLT_2:
                box        int32
                stind.ref
                br         EXIT
            }
            
            // case float64, float64
            CASE_FLOAT64_FLOAT64:
            {
                // switch (op)
                ldarg.0
                ldc.i4.2
                sub
                switch ( 
                    CASE_CEQ_3,
                    CASE_CGT_3,
                    CASE_CLT_3
                    )
                br         EXIT
                
                // case ceq
                CASE_CEQ_3:
                ldarg.s    result
                ldarg.2
                unbox      float64
                ldind.r8
                ldarg.3
                unbox      float64
                ldind.r8
                ceq
                box        int32
                stind.ref
                br         EXIT
                
                // case cgt
                CASE_CGT_3:
                ldarg.s    result
                ldarg.2
                unbox      float64
                ldind.r8
                ldarg.3
                unbox      float64
                ldind.r8
                ldarg.1
                brtrue.s   UNSIGNED_CGT_3
                cgt
                br.s       SAVE_CGT_3
                UNSIGNED_CGT_3:
                cgt.un
                SAVE_CGT_3:
                box        int32
                stind.ref
                br         EXIT
                
                // case clt
                CASE_CLT_3:
                ldarg.s    result
                ldarg.2
                unbox      float64
                ldind.r8
                ldarg.3
                unbox      float64
                ldind.r8
                ldarg.1
                brtrue.s   UNSIGNED_CLT_3
                clt
                br.s       SAVE_CLT_3
                UNSIGNED_CLT_3:
                clt.un
                SAVE_CLT_3:
                box        int32
                stind.ref
                br         EXIT
            }
            
            // case int32, native int
            CASE_INT32_NATIVEINT:
            {
                // switch (op)
                ldarg.0
                ldc.i4.2
                sub
                switch ( 
                    CASE_CEQ_4,
                    CASE_CGT_4,
                    CASE_CLT_4
                    )
                br         EXIT
                
                // case ceq
                CASE_CEQ_4:
                ldarg.s    result
                ldarg.2
                unbox      int32
                ldind.i4
                ldarg.3
                unbox      native int
                ldind.i
                ceq
                box        int32
                stind.ref
                br         EXIT
                
                // case cgt
                CASE_CGT_4:
                ldarg.s    result
                ldarg.2
                unbox      int32
                ldind.i4
                ldarg.3
                unbox      native int
                ldind.i
                ldarg.1
                brtrue.s   UNSIGNED_CGT_4
                cgt
                br.s       SAVE_CGT_4
                UNSIGNED_CGT_4:
                cgt.un
                SAVE_CGT_4:
                box        int32
                stind.ref
                br         EXIT
                
                // case clt
                CASE_CLT_4:
                ldarg.s    result
                ldarg.2
                unbox      int32
                ldind.i4
                ldarg.3
                unbox      native int
                ldind.i
                ldarg.1
                brtrue.s   UNSIGNED_CLT_4
                clt
                br.s       SAVE_CLT_4
                UNSIGNED_CLT_4:
                clt.un
                SAVE_CLT_4:
                box        int32
                stind.ref
                br         EXIT
            }
            
            // case native int, int32
            CASE_NATIVEINT_INT32:
            {
                // switch (op)
                ldarg.0
                ldc.i4.2
                sub
                switch ( 
                    CASE_CEQ_5,
                    CASE_CGT_5,
                    CASE_CLT_5
                    )
                br         EXIT
                
                // case ceq
                CASE_CEQ_5:
                ldarg.s    result
                ldarg.2
                unbox      native int
                ldind.i
                ldarg.3
                unbox      int32
                ldind.i4
                ceq
                box        int32
                stind.ref
                br         EXIT
                
                // case cgt
                CASE_CGT_5:
                ldarg.s    result
                ldarg.2
                unbox      native int
                ldind.i
                ldarg.3
                unbox      int32
                ldind.i4
                ldarg.1
                brtrue.s   UNSIGNED_CGT_5
                cgt
                br.s       SAVE_CGT_5
                UNSIGNED_CGT_5:
                cgt.un
                SAVE_CGT_5:
                box        int32
                stind.ref
                br         EXIT
                
                // case clt
                CASE_CLT_5:
                ldarg.s    result
                ldarg.2
                unbox      native int
                ldind.i
                ldarg.3
                unbox      int32
                ldind.i4
                ldarg.1
                brtrue.s   UNSIGNED_CLT_5
                clt
                br.s       SAVE_CLT_5
                UNSIGNED_CLT_5:
                clt.un
                SAVE_CLT_5:
                box        int32
                stind.ref
                br         EXIT
            }
            
            EXIT:
            ret
        } // end of method DataModelUtils::ComparisonOp
        
        .method public hidebysig static bool  IntOvfOp(int32 op,
                                               bool 'unsigned',
                                               object a,
                                               object b,
                                               int32 operandsKind,
                                               [out] object& result) cil managed
        {
            .maxstack  6
            
            .locals init (
                [0] bool utilResult,
                [1] class [mscorlib]System.Exception e
                )
            
            // utilResult = true;        
            ldc.i4.1
            stloc.0
            
            // result = null;
            ldarg.s    result
            ldnull
            stind.ref
            
            .try
            {
                // switch (operandsKind)
                ldarg.s    operandsKind
                switch ( 
                    CASE_INT32_INT32,
                    CASE_INT64_INT64,
                    CASE_NATIVEINT_NATIVEINT,
                    LEAVE_BLOCK,
                    CASE_INT32_NATIVEINT,
                    CASE_NATIVEINT_INT32
                    )
                leave.s    EXIT
                
                // case int32, int32
                CASE_INT32_INT32:
                ldarg.0
                ldarg.1
                ldarg.2
                ldarg.3
                ldarg.s    result
                call       void CILPE.DataModel.DataModelUtils::intOvfOp0(int32,bool,object,object,object&)
                LEAVE_BLOCK:
                leave.s    EXIT
                
                // case int64, int64
                CASE_INT64_INT64:
                ldarg.0
                ldarg.1
                ldarg.2
                ldarg.3
                ldarg.s    result
                call       void CILPE.DataModel.DataModelUtils::intOvfOp1(int32,bool,object,object,object&)
                leave.s    EXIT
                
                // case native int, native int
                CASE_NATIVEINT_NATIVEINT:
                ldarg.0
                ldarg.1
                ldarg.2
                ldarg.3
                ldarg.s    result
                call       void CILPE.DataModel.DataModelUtils::intOvfOp2(int32,bool,object,object,object&)
                leave.s    EXIT
                
                // case int32, native int
                CASE_INT32_NATIVEINT:
                ldarg.0
                ldarg.1
                ldarg.2
                ldarg.3
                ldarg.s    result
                call       void CILPE.DataModel.DataModelUtils::intOvfOp4(int32,bool,object,object,object&)
                leave.s    EXIT
                
                // case native int, int32
                CASE_NATIVEINT_INT32:
                ldarg.0
                ldarg.1
                ldarg.2
                ldarg.3
                ldarg.s    result
                call       void CILPE.DataModel.DataModelUtils::intOvfOp5(int32,bool,object,object,object&)
                leave.s    EXIT
            }  // end .try
            catch [mscorlib]System.Exception 
            {
                // result = e;
                stloc.1
                ldarg.s    result
                ldloc.1
                stind.ref
                
                // utilResult = false;
                ldc.i4.0
                stloc.0
                leave.s    EXIT
            }  // end handler

            EXIT:
            ldloc.0
            ret
        } // end of method DataModelUtils::IntOvfOp
        
        .method private hidebysig static void  intOvfOp0(int32 op,
                                                 bool 'unsigned',
                                                 object a,
                                                 object b,
                                                 [out] object& result) cil managed
        {
            .maxstack  5
            
            // Loading operands
            ldarg.s    result
            ldarg.2
            unbox      int32
            ldind.i4
            ldarg.3
            unbox      int32
            ldind.i4

            // Performing operation            
            ldarg.0
            brtrue.s   CHECK_AND
            {
                // add.ovf, add.ovf.un
                ldarg.1
                brfalse.s  CASE_ADD_OVF
                add.ovf.un
                br.s       SAVE_RESULT
                CASE_ADD_OVF:
                add.ovf
                br.s       SAVE_RESULT
            }
            
            CHECK_AND:
            ldarg.0
            ldc.i4.1
            bne.un.s   CHECK_DIV_UN
            {
                // and
                and
                br.s       SAVE_RESULT
            }
            
            CHECK_DIV_UN:
            ldarg.0
            ldc.i4.5
            bne.un.s   CHECK_MUL
            {
                // div.un
                div.un
                br.s       SAVE_RESULT
            }
            
            CHECK_MUL:
            ldarg.0
            ldc.i4.6
            bne.un.s   CHECK_OR
            {
                // mul.ovf, mul.ovf.un
                ldarg.1
                brfalse.s  CASE_MUL_OVF
                mul.ovf.un
                br.s       SAVE_RESULT
                CASE_MUL_OVF:
                mul.ovf
                br.s       SAVE_RESULT
            }
            
            CHECK_OR:
            ldarg.0
            ldc.i4.7
            bne.un.s   CHECK_REM_UN
            {
                // or
                or
                br.s       SAVE_RESULT
            }
            
            CHECK_REM_UN:
            ldarg.0
            ldc.i4.8
            bne.un.s   CHECK_SUB
            {
                // rem.un
                rem.un
                br.s       SAVE_RESULT
            }
            
            CHECK_SUB:
            ldarg.0
            ldc.i4.s   11
            bne.un.s   CASE_XOR
            {
                // sub.ovf, sub.ovf.un
                ldarg.1
                brfalse.s  CASE_SUB_OVF
                sub.ovf.un
                br.s       SAVE_RESULT
                CASE_SUB_OVF:
                sub.ovf
                br.s       SAVE_RESULT
            }
            
            CASE_XOR:  
            {
                // xor
                xor
            }
            
            // Saving result
            SAVE_RESULT:
            box        int32
            stind.ref
            ret
        } // end of method DataModelUtils::intOvfOp0
        
        .method private hidebysig static void  intOvfOp1(int32 op,
                                                 bool 'unsigned',
                                                 object a,
                                                 object b,
                                                 [out] object& result) cil managed
        {
            .maxstack  5
            
            // Loading operands
            ldarg.s    result
            ldarg.2
            unbox      int64
            ldind.i8
            ldarg.3
            unbox      int64
            ldind.i8

            // Performing operation            
            ldarg.0
            brtrue.s   CHECK_AND
            {
                // add.ovf, add.ovf.un
                ldarg.1
                brfalse.s  CASE_ADD_OVF
                add.ovf.un
                br.s       SAVE_RESULT
                CASE_ADD_OVF:
                add.ovf
                br.s       SAVE_RESULT
            }
            
            CHECK_AND:
            ldarg.0
            ldc.i4.1
            bne.un.s   CHECK_DIV_UN
            {
                // and
                and
                br.s       SAVE_RESULT
            }
            
            CHECK_DIV_UN:
            ldarg.0
            ldc.i4.5
            bne.un.s   CHECK_MUL
            {
                // div.un
                div.un
                br.s       SAVE_RESULT
            }
            
            CHECK_MUL:
            ldarg.0
            ldc.i4.6
            bne.un.s   CHECK_OR
            {
                // mul.ovf, mul.ovf.un
                ldarg.1
                brfalse.s  CASE_MUL_OVF
                mul.ovf.un
                br.s       SAVE_RESULT
                CASE_MUL_OVF:
                mul.ovf
                br.s       SAVE_RESULT
            }
            
            CHECK_OR:
            ldarg.0
            ldc.i4.7
            bne.un.s   CHECK_REM_UN
            {
                // or
                or
                br.s       SAVE_RESULT
            }
            
            CHECK_REM_UN:
            ldarg.0
            ldc.i4.8
            bne.un.s   CHECK_SUB
            {
                // rem.un
                rem.un
                br.s       SAVE_RESULT
            }
            
            CHECK_SUB:
            ldarg.0
            ldc.i4.s   11
            bne.un.s   CASE_XOR
            {
                // sub.ovf, sub.ovf.un
                ldarg.1
                brfalse.s  CASE_SUB_OVF
                sub.ovf.un
                br.s       SAVE_RESULT
                CASE_SUB_OVF:
                sub.ovf
                br.s       SAVE_RESULT
            }
            
            CASE_XOR:  
            {
                // xor
                xor
            }
            
            // Saving result
            SAVE_RESULT:
            box        int64
            stind.ref
            ret
        } // end of method DataModelUtils::intOvfOp1
        
        .method private hidebysig static void  intOvfOp2(int32 op,
                                                 bool 'unsigned',
                                                 object a,
                                                 object b,
                                                 [out] object& result) cil managed
        {
            .maxstack  5
            
            // Loading operands
            ldarg.s    result
            ldarg.2
            unbox      native int
            ldind.i
            ldarg.3
            unbox      native int
            ldind.i

            // Performing operation            
            ldarg.0
            brtrue.s   CHECK_AND
            {
                // add.ovf, add.ovf.un
                ldarg.1
                brfalse.s  CASE_ADD_OVF
                add.ovf.un
                br.s       SAVE_RESULT
                CASE_ADD_OVF:
                add.ovf
                br.s       SAVE_RESULT
            }
            
            CHECK_AND:
            ldarg.0
            ldc.i4.1
            bne.un.s   CHECK_DIV_UN
            {
                // and
                and
                br.s       SAVE_RESULT
            }
            
            CHECK_DIV_UN:
            ldarg.0
            ldc.i4.5
            bne.un.s   CHECK_MUL
            {
                // div.un
                div.un
                br.s       SAVE_RESULT
            }
            
            CHECK_MUL:
            ldarg.0
            ldc.i4.6
            bne.un.s   CHECK_OR
            {
                // mul.ovf, mul.ovf.un
                ldarg.1
                brfalse.s  CASE_MUL_OVF
                mul.ovf.un
                br.s       SAVE_RESULT
                CASE_MUL_OVF:
                mul.ovf
                br.s       SAVE_RESULT
            }
            
            CHECK_OR:
            ldarg.0
            ldc.i4.7
            bne.un.s   CHECK_REM_UN
            {
                // or
                or
                br.s       SAVE_RESULT
            }
            
            CHECK_REM_UN:
            ldarg.0
            ldc.i4.8
            bne.un.s   CHECK_SUB
            {
                // rem.un
                rem.un
                br.s       SAVE_RESULT
            }
            
            CHECK_SUB:
            ldarg.0
            ldc.i4.s   11
            bne.un.s   CASE_XOR
            {
                // sub.ovf, sub.ovf.un
                ldarg.1
                brfalse.s  CASE_SUB_OVF
                sub.ovf.un
                br.s       SAVE_RESULT
                CASE_SUB_OVF:
                sub.ovf
                br.s       SAVE_RESULT
            }
            
            CASE_XOR:  
            {
                // xor
                xor
            }
            
            // Saving result
            SAVE_RESULT:
            box        native int
            stind.ref
            ret
        } // end of method DataModelUtils::intOvfOp2
        
        .method private hidebysig static void  intOvfOp4(int32 op,
                                                 bool 'unsigned',
                                                 object a,
                                                 object b,
                                                 [out] object& result) cil managed
        {
            .maxstack  5
            
            // Loading operands
            ldarg.s    result
            ldarg.2
            unbox      int32
            ldind.i4
            ldarg.3
            unbox      native int
            ldind.i

            // Performing operation            
            ldarg.0
            brtrue.s   CHECK_AND
            {
                // add.ovf, add.ovf.un
                ldarg.1
                brfalse.s  CASE_ADD_OVF
                add.ovf.un
                br.s       SAVE_RESULT
                CASE_ADD_OVF:
                add.ovf
                br.s       SAVE_RESULT
            }
            
            CHECK_AND:
            ldarg.0
            ldc.i4.1
            bne.un.s   CHECK_DIV_UN
            {
                // and
                and
                br.s       SAVE_RESULT
            }
            
            CHECK_DIV_UN:
            ldarg.0
            ldc.i4.5
            bne.un.s   CHECK_MUL
            {
                // div.un
                div.un
                br.s       SAVE_RESULT
            }
            
            CHECK_MUL:
            ldarg.0
            ldc.i4.6
            bne.un.s   CHECK_OR
            {
                // mul.ovf, mul.ovf.un
                ldarg.1
                brfalse.s  CASE_MUL_OVF
                mul.ovf.un
                br.s       SAVE_RESULT
                CASE_MUL_OVF:
                mul.ovf
                br.s       SAVE_RESULT
            }
            
            CHECK_OR:
            ldarg.0
            ldc.i4.7
            bne.un.s   CHECK_REM_UN
            {
                // or
                or
                br.s       SAVE_RESULT
            }
            
            CHECK_REM_UN:
            ldarg.0
            ldc.i4.8
            bne.un.s   CHECK_SUB
            {
                // rem.un
                rem.un
                br.s       SAVE_RESULT
            }
            
            CHECK_SUB:
            ldarg.0
            ldc.i4.s   11
            bne.un.s   CASE_XOR
            {
                // sub.ovf, sub.ovf.un
                ldarg.1
                brfalse.s  CASE_SUB_OVF
                sub.ovf.un
                br.s       SAVE_RESULT
                CASE_SUB_OVF:
                sub.ovf
                br.s       SAVE_RESULT
            }
            
            CASE_XOR:  
            {
                // xor
                xor
            }
            
            // Saving result
            SAVE_RESULT:
            box        native int
            stind.ref
            ret
        } // end of method DataModelUtils::intOvfOp4
        
        .method private hidebysig static void  intOvfOp5(int32 op,
                                                 bool 'unsigned',
                                                 object a,
                                                 object b,
                                                 [out] object& result) cil managed
        {
            .maxstack  5
            
            // Loading operands
            ldarg.s    result
            ldarg.2
            unbox      native int
            ldind.i
            ldarg.3
            unbox      int32
            ldind.i4

            // Performing operation            
            ldarg.0
            brtrue.s   CHECK_AND
            {
                // add.ovf, add.ovf.un
                ldarg.1
                brfalse.s  CASE_ADD_OVF
                add.ovf.un
                br.s       SAVE_RESULT
                CASE_ADD_OVF:
                add.ovf
                br.s       SAVE_RESULT
            }
            
            CHECK_AND:
            ldarg.0
            ldc.i4.1
            bne.un.s   CHECK_DIV_UN
            {
                // and
                and
                br.s       SAVE_RESULT
            }
            
            CHECK_DIV_UN:
            ldarg.0
            ldc.i4.5
            bne.un.s   CHECK_MUL
            {
                // div.un
                div.un
                br.s       SAVE_RESULT
            }
            
            CHECK_MUL:
            ldarg.0
            ldc.i4.6
            bne.un.s   CHECK_OR
            {
                // mul.ovf, mul.ovf.un
                ldarg.1
                brfalse.s  CASE_MUL_OVF
                mul.ovf.un
                br.s       SAVE_RESULT
                CASE_MUL_OVF:
                mul.ovf
                br.s       SAVE_RESULT
            }
            
            CHECK_OR:
            ldarg.0
            ldc.i4.7
            bne.un.s   CHECK_REM_UN
            {
                // or
                or
                br.s       SAVE_RESULT
            }
            
            CHECK_REM_UN:
            ldarg.0
            ldc.i4.8
            bne.un.s   CHECK_SUB
            {
                // rem.un
                rem.un
                br.s       SAVE_RESULT
            }
            
            CHECK_SUB:
            ldarg.0
            ldc.i4.s   11
            bne.un.s   CASE_XOR
            {
                // sub.ovf, sub.ovf.un
                ldarg.1
                brfalse.s  CASE_SUB_OVF
                sub.ovf.un
                br.s       SAVE_RESULT
                CASE_SUB_OVF:
                sub.ovf
                br.s       SAVE_RESULT
            }
            
            CASE_XOR:  
            {
                // xor
                xor
            }
            
            // Saving result
            SAVE_RESULT:
            box        native int
            stind.ref
            ret
        } // end of method DataModelUtils::intOvfOp5

        .method public hidebysig static void  ShiftOp(
            int32 op, bool 'unsigned',
            object a, object b, int32 operandsKind,
            [out] object& result
            )
        {
            .maxstack  5
            
            // result = null;
            ldarg.s    result
            ldnull
            stind.ref
            
            // switch (operandsKind)
            ldarg.s    operandsKind
            switch ( 
                CASE_INT32_INT32,
                CASE_INT64_INT32,
                CASE_NATIVEINT_INT32,
                CASE_INT32_NATIVEINT,
                CASE_INT64_NATIVEINT,
                CASE_NATIVEINT_NATIVEINT
                )
            br         EXIT
            
            // case int32, int32
            CASE_INT32_INT32:
            ldarg.s    result
            ldarg.2
            unbox      int32
            ldind.i4
            ldarg.3
            unbox      int32
            ldind.i4
            
            ldarg.0
            ldc.i4.s   9
            bne.un.s   LABEL0_0
            shl
            br         SAVE_INT32
            LABEL0_0:
            ldarg.1
            brfalse.s  LABEL1_0
            shr.un
            br         SAVE_INT32
            LABEL1_0:
            shr
            br         SAVE_INT32
            
            // case int64, int32
            CASE_INT64_INT32:
            ldarg.s    result
            ldarg.2
            unbox      int64
            ldind.i8
            ldarg.3
            unbox      int32
            ldind.i4
            
            ldarg.0
            ldc.i4.s   9
            bne.un.s   LABEL0_1
            shl
            br         SAVE_INT64
            LABEL0_1:
            ldarg.1
            brfalse.s  LABEL1_1
            shr.un
            br         SAVE_INT64
            LABEL1_1:
            shr
            br         SAVE_INT64
            
            // case native int, int32
            CASE_NATIVEINT_INT32:
            ldarg.s    result
            ldarg.2
            unbox      native int
            ldind.i
            ldarg.3
            unbox      int32
            ldind.i4
            
            ldarg.0
            ldc.i4.s   9
            bne.un.s   LABEL0_2
            shl
            br.s       SAVE_NATIVEINT
            LABEL0_2:
            ldarg.1
            brfalse.s  LABEL1_2
            shr.un
            br.s       SAVE_NATIVEINT
            LABEL1_2:
            shr
            br.s       SAVE_NATIVEINT
            
            // case int32, native int
            CASE_INT32_NATIVEINT:
            ldarg.s    result
            ldarg.2
            unbox      int32
            ldind.i4
            ldarg.3
            unbox      native int
            ldind.i
            
            ldarg.0
            ldc.i4.s   9
            bne.un.s   LABEL0_3
            shl
            br.s       SAVE_INT32
            LABEL0_3:
            ldarg.1
            brfalse.s  LABEL1_3
            shr.un
            br.s       SAVE_INT32
            LABEL1_3:
            shr
            br.s       SAVE_INT32
            
            // case int64, native int
            CASE_INT64_NATIVEINT:
            ldarg.s    result
            ldarg.2
            unbox      int64
            ldind.i8
            ldarg.3
            unbox      native int
            ldind.i
            
            ldarg.0
            ldc.i4.s   9
            bne.un.s   LABEL0_4
            shl
            br.s       SAVE_INT64
            LABEL0_4:
            ldarg.1
            brfalse.s  LABEL1_4
            shr.un
            br.s       SAVE_INT64
            LABEL1_4:
            shr
            br.s       SAVE_INT64
            
            // case native int, native int
            CASE_NATIVEINT_NATIVEINT:
            ldarg.s    result
            ldarg.2
            unbox      native int
            ldind.i
            ldarg.3
            unbox      native int
            ldind.i
            
            ldarg.0
            ldc.i4.s   9
            bne.un.s   LABEL0_5
            shl
            br.s       SAVE_NATIVEINT
            LABEL0_5:
            ldarg.1
            brfalse.s  LABEL1_5
            shr.un
            br.s       SAVE_NATIVEINT
            LABEL1_5:
            shr

            // Saving native int            
            SAVE_NATIVEINT:
            box        native int
            br.s       STORE
            
            // Saving int64
            SAVE_INT64:
            box        int64
            br.s       STORE
            
            // Saving int32
            SAVE_INT32:
            box        int32

            // Storing result
            STORE:
            stind.ref

            EXIT:
            ret
        } // end of method DataModelUtils::ShiftOp

        .method public hidebysig static bool Convert(
            int32 typeIndex, bool ovf, bool un,
            object a, int32 typeA,
            [out] object& result
            )
        {
            .maxstack  2

            .locals init (
                [0] bool utilResult,
                [1] class [mscorlib]System.Exception e
                )

            // utilResult = true;
            ldc.i4.1
            stloc.0

            // result = null;
            ldarg.s    result
            ldnull
            stind.ref

            .try
            {
                // switch (typeA)
                ldarg.s    typeA
                switch (
                    CASE_SOURCE_INT32,
                    CASE_SOURCE_INT64,
                    CASE_SOURCE_NATIVEINT,
                    CASE_SOURCE_FLOAT64
                    )
                leave      EXIT

                // case when operand type is int32
                CASE_SOURCE_INT32:
                {
                    //if (ovf)
                    ldarg.1
                    brfalse    IF_NOT_OVERFLOW_0
                    {
                        //if (un)
                        ldarg.2
                        brfalse    IF_NOT_UNSIGNED_0
                        {
                            // switch (typeIndex)
                            ldarg.0
                            switch (
                                CASE_INT32_OVF_UN_0,
                                CASE_INT64_OVF_UN_0,
                                CASE_NATIVEINT_OVF_UN_0,
                                FAILURE_OVF_UN_0,
                                CASE_INT8_OVF_UN_0,
                                CASE_UINT8_OVF_UN_0,
                                CASE_INT16_OVF_UN_0,
                                CASE_UINT16_OVF_UN_0,
                                CASE_UINT32_OVF_UN_0,
                                CASE_UINT64_OVF_UN_0,
                                CASE_UNATIVEINT_OVF_UN_0
                                )
                            FAILURE_OVF_UN_0:
                            leave      EXIT

                            // case int32
                            CASE_INT32_OVF_UN_0:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int32
                                ldind.i4
                                conv.ovf.i4.un
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case int64
                            CASE_INT64_OVF_UN_0:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int32
                                ldind.i4
                                conv.ovf.i8.un
                                box        int64
                                stind.ref
                                leave      EXIT
                            }

                            // case native int
                            CASE_NATIVEINT_OVF_UN_0:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int32
                                ldind.i4
                                conv.ovf.i.un
                                box        native int
                                stind.ref
                                leave      EXIT
                            }

                            // case int8
                            CASE_INT8_OVF_UN_0:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int32
                                ldind.i4
                                conv.ovf.i1.un
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case unsigned int8
                            CASE_UINT8_OVF_UN_0:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int32
                                ldind.i4
                                conv.ovf.u1.un
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case int16
                            CASE_INT16_OVF_UN_0:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int32
                                ldind.i4
                                conv.ovf.i2.un
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case unsigned int16
                            CASE_UINT16_OVF_UN_0:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int32
                                ldind.i4
                                conv.ovf.u2.un
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case unsigned int32
                            CASE_UINT32_OVF_UN_0:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int32
                                ldind.i4
                                conv.ovf.u4.un
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case unsigned int64
                            CASE_UINT64_OVF_UN_0:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int32
                                ldind.i4
                                conv.ovf.u8.un
                                box        int64
                                stind.ref
                                leave      EXIT
                            }

                            // case native unsigned int
                            CASE_UNATIVEINT_OVF_UN_0:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int32
                                ldind.i4
                                conv.ovf.u.un
                                box        native int
                                stind.ref
                                leave      EXIT
                            }

                        }

                        // if (! un)
                        IF_NOT_UNSIGNED_0:
                        {
                            // switch (typeIndex)
                            ldarg.0
                            switch (
                                CASE_INT32_OVF_0,
                                CASE_INT64_OVF_0,
                                CASE_NATIVEINT_OVF_0,
                                FAILURE_OVF_0,
                                CASE_INT8_OVF_0,
                                CASE_UINT8_OVF_0,
                                CASE_INT16_OVF_0,
                                CASE_UINT16_OVF_0,
                                CASE_UINT32_OVF_0,
                                CASE_UINT64_OVF_0,
                                CASE_UNATIVEINT_OVF_0
                                )
                            FAILURE_OVF_0:
                            leave      EXIT

                            // case int32
                            CASE_INT32_OVF_0:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int32
                                ldind.i4
                                conv.ovf.i4
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case int64
                            CASE_INT64_OVF_0:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int32
                                ldind.i4
                                conv.ovf.i8
                                box        int64
                                stind.ref
                                leave      EXIT
                            }

                            // case native int
                            CASE_NATIVEINT_OVF_0:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int32
                                ldind.i4
                                conv.ovf.i
                                box        native int
                                stind.ref
                                leave      EXIT
                            }

                            // case int8
                            CASE_INT8_OVF_0:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int32
                                ldind.i4
                                conv.ovf.i1
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case unsigned int8
                            CASE_UINT8_OVF_0:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int32
                                ldind.i4
                                conv.ovf.u1
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case int16
                            CASE_INT16_OVF_0:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int32
                                ldind.i4
                                conv.ovf.i2
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case unsigned int16
                            CASE_UINT16_OVF_0:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int32
                                ldind.i4
                                conv.ovf.u2
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case unsigned int32
                            CASE_UINT32_OVF_0:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int32
                                ldind.i4
                                conv.ovf.u4
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case unsigned int64
                            CASE_UINT64_OVF_0:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int32
                                ldind.i4
                                conv.ovf.u8
                                box        int64
                                stind.ref
                                leave      EXIT
                            }

                            // case native unsigned int
                            CASE_UNATIVEINT_OVF_0:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int32
                                ldind.i4
                                conv.ovf.u
                                box        native int
                                stind.ref
                                leave      EXIT
                            }

                        }

                    }

                    // if (! ovf)
                    IF_NOT_OVERFLOW_0:
                    {
                        //if (un)
                        ldarg.2
                        brfalse    IF_PURE_0
                        {
                            ldarg.s    result
                            ldarg.3
                            unbox      int32
                            ldind.i4
                            conv.r.un
                            box        float64
                            stind.ref
                            leave      EXIT

                        }

                        // if (! un)
                        IF_PURE_0:
                        {
                            // switch (typeIndex)
                            ldarg.0
                            switch (
                                CASE_INT32_0,
                                CASE_INT64_0,
                                CASE_NATIVEINT_0,
                                CASE_FLOAT64_0,
                                CASE_INT8_0,
                                CASE_UINT8_0,
                                CASE_INT16_0,
                                CASE_UINT16_0,
                                CASE_UINT32_0,
                                CASE_UINT64_0,
                                CASE_UNATIVEINT_0,
                                CASE_FLOAT32_0
                                )
                            leave      EXIT

                            // case int32
                            CASE_INT32_0:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int32
                                ldind.i4
                                conv.i4
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case int64
                            CASE_INT64_0:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int32
                                ldind.i4
                                conv.i8
                                box        int64
                                stind.ref
                                leave      EXIT
                            }

                            // case native int
                            CASE_NATIVEINT_0:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int32
                                ldind.i4
                                conv.i
                                box        native int
                                stind.ref
                                leave      EXIT
                            }

                            // case float64
                            CASE_FLOAT64_0:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int32
                                ldind.i4
                                conv.r8
                                box        float64
                                stind.ref
                                leave      EXIT
                            }

                            // case int8
                            CASE_INT8_0:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int32
                                ldind.i4
                                conv.i1
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case unsigned int8
                            CASE_UINT8_0:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int32
                                ldind.i4
                                conv.u1
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case int16
                            CASE_INT16_0:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int32
                                ldind.i4
                                conv.i2
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case unsigned int16
                            CASE_UINT16_0:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int32
                                ldind.i4
                                conv.u2
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case unsigned int32
                            CASE_UINT32_0:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int32
                                ldind.i4
                                conv.u4
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case unsigned int64
                            CASE_UINT64_0:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int32
                                ldind.i4
                                conv.u8
                                box        int64
                                stind.ref
                                leave      EXIT
                            }

                            // case native unsigned int
                            CASE_UNATIVEINT_0:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int32
                                ldind.i4
                                conv.u
                                box        native int
                                stind.ref
                                leave      EXIT
                            }

                            // case float32
                            CASE_FLOAT32_0:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int32
                                ldind.i4
                                conv.r4
                                box        float64
                                stind.ref
                                leave      EXIT
                            }
                        }

                    }
                }

                // case when operand type is int64
                CASE_SOURCE_INT64:
                {
                    //if (ovf)
                    ldarg.1
                    brfalse    IF_NOT_OVERFLOW_1
                    {
                        //if (un)
                        ldarg.2
                        brfalse    IF_NOT_UNSIGNED_1
                        {
                            // switch (typeIndex)
                            ldarg.0
                            switch (
                                CASE_INT32_OVF_UN_1,
                                CASE_INT64_OVF_UN_1,
                                CASE_NATIVEINT_OVF_UN_1,
                                FAILURE_OVF_UN_1,
                                CASE_INT8_OVF_UN_1,
                                CASE_UINT8_OVF_UN_1,
                                CASE_INT16_OVF_UN_1,
                                CASE_UINT16_OVF_UN_1,
                                CASE_UINT32_OVF_UN_1,
                                CASE_UINT64_OVF_UN_1,
                                CASE_UNATIVEINT_OVF_UN_1
                                )
                            FAILURE_OVF_UN_1:
                            leave      EXIT

                            // case int32
                            CASE_INT32_OVF_UN_1:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int64
                                ldind.i8
                                conv.ovf.i4.un
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case int64
                            CASE_INT64_OVF_UN_1:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int64
                                ldind.i8
                                conv.ovf.i8.un
                                box        int64
                                stind.ref
                                leave      EXIT
                            }

                            // case native int
                            CASE_NATIVEINT_OVF_UN_1:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int64
                                ldind.i8
                                conv.ovf.i.un
                                box        native int
                                stind.ref
                                leave      EXIT
                            }

                            // case int8
                            CASE_INT8_OVF_UN_1:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int64
                                ldind.i8
                                conv.ovf.i1.un
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case unsigned int8
                            CASE_UINT8_OVF_UN_1:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int64
                                ldind.i8
                                conv.ovf.u1.un
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case int16
                            CASE_INT16_OVF_UN_1:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int64
                                ldind.i8
                                conv.ovf.i2.un
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case unsigned int16
                            CASE_UINT16_OVF_UN_1:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int64
                                ldind.i8
                                conv.ovf.u2.un
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case unsigned int32
                            CASE_UINT32_OVF_UN_1:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int64
                                ldind.i8
                                conv.ovf.u4.un
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case unsigned int64
                            CASE_UINT64_OVF_UN_1:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int64
                                ldind.i8
                                conv.ovf.u8.un
                                box        int64
                                stind.ref
                                leave      EXIT
                            }

                            // case native unsigned int
                            CASE_UNATIVEINT_OVF_UN_1:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int64
                                ldind.i8
                                conv.ovf.u.un
                                box        native int
                                stind.ref
                                leave      EXIT
                            }

                        }

                        // if (! un)
                        IF_NOT_UNSIGNED_1:
                        {
                            // switch (typeIndex)
                            ldarg.0
                            switch (
                                CASE_INT32_OVF_1,
                                CASE_INT64_OVF_1,
                                CASE_NATIVEINT_OVF_1,
                                FAILURE_OVF_1,
                                CASE_INT8_OVF_1,
                                CASE_UINT8_OVF_1,
                                CASE_INT16_OVF_1,
                                CASE_UINT16_OVF_1,
                                CASE_UINT32_OVF_1,
                                CASE_UINT64_OVF_1,
                                CASE_UNATIVEINT_OVF_1
                                )
                            FAILURE_OVF_1:
                            leave      EXIT

                            // case int32
                            CASE_INT32_OVF_1:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int64
                                ldind.i8
                                conv.ovf.i4
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case int64
                            CASE_INT64_OVF_1:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int64
                                ldind.i8
                                conv.ovf.i8
                                box        int64
                                stind.ref
                                leave      EXIT
                            }

                            // case native int
                            CASE_NATIVEINT_OVF_1:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int64
                                ldind.i8
                                conv.ovf.i
                                box        native int
                                stind.ref
                                leave      EXIT
                            }

                            // case int8
                            CASE_INT8_OVF_1:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int64
                                ldind.i8
                                conv.ovf.i1
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case unsigned int8
                            CASE_UINT8_OVF_1:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int64
                                ldind.i8
                                conv.ovf.u1
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case int16
                            CASE_INT16_OVF_1:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int64
                                ldind.i8
                                conv.ovf.i2
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case unsigned int16
                            CASE_UINT16_OVF_1:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int64
                                ldind.i8
                                conv.ovf.u2
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case unsigned int32
                            CASE_UINT32_OVF_1:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int64
                                ldind.i8
                                conv.ovf.u4
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case unsigned int64
                            CASE_UINT64_OVF_1:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int64
                                ldind.i8
                                conv.ovf.u8
                                box        int64
                                stind.ref
                                leave      EXIT
                            }

                            // case native unsigned int
                            CASE_UNATIVEINT_OVF_1:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int64
                                ldind.i8
                                conv.ovf.u
                                box        native int
                                stind.ref
                                leave      EXIT
                            }

                        }

                    }

                    // if (! ovf)
                    IF_NOT_OVERFLOW_1:
                    {
                        //if (un)
                        ldarg.2
                        brfalse    IF_PURE_1
                        {
                            ldarg.s    result
                            ldarg.3
                            unbox      int64
                            ldind.i8
                            conv.r.un
                            box        float64
                            stind.ref
                            leave      EXIT

                        }

                        // if (! un)
                        IF_PURE_1:
                        {
                            // switch (typeIndex)
                            ldarg.0
                            switch (
                                CASE_INT32_1,
                                CASE_INT64_1,
                                CASE_NATIVEINT_1,
                                CASE_FLOAT64_1,
                                CASE_INT8_1,
                                CASE_UINT8_1,
                                CASE_INT16_1,
                                CASE_UINT16_1,
                                CASE_UINT32_1,
                                CASE_UINT64_1,
                                CASE_UNATIVEINT_1,
                                CASE_FLOAT32_1
                                )
                            leave      EXIT

                            // case int32
                            CASE_INT32_1:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int64
                                ldind.i8
                                conv.i4
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case int64
                            CASE_INT64_1:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int64
                                ldind.i8
                                conv.i8
                                box        int64
                                stind.ref
                                leave      EXIT
                            }

                            // case native int
                            CASE_NATIVEINT_1:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int64
                                ldind.i8
                                conv.i
                                box        native int
                                stind.ref
                                leave      EXIT
                            }

                            // case float64
                            CASE_FLOAT64_1:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int64
                                ldind.i8
                                conv.r8
                                box        float64
                                stind.ref
                                leave      EXIT
                            }

                            // case int8
                            CASE_INT8_1:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int64
                                ldind.i8
                                conv.i1
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case unsigned int8
                            CASE_UINT8_1:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int64
                                ldind.i8
                                conv.u1
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case int16
                            CASE_INT16_1:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int64
                                ldind.i8
                                conv.i2
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case unsigned int16
                            CASE_UINT16_1:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int64
                                ldind.i8
                                conv.u2
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case unsigned int32
                            CASE_UINT32_1:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int64
                                ldind.i8
                                conv.u4
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case unsigned int64
                            CASE_UINT64_1:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int64
                                ldind.i8
                                conv.u8
                                box        int64
                                stind.ref
                                leave      EXIT
                            }

                            // case native unsigned int
                            CASE_UNATIVEINT_1:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int64
                                ldind.i8
                                conv.u
                                box        native int
                                stind.ref
                                leave      EXIT
                            }

                            // case float32
                            CASE_FLOAT32_1:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      int64
                                ldind.i8
                                conv.r4
                                box        float64
                                stind.ref
                                leave      EXIT
                            }
                        }

                    }
                }

                // case when operand type is native int
                CASE_SOURCE_NATIVEINT:
                {
                    //if (ovf)
                    ldarg.1
                    brfalse    IF_NOT_OVERFLOW_2
                    {
                        //if (un)
                        ldarg.2
                        brfalse    IF_NOT_UNSIGNED_2
                        {
                            // switch (typeIndex)
                            ldarg.0
                            switch (
                                CASE_INT32_OVF_UN_2,
                                CASE_INT64_OVF_UN_2,
                                CASE_NATIVEINT_OVF_UN_2,
                                FAILURE_OVF_UN_2,
                                CASE_INT8_OVF_UN_2,
                                CASE_UINT8_OVF_UN_2,
                                CASE_INT16_OVF_UN_2,
                                CASE_UINT16_OVF_UN_2,
                                CASE_UINT32_OVF_UN_2,
                                CASE_UINT64_OVF_UN_2,
                                CASE_UNATIVEINT_OVF_UN_2
                                )
                            FAILURE_OVF_UN_2:
                            leave      EXIT

                            // case int32
                            CASE_INT32_OVF_UN_2:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      native int
                                ldind.i
                                conv.ovf.i4.un
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case int64
                            CASE_INT64_OVF_UN_2:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      native int
                                ldind.i
                                conv.ovf.i8.un
                                box        int64
                                stind.ref
                                leave      EXIT
                            }

                            // case native int
                            CASE_NATIVEINT_OVF_UN_2:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      native int
                                ldind.i
                                conv.ovf.i.un
                                box        native int
                                stind.ref
                                leave      EXIT
                            }

                            // case int8
                            CASE_INT8_OVF_UN_2:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      native int
                                ldind.i
                                conv.ovf.i1.un
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case unsigned int8
                            CASE_UINT8_OVF_UN_2:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      native int
                                ldind.i
                                conv.ovf.u1.un
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case int16
                            CASE_INT16_OVF_UN_2:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      native int
                                ldind.i
                                conv.ovf.i2.un
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case unsigned int16
                            CASE_UINT16_OVF_UN_2:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      native int
                                ldind.i
                                conv.ovf.u2.un
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case unsigned int32
                            CASE_UINT32_OVF_UN_2:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      native int
                                ldind.i
                                conv.ovf.u4.un
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case unsigned int64
                            CASE_UINT64_OVF_UN_2:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      native int
                                ldind.i
                                conv.ovf.u8.un
                                box        int64
                                stind.ref
                                leave      EXIT
                            }

                            // case native unsigned int
                            CASE_UNATIVEINT_OVF_UN_2:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      native int
                                ldind.i
                                conv.ovf.u.un
                                box        native int
                                stind.ref
                                leave      EXIT
                            }

                        }

                        // if (! un)
                        IF_NOT_UNSIGNED_2:
                        {
                            // switch (typeIndex)
                            ldarg.0
                            switch (
                                CASE_INT32_OVF_2,
                                CASE_INT64_OVF_2,
                                CASE_NATIVEINT_OVF_2,
                                FAILURE_OVF_2,
                                CASE_INT8_OVF_2,
                                CASE_UINT8_OVF_2,
                                CASE_INT16_OVF_2,
                                CASE_UINT16_OVF_2,
                                CASE_UINT32_OVF_2,
                                CASE_UINT64_OVF_2,
                                CASE_UNATIVEINT_OVF_2
                                )
                            FAILURE_OVF_2:
                            leave      EXIT

                            // case int32
                            CASE_INT32_OVF_2:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      native int
                                ldind.i
                                conv.ovf.i4
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case int64
                            CASE_INT64_OVF_2:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      native int
                                ldind.i
                                conv.ovf.i8
                                box        int64
                                stind.ref
                                leave      EXIT
                            }

                            // case native int
                            CASE_NATIVEINT_OVF_2:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      native int
                                ldind.i
                                conv.ovf.i
                                box        native int
                                stind.ref
                                leave      EXIT
                            }

                            // case int8
                            CASE_INT8_OVF_2:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      native int
                                ldind.i
                                conv.ovf.i1
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case unsigned int8
                            CASE_UINT8_OVF_2:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      native int
                                ldind.i
                                conv.ovf.u1
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case int16
                            CASE_INT16_OVF_2:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      native int
                                ldind.i
                                conv.ovf.i2
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case unsigned int16
                            CASE_UINT16_OVF_2:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      native int
                                ldind.i
                                conv.ovf.u2
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case unsigned int32
                            CASE_UINT32_OVF_2:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      native int
                                ldind.i
                                conv.ovf.u4
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case unsigned int64
                            CASE_UINT64_OVF_2:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      native int
                                ldind.i
                                conv.ovf.u8
                                box        int64
                                stind.ref
                                leave      EXIT
                            }

                            // case native unsigned int
                            CASE_UNATIVEINT_OVF_2:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      native int
                                ldind.i
                                conv.ovf.u
                                box        native int
                                stind.ref
                                leave      EXIT
                            }

                        }

                    }

                    // if (! ovf)
                    IF_NOT_OVERFLOW_2:
                    {
                        //if (un)
                        ldarg.2
                        brfalse    IF_PURE_2
                        {
                            ldarg.s    result
                            ldarg.3
                            unbox      native int
                            ldind.i
                            conv.r.un
                            box        float64
                            stind.ref
                            leave      EXIT

                        }

                        // if (! un)
                        IF_PURE_2:
                        {
                            // switch (typeIndex)
                            ldarg.0
                            switch (
                                CASE_INT32_2,
                                CASE_INT64_2,
                                CASE_NATIVEINT_2,
                                CASE_FLOAT64_2,
                                CASE_INT8_2,
                                CASE_UINT8_2,
                                CASE_INT16_2,
                                CASE_UINT16_2,
                                CASE_UINT32_2,
                                CASE_UINT64_2,
                                CASE_UNATIVEINT_2,
                                CASE_FLOAT32_2
                                )
                            leave      EXIT

                            // case int32
                            CASE_INT32_2:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      native int
                                ldind.i
                                conv.i4
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case int64
                            CASE_INT64_2:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      native int
                                ldind.i
                                conv.i8
                                box        int64
                                stind.ref
                                leave      EXIT
                            }

                            // case native int
                            CASE_NATIVEINT_2:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      native int
                                ldind.i
                                conv.i
                                box        native int
                                stind.ref
                                leave      EXIT
                            }

                            // case float64
                            CASE_FLOAT64_2:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      native int
                                ldind.i
                                conv.r8
                                box        float64
                                stind.ref
                                leave      EXIT
                            }

                            // case int8
                            CASE_INT8_2:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      native int
                                ldind.i
                                conv.i1
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case unsigned int8
                            CASE_UINT8_2:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      native int
                                ldind.i
                                conv.u1
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case int16
                            CASE_INT16_2:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      native int
                                ldind.i
                                conv.i2
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case unsigned int16
                            CASE_UINT16_2:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      native int
                                ldind.i
                                conv.u2
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case unsigned int32
                            CASE_UINT32_2:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      native int
                                ldind.i
                                conv.u4
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case unsigned int64
                            CASE_UINT64_2:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      native int
                                ldind.i
                                conv.u8
                                box        int64
                                stind.ref
                                leave      EXIT
                            }

                            // case native unsigned int
                            CASE_UNATIVEINT_2:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      native int
                                ldind.i
                                conv.u
                                box        native int
                                stind.ref
                                leave      EXIT
                            }

                            // case float32
                            CASE_FLOAT32_2:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      native int
                                ldind.i
                                conv.r4
                                box        float64
                                stind.ref
                                leave      EXIT
                            }
                        }

                    }
                }

                // case when operand type is float64
                CASE_SOURCE_FLOAT64:
                {
                    //if (ovf)
                    ldarg.1
                    brfalse    IF_NOT_OVERFLOW_3
                    {
                        //if (un)
                        ldarg.2
                        brfalse    IF_NOT_UNSIGNED_3
                        {
                            // switch (typeIndex)
                            ldarg.0
                            switch (
                                CASE_INT32_OVF_UN_3,
                                CASE_INT64_OVF_UN_3,
                                CASE_NATIVEINT_OVF_UN_3,
                                FAILURE_OVF_UN_3,
                                CASE_INT8_OVF_UN_3,
                                CASE_UINT8_OVF_UN_3,
                                CASE_INT16_OVF_UN_3,
                                CASE_UINT16_OVF_UN_3,
                                CASE_UINT32_OVF_UN_3,
                                CASE_UINT64_OVF_UN_3,
                                CASE_UNATIVEINT_OVF_UN_3
                                )
                            FAILURE_OVF_UN_3:
                            leave      EXIT

                            // case int32
                            CASE_INT32_OVF_UN_3:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      float64
                                ldind.r8
                                conv.ovf.i4.un
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case int64
                            CASE_INT64_OVF_UN_3:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      float64
                                ldind.r8
                                conv.ovf.i8.un
                                box        int64
                                stind.ref
                                leave      EXIT
                            }

                            // case native int
                            CASE_NATIVEINT_OVF_UN_3:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      float64
                                ldind.r8
                                conv.ovf.i.un
                                box        native int
                                stind.ref
                                leave      EXIT
                            }

                            // case int8
                            CASE_INT8_OVF_UN_3:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      float64
                                ldind.r8
                                conv.ovf.i1.un
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case unsigned int8
                            CASE_UINT8_OVF_UN_3:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      float64
                                ldind.r8
                                conv.ovf.u1.un
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case int16
                            CASE_INT16_OVF_UN_3:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      float64
                                ldind.r8
                                conv.ovf.i2.un
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case unsigned int16
                            CASE_UINT16_OVF_UN_3:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      float64
                                ldind.r8
                                conv.ovf.u2.un
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case unsigned int32
                            CASE_UINT32_OVF_UN_3:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      float64
                                ldind.r8
                                conv.ovf.u4.un
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case unsigned int64
                            CASE_UINT64_OVF_UN_3:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      float64
                                ldind.r8
                                conv.ovf.u8.un
                                box        int64
                                stind.ref
                                leave      EXIT
                            }

                            // case native unsigned int
                            CASE_UNATIVEINT_OVF_UN_3:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      float64
                                ldind.r8
                                conv.ovf.u.un
                                box        native int
                                stind.ref
                                leave      EXIT
                            }

                        }

                        // if (! un)
                        IF_NOT_UNSIGNED_3:
                        {
                            // switch (typeIndex)
                            ldarg.0
                            switch (
                                CASE_INT32_OVF_3,
                                CASE_INT64_OVF_3,
                                CASE_NATIVEINT_OVF_3,
                                FAILURE_OVF_3,
                                CASE_INT8_OVF_3,
                                CASE_UINT8_OVF_3,
                                CASE_INT16_OVF_3,
                                CASE_UINT16_OVF_3,
                                CASE_UINT32_OVF_3,
                                CASE_UINT64_OVF_3,
                                CASE_UNATIVEINT_OVF_3
                                )
                            FAILURE_OVF_3:
                            leave      EXIT

                            // case int32
                            CASE_INT32_OVF_3:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      float64
                                ldind.r8
                                conv.ovf.i4
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case int64
                            CASE_INT64_OVF_3:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      float64
                                ldind.r8
                                conv.ovf.i8
                                box        int64
                                stind.ref
                                leave      EXIT
                            }

                            // case native int
                            CASE_NATIVEINT_OVF_3:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      float64
                                ldind.r8
                                conv.ovf.i
                                box        native int
                                stind.ref
                                leave      EXIT
                            }

                            // case int8
                            CASE_INT8_OVF_3:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      float64
                                ldind.r8
                                conv.ovf.i1
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case unsigned int8
                            CASE_UINT8_OVF_3:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      float64
                                ldind.r8
                                conv.ovf.u1
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case int16
                            CASE_INT16_OVF_3:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      float64
                                ldind.r8
                                conv.ovf.i2
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case unsigned int16
                            CASE_UINT16_OVF_3:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      float64
                                ldind.r8
                                conv.ovf.u2
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case unsigned int32
                            CASE_UINT32_OVF_3:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      float64
                                ldind.r8
                                conv.ovf.u4
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case unsigned int64
                            CASE_UINT64_OVF_3:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      float64
                                ldind.r8
                                conv.ovf.u8
                                box        int64
                                stind.ref
                                leave      EXIT
                            }

                            // case native unsigned int
                            CASE_UNATIVEINT_OVF_3:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      float64
                                ldind.r8
                                conv.ovf.u
                                box        native int
                                stind.ref
                                leave      EXIT
                            }

                        }

                    }

                    // if (! ovf)
                    IF_NOT_OVERFLOW_3:
                    {
                        //if (un)
                        ldarg.2
                        brfalse    IF_PURE_3
                        {
                            ldarg.s    result
                            ldarg.3
                            unbox      float64
                            ldind.r8
                            conv.r.un
                            box        float64
                            stind.ref
                            leave      EXIT

                        }

                        // if (! un)
                        IF_PURE_3:
                        {
                            // switch (typeIndex)
                            ldarg.0
                            switch (
                                CASE_INT32_3,
                                CASE_INT64_3,
                                CASE_NATIVEINT_3,
                                CASE_FLOAT64_3,
                                CASE_INT8_3,
                                CASE_UINT8_3,
                                CASE_INT16_3,
                                CASE_UINT16_3,
                                CASE_UINT32_3,
                                CASE_UINT64_3,
                                CASE_UNATIVEINT_3,
                                CASE_FLOAT32_3
                                )
                            leave      EXIT

                            // case int32
                            CASE_INT32_3:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      float64
                                ldind.r8
                                conv.i4
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case int64
                            CASE_INT64_3:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      float64
                                ldind.r8
                                conv.i8
                                box        int64
                                stind.ref
                                leave      EXIT
                            }

                            // case native int
                            CASE_NATIVEINT_3:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      float64
                                ldind.r8
                                conv.i
                                box        native int
                                stind.ref
                                leave      EXIT
                            }

                            // case float64
                            CASE_FLOAT64_3:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      float64
                                ldind.r8
                                conv.r8
                                box        float64
                                stind.ref
                                leave      EXIT
                            }

                            // case int8
                            CASE_INT8_3:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      float64
                                ldind.r8
                                conv.i1
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case unsigned int8
                            CASE_UINT8_3:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      float64
                                ldind.r8
                                conv.u1
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case int16
                            CASE_INT16_3:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      float64
                                ldind.r8
                                conv.i2
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case unsigned int16
                            CASE_UINT16_3:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      float64
                                ldind.r8
                                conv.u2
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case unsigned int32
                            CASE_UINT32_3:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      float64
                                ldind.r8
                                conv.u4
                                box        int32
                                stind.ref
                                leave      EXIT
                            }

                            // case unsigned int64
                            CASE_UINT64_3:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      float64
                                ldind.r8
                                conv.u8
                                box        int64
                                stind.ref
                                leave      EXIT
                            }

                            // case native unsigned int
                            CASE_UNATIVEINT_3:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      float64
                                ldind.r8
                                conv.u
                                box        native int
                                stind.ref
                                leave      EXIT
                            }

                            // case float32
                            CASE_FLOAT32_3:
                            {
                                ldarg.s    result
                                ldarg.3
                                unbox      float64
                                ldind.r8
                                conv.r4
                                box        float64
                                stind.ref
                                leave      EXIT
                            }
                        }

                    }
                }

            }  // end .try
            catch [mscorlib]System.Exception
            {
                // result = e;
                stloc.1
                ldarg.s    result
                ldloc.1
                stind.ref

                // utilResult = false;
                ldc.i4.0
                stloc.0
                leave.s    EXIT
            }  // end handler

            EXIT:
            ldloc.0
            ret
        } // end of method DataModelUtils::Convert

        .method public hidebysig static void StoreToBox(object boxed, int32 boxedType, object newVal)
        {
            .maxstack  2
            
            ldarg.1
            
            // switch (boxedType)
            switch ( 
                CASE_INT32,
                CASE_INT64,
                CASE_NATIVEINT,
                CASE_FLOAT64,
                CASE_INT8,
                CASE_UINT8,
                CASE_INT16,
                CASE_UINT16,
                CASE_UINT32,
                CASE_UINT64,
                CASE_UNATIVEINT,
                CASE_FLOAT32,
                CASE_BOOL,
                CASE_CHAR
                )
            br         EXIT
            
            // case int32
            CASE_INT32:
            ldarg.0
            unbox      int32
            ldarg.2
            unbox      int32
            ldind.i4
            stind.i4
            br         EXIT

            // case int64
            CASE_INT64:
            ldarg.0
            unbox      int64
            ldarg.2
            unbox      int64
            ldind.i8
            stind.i8
            br         EXIT

            // case native int
            CASE_NATIVEINT:
            ldarg.0
            unbox      native int
            ldarg.2
            unbox      native int
            ldind.i
            stind.i
            br         EXIT

            // case float64
            CASE_FLOAT64:
            ldarg.0
            unbox      float64
            ldarg.2
            unbox      float64
            ldind.r8
            stind.r8
            br         EXIT

            // case int8
            CASE_INT8:
            ldarg.0
            unbox      int8
            ldarg.2
            unbox      int8
            ldind.i1
            stind.i1
            br         EXIT

            // case unsigned int8
            CASE_UINT8:
            ldarg.0
            unbox      unsigned int8
            ldarg.2
            unbox      unsigned int8
            ldind.u1
            stind.i1
            br         EXIT

            // case int16
            CASE_INT16:
            ldarg.0
            unbox      int16
            ldarg.2
            unbox      int16
            ldind.i2
            stind.i2
            br.s       EXIT

            // case unsigned int16
            CASE_UINT16:
            ldarg.0
            unbox      unsigned int16
            ldarg.2
            unbox      unsigned int16
            ldind.u2
            stind.i2
            br.s       EXIT

            // case unsigned int32
            CASE_UINT32:
            ldarg.0
            unbox      unsigned int32
            ldarg.2
            unbox      unsigned int32
            ldind.u4
            stind.i4
            br.s       EXIT

            // case unsigned int64
            CASE_UINT64:
            ldarg.0
            unbox      unsigned int64
            ldarg.2
            unbox      unsigned int64
            ldind.u8
            stind.i8
            br.s       EXIT

            // case native unsigned int
            CASE_UNATIVEINT:
            ldarg.0
            unbox      native unsigned int
            ldarg.2
            unbox      native unsigned int
            ldind.i
            stind.i
            br.s       EXIT

            // case float32
            CASE_FLOAT32:
            ldarg.0
            unbox      float32
            ldarg.2
            unbox      float32
            ldind.r4
            stind.r4
            br.s       EXIT
            
            // case bool
            CASE_BOOL:
            ldarg.0
            unbox      bool
            ldarg.2
            unbox      bool
            cpobj      bool
            br.s       EXIT
            
            // case char
            CASE_CHAR:
            ldarg.0
            unbox      char
            ldarg.2
            unbox      char
            cpobj      char

            EXIT:
            ret
        } // end of method DataModelUtils::StoreToBox

        .method public hidebysig specialname rtspecialname 
                instance void  .ctor() cil managed
        {
        // Code size       7 (0x7)
        .maxstack  1
        IL_0000:  ldarg.0
        IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
        IL_0006:  ret
        } // end of method DataModelUtils::.ctor

    } // end of class DataModelUtils

} // end of namespace CILPE.DataModel
