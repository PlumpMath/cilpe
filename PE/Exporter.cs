using System;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using System.IO; //Directory

using CILPE.Spec;
using CILPE.ReflectionEx;
using CILPE.CFG;

using CILPE.Config; //Andrew: SpecializeAttribute. ZLP, waiting for Sergei...

namespace CILPE
{
	public class ExportException : Exception
	{
		public ExportException()
		{}
	}

	internal class Set : IEnumerable
	{
		private Hashtable data;
			
		public Set()
		{
			data = new Hashtable();
		}

		public Set(IEnumerable objs)
		{
			data = new Hashtable();
			foreach(object o in objs)
				Add(o);
		}

		public void Add(object o)
		{
			data[o] = null;
		}

		public void Remove(object o)
		{
			data.Remove(o);
		}

		public bool Contains(object o)
		{
			return(data.ContainsKey(o));
		}

		public IEnumerator GetEnumerator()
		{
			return(data.Keys.GetEnumerator());
		}

	}

	public class Exporter
	{
		#region Member access attributes modification

		private static MethodAttributes AddInternalAttribute(MethodAttributes attr)
		{
			MethodAttributes memberAccess = MethodAttributes.MemberAccessMask & attr;
			switch(memberAccess)
			{
				case MethodAttributes.Family:
					memberAccess = MethodAttributes.FamORAssem;
					break;
				case MethodAttributes.Private:
					memberAccess = MethodAttributes.Assembly;
					break;
			}
			return(memberAccess | (~MethodAttributes.MemberAccessMask & attr));
		}

		private static FieldAttributes AddInternalAttribute(FieldAttributes attr)
		{
			FieldAttributes memberAccess = FieldAttributes.FieldAccessMask & attr;
			switch(memberAccess)
			{
				case FieldAttributes.Family:
					memberAccess = FieldAttributes.FamORAssem;
					break;
				case FieldAttributes.Private:
					memberAccess = FieldAttributes.Assembly;
					break;
			}
			return(memberAccess | (~FieldAttributes.FieldAccessMask & attr) );
		}

		#endregion

		static private Type[] GetDeclaredInterfaces(Type type)
		{
			Type[] ifaces1 = type.GetInterfaces();
			/*if(type.IsInterface) 
				return(ifaces1);
			Type[] ifaces2 = type.BaseType.GetInterfaces();
			Type[] ifaces3 = new Type[ifaces1.Length - ifaces2.Length];
			for(int i=0,k=0;i<ifaces1.Length;i++)
			{
				int j = 0;
				for(;j<ifaces2.Length;j++)
				{
					if(ifaces2[j] == ifaces1[i])
						break;
				}
				if(j == ifaces2.Length) //not found
				{
					ifaces3[k] = ifaces1[i];
					k++;
				}
			}
			return(ifaces3);*/
			return(ifaces1);

		}

		static private MethodBodyBlock CreateBody(MethodEx methodEx)
		{
			if(!methodEx.IsVerified)
				return null;
			MethodBodyBlock body;
    		body = Converter.Convert(methodEx);
			return(body);
		}

		static private void ProcessMethod(ILGenerator generator, MethodEx methodEx, MethodBase method, MetaDataMapper mapper)
		{
			if(method.IsAbstract)
				return;
			MethodBodyBlock body = null;
			if(methodEx == null)
				throw new ExportException();

			body = CreateBody(methodEx);
			if(body == null)
				throw new ExportException();

			MetaDataResolver.Map(body,mapper);
			bool verified = CFGVerifier.Check(body);
			if(!verified)
				throw new ExportException();

			Emitter.Emit(generator,body);
		}

		static private int ToInt(object val, int deflt)
		{
			if(val == null)
				return(deflt);
			return((int)val);
		}

		static private void Sort(Type[] types)
		{
			//fixes nesting-nested and base-descendants constraints on type creation order
			//TODO: try to satisfy struct-fields constraint if possible
			Hashtable indexes = new Hashtable(); //Type -> int mapping

			for(int i=0; i<types.Length; i++)
				indexes[types[i]] = i;

			bool more;
			do
			{
				more = false;
				for(int I=0; I<types.Length; I++)
				{
					int i = I;
					Type type = types[i];
					Type baseType = type.BaseType;
					int k = ToInt(indexes[baseType], -1); //-1 if base class is in a different assembly, mb mscorelib
					if(k>i)
					{
						types[k] = type;
						indexes[type] = k;
						types[i] = baseType;
						indexes[baseType] = i;
						more = true;
						i = k;
					}
					Type[] nested = type.GetNestedTypes(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic);
					for(int j=0; j<nested.Length; j++)
					{
						Type nestedType = nested[j];
						k = (int)indexes[nestedType]; //Nested type should be within the same assembly
						if(k<i)
						{
							types[k] = type;
							indexes[type] = k;
							types[i] = nestedType;
							indexes[nestedType] = i;
							more = true;
							i = k;
						}
					}
				}
			}
			while(more);
		}

