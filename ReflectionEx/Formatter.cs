
// ===========================================================================
// CILPE - Partial Evaluator for Common Intermediate Language
// ===========================================================================
// File: 
//     Formatter.cs
//
// Description:
//     Formatting for reflection objects
//
// Author: 
//     Sergei Skorobogatov (Sergei.Skorobogatov@supercompilers.com)
// ===========================================================================

using System;
using System.Reflection;

namespace CILPE.ReflectionEx
{
    /* Implements .Net formatting interfaces for reflection objects */
    public class ReflectionFormatter: ICustomFormatter, IFormatProvider
    {
        #region Internal and private members

        private enum ViewType { Tree, Body };
        private enum Style { CSharp, ILDasm, Short };

        private class FormatSpecifier
        {
            #region Internal and private members

            private bool isValid;
            private Style style;
            private ViewType view;
            private bool qualifiedNames;

            #endregion

            public FormatSpecifier(string format)
            {
                if (format == null)
                {
                    isValid = true;
                    style = Style.CSharp;
                    view = ViewType.Body;
                    qualifiedNames = true;
                }
                else
                {
                    string[] parts = format.Trim().Split(new char[] { ',', ' ' }, 2);

                    isValid = false;
                    if (parts.Length > 0)
                    {
                        if (parts[0].Equals("CSharp"))
                        {
                            isValid = true;
                            style = Style.CSharp;
                        }
                        else if (parts[0].Equals("ILDasm"))
                        {
                            isValid = true;
                            style = Style.ILDasm;
                        }
                        else if (parts[0].Equals("Short"))
                        {
                            isValid = true;
                            style = Style.Short;
                        }
                    }
                    
                    if (isValid)
                    {
                        if (parts.Length == 2)
                        {
                            if (parts[1].Equals("bq"))
                            {
                                view = ViewType.Body;
                                qualifiedNames = true;
                            }
                            else if (parts[1].Equals("b"))
                            {
                                view = ViewType.Body;
                                qualifiedNames = false;
                            }
                            else if (parts[1].Equals("tq"))
                            {
                                view = ViewType.Tree;
                                qualifiedNames = true;
                            }
                            else if (parts[1].Equals("t"))
                            {
                                view = ViewType.Tree;
                                qualifiedNames = false;
                            }
                            else
                            {
                                view = ViewType.Body;
                                qualifiedNames = true;
                            }
                        }
                        else
                        {
                            view = ViewType.Body;
                            qualifiedNames = true;
                        }
                    }
                }
            }

            public bool IsValid { get { return isValid; } }
            public Style Style { get { return style; } }
            public ViewType View { get { return view; } }
            public bool QualifiedNames { get { return qualifiedNames; } }
        }

        private string formatAssembly(FormatSpecifier format, Assembly assembly)
        {
            return format.Style == Style.ILDasm ? assembly.Location : assembly.FullName;
        }

        private string formatModule(FormatSpecifier format, Module module)
        {
            return format.Style == Style.ILDasm ? module.FullyQualifiedName : module.Name;
        }

        private string formatTypeName(FormatSpecifier format, Type type)
        {
            return format.QualifiedNames ? type.FullName : type.Name;
        }

