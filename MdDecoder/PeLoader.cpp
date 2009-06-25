
// ===========================================================================
// CILPE - Partial Evaluator for Common Intermediate Language
// ===========================================================================
// File: 
//     PeLoader.cpp
//
// Description:
//     Loading PE file to memory, working with Unmanaged Metadata API
//
// Author: 
//     Sergei Skorobogatov (Sergei.Skorobogatov@supercompilers.com)
// ===========================================================================

#include "stdafx.h"

#include <stdlib.h>
#include <stdio.h>
#include <corhlpr.h>
#include "PeLoader.h"

#pragma comment(lib, "format.lib")

typedef CILPE::MdDecoder::MdImportHandle _MdImportHandle;

__nogc struct unmMdPair
{
	long token;
	unsigned short *name;
	long extra;
};

__nogc struct unmMethodProps
{
	unsigned short *name;
	long RVA;
    PCCOR_SIGNATURE sig;
};

__nogc struct unmTypeSpec
{
	long token;
	PCCOR_SIGNATURE sig;
};

static _MdImportHandle *unmOpenScope(unsigned char __nogc *peImage, long size);
static void unmCloseScope(_MdImportHandle *handle);
static void unmGetUserStrings(_MdImportHandle *handle, unmMdPair **str, int *count);
static void unmGetAssemblyRefs(_MdImportHandle *handle, unmMdPair **refs, int *count);
static void unmGetModuleToken(_MdImportHandle *handle, long *token);
static void unmGetModuleRefs(_MdImportHandle *handle, unmMdPair **refs, int *count);
static void unmGetTypeDefs(_MdImportHandle *handle, unmMdPair **defs, int *count);
static void unmGetTypeRefs(_MdImportHandle *handle, unmMdPair **refs, int *count);
static void unmGetMethods(_MdImportHandle *handle, long mdClass, unmMdPair **met, int *count);
static void unmGetMethodProps(_MdImportHandle *handle, long mdMethod, unmMethodProps **props);
static void unmGetFields(_MdImportHandle *handle, long mdClass, unmMdPair **fld, int *count);
static void unmGetMemberRefs(_MdImportHandle *handle, long mdClass, unmMdPair **refs, int *count);
static void unmGetTypeSpecs(_MdImportHandle *handle, unmTypeSpec **specs, int *count);
static PCCOR_SIGNATURE unmGetSigFromToken(_MdImportHandle *handle, long tk);

namespace CILPE
{
    namespace MdDecoder
    {
		using namespace System;
		using namespace System::Collections;
        using namespace System::Reflection;
		using namespace System::IO;
		using namespace System::Text;

		__gc class SignatureReader
		{
		private:
			const COR_SIGNATURE __nogc *sig;

		public:
			SignatureReader(const COR_SIGNATURE __nogc *sig);

			unsigned long ReadUlong();
			int ReadInt();
			int ReadToken();

            bool MatchUlong(unsigned long value);

            static Object *parseType(SignatureReader *sigReader, StringBuilder *decls);
            static void missCustomMod(SignatureReader *sigReader);
		};

		SignatureReader::SignatureReader(const COR_SIGNATURE __nogc *sig):
			sig(sig)
		{  }

		unsigned long SignatureReader::ReadUlong()
		{
			unsigned long result = 0;
			sig += CorSigUncompressData(sig,&result);
			return result;
		}

		int SignatureReader::ReadInt()
		{
			int result = 0;
			sig += CorSigUncompressSignedInt(sig,&result);
			return result;
		}

		int SignatureReader::ReadToken()
		{
			int result = 0;
			sig += CorSigUncompressToken(sig,(mdToken*)&result);
			return result;
		}

        bool SignatureReader::MatchUlong(unsigned long value)
        {
            const COR_SIGNATURE __nogc *marker = sig;
            unsigned long n = ReadUlong();

            bool result = n == value;
            if (! result)
                sig = marker;

            return result;
        }

