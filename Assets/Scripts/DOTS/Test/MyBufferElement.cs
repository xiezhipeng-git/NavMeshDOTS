using Unity.Entities;

// InternalBufferCapacity specifies how many elements a buffer can have before
// the buffer storage is moved outside the chunk.
[GenerateAuthoringComponent]
[InternalBufferCapacity(8)]
public struct MyBufferElement : IBufferElementData
{
    // Actual value each buffer element will store.
    public int Value;

    // The following implicit conversions are optional, but can be convenient.
    public static implicit operator int(MyBufferElement e)
    {
        return e.Value;
    }

    public static implicit operator MyBufferElement(int e)
    {
        return new MyBufferElement { Value = e };
    }
}