		static private TypeBuilder[] EmitSource(Module module, ModuleBuilder moduleB, Set replacedMethods, MetaDataMapper mapper)
		{
			ModuleEx moduleEx = new ModuleEx(module);
    	
			ArrayList allMethods = new ArrayList(); 
			ArrayList allCtors = new ArrayList(); 
			ArrayList allTypes = new ArrayList(); 

			Hashtable map = mapper.MapTable;
			//FieldInfo -> FieldBuilder, MethodInfo -> MethodBuilder... mapping
			//Type -> Type mapping is performed through moduleB.GetType(), because of array & ref types

			Type[] types = module.GetTypes();
			Sort(types);
		
			foreach(Type type in types)
			{
				TypeBuilder typeB;
				if(type.DeclaringType != null)
				{
					typeB = (mapper.Map(type.DeclaringType) as TypeBuilder).DefineNestedType(type.Name, type.Attributes, mapper.Map(type.BaseType), mapper.Map(GetDeclaredInterfaces(type)));
					//Don't know how to declare nested enums :((
					//Anyway, "nested-value-types BUG" affects them, too
				}
				else
				{
					typeB = moduleB.DefineType(type.FullName, type.Attributes, mapper.Map(type.BaseType), mapper.Map(GetDeclaredInterfaces(type)));
				}
				allTypes.Add(typeB);
			}

			//The end of type declarations...
			foreach(Type type in types)
			{
				TypeBuilder typeB = mapper.Map(type) as TypeBuilder;
				FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
				foreach(FieldInfo field in fields)
				{
					if(field.IsLiteral)
						continue; //Andrew: ZLP
   				    FieldAttributes attributes = AddInternalAttribute(field.Attributes);
					FieldBuilder fieldB = typeB.DefineField(field.Name, mapper.Map(field.FieldType), attributes);
					map[field] = fieldB;
				}

				PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
				foreach(PropertyInfo property in properties)
				{
					ParameterInfo[] parameters = property.GetIndexParameters();
					Type[] paramTypes = new Type[parameters.Length];
					for(int i=0; i<paramTypes.Length; i++)
						paramTypes[i] = mapper.Map( parameters[i].ParameterType);
					PropertyBuilder propertyB = typeB.DefineProperty(property.Name, property.Attributes, mapper.Map(property.PropertyType), paramTypes);
					map[property] = propertyB;
				}

				EventInfo[] events = type.GetEvents(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
				foreach(EventInfo Event in events)
				{
					EventBuilder eventB = typeB.DefineEvent(Event.Name, Event.Attributes, mapper.Map(Event.EventHandlerType));
					map[Event] = eventB;
				}

				MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
				foreach(MethodInfo method in methods)
				{
					ParameterInfo[] parameters = method.GetParameters();
					Type[] paramTypes = new Type[parameters.Length];
					for(int i=0; i<paramTypes.Length; i++)
						paramTypes[i] = mapper.Map( parameters[i].ParameterType);
					MethodAttributes attributes = AddInternalAttribute(method.Attributes);
					MethodBuilder methodB = typeB.DefineMethod(method.Name, attributes, method.CallingConvention, mapper.Map(method.ReturnType), paramTypes);
					for(int i=0; i<paramTypes.Length; i++)
						methodB.DefineParameter(i+1, parameters[i].Attributes, parameters[i].Name);
					map[method] = methodB;
					if(!replacedMethods.Contains(method))
						allMethods.Add(method);
				}

				ConstructorInfo[] ctors = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
				foreach(ConstructorInfo ctor in ctors)
				{
					ParameterInfo[] parameters = ctor.GetParameters();
					Type[] paramTypes = new Type[parameters.Length];
					for(int i=0; i<paramTypes.Length; i++)
						paramTypes[i] = mapper.Map( parameters[i].ParameterType);
					MethodAttributes attributes = AddInternalAttribute(ctor.Attributes);
					ConstructorBuilder ctorB = typeB.DefineConstructor(attributes, ctor.CallingConvention, paramTypes);
					for(int i=0; i<paramTypes.Length; i++)
						ctorB.DefineParameter(i+1, parameters[i].Attributes, parameters[i].Name);
					map[ctor] = ctorB;
					if(!replacedMethods.Contains(ctor))
						allCtors.Add(ctor);
				}
				if(type.IsValueType)
					typeB.DefineDefaultConstructor(MethodAttributes.Public);
			}//foreach type

			MethodInfo[] globalMethods = module.GetMethods();
			foreach(MethodInfo method in globalMethods)
			{
				ParameterInfo[] parameters = method.GetParameters();
				Type[] paramTypes = new Type[parameters.Length];
				for(int i=0; i<paramTypes.Length; i++)
					paramTypes[i] = mapper.Map( parameters[i].ParameterType);
				MethodAttributes attributes = AddInternalAttribute(method.Attributes);
				MethodBuilder methodB = moduleB.DefineGlobalMethod(method.Name, attributes, method.CallingConvention, mapper.Map(method.ReturnType), paramTypes);
				for(int i=0; i<paramTypes.Length; i++)
					methodB.DefineParameter(i+1, parameters[i].Attributes, parameters[i].Name);
				map[method] = methodB;
				if(!replacedMethods.Contains(method))
					allMethods.Add(method);
			}

			//The end of fields, methods, ctors declarations...

			foreach(MethodInfo method in allMethods)
			{
				ILGenerator generator = (mapper.Map(method) as MethodBuilder).GetILGenerator();
				ProcessMethod(generator, moduleEx.GetMethodEx(method), method, mapper);
			}

			foreach(ConstructorInfo ctor in allCtors)
			{
				ILGenerator generator = (mapper.Map(ctor) as ConstructorBuilder).GetILGenerator();
				ProcessMethod(generator, moduleEx.GetMethodEx(ctor), ctor, mapper);
			}

			TypeBuilder[] result = new TypeBuilder[allTypes.Count];
			allTypes.CopyTo(result);
			return(result);

		}

		private static Set GetReplacedMethods(ResidualAssemblyHolder resHolder)
		{
			//Andrew: ZLP, waiting for Sergei
			ArrayList methods = new ArrayList();
			foreach (MethodBase method in resHolder.SourceHolder.getMethods())
				if (method.IsDefined(typeof(SpecializeAttribute), false))
					methods.Add(method);
			return(new Set(methods));
		}

		/*private class RefsAndArraysBuilderEx : RefsAndArraysBuilder
		{
			private MetaDataMapper mapper;

			public RefsAndArraysBuilderEx(MetaDataMapper mapper)
			{
				this.mapper = mapper;
			}

			public override Type BuildRefType(Type type)
			{
				return( mapper.Map(TypeEx.BuildRefType(type)) );
			}

			public override Type BuildArrayType(Type type)
			{
				return( mapper.Map(TypeEx.BuildArrayType(type)) );
			}
		}*/

		private static Type GetOrBuild(TypeBuilder cilpe, ArrayList list, int i)
		{
			if(list.Count > i)
				return(list[i] as Type);
			if(list.Count == i)
			{
				string name = "$CILPE$PseudoParameter_" + i;
				TypeBuilder type = cilpe.DefineNestedType(name, TypeAttributes.NestedAssembly | TypeAttributes.Class);
				list.Add(type);
				return(type);
			}
			throw new ExportException();
		}

		private static int GetAndInc(Hashtable hash, Type type)
		{
			object obj = hash[type];
			int n;
			if(obj == null)
				n = 0;
			else
			    n = (int)obj;
			hash[type] = n+1;
			return(n);
		}

    	private static void EmitResidual(ResidualAssemblyHolder resHolder, ModuleBuilder moduleB, Set replacedMethods, MetaDataMapper mapper)
		{
			int freeIndex = 0;
			ArrayList pseudoClasses = new ArrayList();
			Hashtable pseudoClassesUsage = new Hashtable(); // Type --> int mapping 

			Hashtable replacingMethods = new Hashtable(); //MBB --> MethodBase
            foreach(MethodBase method in replacedMethods)
				replacingMethods[resHolder[method]] = method;
			TypeBuilder cilpeClass = moduleB.DefineType("$CILPE$", TypeAttributes.NotPublic | TypeAttributes.Class);
			foreach(ResidualMethod id in resHolder.getMethods())
			{
				MethodBodyBlock mbb = resHolder[id];
				MethodBase method = replacingMethods[mbb] as MethodBase;
				if(method != null) //replacing, (Method/Constructor)Builder already defined
				{
					mapper.AddMethodBodyBlock(mbb, mapper.Map(method));
				}
				else //a new method
				{
					if(id.IsConstructor)
					{
						Type[] paramTypes = mapper.Map(MetaDataResolver.GetParamTypes(mbb,false));
						TypeBuilder typeB = mapper.Map(id.SourceMethod.DeclaringType) as TypeBuilder;
						if(typeB != paramTypes[0])
							throw new ExportException();
						for(int i=0; i<paramTypes.Length-1; i++)
						     paramTypes[i] = paramTypes[i+1];
						Type pseudoClass = GetOrBuild(cilpeClass, pseudoClasses, GetAndInc(pseudoClassesUsage, typeB));
						paramTypes[paramTypes.Length-1] = pseudoClass;
						mapper.AddSpecialCtor(mbb);
						ConstructorBuilder ctorB = typeB.DefineConstructor(MethodAttributes.Assembly, CallingConventions.Standard, paramTypes);
						for(int i=0; i<paramTypes.Length-1; i++)
							ctorB.DefineParameter(i+1, ParameterAttributes.None, "A_"+(i+1));
						ctorB.DefineParameter(paramTypes.Length,ParameterAttributes.Optional, "__noname");
						mapper.AddMethodBodyBlock(mbb,ctorB); 
					}
					else
					{
						Type[] paramTypes = mapper.Map(MetaDataResolver.GetParamTypes(mbb,false));
						string name = "$CILPE$" + id.SourceMethod.DeclaringType.FullName + "+" + id.SourceMethod.Name + "$" + (freeIndex++);
						MethodAttributes attributes = MethodAttributes.Assembly | MethodAttributes.Static;
						MethodBuilder methodB = cilpeClass.DefineMethod(name, attributes, CallingConventions.Standard, mapper.Map(mbb.ReturnType), paramTypes);
						for(int i=0; i<paramTypes.Length; i++)
							methodB.DefineParameter(i+1, ParameterAttributes.None, "A_"+(i+1));
						mapper.AddMethodBodyBlock(mbb,methodB); 
					}
				}
			}
			//All residual methods are declared. Define them now

			foreach(MethodBodyBlock body in resHolder)
			{
				MethodBase method = mapper.Map(body);
				MetaDataResolver.Map(body,mapper);
				bool verified = CFGVerifier.Check(body);
				if(!verified)
					throw new ExportException();
				MethodBuilder methodB = method as MethodBuilder;
				ConstructorBuilder ctorB = method as ConstructorBuilder;
				ILGenerator generator = methodB == null ? ctorB.GetILGenerator() : methodB.GetILGenerator();
				Emitter.Emit(generator,body);
			}

			cilpeClass.CreateType();
			foreach(TypeBuilder pseudoClass in pseudoClasses)
				pseudoClass.CreateType();
		}

		public static void Export(ResidualAssemblyHolder resHolder, string assemblyStringName)
		{
            Assembly assembly = resHolder.SourceHolder.Assembly;
			AssemblyName assemblyName = assembly.GetName(true); 
			AppDomain domain = AppDomain.CurrentDomain;
			if(! Directory.Exists("CILPEOutPut"))
			     Directory.CreateDirectory("CILPEOutPut");
			AssemblyBuilder assemblyB = domain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Save, "CILPEOutPut");
			Module module = assembly.GetModules()[0];
			ModuleBuilder moduleB = assemblyB.DefineDynamicModule(module.Name);

			Set replacedMethods = GetReplacedMethods(resHolder);

			MetaDataMapper mapper = new MetaDataMapper(moduleB, resHolder);
			Type[] allTypes = EmitSource(module, moduleB, replacedMethods, mapper);
			EmitResidual(resHolder, moduleB, replacedMethods, mapper);
			
			
			foreach(TypeBuilder type in allTypes)
				type.CreateType();
			moduleB.CreateGlobalFunctions();

			MethodInfo entryPoint = assembly.EntryPoint;
			if(entryPoint != null)
				assemblyB.SetEntryPoint(mapper.Map(entryPoint) as MethodInfo);

			assemblyB.Save(assemblyStringName);

		}
	}
}