        Object *SignatureReader::parseType(SignatureReader *sigReader, StringBuilder *decls)
		{
			Object *result = NULL;

			unsigned long data = sigReader->ReadUlong();
			switch (data)
			{
			case ELEMENT_TYPE_BOOLEAN:
				result = __typeof(System::Boolean);
				break;

			case ELEMENT_TYPE_CHAR:
				result = __typeof(System::Char);
				break;

			case ELEMENT_TYPE_I1:
				result = __typeof(System::SByte);
				break;

			case ELEMENT_TYPE_U1:
				result = __typeof(System::Byte);
				break;

			case ELEMENT_TYPE_I2:
				result = __typeof(System::Int16);
				break;

			case ELEMENT_TYPE_U2:
				result = __typeof(System::UInt16);
				break;

			case ELEMENT_TYPE_I4:
				result = __typeof(System::Int32);
				break;

			case ELEMENT_TYPE_U4:
				result = __typeof(System::UInt32);
				break;

			case ELEMENT_TYPE_I8:
				result = __typeof(System::Int64);
				break;

			case ELEMENT_TYPE_U8:
				result = __typeof(System::UInt64);
				break;

			case ELEMENT_TYPE_R4:
				result = __typeof(System::Single);
				break;

			case ELEMENT_TYPE_R8:
				result = __typeof(System::Double);
				break;

			case ELEMENT_TYPE_I:
                result = __typeof(System::IntPtr);
				break;

			case ELEMENT_TYPE_U:
                result = __typeof(System::UIntPtr);
				break;

			case ELEMENT_TYPE_VALUETYPE:
			case ELEMENT_TYPE_CLASS:
				result = __box(sigReader->ReadToken());
				break;

			case ELEMENT_TYPE_STRING:
				result = __typeof(System::String);
				break;

			case ELEMENT_TYPE_OBJECT:
				result = __typeof(Object);
				break;

			case ELEMENT_TYPE_PTR:
				result = NULL; //TODO fix it
				break;

			case ELEMENT_TYPE_FNPTR:
				/* throw ... */
				break;

			case ELEMENT_TYPE_ARRAY:
				{
					result = parseType(sigReader,decls);
					int rank = sigReader->ReadInt()+1;

					/*int numSizes = sigReader->ReadInt();
					int *sizes;
					if (numSizes > 0)
					{
						sizes = new int [numSizes];
						for (int i = 0; i < numSizes; i++)
							sizes[i] = sigReader->ReadInt();
					}*/

					decls->Append("[");
					for (int i = 0; i < rank-1; i++)
						decls->Append(",");
					decls->Append("]");
				}

				break;

			case ELEMENT_TYPE_SZARRAY:
				decls->Append("[]");
				result = parseType(sigReader,decls);
				break;
			}

			return result;
		}

        void SignatureReader::missCustomMod(SignatureReader *sigReader)
        {
            bool flag;
            do
            {
                flag = sigReader->MatchUlong(ELEMENT_TYPE_CMOD_OPT);
                if (! flag)
                    flag = sigReader->MatchUlong(ELEMENT_TYPE_CMOD_REQD);

                if (flag)
                    sigReader->ReadToken();
            }
            while (flag);
        }
		__nogc struct PeFileHeader
		{
			/* Always 0x14c */
			short Machine;

			/* Number of sections; indicates size of the Section Table, 
				which immediately follows the headers. */
			short SectionsNumber;

			/* Time and date the file was created in seconds since
				January 1st 1970 00:00:00 or 0. */
			long TimeDateStamp;

			/* Always 0 */
			long PtrToSymbolTable;

			/* Always 0 */
			long NumberOfSymbols;

			/* Size of the optional header */
			short OptionalHeaderSize;

			/* Flags indicating attributes of the file */
			short Characteristics;
		};

		__nogc struct SectionHeader
		{
			/* An 8-byte, null-padded ASCII string. There is no terminating null 
				if the string is exactly eight characters long. */
			char Name[8];

			/* Total size of the section when loaded into memory in bytes 
				rounded to Section Alignment. If this value is greater than 
				Size of Raw Data, the section is zero-padded. */
			long VirtualSize;

			/* For executable images this is the address of the first byte of 
				the section, when loaded into memory, relative to the image base. */
			long VirtualAddress;

			/* Size of the initialized data on disk in bytes, shall be a multiple of 
				FileAlignment from the PE header. If this is less than VirtualSize 
				the remainder of the section is zero filled. Because this field is 
				rounded while the VirtualSize field is not it is possible for this 
				to be greater than VirtualSize as well. When a section contains only 
				uninitialized data, this field should be 0. */
			long SizeOfRawData;

			/* RVA to section’s first page within the PE file. This shall be 
				a multiple of FileAlignment from the optional header. When a section 
				contains only uninitialized data, this field should be 0. */
			long PointerToRawData;

			/* RVA of Relocation section. */
			long PointerToRelocations;

			/* Always 0 */
			long PointerToLinenumbers;

			/* Number of relocations, set to 0 if unused. */
			short NumberOfRelocations;

			/* Always 0 */
			short NumberOfLinenumbers;

			/* Flags describing section’s characteristics. */
			long Characteristics;
		};

		/* Section’s characteristics flags */
		const int CNT_CODE = 0x00000020;                /* Section contains executable code. */
		const int CNT_INITIALIZED_DATA = 0x00000040;    /* Section contains initialized data. */
		const int CNT_UNINITIALIZED_DATA = 0x00000080;  /* Section contains uninitialized data. */
		const int MEM_EXECUTE = 0x20000000;             /* Section can be executed as code. */
		const int MEM_READ = 0x40000000;                /* Section can be read. */
		const int MEM_WRITE = 0x80000000;               /* Section can be written to. */

		__gc class CodeSection
		{
		private:
			long filePos, RVA, length;

		public:
			CodeSection(long filePos,long RVA,long length):
				filePos(filePos), RVA(RVA), length(length)
			{  }

			long RvaToFilePos(long rva)
			{
				return (rva >= RVA && rva < RVA+length) ? filePos+rva-RVA : -1;
			}
		};

