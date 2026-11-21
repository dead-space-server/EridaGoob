// SPDX-FileCopyrightText: 2026 Kiwuru / Kitty <olosa15f@gmail.com>
// SPDX-License-Identifier: MIT

using Content.Server.Administration.Logs;
using Content.Server.Preferences.Managers;
using Content.Shared._Erida.SplitPersonality;
using Content.Shared.Actions;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.DetailExaminable;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Systems;
using Content.Shared.Preferences;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.GameObjects;

namespace Content.Server._Erida.SplitPersonality;

public sealed partial class SplitPersonalitySystem : EntitySystem
{
    [Dependency] private readonly IDependencyCollection _dependencies = default!;
    [Dependency] private readonly IServerPreferencesManager _preferences = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly MarkingManager _markings = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;
    [Dependency] private readonly IdentitySystem _identity = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnSpawn);
        SubscribeLocalEvent<SplitPersonalityComponent, SplitPersonalitySwitchActionEvent>(OnSwitch);
    }

    private void OnSpawn(PlayerSpawnCompleteEvent args)
    {
        if (args.Profile.AlternatePersonalitySlot is not { } slot ||
            !_preferences.TryGetCachedPreferences(args.Player.UserId, out var preferences) ||
            slot == preferences.SelectedCharacterIndex ||
            !preferences.Characters.TryGetValue(slot, out var saved) ||
            saved is not HumanoidCharacterProfile alternate ||
            alternate.Species != args.Profile.Species ||
            !TryComp<HumanoidAppearanceComponent>(args.Mob, out var appearance))
            return;

        var component = EnsureComp<SplitPersonalityComponent>(args.Mob);
        component.Primary = args.Profile.Clone();
        component.Alternate = alternate.Clone();
        component.Alternate.EnsureValid(args.Player, _dependencies);
        if (component.Alternate.Species != component.Primary.Species)
        {
            RemComp<SplitPersonalityComponent>(args.Mob);
            return;
        }
        component.PrimaryColors = PersonalityColors.Copy(appearance.MarkingSet);
        component.AlternateColors = BuildPalette(component.Alternate);
        _actions.AddAction(args.Mob, ref component.SwitchAction, "ActionSwitchPersonality");
    }

    private MarkingSet BuildPalette(HumanoidCharacterProfile profile)
    {
        var palette = new MarkingSet();
        foreach (var marking in profile.Appearance.Markings)
        {
            if (!_markings.TryGetMarking(marking, out var prototype))
                continue;
            if (!palette.Markings.TryGetValue(prototype.MarkingCategory, out var list))
                palette.Markings[prototype.MarkingCategory] = list = new();
            list.Add(new Marking(marking));
        }
        palette.Markings[MarkingCategories.Hair] = new()
        {
            new Marking(profile.Appearance.HairStyleId, new[] { profile.Appearance.HairColor })
                { Gradient = profile.Appearance.HairGradient },
        };
        palette.Markings[MarkingCategories.FacialHair] = new()
        {
            new Marking(profile.Appearance.FacialHairStyleId, new[] { profile.Appearance.FacialHairColor })
                { Gradient = profile.Appearance.FacialHairGradient },
        };
        return palette;
    }

    private void OnSwitch(Entity<SplitPersonalityComponent> ent, ref SplitPersonalitySwitchActionEvent args)
    {
        if (args.Handled || args.Performer != ent.Owner || !_mobState.IsAlive(ent.Owner) ||
            !TryComp<HumanoidAppearanceComponent>(ent, out var appearance) ||
            appearance.Species != ent.Comp.Primary.Species)
            return;

        ent.Comp.AlternateActive = !ent.Comp.AlternateActive;
        var profile = ent.Comp.AlternateActive ? ent.Comp.Alternate : ent.Comp.Primary;
        _metadata.SetEntityName(ent, profile.Name);
        if (_config.GetCVar(CCVars.FlavorText))
        {
            var details = EnsureComp<DetailExaminableComponent>(ent);
            details.SetProfile(profile);
            Dirty(ent, details);
        }
        _humanoid.SetTTSVoice(ent, profile.TTSVoice, appearance);
        _humanoid.SetBarkVoice(ent, profile.BarkVoice, appearance);
        // Only Arcan (Demon) recolors its body. Never load an entire profile into a live body.
        if (appearance.Species.Id == "Demon")
        {
            _humanoid.ApplyPersonalityColors(ent, profile.Appearance,
                ent.Comp.AlternateActive ? ent.Comp.AlternateColors : ent.Comp.PrimaryColors, appearance);
        }
        Dirty(ent, appearance);
        _identity.QueueIdentityUpdate(ent);
        _adminLog.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(ent):player} switched personality to {profile.Name}");
        args.Handled = true;
    }
}
