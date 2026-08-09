using Content.Shared._Erida.TTS;
using Robust.Shared.Serialization;

namespace Content.Shared._Erida.TTS;

// ReSharper disable once InconsistentNaming
[Serializable, NetSerializable]
public sealed class RequestGlobalTTSEvent : EntityEventArgs
{
    public VoiceRequestType Text { get;}
    public string VoiceId { get; }

    public RequestGlobalTTSEvent(VoiceRequestType text, string voiceId)
    {
        Text = text;
        VoiceId = voiceId;
    }
}