        MethodSignature::MethodSignature(SignatureReader *sigReader, bool isMethodRef):
            sigReader(sigReader), isMethodRef(isMethodRef)
        {  
            /* Decoding first byte of the signature (calling conventions) */
            unsigned long firstByte = sigReader->ReadUlong();
            bool hasThis = (firstByte & IMAGE_CEE_CS_CALLCONV_HASTHIS) > 0,
                    explicitThis = (firstByte & IMAGE_CEE_CS_CALLCONV_EXPLICITTHIS) > 0,
                    varArg = (firstByte & 0x0F) == IMAGE_CEE_CS_CALLCONV_VARARG;
            callingConv = 
                (CallingConventions)(
                    (hasThis ? CallingConventions::HasThis : 0) |
                    (explicitThis ? CallingConventions::ExplicitThis : 0) |
                    (varArg ? CallingConventions::VarArgs : CallingConventions::Standard)
                );

            /* Reading the number of parameters */
            unsigned long sigParamCount = sigReader->ReadUlong();

            /* Missing RetType signature */
            SignatureReader::missCustomMod(sigReader);
            bool flag = sigReader->MatchUlong(ELEMENT_TYPE_VOID);
            if (! flag)
                flag = sigReader->MatchUlong(ELEMENT_TYPE_TYPEDBYREF);
            if (! flag)
            {
                sigReader->MatchUlong(ELEMENT_TYPE_BYREF);
                StringBuilder *decls = new StringBuilder("");
                SignatureReader::parseType(sigReader,decls);
            }

            /* Reading parameters */
            paramBaseTypes = new Object * [sigParamCount];
			paramDeclarators = new String * [sigParamCount];
            paramCount = 0;

            flag = true;
            for (unsigned long i = 0; i < sigParamCount && flag; i++)
            {
                if (sigReader->MatchUlong(ELEMENT_TYPE_SENTINEL))
                    flag = false;

                if (flag)
                {
                    SignatureReader::missCustomMod(sigReader);

                    if (sigReader->MatchUlong(ELEMENT_TYPE_TYPEDBYREF))
                    {
                        paramBaseTypes[i] = __typeof(TypedReference);
                        paramDeclarators[i] = "";
                    }
                    else
                    {
                        bool isByRef = sigReader->MatchUlong(ELEMENT_TYPE_BYREF);

						StringBuilder *decls = new StringBuilder("");
						paramBaseTypes[i] = SignatureReader::parseType(sigReader,decls);

						if (isByRef)
							decls->Append("&");

                        paramDeclarators[i] = decls->ToString();
                    }

                    if (i > 0 || ! explicitThis)
                        paramCount++;
                }
            }
        }

        bool MethodSignature::Matches(MethodBase *method)
        {
            bool result = false;

            if (method->CallingConvention == callingConv)
            {
                ParameterInfo *params[] = method->GetParameters();

                if (params->Length == (int)paramCount)
                {
                    result = true;

                    for (unsigned long i = 0; i < paramCount && result; i++)
                        result = paramTypes[i]->Equals(params[i]->ParameterType);
                }
            }

            return result;
        }

		PeLoader::PeLoader(String *fileName)
		{
			/* Reading PE file to unmanaged array */
			const unsigned short mode[3] = { 'r', 'b', 0 };
			unsigned short name[_MAX_PATH+1];
			for (int j = 0; j < fileName->Length; j++)
				name[j] = fileName->Chars[j];
			name[fileName->Length] = 0;
			
			FILE *f = _wfopen(name,mode);
			fseek(f,0,SEEK_END);
			peSize = ftell(f);
			fseek(f,0,SEEK_SET);
			peImage = __nogc new unsigned char [peSize];
			fread(peImage,1,peSize,f);
			fclose(f);

			/* The PE format starts with an MS-DOS stub of 128 bytes to be placed 
				at the front of the module. At offset 0x3c in the DOS header 
				is a 4 byte unsigned integer offset to the PE signature 
				(shall be “PE\0\0”), immediately followed by the PE file header. */
			unsigned long offset = *(unsigned long*)(peImage+0x3c);
			char *peSignature = (char *)(peImage+offset);

			/* PE file header is situated after PE signature */
			PeFileHeader __nogc *peFileHeader = (PeFileHeader __nogc*)(peSignature+4);

			/* PE file header may be followed by PE optional header. And after
				both PE file header and PE optional header there is Sections Table */
			SectionHeader __nogc *sections = (SectionHeader __nogc *)
				((char *)peFileHeader+sizeof(PeFileHeader)+peFileHeader->OptionalHeaderSize);
			short sectionsNumber = peFileHeader->SectionsNumber;

			/* Reading sections containing code */
			short i;
			int codeSectionsNum = 0;
			for (i = 0; i < sectionsNumber; i++)
				if (sections[i].Characteristics == (MEM_READ | CNT_CODE | MEM_EXECUTE))
					codeSectionsNum++;

			codeSections = __gc new CodeSection * [codeSectionsNum];

			codeSectionsNum = 0;
			for (i = 0; i < sectionsNumber; i++)
				if (sections[i].Characteristics == (MEM_READ | CNT_CODE | MEM_EXECUTE))
				{
					codeSections[codeSectionsNum++] = 
						new CodeSection(
							sections[i].PointerToRawData,
							sections[i].VirtualAddress,
							sections[i].VirtualSize
							);
				}

			/* Opening scope using Unmanaged Metadata API */
			mdImport = unmOpenScope(peImage,peSize);
		}

