using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace SkinnyJson {
	public sealed class Extract<I> {
		internal sealed class StaticWrapper<Ti, Tt> {
			private static readonly WrapperBase Prototype;
			static StaticWrapper () {
				Prototype = WrapperGenerator.GenerateWrapperPrototype(typeof(Ti), typeof(Tt));
			}
			public static Ti Cast (Tt src) {
				return (Ti)Prototype.NewFromPrototype(src);
			}
		}

		public static I From<T> (T src) {
			if (!typeof(I).IsInterface) throw new ArgumentException("Target type must be an interface");
			return StaticWrapper<I, T>.Cast(src);
		}
	}

	/// <summary>
	/// Helper class for runtime interface extraction.
	/// Do not use directly, call Extract&lt;interface&gt;.From(object) instead.
	/// </summary>
	public class WrapperBase {
		internal protected object Src; // WARNING: this field is directly referenced in the wrapper generator. Don't change it!
		internal object NewFromPrototype (object src) {
			var newWrapper = (WrapperBase)MemberwiseClone();
			newWrapper.Src = src;
			return newWrapper;
		}
	}

	/// <summary> 
	/// Help wrap concrete types in interfaces that they don't explicitly implement.
	/// </summary> 
	internal class WrapperGenerator : IDisposable {
		static readonly AppDomain WrappersAppDomain;
		static readonly AssemblyBuilder AsmBuilder;
		static readonly ModuleBuilder ProxyModule;

		static WrapperGenerator () {
			WrappersAppDomain = Thread.GetDomain();
			AsmBuilder = WrappersAppDomain.DefineDynamicAssembly(
														 new AssemblyName("Wrappers"),
														 AssemblyBuilderAccess.Run);
			ProxyModule = AsmBuilder.DefineDynamicModule("WrapperModule", true);
		}

		public static Type GenerateWrapperType (Type targetType, Type sourceType) {
			var proxyBuilder = GetProxyBuilder(targetType, sourceType);

			FieldInfo srcField = typeof(WrapperBase).GetField("Src", BindingFlags.Instance | BindingFlags.NonPublic);
			if (srcField == null) throw new ApplicationException("Source binding failed!");

			foreach (MethodInfo method in targetType.GetMethods()) {
				BindProxyMethod(targetType, sourceType, srcField, method, proxyBuilder);
			}

			return proxyBuilder.CreateType();
		}

		public static TypeBuilder GetProxyBuilder (Type targetType, Type sourceType) {
			return ProxyModule.DefineType(sourceType.Name + "To" + targetType.Name + "Wrapper",
										  TypeAttributes.NotPublic | TypeAttributes.Sealed | TypeAttributes.Class,
										  typeof(WrapperBase), new[] { targetType });
		}

		public static TypeBuilder GetProxyBuilder (Type targetType) {
			return ProxyModule.DefineType(targetType.Name + "DynImpl",
										  TypeAttributes.Public,
										  typeof(object), new[] { targetType });
		}

		/// <summary>
		/// Emit a new method in a target type that calls a method from the source type.
		/// </summary>
		private static void BindProxyMethod (Type targetType, Type sourceType, FieldInfo srcField, MethodInfo method, TypeBuilder proxyBuilder) {
			var parameters = method.GetParameters();
			var parameterTypes = new Type[parameters.Length];
			for (int i = 0; i < parameters.Length; i++) parameterTypes[i] = parameters[i].ParameterType;

			MethodInfo srcMethod = sourceType.GetMethod(method.Name, parameterTypes);
			if (srcMethod == null)
				throw new MissingMethodException(method.Name + " is not implemented by " + sourceType.FullName + " as required by the " + targetType.FullName + " interface.");

			MethodBuilder methodBuilder = proxyBuilder
				.DefineMethod(method.Name, MethodAttributes.Public | MethodAttributes.Virtual, method.ReturnType, parameterTypes);

			ILGenerator ilGenerator = methodBuilder.GetILGenerator();
			ilGenerator.Emit(OpCodes.Ldarg_0);
			ilGenerator.Emit(OpCodes.Ldfld, srcField);
			for (int i = 1; i < parameters.Length + 1; i++) ilGenerator.Emit(OpCodes.Ldarg, i);

			ilGenerator.Emit(method.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, srcMethod);
			ilGenerator.Emit(OpCodes.Ret);
		}

		public static void BindAutoProperty (TypeBuilder tbuilder, string propertyName, Type propertyType) {
			FieldBuilder fFirst = tbuilder.DefineField("_"+propertyName, propertyType, FieldAttributes.Public);
			PropertyBuilder pFirst = tbuilder.DefineProperty(propertyName, PropertyAttributes.None, propertyType, null);
			//Getter
			MethodBuilder mFirstGet = tbuilder.DefineMethod("get_"+propertyName, MethodAttributes.Public |
				MethodAttributes.SpecialName |
				MethodAttributes.HideBySig, propertyType, Type.EmptyTypes);
			ILGenerator firstGetIL = mFirstGet.GetILGenerator();

			firstGetIL.Emit(OpCodes.Ldarg_0);
			firstGetIL.Emit(OpCodes.Ldfld, fFirst);
			firstGetIL.Emit(OpCodes.Ret);

			//Setter
			MethodBuilder mFirstSet = tbuilder.DefineMethod("set_"+propertyName, MethodAttributes.Public |
				MethodAttributes.SpecialName |
				MethodAttributes.HideBySig, null, new[] { propertyType });

			ILGenerator firstSetIL = mFirstSet.GetILGenerator();

			firstSetIL.Emit(OpCodes.Ldarg_0);
			firstSetIL.Emit(OpCodes.Ldarg_1);
			firstSetIL.Emit(OpCodes.Stfld, fFirst);
			firstSetIL.Emit(OpCodes.Ret);

			pFirst.SetGetMethod(mFirstGet);
			pFirst.SetSetMethod(mFirstSet);
		}

		public static WrapperBase GenerateWrapperPrototype (Type targetType, Type sourceType) {
			Type wrapperType = GenerateWrapperType(targetType, sourceType);
			return (WrapperBase)Activator.CreateInstance(wrapperType);
		}

		public void Dispose () {
			AppDomain.Unload(WrappersAppDomain);
		}
	}
}