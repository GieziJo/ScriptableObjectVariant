# Scriptable Object Variant for Unity (Scriptable Object Data Overrider)
## Description
Adds a field to any scriptable object tagged with the `[SOVariant]` attribute that lets you select an original SO (parent) and override selected fields in the child object.

When changing values in the original, values are automagically propagated to the children.

<img src="https://s2.gifyu.com/images/ScriptableObjectOverrideDemo.gif" width="100%">

## Usage
Add the tag `[SOVariant]` before the class header of any ScriptableObject class you want to be overridable, i.e. to be able to create a variant of.

Example:
```csharp
[SOVariant]
[CreateAssetMenu(fileName = "TestScriptable", menuName = "Create new TestScriptable")]
public class TestScriptable : ScriptableObject
{
    [SerializeField] private float myFloat = 3L;
    [SerializeField] private GameObject myGameObject;
    [SerializeField] private int myInt;
    [SerializeField] private TestScriptable myTestScriptable;
}
```

## Implementation
The whole routine is implemented in [Odin](odininspector.com/)'s [`OdinPropertyProcessor`](https://odininspector.com/tutorials/using-property-resolvers-and-attribute-processors/custom-property-processors). The data with the parent and the overriden fields is kept serialized inside the asset's metadata, set in unity with `AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(targetObject)).userData`.

## Installation
> Requires [Odin](odininspector.com/) to be installed
### Using Unity's package manager
Add the line
```
"ch.giezi.tools.scriptableobjectvariant": "https://github.com/GieziJo/ScriptableObjectVariant.git#master"
```
to the file `Packages/manifest.json` under `dependencies`, or in the `Package Manager` add the link [`https://github.com/GieziJo/ScriptableObjectVariant.git#master`](https://github.com/GieziJo/ScriptableObjectVariant.git#master) under `+ -> "Add package from git URL...`.

### Alternative
Copy the content of `Editor` to your Editor folder inside Unity.

## Known issues and tweakes to be made

### Efficiency
The attribute `[SOVariant]` only acts as tagger, which is then looked for in `SOVariantAttributeProcessor:OdinPropertyProcessor -> ProcessMemberProperties`, where the first line reads:
```csharp
if(!Property.Attributes.Select(attribute => attribute.GetType()).Contains(typeof(SOVariantAttribute)))
    return;
```
The problem with this is that `SOVariantAttributeProcessor` is thus set to be called for every `ScriptableObject`:
```csharp
public class SOVariantAttributeProcessor<T> : OdinPropertyProcessor<T> where T : ScriptableObject
```
There is probably a way to directly call `SOVariantAttributeProcessor` from the attribute, but I haven't found how.

### Selecting the parent object
The selected parent should be of the exact same class as the overriden item (otherwise fields might be missing) and should not be the child itself.
This check is currently done when setting the parent as:
 ```csharp
if (parent.GetType() != target.GetType())
{
    Debug.Log("Only equal types can be selected as parent");
    return;
}

if (AssetDatabase.GetAssetPath(parent) == AssetDatabase.GetAssetPath(target))
{
    Debug.Log("You can't select the same object as parent");
    return;
}
 ```
It would be alot better to directly filter the possible candidates when selecting in the object, but adding the `AssetSelector` attribute with a filter, or building a custom `ValueDropdown` both did not work, not sure why.

### Data serialization (parent and overriden fields)
As mentioned above, the serialized data is kept in `userData`, but is set with `_import.userData = *mySerializedDataString*`. This would override any other data that would come to this field from other scripts, might be an issue.

### Saving Data
Saving data to the `.meta` file occures when the asset is deselected (`Selection.selectionChanged += OnSelectionChanged;`). It would be better to tie this to the serialization and deserialization of the data itself, but unity does not seem to expose the process as a delegate (not sure?), so I haven't found a way to tap into this routine.