		PeLoader::~PeLoader()
		{
			unmCloseScope(mdImport);
			delete [] peImage;
		}

		static void convertPairs(unmMdPair *unmPairs, MdPair (*pairs)[], int count)
		{
			*pairs = new MdPair [count];

			for (int i = 0; i < count; i++)
			{
				(*pairs)[i].token = unmPairs[i].token;
				(*pairs)[i].extra = unmPairs[i].extra;
				(*pairs)[i].name = new String(unmPairs[i].name);
				delete [] unmPairs[i].name;
			}
		}

		void PeLoader::GetUserStrings(MdPair (*str)[])
		{
			int count;
			unmMdPair *unmStr;
			unmGetUserStrings(mdImport,&unmStr,&count);

			if (count > 0)
			{
				convertPairs(unmStr,str,count);
				delete [] unmStr;
			}
			else
				*str = new MdPair [0];
		}

		void PeLoader::GetAssemblyRefs(MdPair (*refs)[])
		{
			int count;
			unmMdPair *unmRefs;
			unmGetAssemblyRefs(mdImport,&unmRefs,&count);

			if (count > 0)
			{
				convertPairs(unmRefs,refs,count);
				delete [] unmRefs;
			}
			else
				*refs = new MdPair [0];
		}

        long PeLoader::GetModuleToken()
        {
            long token;
            unmGetModuleToken(mdImport,&token);
            return token;
        }

		void PeLoader::GetModuleRefs(MdPair (*refs)[])
		{
			int count;
			unmMdPair *unmRefs;
			unmGetModuleRefs(mdImport,&unmRefs,&count);

			if (count > 0)
			{
				convertPairs(unmRefs,refs,count);
				delete [] unmRefs;
			}
			else
				*refs = new MdPair [0];
		}

		void PeLoader::GetTypeDefs(MdPair (*defs)[])
		{
			int count;
			unmMdPair *unmDefs;
			unmGetTypeDefs(mdImport,&unmDefs,&count);

			if (count > 0)
			{
				convertPairs(unmDefs,defs,count);
				delete [] unmDefs;
			}
			else
				*defs = new MdPair [0];
		}

		void PeLoader::GetTypeRefs(MdPair (*refs)[])
		{
			int count;
			unmMdPair *unmRefs;
			unmGetTypeRefs(mdImport,&unmRefs,&count);

			if (count > 0)
			{
				convertPairs(unmRefs,refs,count);
				delete [] unmRefs;
			}
			else
				*refs = new MdPair [0];
		}

		void PeLoader::GetMethods(long mdClass, MdPair (*met)[])
		{
			int count;
			unmMdPair *unmMethods;
			unmGetMethods(mdImport,mdClass,&unmMethods,&count);

			if (count > 0)
			{
				convertPairs(unmMethods,met,count);
				delete [] unmMethods;
			}
			else
				*met = new MdPair [0];
		}

        MethodProps PeLoader::GetMethodProps(long mdMethod)
		{
            MethodProps props;
			unmMethodProps *unmProps;
			unmGetMethodProps(mdImport,mdMethod,&unmProps);

			props.name = new String(unmProps->name);
			delete [] unmProps->name;

            props.sig = new MethodSignature(new SignatureReader(unmProps->sig),false);

			long RVA = unmProps->RVA;
			delete unmProps;
			unsigned char __nogc *ilHeader = NULL;

			for (int i = 0; i < codeSections->Length && ilHeader == NULL; i++)
			{
				long filePos = codeSections[i]->RvaToFilePos(RVA);
				if (filePos != -1)
					ilHeader = peImage+filePos;
			}
			
			if (ilHeader != NULL)
			{
				COR_ILMETHOD *header = (COR_ILMETHOD*)ilHeader;
				COR_ILMETHOD_DECODER *decoder = new COR_ILMETHOD_DECODER(header);
				
				SignatureReader *sigReader = NULL;
				unsigned long localVarCount = 0;
				if (decoder->IsFat())
				{
					int sigToken = decoder->GetLocalVarSigTok();

					if (sigToken != 0)
					{
						PCCOR_SIGNATURE sig = unmGetSigFromToken(mdImport,sigToken);
						sigReader = new SignatureReader(sig);
						if (sigReader->ReadUlong() != IMAGE_CEE_CS_CALLCONV_LOCAL_SIG)
							/* throw ... */;
						else
							localVarCount = sigReader->ReadUlong();
					}
				}

				props.methodCode = 
					MethodCode(
						(int)(decoder->GetMaxStack()),
						(int)(decoder->GetCodeSize()),
						(unsigned char __nogc*)decoder->Code,
                        new EHDecoder(decoder),
						localVarCount
					);

				if (localVarCount > 0)
				{
					for (unsigned long i = 0; i < localVarCount; i++)
					{
						bool isPinned = sigReader->MatchUlong(ELEMENT_TYPE_PINNED);
                        bool isByRef = sigReader->MatchUlong(ELEMENT_TYPE_BYREF);

						StringBuilder *decls = new StringBuilder("");
						Object *baseType = SignatureReader::parseType(sigReader,decls);

						if (isByRef)
							decls->Append("&");

						props.methodCode.AddLocalVar(baseType,decls->ToString());
					}
				}
				
				delete decoder;
			}
			else
				props.methodCode = MethodCode();

            return props;
		}

