using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace SkinnyJson {
    /// <summary>
    /// Generates run-time types for Interfaces
    /// </summary>
	public static class DynamicProxy {
        /// <summary>
        /// Return an instance of the given interface
        /// </summary>
		public static T GetInstanceFor<T> () {
			return (T)GetInstanceFor(typeof(T));
		}

		static readonly ModuleBuilder ModuleBuilder;
		static readonly AssemblyBuilder DynamicAssembly;

        /// <summary>
        /// Return an instance of the given interface
        /// </summary>
		public static object GetInstanceFor (Type targetType) {
			lock (DynamicAssembly) // can race when type has been declared but not built yet
			{
				var constructedType = DynamicAssembly.GetType(ProxyName(targetType)) ?? GetConstructedType(targetType);
				var instance = Activator.CreateInstance(constructedType);
				return instance;
			}
		}

		static string ProxyName(Type targetType)
		{
			return "Proxy" + targetType.Name;
		}

		static DynamicProxy () {
			var assemblyName = new AssemblyName("DynImpl");
			DynamicAssembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
			ModuleBuilder = DynamicAssembly.DefineDynamicModule("DynImplModule");
		}

        static Type GetConstructedType (Type targetType) {
			var typeBuilder = ModuleBuilder.DefineType(ProxyName(targetType), TypeAttributes.Public);


            var gtypes = targetType.GetGenericArguments();
            if (gtypes.Any())
            {
                typeBuilder.DefineGenericParameters(gtypes.Select(g=>g.Name).ToArray());
            }

			foreach (var face in targetType.GetInterfaces()) { IncludeType(face, typeBuilder); }

			IncludeType(targetType, typeBuilder);
			var ctorBuilder = typeBuilder.DefineConstructor(
				MethodAttributes.Public,
				CallingConventions.Standard,
				new Type[] { });
			var ilGenerator = ctorBuilder.GetILGenerator();
			ilGenerator.Emit(OpCodes.Ret);

            try
            {
                return typeBuilder.CreateType();
            }
            catch (TypeLoadException tlex)
            {
                throw new Exception("Failed to build reflection type", tlex);
            }
        }

        static void IncludeType (Type typeOfT, TypeBuilder typeBuilder) {
			var methodInfos = typeOfT.GetMethods();
			foreach (var methodInfo in methodInfos) {
				if (methodInfo.Name.StartsWith("set_")) continue; // we always add a set for a get.

				if (methodInfo.Name.StartsWith("get_")) {
					BindProperty(typeBuilder, methodInfo);
				} else {
					BindMethod(typeBuilder, methodInfo);
				}
			}

			typeBuilder.AddInterfaceImplementation(typeOfT);
		}

		static void BindMethod (TypeBuilder typeBuilder, MethodInfo methodInfo) {
            var parameters = methodInfo.GetParameters();

			var methodBuilder = typeBuilder.DefineMethod(
				methodInfo.Name,
				MethodAttributes.Public | MethodAttributes.Virtual,
				methodInfo.ReturnType,
				parameters.Select(p => p.ParameterType).ToArray()
				);


			var methodILGen = methodBuilder.GetILGenerator();

		    foreach (var parameter in parameters)
		    {
                methodILGen.DeclareLocal(parameter.ParameterType); // help with weird generic types
            }

            if (methodInfo.ReturnType == typeof(void)) {
				methodILGen.Emit(OpCodes.Ret);
			} else {
				if (methodInfo.ReturnType.IsValueType || methodInfo.ReturnType.IsEnum) {
					var getMethod = typeof(Activator).GetMethod("CreateInstance", new[] { typeof(Type) });
					var lb = methodILGen.DeclareLocal(methodInfo.ReturnType);
                    var getTypeFromHandle = typeof(Type).GetMethod("GetTypeFromHandle");

				    if (lb.LocalType == null) return;
                    if (getMethod == null) return;
                    if (getTypeFromHandle == null) return;

					methodILGen.Emit(OpCodes.Ldtoken, lb.LocalType);
					methodILGen.Emit(OpCodes.Call, getTypeFromHandle);
					methodILGen.Emit(OpCodes.Callvirt, getMethod);
					methodILGen.Emit(OpCodes.Unbox_Any, lb.LocalType);
				} else {
					methodILGen.Emit(OpCodes.Ldnull);
				}
				methodILGen.Emit(OpCodes.Ret);
			}

            typeBuilder.DefineMethodOverride(methodBuilder, methodInfo);
        }

        static void BindProperty (TypeBuilder typeBuilder, MethodInfo methodInfo) {
            var ptypes = methodInfo.GetParameters().Select(t=>t.ParameterType).ToArray();

			// Backing Field
			var propertyName = methodInfo.Name.Replace("get_", "");
			var propertyType = methodInfo.ReturnType;
			var backingField = typeBuilder.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);

			//Getter
			var backingGet = typeBuilder.DefineMethod("get_" + propertyName, MethodAttributes.Public |
				MethodAttributes.SpecialName | MethodAttributes.Virtual |
				MethodAttributes.HideBySig, propertyType, Type.EmptyTypes);
			var getIl = backingGet.GetILGenerator();

            
            foreach (var parameter in ptypes) { getIl.DeclareLocal(parameter); }
			getIl.Emit(OpCodes.Ldarg_0);
			getIl.Emit(OpCodes.Ldfld, backingField);
			getIl.Emit(OpCodes.Ret);


			//Setter
			var backingSet = typeBuilder.DefineMethod("set_" + propertyName, MethodAttributes.Public |
				MethodAttributes.SpecialName | MethodAttributes.Virtual |
				MethodAttributes.HideBySig, null, new[] { propertyType });

			var setIl = backingSet.GetILGenerator();
            
            foreach (var parameter in ptypes) { setIl.DeclareLocal(parameter); }
			setIl.Emit(OpCodes.Ldarg_0);
			setIl.Emit(OpCodes.Ldarg_1);
			setIl.Emit(OpCodes.Stfld, backingField);
			setIl.Emit(OpCodes.Ret);

			// Property
			var propertyBuilder = typeBuilder.DefineProperty(propertyName, PropertyAttributes.None, propertyType, ptypes);
			propertyBuilder.SetGetMethod(backingGet);
			propertyBuilder.SetSetMethod(backingSet);
		}
	}
}