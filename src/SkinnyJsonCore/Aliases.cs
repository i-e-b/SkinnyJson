using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using JetBrains.Annotations;

namespace SkinnyJson
{
    /// <summary>
    /// SkinnyJson entry point. Use the static methods of this class to interact with JSON data
    /// </summary>
    /// <remarks>
    /// This is an alias for <see cref="Json"/> to work around naming conflicts
    /// </remarks>
    public abstract class SJson
    {
        /// <summary> Turn an object into a JSON string </summary>
        public static string Freeze(object? obj, JsonSettings? settings = null) => Json.Freeze(obj, settings);

        /// <summary> Write an object to a stream as a JSON string </summary>
        public static void Freeze(object obj, Stream target, JsonSettings? settings = null) => Json.Freeze(obj, target, settings);
        
        /// <summary> Turn an object into a JSON string encoded to a byte array </summary>
        public static byte[] FreezeToBytes(object? obj, JsonSettings? settings = null) => Json.FreezeToBytes(obj, settings);

        /// <summary> Turn a JSON string into a detected object </summary>
        public static object Defrost(string json, JsonSettings? settings = null) => Json.Defrost(json, settings);

        /// <summary> Turn a JSON byte array into a detected object </summary>
        public static object Defrost(byte[] json, JsonSettings? settings = null) => Json.Defrost(json, settings);

        /// <summary> Turn a JSON data stream into a detected object </summary>
        public static object Defrost(Stream json, JsonSettings? settings = null) => Json.Defrost(json, settings);

        /// <summary> Return the type name that SkinnyJson will use for the serialising the object </summary>
        public static string WrapperType(object obj, JsonSettings? settings = null) => Json.WrapperType(obj, settings);

        /// <summary> Turn a JSON string into a specific object </summary>
        public static T Defrost
            <[MeansImplicitUse(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature, ImplicitUseTargetFlags.WithMembers)]T>
            (string json, JsonSettings? settings = null) => Json.Defrost<T>(json, settings);

        /// <summary> Turn a JSON data stream into a specific object </summary>
        public static T Defrost
            <[MeansImplicitUse(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature, ImplicitUseTargetFlags.WithMembers)]T>
            (Stream json, JsonSettings? settings = null) => Json.Defrost<T>(json, settings);

        /// <summary> Turn a JSON byte array into a specific object </summary>
        public static T Defrost
            <[MeansImplicitUse(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature, ImplicitUseTargetFlags.WithMembers)]T>
            (byte[] json, JsonSettings? settings = null) => Json.Defrost<T>(json, settings);

        /// <summary> Turn a JSON string into a runtime type </summary>
        public static object Defrost(string json, Type runtimeType, JsonSettings? settings = null) => Json.Defrost(json, runtimeType, settings);

        /// <summary> Turn a JSON byte array into a runtime type </summary>
        public static object Defrost(byte[] json, Type runtimeType, JsonSettings? settings = null) => Json.Defrost(json, runtimeType, settings);

        /// <summary> Turn a JSON data stream into a runtime type </summary>
        public static object Defrost(Stream json, Type runtimeType, JsonSettings? settings = null) => Json.Defrost(json, runtimeType, settings);

        /// <summary> Turn a JSON string into an object containing properties found </summary>
        public static dynamic DefrostDynamic(string json, JsonSettings? settings = null) => Json.DefrostDynamic(json, settings);

        /// <summary> Turn a JSON string into an object containing properties found </summary>
        public static dynamic DefrostDynamic(Stream json, JsonSettings? settings = null) => Json.DefrostDynamic(json, settings);


        /// <summary>
        /// Turn a sub-path of a JSON document into an enumeration of values, by specific type
        /// </summary>
        /// <remarks>This is intended to extract useful fragments from repository-style files</remarks>
        /// <typeparam name="T">Type of the fragments to be returned</typeparam>
        /// <param name="path">Dotted path through document. If the path can't be found, an empty enumeration will be returned.
        /// An empty path is equivalent to `Defrost&lt;T&gt;`</param>
        /// <param name="json">The JSON document string to read</param>
        /// <param name="settings">Json parsing settings</param>
        public static IEnumerable<T> DefrostFromPath
            <[MeansImplicitUse(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature, ImplicitUseTargetFlags.WithMembers)]T>
            (string path, string json, JsonSettings? settings = null) => Json.DefrostFromPath<T>(path, json, settings);

        /// <summary> Create a copy of an object through serialisation </summary>
        public static T Clone
            <[MeansImplicitUse(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature, ImplicitUseTargetFlags.WithMembers)]T>
            (T obj) => Json.Clone<T>(obj);

        /// <summary>Read a JSON object into an anonymous .Net object</summary>
        public static object Parse(string json, JsonSettings? settings = null) => Json.Parse(json, settings);

        /// <summary>
        /// Deserialise a string, perform some edits then reform as a new string
        /// </summary>
        public static string Edit(string json, Action<dynamic> editAction, JsonSettings? settings = null) => Json.Edit(json, editAction, settings);

        /// <summary>
        /// Pretty print a JSON string. This is done without value parsing.
        /// <p/>
        /// Note that any JS comments in the input are removed in the output.
        /// </summary>
        public static string Beautify(string input) => Json.Beautify(input);

        /// <summary>
        /// Pretty print a JSON data stream to another stream.
        /// This is done without value parsing or buffering, so very large streams can be processed.
        /// The input and output encodings can be the same or different.
        /// <p/>
        /// Note that any JS comments in the input are removed in the output.
        /// </summary>
        public static void BeautifyStream(Stream input, Encoding inputEncoding, Stream output, Encoding outputEncoding) => Json.BeautifyStream(input, inputEncoding, output, outputEncoding);

        /// <summary>Fill the members of an .Net object from a JSON object string</summary>
        /// <remarks>Alias for <see cref="FillObject"/></remarks>
        public static object? DefrostInto(object input, string json, JsonSettings? settings = null) => Json.DefrostInto(input, json, settings);

        /// <summary>Fill the members of an .Net object from a JSON object string</summary>
        public static object? FillObject(object input, string json, JsonSettings? settings = null) => Json.FillObject(input, json, settings);

        /// <summary>
        /// Convert a hex string to a byte array.
        /// <p/>
        /// Use <c>Convert.FromHexString</c> where available
        /// </summary>
        public static byte[]? HexToByteArray(string? hex) => Json.HexToByteArray(hex);
    }
}