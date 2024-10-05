using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace SkinnyJson
{
    /// <summary>
    /// Settings for serialising and deserialising.
    /// </summary>
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class JsonSettings
    {
        #region Standard sets

        /// <summary>
        /// Date formats we expect from JSON strings
        /// </summary>
        public static readonly string[] StandardDateFormatsInPreferenceOrder = {
            // ReSharper disable StringLiteralTypo
            "yyyy-MM-ddTHH:mm:ss",  // correct ISO 8601 'extended'
            "yyyy-MM-dd HH:mm:ss",  // Common variant of ISO 8601
            "yyyy-MM-dd H:mm:ss",   // Erlang style
            "yyyy-MM-ddTH:mm:ss",   // Erlang style with a T
            "yyyy-MM-ddTHH:mm:ssK", // with zone specifier
            "yyyy-MM-dd HH:mm:ssK", // with zone specifier, but no T
            "yyyy-MM-ddTHHmmss",    // ISO 8601 'basic'
            "yyyy-MM-dd",           // ISO 8601, just the date
            // ReSharper restore StringLiteralTypo
        };

        /// <summary>
        /// Default SkinnyJson parameters.
        /// <p>Uses large number support, Base64 Guids, anonymous types, case insensitive matching, and strict matching.</p>
        /// <p>Excludes source type information and global types</p>
        /// </summary>
        public static readonly JsonSettings Default = new() {
            UseOptimizedDatasetSchema = true,
            UseFastGuid = true,
            SerializeNullValues = true,
            UseUtcDateTime = true,
            ShowReadOnlyProperties = false,
            UsingGlobalTypes = false,
            IgnoreCaseOnDeserialize = true,
            StrictMatching = true,
            EnableAnonymousTypes = true,
            UseTypeExtensions = false,
            UseWideNumbers = true,
            DateFormats = StandardDateFormatsInPreferenceOrder
        };
        
        /// <summary>
        /// SkinnyJson parameters, with some less-supported features turned off.
        /// <p>Uses anonymous types, and strict matching.</p>
        /// <p>Excludes large number support, case insensitive matching, Base64 Guids, source type information and global types</p>
        /// </summary>
        public static readonly JsonSettings Compatible = new() {
            UseOptimizedDatasetSchema = true,
            UseFastGuid = false,
            SerializeNullValues = true,
            UseUtcDateTime = true,
            ShowReadOnlyProperties = false,
            UsingGlobalTypes = false,
            IgnoreCaseOnDeserialize = false,
            StrictMatching = true,
            EnableAnonymousTypes = true,
            UseTypeExtensions = false,
            UseWideNumbers = false,
            DateFormats = StandardDateFormatsInPreferenceOrder
        };
        
        /// <summary>
        /// SkinnyJson parameters for reading and writing strongly-typed messages.
        /// This is not compatible with most other JSON libraries.
        /// <p>Uses source type information and global types</p>
        /// <p>Excludes anonymous types, case insensitivity</p>
        /// </summary>
        public static readonly JsonSettings TypeConstrained = new() {
            UseOptimizedDatasetSchema = true,
            UseFastGuid = true,
            SerializeNullValues = true,
            UseUtcDateTime = true,
            ShowReadOnlyProperties = false,
            UsingGlobalTypes = true,
            IgnoreCaseOnDeserialize = false,
            StrictMatching = true,
            EnableAnonymousTypes = false,
            UseTypeExtensions = true,
            UseWideNumbers = true,
            DateFormats = StandardDateFormatsInPreferenceOrder
        };
        #endregion Standard sets
        
        #region Settings
// ReSharper disable RedundantDefaultFieldInitializer

        /// <summary>
        /// String encoding to use for streams, when no specific encoding is provided.
        /// Initial value is UTF8.
        /// </summary>
        public Encoding StreamEncoding { get; init; } = new UTF8Encoding(false);

        /// <summary>
        /// String-to-Date and Date-to-String formats, in descending preference order.
        /// Highest preference will be used for serialisation.
        /// </summary>
        public string[] DateFormats { get; init; } = StandardDateFormatsInPreferenceOrder;

        /// <summary>
        /// If set to true, numeric values will be parsed as high-precision types.
        /// Otherwise, numeric values are parsed as double-precision floats.
        /// </summary>
        public bool UseWideNumbers { get; init; } = true;
        
        /// <summary>
        /// Use a special format for Sql Datasets. Default true
        /// </summary>
        public bool UseOptimizedDatasetSchema { get; init; } = true;

        /// <summary>
        /// Use Base64 encoding for Guids. If false, uses Hex.
        /// Default true
        /// </summary>
        public bool UseFastGuid { get; init; } = true;

        /// <summary>
        /// Insert null values into JSON output. Otherwise remove field.
        /// Default true
        /// </summary>
        public bool SerializeNullValues { get; init; } = true;

        /// <summary>
        /// Force date times to UTC. Default true
        /// </summary>
        public bool UseUtcDateTime { get; init; } = true;

        /// <summary>
        /// Serialise properties that can't be written on deserialise. Default false
        /// </summary>
        public bool ShowReadOnlyProperties { get; init; } = false;

        /// <summary>
        /// Declare types once at the start of a document. Otherwise declare in each object.
        /// Default true, but overridden by `EnableAnonymousTypes`
        /// </summary>
        public bool UsingGlobalTypes { get; init; } = false;

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
        public bool IgnoreCaseOnDeserialize { get; init; } = true;

        /// <summary>
        /// Default true.
        /// <p/>
        /// When true, and an object is deserialised which has values in the JSON side, but none of them match
        /// the class definition, then deserialisation fails.
        /// <p/>
        /// When false, an object with mismatching data is allowed to pass, and may result in empty objects
        /// in the class model returned.
        /// </summary>
        public bool StrictMatching { get; init; } = true;

        /// <summary>
        /// Default true. If false, source type information will be included in serialised output.
        /// <p/>
        /// Overrides `UseExtensions` and `UsingGlobalTypes` 
        /// Directly serialising an anonymous type will use these settings for that call, without needing a global setting.
        /// </summary>
        public bool EnableAnonymousTypes { get; init; } = true;

        /// <summary>
        /// Default true. When set, the deserialiser will try to find and write to backing fields for get-only properties.
        /// </summary>
        public bool SearchForBackingFields { get; init; } = true;

        /// <summary>
        /// Add type and schema information to output JSON, using $type, $types, $schema and $map properties.
        /// </summary>
        public bool UseTypeExtensions { get; init; } = false;
        // ReSharper restore RedundantDefaultFieldInitializer
        #endregion Settings
        
        #region Mutators
        
        /// <summary>
        /// Copy of these settings, but with type extensions disabled and anonymous types enabled
        /// </summary>
        public JsonSettings WithAnonymousTypes()
        {
            return new JsonSettings {
                EnableAnonymousTypes = true,
                UsingGlobalTypes = false,
                UseTypeExtensions = false,
                
                IgnoreCaseOnDeserialize = IgnoreCaseOnDeserialize,
                SerializeNullValues = SerializeNullValues,
                ShowReadOnlyProperties = ShowReadOnlyProperties,
                UseFastGuid = UseFastGuid,
                UseOptimizedDatasetSchema = UseOptimizedDatasetSchema,
                UseUtcDateTime = UseUtcDateTime,
                StrictMatching = StrictMatching,
                StreamEncoding = StreamEncoding,
                DateFormats = DateFormats
            };
        }

        /// <summary>
        /// Change the byte-to-string encoding type
        /// </summary>
        public JsonSettings WithEncoding(Encoding encoding)
        {
            return new JsonSettings {
                StreamEncoding = encoding,
                
                EnableAnonymousTypes = EnableAnonymousTypes,
                UsingGlobalTypes = UsingGlobalTypes,
                UseTypeExtensions = UseTypeExtensions,
                IgnoreCaseOnDeserialize = IgnoreCaseOnDeserialize,
                SerializeNullValues = SerializeNullValues,
                ShowReadOnlyProperties = ShowReadOnlyProperties,
                UseFastGuid = UseFastGuid,
                UseOptimizedDatasetSchema = UseOptimizedDatasetSchema,
                UseUtcDateTime = UseUtcDateTime,
                StrictMatching = StrictMatching,
                DateFormats = DateFormats
            };
        }
        
        /// <summary>
        /// Set the acceptable date formats, in descending preference order.
        /// Highest preference will be used for serialisation.
        /// </summary>
        public JsonSettings WithDateFormats(params string[] dateFormats)
        {
            return new JsonSettings {
                DateFormats = dateFormats,
                
                StreamEncoding = StreamEncoding,
                EnableAnonymousTypes = EnableAnonymousTypes,
                UsingGlobalTypes = UsingGlobalTypes,
                UseTypeExtensions = UseTypeExtensions,
                IgnoreCaseOnDeserialize = IgnoreCaseOnDeserialize,
                SerializeNullValues = SerializeNullValues,
                ShowReadOnlyProperties = ShowReadOnlyProperties,
                UseFastGuid = UseFastGuid,
                UseOptimizedDatasetSchema = UseOptimizedDatasetSchema,
                UseUtcDateTime = UseUtcDateTime,
                StrictMatching = StrictMatching
            };
        }
        
        /// <summary>
        /// Add case sensitive matching to these parameters
        /// </summary>
        public JsonSettings WithCaseSensitivity()
        {
            return new JsonSettings {
                IgnoreCaseOnDeserialize = false,
                
                DateFormats = DateFormats,
                StreamEncoding = StreamEncoding,
                EnableAnonymousTypes = EnableAnonymousTypes,
                UsingGlobalTypes = UsingGlobalTypes,
                UseTypeExtensions = UseTypeExtensions,
                SerializeNullValues = SerializeNullValues,
                ShowReadOnlyProperties = ShowReadOnlyProperties,
                UseFastGuid = UseFastGuid,
                UseOptimizedDatasetSchema = UseOptimizedDatasetSchema,
                UseUtcDateTime = UseUtcDateTime,
                StrictMatching = StrictMatching
            };
        }

        #endregion Mutators
        
        /// <summary>
        /// Unique key for this parameter set.
        /// Used to key various caches.
        /// </summary>
        internal int ParameterKey()
        {
            var x =
                + I(UseOptimizedDatasetSchema, 0)
                + I(UseFastGuid, 1)
                + I(SerializeNullValues, 2)
                + I(UseUtcDateTime, 3)
                + I(ShowReadOnlyProperties, 4)
                + I(UsingGlobalTypes, 5)
                + I(IgnoreCaseOnDeserialize, 6)
                + I(StrictMatching, 7)
                + I(EnableAnonymousTypes, 8)
                + I(UseTypeExtensions, 9);

            return x;

            int I(bool b, int s) => b ? 1 << s : 0;
        }

    }
}