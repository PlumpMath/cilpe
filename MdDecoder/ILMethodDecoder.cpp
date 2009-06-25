
// ===========================================================================
// ILPE - Partial Evaluator for Common Intermediate Language
// ===========================================================================
// File: 
//     ILMethodDecoder.cpp
//
// Description:
//     Reading IL method bodies
//
// Author: 
//     Sergei Skorobogatov
// ===========================================================================

#include "stdafx.h"

#include <stddef.h>
#include "PeLoader.h"
#include "ILMethodDecoder.h"

namespace CILPE
{
    namespace MdDecoder
    {
		using namespace System::Collections;
		using namespace System::Reflection;

		/*ILMethodDecoder::ILMethodDecoder(MethodBase *method, Module *module)
        {
			if (moduleHash == NULL)
				moduleHash = new ModuleHash();

			hash = moduleHash->GetMetadataHash(module);
			
			Reset();
            enum MethodImplAttributes attrs = method->GetMethodImplementationFlags();

            if ((attrs & MethodImplAttributes::CodeTypeMask) == MethodImplAttributes::IL)
				methodCode = hash->getMethodCode(method).copy();
            else
                methodCode = MethodCode();
        }*/

        ILMethodDecoder::ILMethodDecoder(MethodCode methodCode, Hashtable *mdHash)
            : methodCode(methodCode), mdHash(mdHash)
        {
            Reset();
        }

		ILMethodDecoder::~ILMethodDecoder()
		{
			delete [] methodCode.code;
		}

		int ILMethodDecoder::ReadCode()
		{
			Byte result = methodCode.code[pos++];
			if (result == 0xFE)
				result = doubleByteCodesOrigin + methodCode.code[pos++];
			return result;
		}

		Int16 ILMethodDecoder::ReadInt8()
		{
			return (Int16)(*((SByte*)(methodCode.code) + (pos++)));
		}

		Int32 ILMethodDecoder::ReadInt32()
		{
			Int32 result = *((Int32*)(methodCode.code+pos));
			pos += sizeof(Int32);
			return result;
		}

		Int64 ILMethodDecoder::ReadInt64()
		{
			Int64 result = *((Int64*)(methodCode.code+pos));
			pos += sizeof(Int64);
			return result;
		}

		Byte ILMethodDecoder::ReadUint8()
		{
			return methodCode.code[pos++];
		}

		Int32 ILMethodDecoder::ReadUint16()
		{
			UInt16 result = *((UInt16*)(methodCode.code+pos));
			pos += sizeof(UInt16);
			return (Int32)result;
		}

		Single ILMethodDecoder::ReadFloat32()
		{
			Single result = *((Single*)(methodCode.code+pos));
			pos += sizeof(Single);
			return result;
		}

		Double ILMethodDecoder::ReadFloat64()
		{
			Double result = *((Double*)(methodCode.code+pos));
			pos += sizeof(Double);
			return result;
		}

		Int32 ILMethodDecoder::ReadSwitch()[]
		{
			Int32 count = ReadInt32();
			Int32 result[] = __gc new Int32[count];
			for (int i = 0; i < count; i++)
				result[i] = ReadInt32();
			return result;
		}

		Object *ILMethodDecoder::ReadToken()
		{
			Int32 tk = ReadInt32();
			Object *obj = mdHash->get_Item(__box(tk));
			
			if (obj)
				return obj;
			else
				return String::Format("{0}",__box(tk));
		}

		Type *ILMethodDecoder::GetLocalVarTypes()[]
		{
			int count = 0;
			if (methodCode.locVarBaseTypes != NULL)
				count = methodCode.locVarBaseTypes->Length;

			Type *result[] = new Type * [count];

			for (int i = 0; i < count; i++)
				result[i] = dynamic_cast <Type*> (methodCode.locVarBaseTypes[i]);

			return result;
		}
    }
}
