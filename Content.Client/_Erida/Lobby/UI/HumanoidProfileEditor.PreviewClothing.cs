// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client._Erida.Lobby;
using Content.Shared._Erida.CCVar;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;

namespace Content.Client.Lobby.UI;

/// <summary>
/// Preview clothing modes of the profile editor: all clothing, underwear only or naked.
/// </summary>
public sealed partial class HumanoidProfileEditor
{
    private static readonly PreviewClothing[] PreviewClothingModes =
        [PreviewClothing.All, PreviewClothing.Underwear, PreviewClothing.Naked];

    /// <summary>
    /// Slots kept on the dummy in the underwear mode.
    /// </summary>
    private static readonly string[] UnderwearSlots = ["underweart", "underwearb", "socks"];

    private PreviewClothing _previewClothing;

    private void InitializePreviewClothing()
    {
        _previewClothing = (PreviewClothing) Math.Clamp(_cfgManager.GetCVar(ECCVars.PreviewClothing),
            (int) PreviewClothing.All, (int) PreviewClothing.Naked);

        foreach (var clothing in PreviewClothingModes)
        {
            PreviewClothingButton.AddItem(Loc.GetString($"erida-preview-clothing-{clothing.ToString().ToLowerInvariant()}"), (int) clothing);
        }

        PreviewClothingButton.SelectId((int) _previewClothing);
        PreviewClothingButton.OnItemSelected += args =>
        {
            PreviewClothingButton.SelectId(args.Id);
            _previewClothing = (PreviewClothing) args.Id;
            _cfgManager.SetCVar(ECCVars.PreviewClothing, args.Id);
            _cfgManager.SaveToFile();
            ReloadPreview();
        };
    }

    /// <summary>
    /// Takes off everything except the underwear and empties the hands. Goes from the last slot, so pockets
    /// and belts come off before the jumpsuit they hang on.
    /// </summary>
    private void StripPreviewToUnderwear()
    {
        var inventory = _entManager.System<InventorySystem>();
        if (!inventory.TryGetSlots(PreviewDummy, out var slots))
            return;

        for (var i = slots.Length - 1; i >= 0; i--)
        {
            var slot = slots[i].Name;
            if (Array.IndexOf(UnderwearSlots, slot) >= 0)
                continue;

            if (inventory.TryUnequip(PreviewDummy, slot, out var item, silent: true, force: true, reparent: false))
                _entManager.DeleteEntity(item.Value);
        }

        List<EntityUid> held = [.. _entManager.System<SharedHandsSystem>().EnumerateHeld(PreviewDummy)];
        foreach (var item in held)
        {
            _entManager.DeleteEntity(item);
        }
    }
}
