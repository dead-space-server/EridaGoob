using Robust.Shared.Configuration;

namespace Content.Shared._BlackM.CCVar;

[CVarDefs]
public sealed class BlackMCVars
{
    public static readonly CVarDef<float> LightBloomStrength =
        CVarDef.Create("blackm.light_bloom_strength", 0.7f, CVar.CLIENTONLY | CVar.ARCHIVE);
}
