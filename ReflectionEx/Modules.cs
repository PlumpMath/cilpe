
// ===========================================================================
// CILPE - Partial Evaluator for Common Intermediate Language
// ===========================================================================
// File: 
//     Modules.cs
//
// Description:
//     Reflection extensions for modules
//
// Author: 
//     Sergei Skorobogatov (Sergei.Skorobogatov@supercompilers.com)
// ===========================================================================

using System;
using System.Collections;
using System.Reflection;
using System.IO;
using CILPE.MdDecoder;

namespace CILPE.ReflectionEx
{
    /* Class, that extends functionality of System.Reflection.Module class,
     * allowing to access method bodies and related data.
     */
    public class ModuleEx: IEnumerable
    {
        #region Private and internal members

        private Module module;
        private Hashtable bodiesHash;

        private Type formType(Hashtable hash, object baseType, string decls)
        {
            Type result = baseType as Type;

            if (result == null)
                result = hash[baseType] as Type;

            if (! decls.Equals(""))
            {
                string typeName = string.Concat(result.FullName,decls);
                result = result.Module.GetType(typeName);
            }

            return result;
        }

        private void fixLocalVars(Hashtable hash, MethodCode methodCode)
        {
            if (methodCode.locVarBaseTypes != null)
                for (int i = 0; i < methodCode.locVarBaseTypes.Length; i++)
                {
                    methodCode.locVarBaseTypes[i] =
                        formType(
                            hash,
                            methodCode.locVarBaseTypes[i],
                            methodCode.locVarDeclarators[i]
                            );
                }
        }

        private void fixParameters(Hashtable hash, MethodSignature sig)
        {
            sig.paramTypes = new Type [sig.paramCount];

            for (ulong i = 0; i < sig.paramCount; i++)
            {
                sig.paramTypes[i] =
                    formType(
                        hash,
                        sig.paramBaseTypes[i],
                        sig.paramDeclarators[i]
                        );
            }
        }

        #endregion

