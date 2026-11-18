// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Shared._Orion.InteQ;

[Serializable, NetSerializable]
public enum InteQCallsignUIKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class InteQCallsignBuiState(string name) : BoundUserInterfaceState
{
    public readonly string Name = name;
}

[Serializable, NetSerializable]
public sealed class InteQCallsignSetNameMessage(string name) : BoundUserInterfaceMessage
{
    public readonly string Name = name;
}

[Serializable, NetSerializable]
public sealed class InteQCallsignRandomMessage : BoundUserInterfaceMessage;
