// SPDX-FileCopyrightText: 2026 Kiwuru / Kitty <olosa15f@gmail.com>
// SPDX-License-Identifier: MIT

using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    public static readonly CVarDef<float> PersonalityBrainDamage = CVarDef.Create("personality.brain_damage", 175f, CVar.SERVERONLY);
    public static readonly CVarDef<float> PersonalityTraumaChance = CVarDef.Create("personality.trauma_chance", 0.25f, CVar.SERVERONLY);
    public static readonly CVarDef<float> PersonalityMinimumInterval = CVarDef.Create("personality.minimum_interval", 180f, CVar.SERVERONLY);
    public static readonly CVarDef<float> PersonalityMaximumInterval = CVarDef.Create("personality.maximum_interval", 600f, CVar.SERVERONLY);
}