        private string formatType(FormatSpecifier format, Type type, bool NameRes)
        {
            string result = null;

            if (type.HasElementType)
            {
                String elemType = formatType(format,type.GetElementType(),NameRes);

                if (type.IsArray)
                {
                    int rank = type.GetArrayRank();

                    if (rank == 1)
                        result = elemType + "[]";
                    else
                    {
                        result = elemType + '[';

                        for (int i = 0; i < rank; i++)
                        {
                            if (format.Style == Style.ILDasm)
                                result += "0...";

                            if (i < rank-1)
                                result += ',';
                        }

                        result += ']';
                    }
                }
                else if (type.IsByRef)
                {
                    if (format.Style == Style.ILDasm)
                        result = elemType + '&';
                    else
                        result = "ref " + elemType;
                }
            }
            else
            {
                if (format.View == ViewType.Tree)
                    result = format.Style == Style.ILDasm ? type.FullName : type.Name;
                else
                {
                    if (format.Style == Style.ILDasm)
                    {
                        if (type == typeof(void) && ! NameRes)
                            result = "void";
                        else if (type.IsPrimitive && ! NameRes)
                        {
                            if (type == typeof(bool))
                                result = "bool";
                            else if (type == typeof(char))
                                result = "char";
                            else if (type == typeof(float))
                                result = "float32";
                            else if (type == typeof(double))
                                result = "float64";
                            else if (type == typeof(sbyte))
                                result = "int8";
                            else if (type == typeof(short))
                                result = "int16";
                            else if (type == typeof(int))
                                result = "int32";
                            else if (type == typeof(long))
                                result = "int64";
                            else if (type == typeof(byte))
                                result = "unsigned int8";
                            else if (type == typeof(ushort))
                                result = "unsigned int16";
                            else if (type == typeof(uint))
                                result = "unsigned int32";
                            else if (type == typeof(ulong))
                                result = "unsigned int64";
                            else if (type == typeof(System.IntPtr))
                                result = "native int";
                            else if (type == typeof(System.UIntPtr))
                                result = "native unsigned int";
                        }
                        else if (type.IsValueType || type.IsEnum)
                        {
                            if (type == typeof(System.IntPtr) && ! NameRes)
                                result = "native int";
                            else if (type == typeof(System.UIntPtr) && ! NameRes)
                                result = "native unsigned int";
                            else if (type == typeof(System.TypedReference) && ! NameRes)
                                result = "typedref";
                            else
                                result = (NameRes ? "" : "valuetype ") + 
                                    formatTypeName(format,type);
                        }
                        else if (type.IsClass || type.IsInterface)
                        {
                            if (type == typeof(System.Object) && ! NameRes)
                                result = "object";
                            else if (type == typeof(System.String) && ! NameRes)
                                result = "string";
                            else
                                result = (NameRes ? "" : "class ") + 
                                    formatTypeName(format,type);
                        }
                    }
                    else
                    {
                        if (type == typeof(void) && ! NameRes)
                            result = "void";
                        else if (type.IsPrimitive && ! NameRes)
                        {
                            if (type == typeof(bool))
                                result = "bool";
                            else if (type == typeof(char))
                                result = "char";
                            else if (type == typeof(float))
                                result = "float";
                            else if (type == typeof(double))
                                result = "double";
                            else if (type == typeof(sbyte))
                                result = "sbyte";
                            else if (type == typeof(short))
                                result = "short";
                            else if (type == typeof(int))
                                result = "int";
                            else if (type == typeof(long))
                                result = "long";
                            else if (type == typeof(byte))
                                result = "byte";
                            else if (type == typeof(ushort))
                                result = "ushort";
                            else if (type == typeof(uint))
                                result = "uint";
                            else if (type == typeof(ulong))
                                result = "ulong";
                            else if (type == typeof(System.IntPtr))
                                result = "IntPtr";
                            else if (type == typeof(System.UIntPtr))
                                result = "IntPtr";
                        }
                        else if (type.IsValueType || type.IsEnum)
                        {
                            if (type == typeof(System.Decimal) && ! NameRes)
                                result = "decimal";
                            else
                                result = formatTypeName(format,type);
                        }
                        else if (type.IsClass || type.IsInterface)
                        {
                            if (type == typeof(System.Object) && ! NameRes)
                                result = "object";
                            else if (type == typeof(System.String) && ! NameRes)
                                result = "string";
                            else
                                result = formatTypeName(format,type);
                        }
                    }
                }
            }

            return result;
        }

        private string formatConstructorInfo(FormatSpecifier format, ConstructorInfo ctor)
        {
            string result = null;

            if (format.Style == Style.ILDasm)
            {
                if (format.View == ViewType.Tree)
                    result = ".ctor : void" + formatParameters(format,ctor.GetParameters());
                else
                    result = "instance void " + formatType(format,ctor.DeclaringType,true) +
                        "::.ctor" + formatParameters(format,ctor.GetParameters());
            }
            else
            {
                if (format.View == ViewType.Tree)
                    result = ctor.DeclaringType.Name + formatParameters(format,ctor.GetParameters());
                else
                {
                    if (format.Style == Style.CSharp)
                        result = formatType(format,ctor.DeclaringType,true) +
                            formatParameters(format,ctor.GetParameters());
                    else
                        result = formatType(format,ctor.DeclaringType,true) + "(.)";
                }
            }

            return result;
        }

