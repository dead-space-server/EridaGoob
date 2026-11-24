// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Client._Erida.Humanoid;
using Content.Client._Erida.Lobby.UI;
using Content.Client._Erida.Stylesheets;
using Content.Client._Erida.UserInterface;
using Content.Client._Erida.UserInterface.Controls;
using Content.Client.Lobby.UI.Roles;
using Content.Client.Sprite;
using Content.Shared._Erida.Roles;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Enums;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private const float ProfileActionsMinWidth = 200f;
    private const float FormLabelWidth = 200f;
    private const float FormFieldWidth = 260f;
    private const float FormRowHeight = 30f;
    private const float FormGap = 6f;
    private const float CustomSpeciesButtonWidth = 110f;
    private const float ResetButtonWidth = 28f;
    private const float ResetIconSize = 16f;
    private const float CardGap = 10f;

    private JobAlternateTitleSystem? _jobAltTitles;
    private bool _profilePreviewQueued;

    private ShapedButton _saveAction = default!;
    private ShapedButton _resetAction = default!;
    private Popup _fileMenu = default!;
    private BoxContainer _fileMenuRows = default!;

    private Control? _colorsCard;

    /// <summary>
    /// Reloads the preview once on the next frame. Color sliders fire on every mouse move, often several times
    /// a frame, and each reload rebuilds every marking layer of the dummy.
    /// </summary>
    private void QueueProfilePreview()
    {
        _profilePreviewQueued = true;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        RestyleColorsOnce();

        if (!_profilePreviewQueued)
            return;

        _profilePreviewQueued = false;
        ReloadProfilePreview();
    }

    private void InitializeMarkingGradients()
    {
        HairStylePicker.OnGradientChanged += args =>
        {
            if (Profile is null || args.marking.MarkingColors.Count == 0)
                return;

            Profile = Profile.WithCharacterAppearance(Profile.Appearance
                .WithHairColor(args.marking.MarkingColors[0])
                .WithHairGradient(args.marking.Gradient));
            UpdateCMarkingsHair();
            QueueProfilePreview();
        };

        FacialHairPicker.OnGradientChanged += args =>
        {
            if (Profile is null || args.marking.MarkingColors.Count == 0)
                return;

            Profile = Profile.WithCharacterAppearance(Profile.Appearance
                .WithFacialHairColor(args.marking.MarkingColors[0])
                .WithFacialHairGradient(args.marking.Gradient));
            UpdateCMarkingsFacialHair();
            QueueProfilePreview();
        };

        CopySkinColorButton.OnPressed += _ => CopySkinColorToMarkings();
    }

    /// <summary>
    /// Recolors every sprite of every marking into the skin color, keeping each sprite's alpha.
    /// Hair and facial hair aren't markings in the profile, so they keep their colors.
    /// </summary>
    private void CopySkinColorToMarkings()
    {
        if (Profile is null)
            return;

        var skin = Profile.Appearance.SkinColor;
        var markings = new List<Marking>(Profile.Appearance.Markings.Count);
        foreach (var marking in Profile.Appearance.Markings)
        {
            var copy = new Marking(marking);
            for (var i = 0; i < copy.MarkingColors.Count; i++)
            {
                copy.SetColor(i, skin.WithAlpha(copy.MarkingColors[i].A));
            }

            markings.Add(copy);
        }

        Profile = Profile.WithCharacterAppearance(Profile.Appearance.WithMarkings(markings));
        UpdateMarkings();
        QueueProfilePreview();
    }

    private void RemoveForeignMarkings()
    {
        if (Profile == null)
            return;

        Profile = Profile.WithCharacterAppearance(_markingManager.WithoutForeignMarkings(Profile.Appearance, Profile.Species));
        UpdateMarkings();
        ReloadPreview();
    }

    /// <summary>
    /// Replaces the job name of the selector with a picker of its alternate titles, if the job has any.
    /// </summary>
    private void SetupAltTitles(RequirementsSelector selector, JobPrototype job)
    {
        _jobAltTitles ??= _entManager.System<JobAlternateTitleSystem>();
        if (!_jobAltTitles.Enabled)
            return;

        var titles = _jobAltTitles.GetTitles(job.ID);
        if (titles.Count == 0)
            return;

        var picker = new EridaAltTitlePicker(job.LocalizedName, titles);
        picker.SetGender(Profile?.Gender ?? Gender.Epicene);
        if (Profile != null && Profile.JobAlternateTitles.TryGetValue(job.ID, out var selected))
            picker.Select(selected);

        picker.OnSelected += title =>
        {
            Profile = Profile?.WithJobAltTitle(job.ID, title);

            foreach (var (jobId, other) in _jobPriorities)
            {
                if (jobId == job.ID && other.AltTitlePicker != picker)
                    other.AltTitlePicker?.Select(title);
            }

            SetDirty();
        };

        selector.SetAltTitlePicker(picker);
    }

    /// <summary>
    /// Updates alternate titles to the gendered forms of the current profile.
    /// </summary>
    private void UpdateAltTitleGender()
    {
        var gender = Profile?.Gender ?? Gender.Epicene;
        foreach (var (_, selector) in _jobPriorities)
        {
            selector.AltTitlePicker?.SetGender(gender);
        }
    }

    private void InitializeEridaLayout()
    {
        LayoutNameBox();
        LayoutAppearanceForm();
        LayoutAppearanceColumns();
        LayoutPreview();
        InitializeProfileActions();
    }

    /// <summary>
    /// Puts the appearance fields in a column right after their labels instead of pushing them to the far edge,
    /// and swaps the clothing toggle and the slider resets for a checkbox and icon buttons.
    /// </summary>
    private void LayoutAppearanceForm()
    {
        if (SpeciesButton.Parent?.Parent is not BoxContainer form)
            return;

        form.SeparationOverride = 4;

        foreach (var child in form.Children)
        {
            if (child is not BoxContainer { Orientation: BoxContainer.LayoutOrientation.Horizontal } row)
                continue;

            row.SeparationOverride = (int) FormGap;
            row.MinHeight = FormRowHeight;

            foreach (var item in row.Children)
            {
                if (item.GetType() == typeof(Control) && item.HorizontalExpand)
                    item.Visible = false;
            }

            if (row.ChildCount == 0 || row.GetChild(0) is not Label label)
                continue;

            label.VerticalAlignment = VAlignment.Center;
            if (row.ChildCount > 1)
                label.MinWidth = FormLabelWidth;
        }

        Control[] fields = [AgeEdit, CustomSpeciesEdit, SexButton, PronounsButton, SpawnPriorityButton];
        foreach (var field in fields)
        {
            field.SetWidth = FormFieldWidth;
            field.HorizontalAlignment = HAlignment.Left;
            field.VerticalAlignment = VAlignment.Center;
        }

        SpeciesInfoButton.SetPositionLast();
        IsCustomSpecies.SetWidth = CustomSpeciesButtonWidth;
        SpeciesButton.SetWidth = FormFieldWidth - FormGap - CustomSpeciesButtonWidth;

        Control[] sliders = [HeightSlider, WidthSlider];
        foreach (var slider in sliders)
        {
            slider.HorizontalExpand = false;
            slider.SetWidth = FormFieldWidth - FormGap - ResetButtonWidth;
            slider.Margin = new Thickness(0);
            slider.VerticalAlignment = VAlignment.Center;
        }

        ReplaceWithIconButton(HeightReset, () =>
        {
            HeightSlider.Value = GetEditedSpecies().DefaultHeight;
            UpdateDimensions(SliderUpdate.Height);
        });

        ReplaceWithIconButton(WidthReset, () =>
        {
            WidthSlider.Value = GetEditedSpecies().DefaultWidth;
            UpdateDimensions(SliderUpdate.Width);
        });
    }

    /// <summary>
    /// Groups the appearance tab into cards: the compact form with the skin and eye colors beside it,
    /// and the hair pickers under them. Vanilla stacks everything on the bare tab and leaves the right side empty.
    /// </summary>
    private void LayoutAppearanceColumns()
    {
        if (SpeciesButton.Parent?.Parent is not { Parent: { } list } form
            || Skin.Parent is not { } skin
            || EyeColorPicker.Parent is not { } eyes
            || HairStylePicker.Parent is not { } hair)
        {
            return;
        }

        var formIndex = form.GetPositionInParent();
        form.Orphan();
        skin.Orphan();
        eyes.Orphan();

        var colors = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = (int) CardGap,
        };
        colors.AddChild(skin);
        colors.AddChild(eyes);
        skin.Margin = new Thickness(0);
        eyes.Margin = new Thickness(0);

        _colorsCard = CreateCard(colors, "erida-profile-editor-section-colors");
        _colorsCard.HorizontalExpand = true;

        var formCard = CreateCard(form, "erida-profile-editor-section-basic");
        form.HorizontalExpand = false;
        form.Margin = new Thickness(0);

        var columns = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = (int) CardGap,
            Margin = new Thickness(CardGap),
            Children = { formCard, _colorsCard },
        };

        list.AddChild(columns);
        columns.SetPositionInParent(formIndex);

        var hairIndex = hair.GetPositionInParent();
        hair.Orphan();
        hair.Margin = new Thickness(0);
        if (hair is BoxContainer hairBox)
        {
            hairBox.Orientation = BoxContainer.LayoutOrientation.Vertical;
            hairBox.SeparationOverride = (int) CardGap * 2;
        }

        HairStylePicker.UseWideLayout();
        FacialHairPicker.UseWideLayout();

        var hairCard = CreateCard(hair, null);
        hairCard.Margin = new Thickness(CardGap, 0, CardGap, CardGap);
        list.AddChild(hairCard);
        hairCard.SetPositionInParent(hairIndex);

        EridaColorSliders.Restyle(_colorsCard);
    }

    /// <summary>
    /// Puts the character preview on a card with the rotation buttons and the clothing mode,
    /// which only affects the preview.
    /// </summary>
    private void LayoutPreview()
    {
        if (SpriteView.Parent is not BoxContainer { Parent: { } root } preview)
            return;

        var index = preview.GetPositionInParent();
        preview.Orphan();
        preview.VerticalAlignment = VAlignment.Top;

        if (PreviewClothingButton.Parent is { } clothesRow)
        {
            PreviewClothingButton.Orphan();
            clothesRow.Visible = false;
        }

        PreviewClothingButton.HorizontalAlignment = HAlignment.Stretch;
        preview.AddChild(PreviewClothingButton);

        var card = CreateCard(preview, null);
        card.VerticalAlignment = VAlignment.Center;
        card.Margin = new Thickness(0, CardGap, CardGap, CardGap);
        root.AddChild(card);
        card.SetPositionInParent(index);
    }

    /// <summary>
    /// The antag list paints fixed colors through PanelOverride, swap them for Erida classes.
    /// </summary>
    private void RestyleAntags()
    {
        foreach (var child in AntagList.Children)
        {
            if (child is ContainerButton { ChildCount: > 0 } header && header.GetChild(0) is PanelContainer panel)
            {
                panel.PanelOverride = null;
                panel.AddStyleClass(EridaStyleClass.VanillaHeader);
                continue;
            }

            RestyleAntagCards(child);
        }
    }

    private static void RestyleAntagCards(Control control)
    {
        foreach (var child in control.Children)
        {
            if (child is not AntagSelector card)
            {
                RestyleAntagCards(child);
                continue;
            }

            card.PanelOverride = null;
            card.AddStyleClass(EridaStyleClass.VanillaCompactCard);
            card.FindControl<Label>("AntagName").AddStyleClass(EridaStyleClass.VanillaHeading);

            if (card.FindControl<RichTextLabel>("AntagDescription").Parent is PanelContainer description)
            {
                description.PanelOverride = null;
                description.AddStyleClass(EridaStyleClass.VanillaInset);
            }
        }
    }

    private static PanelContainer CreateCard(Control content, string? title)
    {
        var box = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 8,
        };

        if (title != null)
        {
            var heading = new Label { Text = Loc.GetString(title) };
            heading.AddStyleClass(EridaStyleClass.TextMuted);
            box.AddChild(heading);
        }

        box.AddChild(content);

        var card = new PanelContainer { Children = { box } };
        card.AddStyleClass(EridaStyleClass.VanillaCard);
        return card;
    }

    /// <summary>
    /// The RGB skin selector is created after the layout, so the color card is restyled once more on the first frame.
    /// </summary>
    private void RestyleColorsOnce()
    {
        if (_colorsCard == null)
            return;

        EridaColorSliders.Restyle(_colorsCard);
        _colorsCard = null;
    }

    private SpeciesPrototype GetEditedSpecies()
    {
        return _species.Find(x => x.ID == Profile?.Species) ?? _species[0];
    }

    private static void ReplaceWithIconButton(Button button, Action action)
    {
        var icon = new ShapedButton
        {
            Icon = EridaIcons.Get("restart_alt"),
            IconSize = ResetIconSize,
            ToolTip = button.Text,
            SetWidth = ResetButtonWidth,
            VerticalAlignment = VAlignment.Center,
        };
        icon.AddStyleClass(EridaStyleClass.ButtonGhost);
        icon.OnPressed += _ => action();

        button.Visible = false;
        button.Parent?.AddChild(icon);
    }

    /// <summary>
    /// Spaces out the name row and stretches the randomize button under it, vanilla centers it flush against the row.
    /// </summary>
    private void LayoutNameBox()
    {
        if (NameEdit.Parent is not BoxContainer { Parent: BoxContainer column } row)
            return;

        row.SeparationOverride = 6;
        column.SeparationOverride = 6;

        var palette = IoCManager.Resolve<EridaThemeManager>().Palette;
        var warning = Loc.GetString("humanoid-profile-editor-naming-rules-warning");
        var shortWarning = Loc.GetString("erida-profile-editor-naming-rules-short");
        WarningLabel.SetMessage(FormattedMessage.FromMarkupPermissive(
            $"[color={palette.Error.WithAlpha(0.85f).ToHex()}][font size=11]⚠ {shortWarning}[/font][/color]"));
        WarningLabel.ToolTip = FormattedMessage.FromMarkupPermissive(warning).ToString();
        WarningLabel.MouseFilter = MouseFilterMode.Stop;

        NameEdit.Margin = new Thickness(0);
        NameEdit.HorizontalExpand = true;

        var randomize = new ShapedButton
        {
            Icon = EridaIcons.Get("sparkle"),
            ToolTip = RandomizeEverythingButton.Text,
            SetWidth = 34,
        };
        randomize.AddStyleClass(EridaStyleClass.ButtonTonal);
        randomize.OnPressed += _ => RandomizeEverything();

        RandomizeEverythingButton.Visible = false;
        row.AddChild(randomize);
    }

    /// <summary>
    /// Replaces the column of vanilla profile buttons with a save button, a reset button shown only with unsaved
    /// changes, and a menu for the file actions.
    /// </summary>
    private void InitializeProfileActions()
    {
        _saveAction = new ShapedButton { HorizontalExpand = true, MinHeight = 32 };
        _saveAction.OnPressed += _ => Save?.Invoke();

        _resetAction = new ShapedButton
        {
            Text = Loc.GetString("humanoid-profile-editor-reset-button"),
            Icon = EridaIcons.Get("close"),
            HorizontalExpand = true,
        };
        _resetAction.AddStyleClass(EridaStyleClass.ButtonGhost);
        _resetAction.OnPressed += _ => SetProfile(
            (HumanoidCharacterProfile?) _preferencesManager.Preferences?.SelectedCharacter,
            _preferencesManager.Preferences?.SelectedCharacterIndex);

        var file = new ShapedButton
        {
            Text = Loc.GetString("erida-profile-editor-file"),
            Icon = EridaIcons.Get("expand_more"),
            HorizontalExpand = true,
        };
        file.AddStyleClass(EridaStyleClass.ButtonTonal);
        file.Content.GetChild(0).SetPositionLast();
        file.OnPressed += _ => OpenFileMenu(file);

        _fileMenuRows = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Margin = new Thickness(6),
            SeparationOverride = 2,
        };

        var panel = new ShapedPanel { Orientation = BoxContainer.LayoutOrientation.Vertical };
        panel.AddStyleClass(EridaStyleClass.Popup);
        panel.AddChild(_fileMenuRows);

        _fileMenu = new Popup { Children = { panel } };
        _fileMenu.OnPopupHide += _fileMenu.Orphan;

        var actions = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            MinWidth = ProfileActionsMinWidth,
            SeparationOverride = 6,
            Children =
            {
                _saveAction,
                new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Horizontal,
                    SeparationOverride = 6,
                    Children = { _resetAction, file },
                },
            },
        };

        ProfileHighlight.GetChild(0).Visible = false;
        ProfileHighlight.AddChild(actions);
        UpdateProfileActions();
    }

    /// <summary>
    /// Shows saving as the main action while there are unsaved changes, and a quiet status otherwise.
    /// </summary>
    private void UpdateProfileActions()
    {
        var dirty = Profile != null && IsDirty;

        _saveAction.Disabled = !dirty;
        _saveAction.Text = Loc.GetString(dirty ? "humanoid-profile-editor-save-button" : "erida-profile-editor-saved");
        _saveAction.Icon = EridaIcons.Get(dirty ? "save" : "check_circle");
        _saveAction.SetOnlyStyleClass(dirty ? EridaStyleClass.ButtonFilled : EridaStyleClass.ButtonGhost);

        _resetAction.Visible = dirty;
    }

    private void OpenFileMenu(Control anchor)
    {
        if (anchor.Root == null)
            return;

        _fileMenuRows.RemoveAllChildren();
        AddFileMenuRow("humanoid-profile-editor-import-button", _exporting, ImportProfile);
        AddFileMenuRow("humanoid-profile-editor-export-button", _exporting, ExportProfile);
        AddFileMenuRow("humanoid-profile-editor-export-image-button", false, ExportImage);
        AddFileMenuRow("humanoid-profile-editor-open-image-button", false,
            () => _resManager.UserData.OpenOsWindow(ContentSpriteSystem.Exports));

        anchor.Root.ModalRoot.AddChild(_fileMenu);
        _fileMenu.Measure(Vector2Helpers.Infinity);

        var position = anchor.GlobalPosition + new Vector2(0, anchor.Size.Y + 2);
        var size = new Vector2(MathF.Max(anchor.Width, _fileMenu.DesiredSize.X), _fileMenu.DesiredSize.Y);
        _fileMenu.Open(UIBox2.FromDimensions(position, size));
    }

    private void AddFileMenuRow(string text, bool disabled, Action action)
    {
        var row = new ShapedButton
        {
            Text = Loc.GetString(text),
            HorizontalExpand = true,
            Disabled = disabled,
        };
        row.AddStyleClass(EridaStyleClass.ButtonListItem);
        row.Content.HorizontalAlignment = HAlignment.Left;
        row.Content.Margin = new Thickness(8, 0);
        row.OnPressed += _ =>
        {
            _fileMenu.Close();
            action();
        };

        _fileMenuRows.AddChild(row);
    }
}
