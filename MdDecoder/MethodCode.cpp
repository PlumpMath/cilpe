
// ===========================================================================
// CILPE - Partial Evaluator for Common Intermediate Language
// ===========================================================================
// File: 
//     MethodCode.cpp
//
// Description:
//     Intermediate representation of method's body and
//     decoder of exception handling clauses
//
// Author: 
//     Sergei Skorobogatov (Sergei.Skorobogatov@supercompilers.com)
// ===========================================================================

#include "stdafx.h"

#include <stddef.h>
#include <corhlpr.h>
#include "MethodCode.h"

namespace CILPE
{
    namespace MdDecoder
    {
        EHDecoder::EHDecoder(void __nogc *ilmethoddecoder)
        {
            COR_ILMETHOD_DECODER *decoder = (COR_ILMETHOD_DECODER *)ilmethoddecoder;
            count = decoder->EHCount();

            kinds = new Int32 [count];
            tryOfs = new Int32 [count];
            tryLens = new Int32 [count];
            hOfs = new Int32 [count];
            hLens = new Int32 [count];
            params = new Object * [count];

            if (count)
            {
                const COR_ILMETHOD_SECT_EH *ehTable = decoder->EH;

                for (int i = 0; i < count; i++)
                {
                    IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT buf;

                    const IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT *clause =
                        ehTable->EHClause(i,&buf);

                    int flags = clause->Flags;

                    if (flags == COR_ILEXCEPTION_CLAUSE_FILTER)
                        kinds[i] = USER_FILTERED_HANDLER;
                    else if (flags == COR_ILEXCEPTION_CLAUSE_FINALLY)
                        kinds[i] = FINALLY_HANDLER;
                    else if (flags == COR_ILEXCEPTION_CLAUSE_FAULT)
                        kinds[i] = FAULT_HANDLER;
                    else
                        kinds[i] = TYPE_FILTERED_HANDLER;

                    tryOfs[i] = clause->TryOffset;
                    tryLens[i] = clause->TryLength;
                    hOfs[i] = clause->HandlerOffset;
                    hLens[i] = clause->HandlerLength;

                    params[i] = NULL;
                    if (kinds[i] == TYPE_FILTERED_HANDLER)
                        params[i] = __box((Int32)(clause->ClassToken));
                    else if (kinds[i] == USER_FILTERED_HANDLER)
                        params[i] = __box((Int32)(clause->FilterOffset));
                }
            }
        }

        void EHDecoder::FixParams(Hashtable *hash)
        {
            for (int i = 0; i < count; i++)
                if (kinds[i] == TYPE_FILTERED_HANDLER)
                {
                    Int32 token = * (dynamic_cast <__box Int32 *> (params[i]));
                    params[i] = dynamic_cast <Type*> (hash->get_Item(__box(token)));
                }
        }

		MethodCode::MethodCode(int maxStack, int codeSize, 
			unsigned char __nogc *code, EHDecoder *ehDecoder, int locVarCount):
			maxStack(maxStack), codeSize(codeSize), ehDecoder(ehDecoder), pos(0)
		{
			this->code = __nogc new unsigned char [codeSize];
			memcpy(this->code,code,codeSize);

			locVarBaseTypes = __gc new Object * [locVarCount];
			locVarDeclarators = __gc new String * [locVarCount];
		}

		MethodCode MethodCode::copy()
		{
			MethodCode result = *this;
			result.code = __nogc new unsigned char [codeSize];
			memcpy(result.code,code,codeSize);
			return result;
		}
    }
}
