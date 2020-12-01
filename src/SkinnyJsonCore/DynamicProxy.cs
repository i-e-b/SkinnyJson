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

        private static readonly ModuleBuilder _moduleBuilder;
        private static readonly AssemblyBuilder _dynamicAssembly;

        /// <summary>
        /// Return an instance of the given interface
        /// </summary>
		public static object GetInstanceFor (Type targetType) {
			lock (_dynamicAssembly) // can race when type has been declared but not built yet
			{
				var constructedType = _dynamicAssembly.GetType(ProxyName(targetType)) ?? GetConstructedType(targetType);
				return Activator.CreateInstance(constructedType)!;
			}
		}

		static string ProxyName(Type targetType)
		{
			return "Proxy" + targetType.Name;
		}

		static DynamicProxy () {
			var assemblyName = new AssemblyName("DynImpl");
			_dynamicAssembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
			_moduleBuilder = _dynamicAssembly.DefineDynamicModule("DynImplModule");
		}

        static Type GetConstructedType (Type targetType) {
			var typeBuilder = _moduleBuilder.DefineType(ProxyName(targetType), TypeAttributes.Public);

            var gTypes = targetType.GetGenericArguments();
            if (gTypes.Any())
            {
                typeBuilder.DefineGenericParameters(gTypes.Select(g=>g.Name).ToArray());
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
                return typeBuilder.CreateTypeInfo()!;
            }
            catch (TypeLoadException ex)
            {
                throw new Exception("Failed to build reflection type", ex);
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


			var methodIlGen = methodBuilder.GetILGenerator();

		    foreach (var parameter in parameters)
		    {
                methodIlGen.DeclareLocal(parameter.ParameterType); // help with weird generic types
            }

            if (methodInfo.ReturnType == typeof(void)) {
				methodIlGen.Emit(OpCodes.Ret);
			} else {
				if (methodInfo.ReturnType.IsValueType || methodInfo.ReturnType.IsEnum) {
					var getMethod = typeof(Activator).GetMethod("CreateInstance", new[] { typeof(Type) });
					var lb = methodIlGen.DeclareLocal(methodInfo.ReturnType);
                    var getTypeFromHandle = typeof(Type).GetMethod("GetTypeFromHandle");

				    if (lb.LocalType == null) return;
                    if (getMethod == null) return;
                    if (getTypeFromHandle == null) return;

					methodIlGen.Emit(OpCodes.Ldtoken, lb.LocalType);
					methodIlGen.Emit(OpCodes.Call, getTypeFromHandle);
					methodIlGen.Emit(OpCodes.Callvirt, getMethod);
					methodIlGen.Emit(OpCodes.Unbox_Any, lb.LocalType);
				} else {
					methodIlGen.Emit(OpCodes.Ldnull);
				}
				methodIlGen.Emit(OpCodes.Ret);
			}

            typeBuilder.DefineMethodOverride(methodBuilder, methodInfo);
        }

        static void BindProperty (TypeBuilder typeBuilder, MethodInfo methodInfo) {
            var pTypes = methodInfo.GetParameters().Select(t=>t.ParameterType).ToArray();

			// Backing Field
			var propertyName = methodInfo.Name.Replace("get_", "");
			var propertyType = methodInfo.ReturnType;
			var backingField = typeBuilder.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);

			//Getter
			var backingGet = typeBuilder.DefineMethod("get_" + propertyName, MethodAttributes.Public |
				MethodAttributes.SpecialName | MethodAttributes.Virtual |
				MethodAttributes.HideBySig, propertyType, Type.EmptyTypes);
			var getIl = backingGet.GetILGenerator();

            
            foreach (var parameter in pTypes) { getIl.DeclareLocal(parameter); }
			getIl.Emit(OpCodes.Ldarg_0);
			getIl.Emit(OpCodes.Ldfld, backingField);
			getIl.Emit(OpCodes.Ret);


			//Setter
			var backingSet = typeBuilder.DefineMethod("set_" + propertyName, MethodAttributes.Public |
				MethodAttributes.SpecialName | MethodAttributes.Virtual |
				MethodAttributes.HideBySig, null, new[] { propertyType });

			var setIl = backingSet.GetILGenerator();
            
            foreach (var parameter in pTypes) { setIl.DeclareLocal(parameter); }
			setIl.Emit(OpCodes.Ldarg_0);
			setIl.Emit(OpCodes.Ldarg_1);
			setIl.Emit(OpCodes.Stfld, backingField);
			setIl.Emit(OpCodes.Ret);

			// Property
			var propertyBuilder = typeBuilder.DefineProperty(propertyName, PropertyAttributes.None, propertyType, pTypes);
			propertyBuilder.SetGetMethod(backingGet);
			propertyBuilder.SetSetMethod(backingSet);
		}
	}
}