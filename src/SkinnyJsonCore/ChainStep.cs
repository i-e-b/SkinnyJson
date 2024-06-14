namespace SkinnyJson
{
    /// <summary>
    /// Represents a step in a dynamic call chain
    /// </summary>
    internal class ChainStep
    {
        /// <summary>
        /// Name of property
        /// </summary>
        public string? Name { get; set; }
        /// <summary>
        /// Index lookup, if any
        /// </summary>
        public int? SingleIndex { get; set; }

        /// <summary>
        /// True if there is an index
        /// </summary>
        public bool IsIndex => SingleIndex != null;

        /// <summary>
        /// Make a property lookup
        /// </summary>
        public static ChainStep PropertyStep(string? name)
        {
            return new ChainStep{
                Name = name,
                SingleIndex = null
            };
        }

        /// <summary>
        /// Make an indexed lookup
        /// </summary>
        public static ChainStep IndexStep(int index)
        {
            return new ChainStep{
                Name = null,
                SingleIndex = index
            };
        }

    }
}