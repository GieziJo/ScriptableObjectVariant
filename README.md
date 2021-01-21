# Scriptable Object Data Overrider for Unity (Scriptable Object Variant)
## Description
Adds a field to any scriptable object tagged with the `[Parent]` attribute that lets you select a parent and override selected fields in the child object.

![Demo](https://media.giphy.com/media/CqxAViacRQvG5JQMsp/giphy.gif)

## Usage
Add the tag `[Parent]` before the class header of any ScriptableObject class you want to be overridable.

Example:
```csharp
[Parent]
[CreateAssetMenu(menuName = "Create TestScriptable", fileName = "TestScriptable", order = 0)]
public class TestScriptable : SerializedScriptableObject
{
    public GameObject GameObject;
    public float Size;
    [SerializeField] private float secondSIze;
}
```

## Implementation
The whole routine is implemented in [Odin](odininspector.com/)'s [`OdinPropertyProcessor`](https://odininspector.com/tutorials/using-property-resolvers-and-attribute-processors/custom-property-processors). The data with the parent and the overriden fields is kept serialized inside the asset's metadata, set in unity with `AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(targetObject)).userData`.

## Installation
> Requires [Odin](odininspector.com/) to be installed
### Using Unity's package manager
Add the line
```
"ch.giezi.tools.scriptableobjectdataoverrider": "https://github.com/GieziJo/ScriptableObjectDataOverrider.git#master"
```
to the file `Packages/manifest.json` under `dependencies`.

### Alternative
Copy the content of `Editor` to your Editor folder inside Unity.

## Known issues and tweakes
### Efficiency
The attribute `[Parent]` only acts as tagger, which is then looked for in `ParentAttributeProcessor:OdinPropertyProcessor -> ProcessMemberProperties`, where the first line reads:
```csharp
if(!Property.Attributes.Select(attribute => attribute.GetType()).Contains(typeof(ParentAttribute)))
    return;
```
The problem with this is that `ParentAttributeProcessor` is thus set to be called for every `ScriptableObject`:
```csharp
public class ParentAttributeProcessor<T> : OdinPropertyProcessor<T> where T : ScriptableObject
```
There is probably a way to direclty call `ParentAttributeProcessor` from the attribute, but I haven't found how.

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

### Making not overriden fields `ReadOnly`
Adding `ReadOnlyAttribute` (_actually `DisableIfAttribute( "@true" )` because `ReadOnlyAttribute` did nothing ([bug?](https://bitbucket.org/sirenix/odin-inspector/issues/747/in-odinpropertyprocessor))_) to fields which are not overriden yielded in the checkbox being disabled. The workaround for now is a rect drawn ontop of the field, which "greys out" the field. Values can still be changed but reverse to the not overriden parent value when deselected.

## Data serialization (parent and overriden fields)
As mentioned above, the serialized data is kept in `userData`, but is set with `_import.userData = *mySerializedDataString*`. This would override any other data that would come to this field from other scripts, might be an issue.