		void PeLoader::GetFields(long mdClass, MdPair (*fld)[])
		{
			int count;
			unmMdPair *unmFields;
			unmGetFields(mdImport,mdClass,&unmFields,&count);

			if (count > 0)
			{
				convertPairs(unmFields,fld,count);
				delete [] unmFields;
			}
			else
				*fld = new MdPair [0];
		}

		void PeLoader::GetMemberRefs(long mdClass, MdMemberRef (*refs)[])
		{
			int count;
			unmMdPair *unmRefs;
			unmGetMemberRefs(mdImport,mdClass,&unmRefs,&count);

			if (count > 0)
			{
                MdPair pairs[];
				convertPairs(unmRefs,&pairs,count);
				delete [] unmRefs;

                *refs = new MdMemberRef [pairs->Length];
                for (int i = 0; i < pairs->Length; i++)
                {
                    (*refs)[i].token = pairs[i].token;
                    (*refs)[i].name = pairs[i].name;

                    SignatureReader *sigReader =
                        new SignatureReader((PCCOR_SIGNATURE)(pairs[i].extra));

                    if (sigReader->MatchUlong(IMAGE_CEE_CS_CALLCONV_FIELD))
                        (*refs)[i].sig = NULL;
                    else
                        (*refs)[i].sig = new MethodSignature(sigReader,true);
                }
			}
			else
				*refs = new MdMemberRef [0];
		}

		void PeLoader::GetTypeSpecs(MdTypeSpec (*specs)[])
		{
			int count;
			unmTypeSpec *unmSpecs;
			unmGetTypeSpecs(mdImport,&unmSpecs,&count);

			if (count > 0)
			{
				*specs = new MdTypeSpec [count];

				for (int i = 0; i < count; i++)
				{
					(*specs)[i].token = unmSpecs[i].token;

					PCCOR_SIGNATURE sig = unmSpecs[i].sig;
					SignatureReader *sigReader = new SignatureReader(sig);

					StringBuilder *decls = new StringBuilder("");
					Object *baseType = SignatureReader::parseType(sigReader,decls);

					(*specs)[i].baseType = baseType;
					(*specs)[i].decls = decls->ToString();
				}

				delete [] unmSpecs;
			}
			else
				*specs = new MdTypeSpec [0];
		}
	}
}

#pragma unmanaged

#include <windows.h>

static IMetaDataDispenser *dispenser = NULL;

static void Init()
{
    CoInitialize(NULL);

	HRESULT h;
    h = CoCreateInstance(CLSID_CorMetaDataDispenser,NULL,CLSCTX_INPROC_SERVER, 
			IID_IMetaDataDispenserEx,(void **)&dispenser);
}

struct CILPE::MdDecoder::MdImportHandle
{
	unsigned char *metadata;
	IMetaDataImport *mdimp;
	IMetaDataAssemblyImport *mdasimp;
};

static _MdImportHandle *unmOpenScope(unsigned char __nogc *peImage, long size)
{
	if (dispenser == NULL)
		Init();

	_MdImportHandle *result = new _MdImportHandle;

	result->metadata = peImage;
	result->mdimp = NULL;
	HRESULT h = dispenser->OpenScopeOnMemory(result->metadata,size,0,IID_IMetaDataImport,(IUnknown**)&(result->mdimp));

	if (h)
		result = NULL;
	else
	{
		result->mdasimp = NULL;
		h = dispenser->OpenScopeOnMemory(result->metadata,size,0,IID_IMetaDataAssemblyImport,(IUnknown**)&(result->mdasimp));

		if (h)
			result = NULL;
	}

	return result;
}

void unmCloseScope(_MdImportHandle *handle)
{
	handle->mdimp->Release();
	handle->mdasimp->Release();
}

