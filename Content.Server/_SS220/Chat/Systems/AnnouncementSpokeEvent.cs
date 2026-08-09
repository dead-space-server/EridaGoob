using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.SS220.Chat.Systems;

public sealed class AnnouncementSpokeEvent : EntityEventArgs
{
    public readonly Filter Source;
    public readonly string AnnouncementSound;
    public readonly AudioParams AnnouncementSoundParams;
    public readonly string Message;
    public readonly string VoiceId;

    public AnnouncementSpokeEvent(Filter source, string announcementSound, AudioParams announcementSoundParams, string message, string voiceId = "Announcer")
    {
        Source = source;
        Message = message;
        VoiceId = voiceId;
        AnnouncementSound = announcementSound;
        AnnouncementSoundParams = announcementSoundParams;
    }
}

public sealed class RadioSpokeEvent : EntityEventArgs
{
    public readonly EntityUid Source;
    public readonly string Message;


    public RadioSpokeEvent(EntityUid source, string message)
    {
        Source = source;
        Message = message;
    }
}
