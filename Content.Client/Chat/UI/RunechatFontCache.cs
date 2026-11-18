using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Chat.UI;

public sealed class RunechatFontCache
{
    [Dependency] private IResourceCache _resources = default!;

    private const string FallbackFontPath = "/Fonts/unifont-17.0.03.otf"; // Erida edit

    private readonly Dictionary<(string, int), Font> _fonts = new();
    private readonly Dictionary<(string, int), Stylesheet> _sheets = new();

    public Font GetFont(string path, int size)
    {
        if (_fonts.TryGetValue((path, size), out var font))
            return font;

        // Erida start
        font = new StackedFont(
            new VectorFont(_resources.GetResource<FontResource>(path), size),
            new VectorFont(_resources.GetResource<FontResource>(FallbackFontPath), size));
        // Erida end
        _fonts[(path, size)] = font;
        return font;
    }

    public Stylesheet GetStylesheet(string path, int size)
    {
        if (_sheets.TryGetValue((path, size), out var sheet))
            return sheet;

        sheet = new Stylesheet(new[]
        {
            new StyleRule(
                new SelectorElement(typeof(RichTextLabel), null, null, null),
                new[] { new StyleProperty(Label.StylePropertyFont, GetFont(path, size)) }),
        });
        _sheets[(path, size)] = sheet;
        return sheet;
    }
}