static void unmGetUserStrings(_MdImportHandle *handle, unmMdPair **str, int *count)
{
	*str = NULL;
	*count = 0;

	mdToken tmp;
	HCORENUM Enum = 0;
	ULONG tokenCount;
	HRESULT h = handle->mdimp->EnumUserStrings(&Enum,&tmp,1,&tokenCount);

	if (h)
		return;

	h = handle->mdimp->CountEnum(Enum,(ULONG*)count);

	if (h)
		return;

	if (*count > 0)
	{
		mdToken *tokens = new mdToken [*count];
		tokens[0] = tmp;

		if (*count > 1)
		{
			h = handle->mdimp->EnumUserStrings(&Enum,tokens+1,*count-1,&tokenCount);

			if (h)
				return;
		}

		handle->mdimp->CloseEnum(Enum);

		*str = new unmMdPair [*count];
		for (int i = 0; i < *count; i++)
		{
			(*str)[i].token = tokens[i];

			unsigned short s[4096];
			ULONG len;
			handle->mdimp->GetUserString(tokens[i],s,1024,&len);

			(*str)[i].name = new unsigned short [len+1];
			for (int j = 0; j < (int)len; j++)
				(*str)[i].name[j] = s[j];
			(*str)[i].name[len] = 0;

			(*str)[i].extra = 0;
		}

		delete [] tokens;
	}
	else
		handle->mdimp->CloseEnum(Enum);
}

static void unmGetAssemblyRefs(_MdImportHandle *handle, unmMdPair **refs, int *count)
{
	*refs = NULL;
	*count = 0;

	HRESULT h;
	mdAssemblyRef tmp;
	HCORENUM Enum = 0;
	ULONG tokenCount;

	int size = 16, pos = 0;
	mdAssemblyRef *tokens = new mdAssemblyRef [size];

	do
	{
		h = handle->mdasimp->EnumAssemblyRefs(&Enum,&tmp,1,&tokenCount);

		//if (h)
		//	return;

		*count += tokenCount;

		if (tokenCount > 0)
		{
			tokens[pos++] = tmp;

			if (pos == size)
			{
				mdAssemblyRef *newTokens = new mdAssemblyRef [size+16];
				for (int i = 0; i < size; i++)
					newTokens[i] = tokens[i];
				size += 16;
				delete [] tokens;
				tokens = newTokens;
			}
		}
	}
	while (tokenCount > 0);

	handle->mdasimp->CloseEnum(Enum);

	if (*count > 0)
	{
		*refs = new unmMdPair [*count];

		for (int i = 0; i < *count; i++)
		{
			(*refs)[i].token = tokens[i];

			unsigned short assemblyName[1024];
			ULONG len;
			handle->mdasimp->GetAssemblyRefProps(
				tokens[i],
				NULL,
				NULL,
				assemblyName,
				1024,
				&len,
				NULL,
				NULL,
				NULL,
				NULL
				);

			(*refs)[i].name = new unsigned short [len+1];
			for (int j = 0; j < (int)len; j++)
				(*refs)[i].name[j] = assemblyName[j];
			(*refs)[i].name[len] = 0;

			(*refs)[i].extra = 0;
		}

		delete [] tokens;
	}
}

static void unmGetModuleToken(_MdImportHandle *handle, long *token)
{
    HRESULT h = handle->mdimp->GetModuleFromScope((mdModule*)token);

    if (h)
        *token = 0;
}

static void unmGetModuleRefs(_MdImportHandle *handle, unmMdPair **refs, int *count)
{
	*refs = NULL;
	*count = 0;

	mdModuleRef tmp;
	HCORENUM Enum = 0;
	ULONG tokenCount;
	HRESULT h = handle->mdimp->EnumModuleRefs(&Enum,&tmp,1,&tokenCount);

	if (h)
		return;

	h = handle->mdimp->CountEnum(Enum,(ULONG*)count);

	if (h)
		return;

	if (*count > 0)
	{
		mdModuleRef *tokens = new mdModuleRef [*count];
		tokens[0] = tmp;

		if (*count > 1)
		{
			h = handle->mdimp->EnumModuleRefs(&Enum,tokens+1,*count-1,&tokenCount);

			if (h)
				return;
		}

		handle->mdimp->CloseEnum(Enum);

		*refs = new unmMdPair [*count];
		for (int i = 0; i < *count; i++)
		{
			(*refs)[i].token = tokens[i];

			unsigned short moduleName[1024];
			ULONG len;
			handle->mdimp->GetModuleRefProps(tokens[i],moduleName,1024,&len);

			(*refs)[i].name = new unsigned short [len+1];
			for (int j = 0; j < (int)len; j++)
				(*refs)[i].name[j] = moduleName[j];
			(*refs)[i].name[len] = 0;

			(*refs)[i].extra = 0;
		}

		delete [] tokens;
	}
	else
		handle->mdimp->CloseEnum(Enum);
}

