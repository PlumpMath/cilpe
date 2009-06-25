using System;
using System.Reflection;
using System.Collections;

using CILPE.CFG;
using CILPE.ReflectionEx;
using CILPE.Spec; //getMethodBodyBlock(Node);

namespace CILPE
{
	public class MetaDataMapper
	{
		private Hashtable map;
		private Module module; //ModuleBuilder
		private Hashtable methodByMBB; //MBB -> MethodBase mapping
		private ResidualAssemblyHolder holder;
		private Hashtable types; //Name -> Type mapping. To patch MS bug in ModuleBuilder 
		private Set specialCtors;//ctors with pseudo parameter RefsAndArraysBuilder

		public MetaDataMapper(Module module, ResidualAssemblyHolder holder)
		{
			map = new Hashtable();;
			methodByMBB = new Hashtable();
			types = new Hashtable();
            specialCtors = new Set();
			this.module = module;
			this.holder = holder;
		}

		public Hashtable MapTable
		{
			get{ return(map); }
		}

		public ResidualAssemblyHolder Holder
		{
			get{ return(holder); }
		}

		public void AddMethodBodyBlock(MethodBodyBlock mbb, MethodBase method)
		{
			methodByMBB[mbb] = method;
		}

		public void AddSpecialCtor(MethodBodyBlock ctor)
		{
			specialCtors.Add(ctor);
		}

		public bool HasPseudoParameter(MethodBodyBlock ctor)
		{
			return(specialCtors.Contains(ctor));
		}

		public Type Map(Type x)
		{
			if(x == null)
				return(null);
			Type t = types[x.FullName] as Type;
			if(t != null)
				return(t);
			t = module.GetType(x.FullName);
			if(t != null)
			{
				types[x.FullName] = t;
				return(t);
			}
			else
				return(x);
		}

		public Type[] Map(Type[] types)
		{
			Type [] result = new Type[types.Length];
			for(int i=0;i<types.Length;i++)
				result[i] = Map(types[i]);
			return(result);
		}

		public FieldInfo Map(FieldInfo x)
		{
			if(map.ContainsKey(x))
				return(map[x] as FieldInfo);
			else
				return(x);
		}

		public MethodBase Map(MethodBase x)
		{
			if(map.ContainsKey(x))
				return(map[x] as MethodBase);
			else
				return(x);
		}

		public ConstructorInfo Map(ConstructorInfo x)
		{
			if(map.ContainsKey(x))
				return(map[x] as ConstructorInfo);
			else
				return(x);
		}

		public MethodInfoExtention Map(MethodInfoExtention method)
		{
			return(new MethodInfoExtention(Map(method.Method), method.IsVirtCall, Map(method.Params)));
		}

		public MethodBase Map(MethodBodyBlock mbb)
		{
			return(methodByMBB[mbb] as MethodBase);
		}
	}

	public class MetaDataResolver
	{

		public static Type[] GetParamTypes(MethodBodyBlock mbb, bool doRemoveThis)
		{
			ArrayList paramTypesList = new ArrayList();
			int i0 = doRemoveThis ? 1 : 0;
			int I = mbb.Variables.ParameterMapper.Count;
			for(int i=i0; i<I; i++)
			{
				Variable var = mbb.Variables.ParameterMapper[i];
				paramTypesList.Add(var.Type);
			}
			Type[] paramTypes = new Type[paramTypesList.Count];
			paramTypesList.CopyTo(paramTypes);
			return(paramTypes);
		}

		private class NodeMapper 
		{
			MetaDataMapper mapper;
			
			public NodeMapper(MetaDataMapper mapper)
			{
				this.mapper = mapper; 	
			}

			public void Callback(Node node)
			{
				if(node is ITypedNode)
				{
					ITypedNode theNode = node as ITypedNode;
					theNode.Type = mapper.Map(theNode.Type);
				}
				if(node is ManageField)
				{
					ManageField theNode = node as ManageField;
					theNode.Field = mapper.Map(theNode.Field);
				}
				if(node is MethodBodyBlock)
				{
					MethodBodyBlock body = node as MethodBodyBlock;
					foreach(Variable var in body.Variables)
						var.Type = mapper.Map(var.Type);
					body.ReturnType = mapper.Map(body.ReturnType);
				}
				if(node is CallMethod)
				{
					CallMethod call = node as CallMethod;
					ResidualMethod id = Specialization.GetResidualMethod(call);
					if(id != null)
					{
						MethodBodyBlock mbb = mapper.Holder[id];
						node.Options["HasPseudoParameter"] = mapper.HasPseudoParameter(mbb);
						call.MethodWithParams = new MethodInfoExtention(mapper.Map(mbb), call.IsVirtCall, mapper.Map(GetParamTypes(mbb, id.IsConstructor)) );
					}
					else
						call.MethodWithParams = mapper.Map(call.MethodWithParams);
				}
				if(node is NewObject)
				{
					NewObject newObj = node as NewObject;
					ResidualMethod id = Specialization.GetResidualMethod(node);
					if(id != null)
					{
						MethodBodyBlock mbb = mapper.Holder[id];
						newObj.CtorWithParams = new MethodInfoExtention(mapper.Map(mbb), false, mapper.Map(GetParamTypes(mbb, id.IsConstructor)));
						node.Options["HasPseudoParameter"] = mapper.HasPseudoParameter(mbb);
					}
					else
						newObj.CtorWithParams = mapper.Map(newObj.CtorWithParams);
				}
				if(node is CreateDelegate)
				{
					CreateDelegate crDel = node as CreateDelegate;
					//Andrew TODO: Map this node... 
				}

				// Graph should be verified AFTER the metadata is resolved
				 
			}
		}

		internal static void Map(MethodBodyBlock body, MetaDataMapper map)
		{
			NodeMapper mapper = new NodeMapper(map);
			ForEachVisitor.ForEach(body,new ForEachCallback(mapper.Callback));
		}
	}
}
