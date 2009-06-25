
// ===========================================================================
// CILPE - Partial Evaluator for Common Intermediate Language
// ===========================================================================
// File: 
//     PeLoader.h
//
// Description:
//     Loading PE file to memory, working with Unmanaged Metadata API
//
// Author: 
//     Sergei Skorobogatov (Sergei.Skorobogatov@supercompilers.com)
// ===========================================================================

#pragma once

#include "MethodCode.h"

namespace CILPE
{
    namespace MdDecoder
    {
		using namespace System;
		using namespace System::Collections;
        using namespace System::Reflection;
		using namespace System::Text;

		__gc class CodeSection;
		__gc class SignatureReader;

		struct MdImportHandle;

		public __value struct MdPair
		{
			long token;
			String *name;
			long extra;
		};

        __gc class SignatureReader;

        public __gc class MethodSignature
        {
        private:
            SignatureReader *sigReader;
            bool isMethodRef;

            CallingConventions callingConv;

        public:
            Object *paramBaseTypes[];
			String *paramDeclarators[];

            unsigned long paramCount;
            Type *paramTypes[];

            MethodSignature(SignatureReader *sigReader, bool isMethodRef);
            bool Matches(MethodBase *method);
        };

		public __value struct MethodProps
		{
			String *name;
			MethodCode methodCode;
            MethodSignature *sig;
		};

        public __value struct MdMemberRef
        {
            long token;
            String *name;
            MethodSignature *sig;
        };

		public __value struct MdTypeSpec
		{
			long token;
			Object *baseType;
			String *decls;
		};

		public __gc class PeLoader
		{
		private:
			unsigned char __nogc *peImage;
			long peSize;
			MdImportHandle *mdImport;

			CodeSection *codeSections[];

		public:
			PeLoader(String *fileName);
			~PeLoader();

			void GetUserStrings(MdPair (*str)[]);
			void GetAssemblyRefs(MdPair (*refs)[]);
            long GetModuleToken();
			void GetModuleRefs(MdPair (*refs)[]);
			void GetTypeDefs(MdPair (*defs)[]);
			void GetTypeRefs(MdPair (*refs)[]);
			void GetMethods(long mdClass, MdPair (*met)[]);
			MethodProps GetMethodProps(long mdMethod);
			void GetFields(long mdClass, MdPair (*fld)[]);
			void GetMemberRefs(long mdClass, MdMemberRef (*refs)[]);
			void GetTypeSpecs(MdTypeSpec (*specs)[]);
		};
	}
}