static void unmGetTypeDefs(_MdImportHandle *handle, unmMdPair **defs, int *count)
{
	*defs = NULL;
	*count = 0;

	mdTypeDef tmp;
	HCORENUM Enum = 0;
	ULONG tokenCount;
	HRESULT h = handle->mdimp->EnumTypeDefs(&Enum,&tmp,1,&tokenCount);

	if (h)
		return;

	h = handle->mdimp->CountEnum(Enum,(ULONG*)count);

	if (h)
		return;

	if (*count > 0)
	{
		mdTypeDef *tokens = new mdTypeDef [*count];
		tokens[0] = tmp;

		if (*count > 1)
		{
			h = handle->mdimp->EnumTypeDefs(&Enum,tokens+1,*count-1,&tokenCount);

			if (h)
				return;
		}

		handle->mdimp->CloseEnum(Enum);

		*defs = new unmMdPair [*count];
		for (int i = 0; i < *count; i++)
		{
			(*defs)[i].token = tokens[i];

			unsigned short typeName[1024];
			ULONG typeNameLen;
			DWORD typeDefFlags;
			mdToken tkSuperclass;
			handle->mdimp->GetTypeDefProps(
				tokens[i],
				typeName,
				1024,
				&typeNameLen,
				&typeDefFlags,
				&tkSuperclass
				);

			typeDefFlags &= tdVisibilityMask;
			if (typeDefFlags >= tdNestedPublic &&
				typeDefFlags <= tdNestedFamORAssem)
			{
				mdTypeDef tkEncloser;
				handle->mdimp->GetNestedClassProps(tokens[i],&tkEncloser);
				(*defs)[i].extra = tkEncloser;
			}
			else
				(*defs)[i].extra = 0;

			(*defs)[i].name = new unsigned short [typeNameLen+1];
			for (int j = 0; j < (int)typeNameLen; j++)
				(*defs)[i].name[j] = typeName[j];
			(*defs)[i].name[typeNameLen] = 0;
		}

		delete [] tokens;
	}
	else
		handle->mdimp->CloseEnum(Enum);
}

static void unmGetTypeRefs(_MdImportHandle *handle, unmMdPair **refs, int *count)
{
	*refs = NULL;
	*count = 0;

	mdTypeRef tmp;
	HCORENUM Enum = 0;
	ULONG tokenCount;
	HRESULT h = handle->mdimp->EnumTypeRefs(&Enum,&tmp,1,&tokenCount);

	if (h)
		return;

	h = handle->mdimp->CountEnum(Enum,(ULONG*)count);

	if (h)
		return;

	if (*count > 0)
	{
		mdTypeRef *tokens = new mdTypeRef [*count];
		tokens[0] = tmp;

		if (*count > 1)
		{
			h = handle->mdimp->EnumTypeRefs(&Enum,tokens+1,*count-1,&tokenCount);

			if (h)
				return;
		}

		handle->mdimp->CloseEnum(Enum);

		*refs = new unmMdPair [*count];
		for (int i = 0; i < *count; i++)
		{
			(*refs)[i].token = tokens[i];

			unsigned short typeName[1024];
			ULONG typeNameLen;
			mdToken tkResScope;
			handle->mdimp->GetTypeRefProps(
				tokens[i],
				&tkResScope,
				typeName,
				1024,
				&typeNameLen
				);

			(*refs)[i].extra = tkResScope;

			(*refs)[i].name = new unsigned short [typeNameLen+1];
			for (int j = 0; j < (int)typeNameLen; j++)
				(*refs)[i].name[j] = typeName[j];
			(*refs)[i].name[typeNameLen] = 0;
		}

		delete [] tokens;
	}
	else
		handle->mdimp->CloseEnum(Enum);
}

static void unmGetMethods(_MdImportHandle *handle, long mdClass, unmMdPair **met, int *count)
{
	*met = NULL;
	*count = 0;

	mdToken tmp;
	HCORENUM Enum = 0;
	ULONG tokenCount;
	HRESULT h = handle->mdimp->EnumMethods(&Enum,mdClass,&tmp,1,&tokenCount);

	if (h)
		return;

	h = handle->mdimp->CountEnum(Enum,(ULONG*)count);

	if (h)
		return;

	if (*count > 0)
	{
		mdToken *tokens = new mdToken [*count];
		tokens[0] = tmp;
		
		if (*count > 1)
		{
			h = handle->mdimp->EnumMethods(&Enum,mdClass,tokens+1,*count-1,&tokenCount);

			if (h)
				return;
		}

		handle->mdimp->CloseEnum(Enum);

		*met = new unmMdPair [*count];
		for (int i = 0; i < *count; i++)
		{
			(*met)[i].token = tokens[i];
			(*met)[i].name = NULL;
			(*met)[i].extra = 0;
		}

		delete [] tokens;
	}
	else
		handle->mdimp->CloseEnum(Enum);
}

static void unmGetMethodProps(_MdImportHandle *handle, long mdMethod, unmMethodProps **props)
{
	*props = NULL;

	unsigned short name[1024];
    PCCOR_SIGNATURE sig;
	ULONG len, RVA, sigLen;

	HRESULT h = handle->mdimp->GetMethodProps(
		mdMethod,
		NULL,
		name,1024,&len,
		NULL,
        &sig,
        &sigLen,
		&RVA,
		NULL
		);

	if (h)
		return;

	*props = new unmMethodProps;
	(*props)->name = new unsigned short [len+1];
	for (int i = 0; i <= (int)len; i++)
		(*props)->name[i] = name[i];

	(*props)->RVA = RVA;

    (*props)->sig = sig;
}

