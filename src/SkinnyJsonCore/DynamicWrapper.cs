﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;

namespace SkinnyJson
{
    /// <summary>
    /// Wrapper around a parser object
    /// </summary>
    public class DynamicWrapper : DynamicObject
    {
        internal readonly object Parsed;
        private readonly List<ChainStep> _chain;

        /// <summary>
        /// Wrap a parser output object in a dynamic object
        /// </summary>
        public DynamicWrapper(object parsed)
        {
            Parsed = parsed;
            _chain = new List<ChainStep>();
        }

        /// <summary>
        /// Wrap parser output object in a dynamic object with a query path chain
        /// </summary>
        public DynamicWrapper(object parsed, List<ChainStep> oldChain, ChainStep nextStep)
        {
            Parsed = parsed;
            _chain = new List<ChainStep>(oldChain) { nextStep };
        }



        /// <summary>
        /// Syntax: dyn.path.elems()
        /// Access value at position
        /// </summary>
        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            _chain.Add(ChainStep.PropertyStep(binder.Name));
            result = null;
            switch (args.Length)
            {
                case 0: // Get value
                    // walk the chain, walking the parsed object. On missing or mismatch, short-circuit and return null
                    result = Walk(Parsed, _chain);
                    break;
                case 1: // Set value? Throw?
                    // TODO: implement
                    break;
                default: // don't understand!
                    throw new ArgumentException("Multiple arguments not allowed (at " + GetChainString() + ")");
            }
            return true;
        }

        /// <summary>
        /// Syntax: dyn.path.elems[0]()
        /// Try to directly invoke an instance
        /// </summary>
        public override bool TryInvoke(InvokeBinder binder, object[] args, out object result)
        {
            result = null;
            switch (args.Length)
            {
                case 0: // Get value
                    // walk the chain, walking the parsed object. On missing or mismatch, short-circuit and return null
                    result = Walk(Parsed, _chain);
                    break;
                case 1: // Set value? Throw?
                    // TODO: implement
                    break;
                default: // don't understand!
                    throw new ArgumentException("Multiple arguments not allowed (at " + GetChainString() + ")");
            }
            return true;
        }

        /// <summary>
        /// Syntax: dyn.parent.child.grandchild
        /// Add a step in the path to access
        /// </summary>
        public override bool TryGetMember (GetMemberBinder binder, out object result) {
            // No actual access here, just record the request.
            result = Add(ChainStep.PropertyStep(binder.Name));
            return true;
        }


        /// <summary>
        /// Syntax: Check.That....[predicate]
        /// applies predicate to matching paths.
        /// </summary>
        public override bool TryGetIndex (GetIndexBinder binder, object[] indexes, out object result) {
            // we've been passed an enumeration argument
            switch (indexes.Length)
            {
                case 1: // either like `list[1]` or `list["any"]`
                    if (indexes[0] is int i) result = Add(ChainStep.IndexStep(i));
                    else result = Add(ChainStep.PropertyStep(indexes[0].ToString()));
                    break;
                default: // don't understand!
                    throw new ArgumentException("Multi-dimensional indexes not allowed (at " + GetChainString() + ")");
            }
            return true;
        }

        /// <summary>
        /// Syntax: dyn.path.elems[0] = x
        /// Update wrapped object at path with a new array item
        /// </summary>
        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
        {
            // find the path so far, then update found array
            var targ = Walk(Parsed, _chain);

            if (indexes.Length != 1) throw new ArgumentException("Multi-dimensional indexes not allowed (at " + GetChainString() + ")");

            switch (indexes[0])
            {
                case int i:
                    if (!(targ is ArrayList arr)) return false;
                    arr[i] = value;
                    return true;
                case string s:
                    if (!(targ is Dictionary<string, object> dict)) return false;
                    if (dict.ContainsKey(s)) dict[s] = value;
                    else dict.Add(s, value);
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Syntax: dyn.path.elems = x
        /// Update wrapped object at path with a new value
        /// </summary>
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            // find the object at path so far. If dict, update entry. Otherwise change value
            var targ = Walk(Parsed, _chain);
            
            if (targ is Dictionary<string, object> dict) {
                if (dict.ContainsKey(binder.Name)) dict[binder.Name] = value;
                else dict.Add(binder.Name, value);
                return true;
            }

            // I can't think how it would make sense to get here, so I will reject until I find a good case
            return false;
        }

        /// <summary>
        /// Handle conversions
        /// </summary>
        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            var targ = Walk(Parsed, _chain);

            var tc = new TypeConverter();
            try
            {
                result = tc.ConvertTo(targ, binder.Type);
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }

        private string GetChainString()
        {
            return string.Join("", _chain.Select(c =>
            {
                if (c.Name != null) return "." + c.Name;
                if (c.SingleIndex != null) return "[" + c.SingleIndex + "]";
                return "<?>";
            }));
        }

        private dynamic Add (ChainStep name) {
            return new DynamicWrapper(Parsed, _chain, name);
        }

        private static object Walk(object parsed, List<ChainStep> chain)
        {
            var targ = parsed;
            foreach (var step in chain)
            {
                if (step.IsIndex)
                {
                    if ( ! (targ is ArrayList arr) ) return null;
                    if (arr.Count > step.SingleIndex) {
                        targ = arr[(int)step.SingleIndex];
                        continue;
                    }
                    return null;
                }

                // expecting a property name now
                if ( ! (targ is Dictionary<string, object> dict)) return null;
                if ( dict.ContainsKey(step.Name) ) {
                    targ = dict[step.Name];
                    continue;
                }

                return null;
            }

            return targ;
        }

        /// <summary>
        /// Helper.
        /// </summary>
        public delegate object Valuate();
        
        /// <summary>
        /// Cast path to an integer value
        /// </summary>
        public static explicit operator Valuate(DynamicWrapper d){
            return () => Walk(d.Parsed, d._chain);
        }

        /// <summary>
        /// Cast to object by resolving path
        /// </summary>
        public static object ToObject(DynamicWrapper d)
        {
            return Walk(d.Parsed, d._chain);
        }

        /// <summary>
        /// Cast path to an integer value
        /// </summary>
        public static explicit operator int(DynamicWrapper d){
            var targ = Walk(d.Parsed, d._chain);
            if (targ == null) throw new NullReferenceException();
            return int.Parse(targ.ToString());
        }

        /// <summary>
        /// Cast path to an integer value
        /// </summary>
        public static explicit operator string(DynamicWrapper d){
            var targ = Walk(d.Parsed, d._chain);
            if (targ == null) return null;
            return targ.ToString();
        }
    }
}