        private string formatMethodInfo(FormatSpecifier format, MethodInfo method)
        {
            string result = null;

            if (format.Style == Style.ILDasm)
            {
                if (format.View == ViewType.Tree)
                    result = method.Name + " : " + formatType(format,method.ReturnType,false) +
                        ' ' + formatParameters(format,method.GetParameters());
                else
                    result = (method.IsStatic ? "" : "instance ") +
                        formatType(format,method.ReturnType,false) + ' ' +
                        (method.DeclaringType != null ? formatType(format,method.DeclaringType,true) + "::" : "") +
                        method.Name + formatParameters(format,method.GetParameters());
            }
            else
            {
                string declType = format.View == ViewType.Body && method.DeclaringType != null ? 
                    formatType(format,method.DeclaringType,true) + '.' : "";

                if (format.Style == Style.CSharp)
                    result = formatType(format,method.ReturnType,false) + ' ' + declType +
                        method.Name + formatParameters(format,method.GetParameters());
                else
                    result = declType + method.Name + "(.)";
            }

            return result;
        }

        private string formatFieldInfo(FormatSpecifier format, FieldInfo field)
        {
            string result = null;

            if (format.Style == Style.ILDasm)
            {
                if (format.View == ViewType.Tree)
                {
                    result = field.Name + " : ";
                    
                    if (field.IsPublic)
                        result += "public ";
                    else if (field.IsPrivate)
                        result += "private ";
                    else if (field.IsAssembly)
                        result += "assembly ";
                    else if (field.IsFamily)
                        result += "family ";
                    else if (field.IsFamilyAndAssembly)
                        result += "famandassem ";
                    else if (field.IsFamilyOrAssembly)
                        result += "famorassem ";

                    result += formatType(format,field.FieldType,false);
                }
                else
                    result = formatType(format,field.FieldType,false) + ' ' +
                        (field.DeclaringType != null ? formatType(format,field.DeclaringType,true) + "::" : "") +
                        field.Name;
            }
            else
            {
                string declType = format.View == ViewType.Body && field.DeclaringType != null ? 
                    formatType(format,field.DeclaringType,true) + '.' : "";

                if (format.Style == Style.CSharp)
                    result = formatType(format,field.FieldType,false) + ' ' + declType + field.Name;
                else
                    result = declType + field.Name;
            }

            return result;
        }

        private string formatParameterInfo(FormatSpecifier format, ParameterInfo parm)
        {
            Type type = parm.ParameterType;
            string result = null;

            if (type.IsByRef && parm.IsOut)
            {
                if (format.Style == Style.ILDasm)
                    result = "[out] " + formatType(format,type,false);
                else
                    result = "out " + formatType(format,type.GetElementType(),false);
            }
            else
                result = formatType(format,type,false);

             return result;
        }

        private string formatParameters(FormatSpecifier format, ParameterInfo[] parms)
        {
            string result = "(";

            for (int i = 0; i < parms.Length; i++)
                result += formatParameterInfo(format,parms[i]) + (i < parms.Length-1 ? ", " : "");

            return result + ')';
        }

        #endregion

        public static readonly ReflectionFormatter formatter = new ReflectionFormatter();

        /* Gets an object that provides formatting services for the specified type */
        public object GetFormat(Type service)
        {
            return service == typeof(ICustomFormatter) ? this : null;
        }

        /* Converts the value of a specified reflection object to an equivalent 
           string representation using specified format */
        public string Format(string format, object arg, IFormatProvider formatProvider)
        {
            if (arg == null)
                throw new ArgumentNullException("arg");

            string result = null;
            FormatSpecifier specifier = new FormatSpecifier(format);

            if (specifier.IsValid)
            {
                if (arg is Assembly)
                    result = formatAssembly(specifier,arg as Assembly);
                else if (arg is Module)
                    result = formatModule(specifier,arg as Module);
                else if (arg is Type)
                    result = formatType(specifier,arg as Type,false);
                else if (arg is ConstructorInfo)
                    result = formatConstructorInfo(specifier,arg as ConstructorInfo);
                else if (arg is MethodInfo)
                    result = formatMethodInfo(specifier,arg as MethodInfo);
                else if (arg is FieldInfo)
                    result = formatFieldInfo(specifier,arg as FieldInfo);
                else if (arg is ParameterInfo)
                    result = formatParameterInfo(specifier,arg as ParameterInfo);
                else if (arg is Instruction)
                    result = (arg as Instruction).ToString(format,this);
            }
            
            if (result == null)
                    result = arg.ToString();

            return result;
        }
    }
}
