using Robust.Shared.Serialization;

namespace Content.Goobstation.Shared.Paint;

[Serializable, NetSerializable]
public enum GoobPhotocopierVisuals : byte
{
    State,
}

[Serializable, NetSerializable]
public enum PhotocopierState : byte
{
    Idle,
    Scanning,
    Printing,
}
