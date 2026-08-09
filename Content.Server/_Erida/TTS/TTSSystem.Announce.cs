using System.Threading.Tasks;
using Content.Server._Erida.TTS;
using Content.Server.SS220.Chat.Systems;
using Content.Shared._Erida.TTS;
using Content.Shared.SS220.AnnounceTTS;

// ReSharper disable once CheckNamespace
namespace Content.Server._Erida.TTS;

public sealed partial class TTSSystem
{
    private async void OnAnnouncementSpoke(AnnouncementSpokeEvent args)
    {
        if (!_isEnabled ||
            args.Message.Length > MaxMessageChars * 2 ||
            !_prototypeManager.TryIndex<TTSVoicePrototype>(args.VoiceId, out var protoVoice))
        {
            RaiseNetworkEvent(new AnnounceTTSEvent(new byte[]{}, args.AnnouncementSound, args.AnnouncementSoundParams), args.Source);
            return;
        }

        byte[]? soundData = null;
        try
        {
            soundData = await GenerateTtsAnnouncement(args.Message, protoVoice.Speaker);

        }
        catch (Exception)
        {
            // skip!
        }
        soundData ??= new byte[] { };
        RaiseNetworkEvent(new AnnounceTTSEvent(soundData, args.AnnouncementSound, args.AnnouncementSoundParams), args.Source);
    }

    private async Task<byte[]?> GenerateTtsAnnouncement(string text, string speaker)
    {
        var textSanitized = Sanitize(text);
        if (textSanitized == "") return null;
        if (char.IsLetter(textSanitized[^1]))
            textSanitized += ".";

        var position = textSanitized.LastIndexOf("Отправитель", StringComparison.InvariantCulture);
        if (position != -1)
        {
            textSanitized = textSanitized[..position];
        }

        var textSsml = ToSsmlText(textSanitized, SoundTraits.RateFast);

        return await _ttsManager.ConvertTextToSpeech(speaker, textSsml);
    }
}