static void unmGetFields(_MdImportHandle *handle, long mdClass, unmMdPair **fld, int *count)
{
	*fld = NULL;
	*count = 0;

	mdToken tmp;
	HCORENUM Enum = 0;
	ULONG tokenCount;
	HRESULT h = handle->mdimp->EnumFields(&Enum,mdClass,&tmp,1,&tokenCount);

	if (h)
		return;

	h = handle->mdimp->CountEnum(Enum,(ULONG*)count);

	if (h)
		return;

	if (*count > 0)
	{
		mdToken *tokens = new mdToken [*count];
		tokens[0] = tmp;

		if (*count > 1)
		{
			h = handle->mdimp->EnumFields(&Enum,mdClass,tokens+1,*count-1,&tokenCount);

			if (h)
				return;
		}

		handle->mdimp->CloseEnum(Enum);

		*fld = new unmMdPair [*count];
		for (int i = 0; i < *count; i++)
		{
			(*fld)[i].token = tokens[i];
			(*fld)[i].extra = 0;

			unsigned short name[1024];
			ULONG len = 0;
			handle->mdimp->GetFieldProps(
				tokens[i],
				NULL,
				name,
				1024,
				&len,
				NULL,
				NULL,
				NULL,
				NULL,
				NULL,
				NULL
				);

			(*fld)[i].extra = len;

			(*fld)[i].name = new unsigned short [len+1];
			for (int j = 0; j < (int)len; j++)
				(*fld)[i].name[j] = name[j];
			//(*fld)[i].name[len] = 0;
		}

		delete [] tokens;
	}
	else
		handle->mdimp->CloseEnum(Enum);
}

static void unmGetMemberRefs(_MdImportHandle *handle, long mdClass, unmMdPair **refs, int *count)
{
	*refs = NULL;
	*count = 0;

	mdToken tmp;
	HCORENUM Enum = 0;
	ULONG tokenCount;
	HRESULT h = handle->mdimp->EnumMemberRefs(&Enum,mdClass,&tmp,1,&tokenCount);

	if (h)
		return;

	h = handle->mdimp->CountEnum(Enum,(ULONG*)count);

	if (h)
		return;

	if (*count > 0)
	{
		mdToken *tokens = new mdToken [*count];
		tokens[0] = tmp;

		if (*count > 1)
		{
			h = handle->mdimp->EnumMemberRefs(&Enum,mdClass,tokens+1,*count-1,&tokenCount);

			if (h)
				return;
		}

		handle->mdimp->CloseEnum(Enum);

		*refs = new unmMdPair [*count];
		for (int i = 0; i < *count; i++)
		{
			(*refs)[i].token = tokens[i];

			unsigned short name[1024];
            PCCOR_SIGNATURE sig;
			ULONG len, sigLen;
			handle->mdimp->GetMemberRefProps(
				tokens[i],
				NULL,
				name,
				1024,
				&len,
				&sig,
				&sigLen
				);

			(*refs)[i].name = new unsigned short [len+1];
			for (int j = 0; j < (int)len; j++)
				(*refs)[i].name[j] = name[j];
			(*refs)[i].name[len] = 0;

            (*refs)[i].extra = (long)sig;
		}

		delete [] tokens;
	}
	else
		handle->mdimp->CloseEnum(Enum);
}

void unmGetTypeSpecs(_MdImportHandle *handle, unmTypeSpec **specs, int *count)
{
	*specs = NULL;
	*count = 0;

	mdTypeSpec tmp;
	HCORENUM Enum = 0;
	ULONG tokenCount;
	HRESULT h = handle->mdimp->EnumTypeSpecs(&Enum,&tmp,1,&tokenCount);

	if (h)
		return;

	h = handle->mdimp->CountEnum(Enum,(ULONG*)count);

	if (h)
		return;

	if (*count > 0)
	{
		mdTypeSpec *tokens = new mdTypeSpec [*count];
		tokens[0] = tmp;

		if (*count > 1)
		{
			h = handle->mdimp->EnumTypeSpecs(&Enum,tokens+1,*count-1,&tokenCount);

			if (h)
				return;
		}

		handle->mdimp->CloseEnum(Enum);

		*specs = new unmTypeSpec [*count];
		for (int i = 0; i < *count; i++)
		{
			(*specs)[i].token = tokens[i];

			ULONG len;
			PCCOR_SIGNATURE sig;
			handle->mdimp->GetTypeSpecFromToken(tokens[i],&sig,&len);

			(*specs)[i].sig = sig;
		}

		delete [] tokens;
	}
	else
		handle->mdimp->CloseEnum(Enum);
}

PCCOR_SIGNATURE unmGetSigFromToken(_MdImportHandle *handle, long tk)
{
	PCCOR_SIGNATURE result;
	ULONG count;
	handle->mdimp->GetSigFromToken(tk,&result,&count);
	return result;
}
