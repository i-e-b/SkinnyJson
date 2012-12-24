SkinnyJson
==========
Based on FastJson: http://fastjson.codeplex.com/

SkinnyJson has a cleaned-up interface, and handles interface based serialisation much better.
SkinnyJson was designed to handle Event Store messages, and is tuned to
deal with situations where a common interface declaration is available, but the original serialised objects are not available.

TODO:
-----
* Abstract ( (Reader/Writer) | (path-value) ) intermediary to be able to serialise to other formats
* Classes which inherit `List<T>` should use special list deserialiser
* Where object is like: `ISomeThing[]{Subclass1, Subclass2}`, should
  deserialse types and be able to cast and use `is` on the contents.
