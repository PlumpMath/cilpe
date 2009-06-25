
// ===========================================================================
// ILPE - Partial Evaluator for Common Intermediate Language
// ===========================================================================
// File: 
//     ILMethodDecoder.h
//
// Description:
//     Reading IL method bodies
//
// Author: 
//     Sergei Skorobogatov
// ===========================================================================

#pragma once

namespace CILPE
{
    namespace MdDecoder
    {
        using namespace System;

	    public __gc class ILMethodDecoder
	    {
        private:
			static const doubleByteCodesOrigin = 0xE1;

            int pos;
            MethodCode methodCode;
            Hashtable *mdHash;

		public:
            /*ILMethodDecoder(MethodBase *method, Module *module);*/
            ILMethodDecoder(MethodCode methodCode, Hashtable *mdHash);
			~ILMethodDecoder();

            __property bool get_IsIL() { return methodCode.code != NULL; }
			__property int get_CodeSize() { return methodCode.codeSize; }
            __property int get_MaxStack() { return methodCode.maxStack; }

			void Reset() { pos = 0; }
			int GetOffset() { return pos; }
			bool EndOfCode() { return pos == methodCode.codeSize; }
			int ReadCode();

			Int16 ReadInt8();
			Int32 ReadInt32();
			Int64 ReadInt64();
			Byte ReadUint8();
			Int32 ReadUint16();
			Single ReadFloat32();
			Double ReadFloat64();
			Int32 ReadSwitch()[];
			Object *ReadToken();

            EHDecoder *GetEHDecoder() { return methodCode.ehDecoder; }
			Type *GetLocalVarTypes()[];
	    };
    }
}