        /* Creates new instance of ModuleEx class */
        public ModuleEx(Module module)
        {
            int i;
            this.module = module;
            Hashtable hash = new Hashtable();
            bodiesHash = new Hashtable();

            string moduleLocation = module.FullyQualifiedName;
            PeLoader peLoader = new PeLoader(moduleLocation);

            /* Adding user strings to hash */
            MdPair[] userStrings = null;
            peLoader.GetUserStrings(ref userStrings);
            int userStringsCount = userStrings.Length;
            for (i = 0; i < userStringsCount; i++)
                hash.Add(userStrings[i].token,userStrings[i].name);

            /* Reading assembly references */
            MdPair[] assemblyRefs = null;
            peLoader.GetAssemblyRefs(ref assemblyRefs);
            int assemblyRefsCount = assemblyRefs.Length;

            /* Loading referenced assemblies */
            Hashtable assemblyHash = new Hashtable();
            AssemblyName[] assemblyRefNames = module.Assembly.GetReferencedAssemblies();
                for (i = 0; i < assemblyRefNames.Length; i++)
                {
                    Assembly referencedAssembly = Assembly.Load(assemblyRefNames[i]);
                    assemblyHash.Add(
                        assemblyRefNames[i].Name,
                        referencedAssembly
                        );
                }

            /* Adding assembly references to hash */
            for (i = 0; i < assemblyRefsCount; i++)
            {
                Assembly assembly = assemblyHash[assemblyRefs[i].name] as Assembly;

                if (assembly != null)
                    hash.Add(assemblyRefs[i].token,assembly);
            }

            /* Making hash table of modules */
            Assembly[] assemblyArray = new Assembly [assemblyHash.Count+1];
            assemblyArray[0] = module.Assembly;
            assemblyHash.Values.CopyTo(assemblyArray,1);

            Hashtable moduleHash = new Hashtable();
            for (i = 0; i < assemblyArray.Length; i++)
            {
                Module[] moduleArray = assemblyArray[i].GetModules();
                for (int j = 0; j < moduleArray.Length; j++)
                    moduleHash.Add(moduleArray[j].Name,moduleArray[j]);
            }

            /* Adding current module to hash */
            hash.Add(peLoader.GetModuleToken(),module);

            /* Reading module references */
            MdPair[] moduleRefs = null;
            peLoader.GetModuleRefs(ref moduleRefs);
            int moduleRefsCount = moduleRefs.Length;

            /* Adding modules to hash */
            for (i = 0; i < moduleRefsCount; i++)
            {
                Module mod = moduleHash[moduleRefs[i].name] as Module;

                if (mod != null)
                    hash.Add(moduleRefs[i].token,mod);
            }

            /* Reading type references */
            MdPair[] typeRefs = null;
            peLoader.GetTypeRefs(ref typeRefs);
            int typeRefsCount = typeRefs.Length;

            /* Adding not nested refrenced types to hash */
            for (i = 0; i < typeRefsCount; i++)
            {
                object container = hash[typeRefs[i].extra];
                Assembly assembly = container as Assembly;
                Module mod = container as Module;

                if (assembly != null || mod != null)
                    typeRefs[i].extra = 0;

                if (assembly != null)
                    hash.Add(typeRefs[i].token,assembly.GetType(typeRefs[i].name));
                else if (mod != null)
                    hash.Add(typeRefs[i].token,mod.GetType(typeRefs[i].name));
            }

            /* Adding nested refrenced types to hash */
            bool flag = true;
            while (flag)
            {
                flag = false;

                for (i = 0; i < typeRefsCount; i++)
                    if (typeRefs[i].extra != 0)
                    {
                        Type encloser = hash[typeRefs[i].extra] as Type;

                        if (encloser == null)
                            flag = true;
                        else
                        {
                            typeRefs[i].extra = 0;
                            Type type = 
                                encloser.GetNestedType(
                                    typeRefs[i].name,
                                    BindingFlags.Public | BindingFlags.NonPublic
                                    );

                            hash.Add(typeRefs[i].token,type);
                        }
                    }
            }

            /* Reading type definitions */
            MdPair[] typeDefs = null;
            peLoader.GetTypeDefs(ref typeDefs);
            int typeDefsCount = typeDefs.Length;

            /* Adding not nested defined types to hash */
            for (i = 0; i < typeDefsCount; i++)
                if (typeDefs[i].extra == 0)
                    hash.Add(typeDefs[i].token,module.Assembly.GetType(typeDefs[i].name));

            /* Adding nested defined types to hash */
            flag = true;
            while (flag)
            {
                flag = false;

                for (i = 0; i < typeDefsCount; i++)
                    if (typeDefs[i].extra != 0)
                    {
                        Type encloser = hash[typeDefs[i].extra] as Type;

                        if (encloser == null)
                            flag = true;
                        else
                        {
                            typeDefs[i].extra = 0;
                            Type type = 
                                encloser.GetNestedType(
                                    typeDefs[i].name,
                                    BindingFlags.Public | BindingFlags.NonPublic
                                    );

                            hash.Add(typeDefs[i].token,type);
                        }
                    }
            }

            /* Reading type specs */
            MdTypeSpec[] typeSpecs = null;
            peLoader.GetTypeSpecs(ref typeSpecs);
            int typeSpecsCount = typeSpecs.Length;

            /* Adding type specs to hash */
            for (i = 0; i < typeSpecsCount; i++)
            {
                Type t = formType(hash,typeSpecs[i].baseType,typeSpecs[i].decls);
                hash.Add(typeSpecs[i].token,t);
            }

            /* Adding members (methods and fields) for every type */
            int[] tokens = new int[typeDefsCount+typeRefsCount+typeSpecsCount];
            for (i = 0; i < typeDefsCount; i++)
                tokens[i] = typeDefs[i].token;
            for (i = 0; i < typeRefsCount; i++)
                tokens[typeDefsCount+i] = typeRefs[i].token;
            for (i = 0; i < typeSpecsCount; i++)
                tokens[typeDefsCount+typeRefsCount+i] = typeSpecs[i].token;

            for (i = 0; i < tokens.Length; i++)
            {
                Int32 tkTyp = tokens[i];
                Type typ = hash[tkTyp] as Type;

                MdMemberRef[] members = null;
                peLoader.GetMemberRefs(tkTyp,ref members);
                int membersCount = members.Length;

                for (int j = 0; j < membersCount; j++)
                {
                    MemberInfo[] instanceMemb = 
                        typ.GetMember(
                            members[j].name,
                            MemberTypes.Constructor | MemberTypes.Method | MemberTypes.Field,
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                            );

                    MemberInfo[] staticMemb = 
                        typ.GetMember(
                            members[j].name,
                            MemberTypes.Constructor | MemberTypes.Method | MemberTypes.Field,
                            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
                            );

                    int membCount = instanceMemb.Length + staticMemb.Length;

                    if (membCount > 0)
                    {
                        MemberInfo[] memb = new MemberInfo [membCount];
                        instanceMemb.CopyTo(memb,0);
                        staticMemb.CopyTo(memb,instanceMemb.Length);

                        if (members[j].sig == null)
                            hash.Add(members[j].token,memb[0]);
                        else
                        {
                            fixParameters(hash,members[j].sig);

                            flag = true;
                            for (int k = 0; k < membCount && flag; k++)
                                if (memb[k] is MethodBase)
                                {
                                    MethodBase method = memb[k] as MethodBase;

                                    if (members[j].sig.Matches(method))
                                    {
                                        hash.Add(members[j].token,memb[k]);
                                        flag = false;
                                    }
                                }
                        }
                    }
                    //else
                    //    throw ...;
                }
            }

            /* Adding fields for every defined type */
            for (i = 0; i < typeDefsCount; i++)
            {
                Int32 tkTyp = typeDefs[i].token;
                Type typ = hash[tkTyp] as Type;

                MdPair[] fields = null;
                peLoader.GetFields(tkTyp,ref fields);
                int fieldsCount = fields.Length;

                for (int j = 0; j < fieldsCount; j++)
                {
                    MemberInfo[] members = 
                        typ.GetMember(
                        fields[j].name,
                        MemberTypes.Field,
                        BindingFlags.DeclaredOnly | BindingFlags.Instance |
                        BindingFlags.Public | BindingFlags.NonPublic
                        );

                    if (members.Length == 0)
                        members = 
                            typ.GetMember(
                            fields[j].name,
                            MemberTypes.Field,
                            BindingFlags.DeclaredOnly | BindingFlags.Static | 
                            BindingFlags.Public | BindingFlags.NonPublic
                            );

                    if (members.Length > 0)
                        hash.Add(fields[j].token,members[0]);
                }
            }

            /* Adding methods for every defined type */
            Hashtable baseToProps = new Hashtable();
            for (i = 0; i < typeDefsCount; i++)
            {
                Int32 tkTyp = typeDefs[i].token;
                Type typ = hash[tkTyp] as Type;

                MdPair[] methods = null;
                peLoader.GetMethods(tkTyp,ref methods);
                int methodsCount = methods.Length;

                for (int j = 0; j < methodsCount; j++)
                {
                    MethodProps props = peLoader.GetMethodProps(methods[j].token);

                    MemberInfo[] instanceMembers = 
                        typ.GetMember(
                            props.name,
                            MemberTypes.Constructor | MemberTypes.Method,
                            BindingFlags.DeclaredOnly | BindingFlags.Instance | 
                            BindingFlags.Public | BindingFlags.NonPublic
                            );

                    MemberInfo[] staticMembers = 
                        typ.GetMember(
                            props.name,
                            MemberTypes.Constructor | MemberTypes.Method,
                            BindingFlags.DeclaredOnly | BindingFlags.Static |
                            BindingFlags.Public | BindingFlags.NonPublic
                            );

                    int membersCount = instanceMembers.Length + staticMembers.Length;

                    if (membersCount > 0)
                    {
                        MethodBase[] members = new MethodBase [membersCount];
                        instanceMembers.CopyTo(members,0);
                        staticMembers.CopyTo(members,instanceMembers.Length);

                        fixParameters(hash,props.sig);

                        flag = true;
                        for (int k = 0; k < membersCount && flag; k++)
                            if (props.sig.Matches(members[k]))
                            {
                                hash.Add(methods[j].token,members[k]);

                                if (props.methodCode.codeSize != 0)
                                    baseToProps.Add(members[k],props);

                                flag = false;
                            }
                    }
                    //else
                    //    throw ...;
                }
            }

            /* Adding defined global methods */
            MdPair[] globalMethods = null;
            peLoader.GetMethods(0,ref globalMethods);
            int globalMethodsCount = globalMethods.Length;

            MethodBase[] globalMethodsObj = module.GetMethods();

            for (i = 0; i < globalMethodsCount; i++)
            {
                MethodProps props =  peLoader.GetMethodProps(globalMethods[i].token);
                fixParameters(hash,props.sig);

                flag = true;
                for (int j = 0; j < globalMethodsObj.Length && flag; j++)
                {
                    if (props.name.Equals(globalMethodsObj[j].Name))
                        if (props.sig.Matches(globalMethodsObj[j]))
                        {
                            hash.Add(globalMethods[i].token,globalMethodsObj[j]);

                            if (props.methodCode.codeSize != 0)
                                baseToProps.Add(globalMethodsObj[j],props);

                            flag = false;
                        }
                }
            }

            /* Reading bodies of defined methods */
            foreach (DictionaryEntry pair in baseToProps)
            {
                MethodBase method = pair.Key as MethodBase;
                MethodProps props = (MethodProps)(pair.Value);

                fixLocalVars(hash,props.methodCode);
                
                if (props.methodCode.ehDecoder != null)
                    props.methodCode.ehDecoder.FixParams(hash);
                
                bodiesHash.Add(
                    method,
                    new MethodEx(method,new ILMethodDecoder(props.methodCode,hash))
                    );
            }

            /* Debug output */
//            DictionaryEntry[] tokens = new DictionaryEntry [hash.Count];
//            hash.CopyTo(tokens,0);
//
//            StreamWriter w = new StreamWriter("debug_tokens.txt");
//            for (i = 0; i < hash.Count; i++)
//            {
//                Int32 key = (Int32)(tokens[i].Key);
//                object value = tokens[i].Value;
//                w.Write(key.ToString("X"));
//                w.Write(" -> ");
//                w.Write(value.GetType().ToString());
//                w.Write(" -> ");
//                w.WriteLine(value.ToString());
//            }
//
//            w.Close();
        }

        /* Reflection object for represented module */
        public Module Module { get { return module; } }

        /* Returns MethodEx object corresponding to specified method.
         * MethodEx object allows to read method body and related data.
         */
        public MethodEx GetMethodEx(MethodBase method) 
        { 
            return bodiesHash[method] as MethodEx; 
        }

        /* Returns an enumerator that can iterate through methods */
        public IEnumerator GetEnumerator() 
        { 
            return bodiesHash.Keys.GetEnumerator(); 
        }
    }
}
