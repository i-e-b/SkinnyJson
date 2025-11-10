namespace SkinnyJson
{
    /// <summary>
    /// Base class for custom JSON converters
    /// </summary>
    public interface ICustomJsonConverter<T>
    {
        /// <summary>
        /// Convert an object extracted from the source JSON into the target type
        /// </summary>
        public T? FromJson(object value);

        /// <summary>
        /// Convert a value to a JSON object.
        /// This should return an object value that will be converted to JSON normally
        /// </summary>
        public object? ToJson(T? source);
    }
}