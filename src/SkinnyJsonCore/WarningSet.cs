using System.Collections.Generic;

namespace SkinnyJson
{
    internal class WarningSet
    {
        private readonly HashSet<string> _messages = new();
        public bool Any => _messages.Count > 0;

        public void Append(string msg)
        {
            _messages.Add(msg);
        }

        public override string ToString()
        {
            if (_messages.Count < 1) return "";
            return string.Join("; ", _messages);
        }
    }
}