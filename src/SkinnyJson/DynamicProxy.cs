using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace SkinnyJson {
	public static class DynamicProxy{
		public static T GetInstanceFor<T> () {
			return (T)GetInstanceFor(typeof(T));
		}

		public static object GetInstanceFor (Type typeOfT) {
			var methodInfos = typeOfT.GetMethods();
			var assName = new AssemblyName("testAssembly");
			var assBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assName, AssemblyBuilderAccess.RunAndSave);
			var moduleBuilder = assBuilder.DefineDynamicModule("testModule", "test.dll");
			var typeBuilder = moduleBuilder.DefineType(typeOfT.Name + "Proxy", TypeAttributes.Public);

			typeBuilder.AddInterfaceImplementation(typeOfT);
			var ctorBuilder = typeBuilder.DefineConstructor(
				MethodAttributes.Public,
				CallingConventions.Standard,
				new Type[] { });
			var ilGenerator = ctorBuilder.GetILGenerator();
			ilGenerator.EmitWriteLine("Creating Proxy instance");
			ilGenerator.Emit(OpCodes.Ret);
			foreach (var methodInfo in methodInfos) {
				if (methodInfo.Name.StartsWith("set_")) {
					BindSetProperty(typeBuilder, methodInfo);
				} else if (methodInfo.Name.StartsWith("get_")) {
					BindGetProperty(typeBuilder, methodInfo);
					BindMethod(typeBuilder, methodInfo);
				} else {
					BindMethod(typeBuilder, methodInfo);
				}
			}

			Type constructedType = typeBuilder.CreateType();
			var instance = Activator.CreateInstance(constructedType);
			return instance;
		}

		static void BindMethod(TypeBuilder typeBuilder, MethodInfo methodInfo) {
			var methodBuilder = typeBuilder.DefineMethod(
				methodInfo.Name,
				MethodAttributes.Public | MethodAttributes.Virtual,
				methodInfo.ReturnType,
				methodInfo.GetParameters().Select(p => p.GetType()).ToArray()
				);
			var methodILGen = methodBuilder.GetILGenerator();
			if (methodInfo.ReturnType == typeof(void)) {
				methodILGen.Emit(OpCodes.Ret);
			} else {
				if (methodInfo.ReturnType.IsValueType || methodInfo.ReturnType.IsEnum) {
					MethodInfo getMethod = typeof(Activator).GetMethod("CreateInstance",
					                                                   new[] { typeof(Type) });
					LocalBuilder lb = methodILGen.DeclareLocal(methodInfo.ReturnType);
					methodILGen.Emit(OpCodes.Ldtoken, lb.LocalType);
					methodILGen.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle"));
					methodILGen.Emit(OpCodes.Callvirt, getMethod);
					methodILGen.Emit(OpCodes.Unbox_Any, lb.LocalType);
				} else {
					methodILGen.Emit(OpCodes.Ldnull);
				}
				methodILGen.Emit(OpCodes.Ret);
			}
			typeBuilder.DefineMethodOverride(methodBuilder, methodInfo);
		}

		public static void BindGetProperty (TypeBuilder typeBuilder, MethodInfo methodInfo) {
			string propertyName = methodInfo.Name.Replace("get_","");
			Type propertyType = methodInfo.ReturnType;
			FieldBuilder backingField = typeBuilder.DefineField("_"+propertyName, propertyType, FieldAttributes.Public);
			PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(propertyName, PropertyAttributes.None, propertyType, null);
			//Getter
			MethodBuilder backingGet = typeBuilder.DefineMethod("get_"+propertyName, MethodAttributes.Public |
				MethodAttributes.SpecialName |
				MethodAttributes.HideBySig, propertyType, Type.EmptyTypes);
			ILGenerator firstGetIL = backingGet.GetILGenerator();

			firstGetIL.Emit(OpCodes.Ldarg_0);
			firstGetIL.Emit(OpCodes.Ldfld, backingField);
			firstGetIL.Emit(OpCodes.Ret);

			propertyBuilder.SetGetMethod(backingGet);
		}
		public static void BindSetProperty (TypeBuilder typeBuilder, MethodInfo methodInfo) {
			string propertyName = methodInfo.Name.Replace("set_","");
			Type propertyType = methodInfo.GetParameters()[0].ParameterType;
			FieldBuilder backingField = typeBuilder.DefineField("_"+propertyName, propertyType, FieldAttributes.Public);
			PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(propertyName, PropertyAttributes.None, propertyType, null);

			//Setter
			MethodBuilder backingSet = typeBuilder.DefineMethod("set_"+propertyName, MethodAttributes.Public |
				MethodAttributes.SpecialName |
				MethodAttributes.HideBySig, null, new[] { propertyType });

			ILGenerator firstSetIL = backingSet.GetILGenerator();

			firstSetIL.Emit(OpCodes.Ldarg_0);
			firstSetIL.Emit(OpCodes.Ldarg_1);
			firstSetIL.Emit(OpCodes.Stfld, backingField);
			firstSetIL.Emit(OpCodes.Ret);
			propertyBuilder.SetSetMethod(backingSet);
		}
	}
}