
// ===========================================================================
// CILPE - Partial Evaluator for Common Intermediate Language
// ===========================================================================
// File: 
//     MethodCode.h
//
// Description:
//     Intermediate representation of method's body and
//     decoder of exception handling clauses
//
// Author: 
//     Sergei Skorobogatov (Sergei.Skorobogatov@supercompilers.com)
// ===========================================================================

#pragma once

namespace CILPE
{
    namespace MdDecoder
    {
        using namespace System;
		using namespace System::Collections;

        enum EHKind
        {
            FINALLY_HANDLER = 0,
            FAULT_HANDLER = 1,
            TYPE_FILTERED_HANDLER = 2,
            USER_FILTERED_HANDLER = 3
        };

        public __gc class EHDecoder
        {
        private:
            int count;

            Int32 kinds[], tryOfs[], tryLens[], hOfs[], hLens[];
            Object *params[];
        public:
            EHDecoder(void __nogc *ilmethoddecoder);
            void FixParams(Hashtable *hash);

            int GetCount() { return count; }

            int GetKind(int index) { return kinds[index]; }
            int GetTryOfs(int index) { return tryOfs[index]; }
            int GetTryLen(int index) { return tryLens[index]; }
            int GetHOfs(int index) { return hOfs[index]; }
            int GetHLen(int index) { return hLens[index]; }
            int GetFOfs(int index) { return *(dynamic_cast <__box Int32*> (params[index])); }
            Object *GetClass(int index) { return params[index]; }
        };

        public __value struct MethodCode
		{
			int maxStack, codeSize;
            unsigned char __nogc *code;
            EHDecoder *ehDecoder;

			Object *locVarBaseTypes[];
			String *locVarDeclarators[];
			int pos;

			MethodCode(): maxStack(0), codeSize(0), code(NULL), pos(0)
			{
				locVarBaseTypes = NULL;
				locVarDeclarators = NULL;
			}

			MethodCode(int maxStack, int codeSize, 
				unsigned char __nogc *code, EHDecoder *ehDecoder, int locVarCount);

			MethodCode copy();

			void AddLocalVar(Object *baseType, String *declarators)
			{
				locVarBaseTypes[pos] = baseType;
				locVarDeclarators[pos++] = declarators;
			}
		};        
    }
}
