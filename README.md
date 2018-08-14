SkinnyJson
==========
Based on FastJson: https://github.com/mgholam/fastJSON

Nuget:

* Default: https://www.nuget.org/packages/SkinnyJson/
* Strong-named: https://www.nuget.org/packages/SkinnyJson/

The default is usually ahead of the strong-named version

SkinnyJson has a cleaned-up interface, and handles interface based serialisation much better.
SkinnyJson was designed to handle Event Store messages, and is tuned to
deal with situations where a common interface declaration is available, but the original serialised objects are not available.

To adjust settings globally, look in `SkinnyJson.Json.DefaultParameters`

Common use cases:
----------

Deserialise a known type:
```csharp
IMyInterface values = Json.Defrost<IMyInterface>(jsonString);
```
Serialise any object to JSON:
```csharp
string jsonString = Json.Freeze(myObject);
```

Pretty print a JSON string: (there is also a streaming version for very large files)
```csharp
var newString = Json.Beautify(oldString);
```

Create a deep copy of an object:
```csharp
var newObject = Json.Clone(oldObject);
```

Deserialise to a dynamic type for unknown schemas:
```csharp
dynamic obj = Json.DefrostDynamic(jsonString);
Console.WriteLine(obj.MyProperty.MyArray[4].Prop2()); // use `()` to read a value.
var missing = obj.NotHere.child.grandchild(); // results in null. Dynamic does null propagation.

obj.MyProperty.other = "hello"; // can update properties
var updatedJson = Json.Freeze(obj); // and serialise the result
```

Inline editing:
```csharp
string newJson = Json.Edit(oldJson, d => {
    d.myProperty.updated = true;
    d.info.updates[0].dateTime = DateTime.Now.ToString();
});
```

See the test cases for deeper examples
