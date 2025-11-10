using System;

namespace SkinnyJson
{
    /// <summary>
    /// Instructs the serializer to use the specified converter when serialising the decorated property
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class CustomJsonConverterAttribute : Attribute
    {
        /// <summary>
        /// Gets the converter to use
        /// </summary>
        public Type ConverterType { get; }

        /// <summary>
        /// The parameter list to use when constructing the JsonConverter described by <see cref="ConverterType"/>.
        /// If <c>null</c>, the default constructor is used.
        /// </summary>
        public object[]? ConverterParameters { get; }

        /// <summary>
        /// <p>Initializes a new instance of the <see cref="CustomJsonConverterAttribute"/> class.</p>
        /// The <paramref name="converterType"/> should be one of:
        /// <ul>
        /// <li><c>System.Text.Json.Serialization.JsonConverter&lt;T&gt;</c></li>
        /// <li><c>System.Text.Json.Serialization.JsonConverter</c></li>
        /// <li><c>SkinnyJson.CustomJsonConverter&lt;T&gt;</c></li>
        /// </ul>
        /// </summary>
        /// <param name="converterType">Type of the JsonConverter.</param>
        public CustomJsonConverterAttribute(Type converterType)
        {
            ConverterType = converterType ?? throw new ArgumentNullException(nameof(converterType));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomJsonConverterAttribute"/> class.
        /// </summary>
        /// <param name="converterType">Type of the converter.</param>
        /// <param name="converterParameters">Parameter list to use when constructing the converter. Can be <c>null</c>.</param>
        public CustomJsonConverterAttribute(Type converterType, params object[] converterParameters) : this(converterType)
        {
            ConverterParameters = converterParameters;
        }
    }

    /// <summary>
    /// Set a custom name for given property or field.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class JsonNameAttribute : Attribute
    {
        /// <summary>
        /// Create a new custom name attribute
        /// </summary>
        public JsonNameAttribute(string name) { Name = name; }

        /// <summary>
        /// Custom name
        /// </summary>
        public string Name { get; }
    }
}