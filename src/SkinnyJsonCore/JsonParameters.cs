namespace SkinnyJson
{
    /// <summary>
    /// Parameters for serialising and deserialising.
    /// </summary>
    public class JsonParameters
    {
// ReSharper disable RedundantDefaultFieldInitializer
        /// <summary>
        /// Use a special format for Sql Datasets. Default true
        /// </summary>
        public bool UseOptimizedDatasetSchema = true;

        /// <summary>
        /// Use Base64 encoding for Guids. If false, uses Hex.
        /// Default true
        /// </summary>
        public bool UseFastGuid = true;

        /// <summary>
        /// Insert null values into JSON output. Otherwise remove field.
        /// Default true
        /// </summary>
        public bool SerializeNullValues = true;

        /// <summary>
        /// Force date times to UTC. Default true
        /// </summary>
        public bool UseUtcDateTime = true;

        /// <summary>
        /// Serialise properties that can't be written on deserialise. Default false
        /// </summary>
        public bool ShowReadOnlyProperties = false;

        /// <summary>
        /// Declare types once at the start of a document. Otherwise declare in each object.
        /// Default true, but overridden by `EnableAnonymousTypes`
        /// </summary>
        public bool UsingGlobalTypes = true;

        /// <summary>
        /// Allow case insensitive matching on deserialise. Default false
        /// </summary>
        public bool IgnoreCaseOnDeserialize
        {
            get => _ignoreCaseOnDeserialize;
            set {
                if (_ignoreCaseOnDeserialize != value) Json.ClearCaches(); // we cache case transformation results
                _ignoreCaseOnDeserialize = value;
            }
        }

        /// <summary>
        /// Default true. If false, source type information will be included in serialised output.<para></para>
        /// Sets `UseExtensions` and `UsingGlobalTypes` to false.
        /// Directly serialising an anonymous type will use these settings for that call, without needing a global setting.
        /// </summary>
        public bool EnableAnonymousTypes = true;

        /// <summary>
        /// Add type and schema information to output JSON, using $type, $types, $schema and $map properties.
        /// Default true, but overridden by `EnableAnonymousTypes`
        /// </summary>
        public bool UseExtensions = true;

        private bool _ignoreCaseOnDeserialize = false;
        // ReSharper restore RedundantDefaultFieldInitializer

        internal JsonParameters Clone()
        {
            return new JsonParameters {
                EnableAnonymousTypes = EnableAnonymousTypes,
                IgnoreCaseOnDeserialize = IgnoreCaseOnDeserialize,
                SerializeNullValues = SerializeNullValues,
                ShowReadOnlyProperties = ShowReadOnlyProperties,
                UseExtensions = UseExtensions,
                UseFastGuid = UseFastGuid,
                UseOptimizedDatasetSchema = UseOptimizedDatasetSchema,
                UseUtcDateTime = UseUtcDateTime,
                UsingGlobalTypes = UsingGlobalTypes
            };
        }

        /// <summary>
        /// Restore all settings to their defaults.
        /// </summary>
        public void Reset()
        {
            EnableAnonymousTypes = true;
            IgnoreCaseOnDeserialize = false;
            SerializeNullValues = true;
            ShowReadOnlyProperties = false;
            UseExtensions = true;
            UseFastGuid = true;
            UseOptimizedDatasetSchema = true;
            UseUtcDateTime = true;
            UsingGlobalTypes = true;
        }
    }
}