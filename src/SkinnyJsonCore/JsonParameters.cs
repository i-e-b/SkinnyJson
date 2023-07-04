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
        public bool ShowReadOnlyProperties
        {
            get => _showReadOnlyProperties;
            set {
                if (_showReadOnlyProperties != value) Json.ClearCaches(); // we cache type inspections
                _showReadOnlyProperties = value;
            }
        }

        /// <summary>
        /// Declare types once at the start of a document. Otherwise declare in each object.
        /// Default true, but overridden by `EnableAnonymousTypes`
        /// </summary>
        public bool UsingGlobalTypes = true;

        // ReSharper disable CommentTypo
        /// <summary>
        /// <p>Default false.</p>
        /// Allow case insensitive matching on deserialise. Also ignores underscores, dashes, and spaces in object keys.
        /// </summary>
        /// <remarks>
        /// If case insensitive matching is enabled, these are all considered equal keys:
        /// <ul>
        /// <li>CASE_INSENSITIVE</li>
        /// <li>CASE-INSENSITIVE</li>
        /// <li>CASEINSENSITIVE</li>
        /// <li>case_insensitive</li>
        /// <li>case-insensitive</li>
        /// <li>Case Insensitive</li>
        /// <li>case insensitive</li>
        /// <li>CaseInsensitive</li>
        /// <li>caseInsensitive</li>
        /// <li>caseinsensitive</li>
        /// </ul>
        /// </remarks>
        // ReSharper restore CommentTypo
        public bool IgnoreCaseOnDeserialize
        {
            get => _ignoreCaseOnDeserialize;
            set {
                if (_ignoreCaseOnDeserialize != value) Json.ClearCaches(); // we cache case transformation results
                _ignoreCaseOnDeserialize = value;
            }
        }

        /// <summary>
        /// Default true.
        /// <p/>
        /// When true, and an object is deserialised which has values in the JSON side, but none of them match
        /// the class definition, then deserialisation fails.
        /// <p/>
        /// When false, an object with mismatching data is allowed to pass, and may result in empty objects
        /// in the class model returned.
        /// </summary>
        public bool StrictMatching = true;

        /// <summary>
        /// Default true. If false, source type information will be included in serialised output.
        /// <p/>
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

        private bool _showReadOnlyProperties = false;
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