// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Numerics;
using Content.Client._Erida.Stylesheets;
using Content.Client._Erida.UserInterface;
using Content.Client._Erida.UserInterface.Controls;
using Content.Client.Administration.Managers;
using Content.Goobstation.Common.ServerCurrency;
using Content.Goobstation.Shared.ServerCurrency;
using Robust.Client.Console;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._Erida.ServerCurrency;

/// <summary>
/// Coin balance, transfers and the token shop. Replaces the Goobstation CurrencyWindow.
/// </summary>
public sealed partial class EridaCurrencyWindow : EridaWindow
{
    [Dependency] private IClientAdminManager _admin = default!;
    [Dependency] private IClientConsoleHost _console = default!;
    [Dependency] private ICommonCurrencyManager _currency = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private EridaThemeManager _theme = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IUserInterfaceManager _ui = default!;

    private const string AntagCategory = "antag";
    private const string AdminAddCommand = "balance:add";

    private static readonly TimeSpan ConfirmWindow = TimeSpan.FromSeconds(1.5);
    private static readonly TimeSpan NoticeTime = TimeSpan.FromSeconds(3);

    private readonly Label _balance;
    private readonly Label _notice;
    private readonly BoxContainer _listings;
    private readonly List<(ShapedButton Tab, string Category)> _tabs = [];
    private readonly List<ListingRow> _rows = [];

    private string _category = "main";
    private ListingRow? _confirming;
    private TimeSpan _confirmUntil;
    private TimeSpan _noticeUntil;

    public event Action<ProtoId<TokenListingPrototype>>? OnBuy;

    public EridaCurrencyWindow()
    {
        IoCManager.InjectDependencies(this);

        Title = Loc.GetString("gs-balanceui-title");
        SetSize = new Vector2(640, 640);
        MinSize = new Vector2(520, 420);

        _balance = new Label { HorizontalExpand = true, VerticalAlignment = VAlignment.Center };
        _balance.AddStyleClass(EridaStyleClass.Title);

        _notice = new Label { Visible = false, VerticalAlignment = VAlignment.Center };
        _notice.AddStyleClass(EridaStyleClass.TextValue);

        var balanceRow = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 10,
            Children = { CreateIcon("workspace_premium"), _balance, _notice },
        };

        Contents.AddChild(CreateSection(balanceRow));
        Contents.AddChild(CreateSection(CreateTransferRow("gs-balanceui-gift-label",
            "gs-balanceui-gift-player",
            "gs-balanceui-gift-value",
            (player, amount) => _console.ExecuteCommand($"gift {player} {amount}"))));

        _listings = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            SeparationOverride = 2,
        };

        Contents.AddChild(CreateShop());

        if (_admin.CanCommand(AdminAddCommand))
        {
            Contents.AddChild(CreateSection(CreateTransferRow("gs-balanceui-admin-add-label",
                "gs-balanceui-admin-add-player",
                "gs-balanceui-admin-add-value",
                (player, amount) => _console.ExecuteCommand($"{AdminAddCommand} {player} {amount}"))));
        }

        PopulateListings();
        UpdateBalance();
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();

        _currency.ClientBalanceChange += UpdateBalance;
        _theme.PaletteChanged += OnPaletteChanged;
        OnPaletteChanged(_theme.Palette);
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();

        _currency.ClientBalanceChange -= UpdateBalance;
        _theme.PaletteChanged -= OnPaletteChanged;
    }

    /// <summary>
    /// Fields and tabs are vanilla controls, the scoped sheet recolors them from the palette.
    /// </summary>
    private void OnPaletteChanged(EridaPalette palette)
    {
        if (_ui.Stylesheet is { } sheet)
            Stylesheet = _theme.GetScopedStylesheet(sheet);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        var now = _timing.RealTime;

        if (_confirming != null && now > _confirmUntil)
        {
            _confirming.SetConfirming(false);
            _confirming = null;
        }

        if (_notice.Visible && now > _noticeUntil)
            _notice.Visible = false;
    }

    private Control CreateShop()
    {
        var title = new Label { Text = Loc.GetString("gs-balanceui-shop-label") };
        title.AddStyleClass(EridaStyleClass.Title);

        var tabs = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 6,
        };

        foreach (var (category, text) in new[]
                 {
                     ("main", "gs-balanceui-shop-tab-main"),
                     (AntagCategory, "gs-balanceui-shop-tab-antag"),
                 })
        {
            var tab = new ShapedButton { Text = Loc.GetString(text), MinHeight = 28 };
            tab.AddStyleClass(EridaStyleClass.ButtonChip);
            tab.OnPressed += _ => SelectCategory(category);
            tabs.AddChild(tab);
            _tabs.Add((tab, category));
        }

        var header = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 10,
            Children = { title, new Control { HorizontalExpand = true }, tabs },
        };

        var footer = new Label { Text = Loc.GetString("gs-balanceui-shop-footer") };
        footer.AddStyleClass(EridaStyleClass.TextMuted);

        var body = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 8,
            VerticalExpand = true,
            Children =
            {
                header,
                new ScrollContainer
                {
                    VerticalExpand = true,
                    HScrollEnabled = false,
                    Children = { _listings },
                },
                footer,
            },
        };

        var section = CreateSection(body);
        section.VerticalExpand = true;
        return section;
    }

    private void PopulateListings()
    {
        _listings.RemoveAllChildren();
        _rows.Clear();

        var listings = _prototypes.EnumeratePrototypes<TokenListingPrototype>()
            .OrderByDescending(listing => listing.Price)
            .ThenBy(listing => listing.Order);

        foreach (var listing in listings)
        {
            var row = new ListingRow(listing, _currency.Stringify(listing.Price));
            row.Button.OnPressed += _ => OnListingPressed(row);
            _listings.AddChild(row.Button);
            _rows.Add(row);
        }

        SelectCategory(_category);
    }

    private void SelectCategory(string category)
    {
        _category = category;

        foreach (var (tab, tabCategory) in _tabs)
        {
            tab.IsActive = tabCategory == category;
        }

        foreach (var row in _rows)
        {
            row.Button.Visible = (row.Listing.Category == AntagCategory) == (category == AntagCategory);
        }
    }

    /// <summary>
    /// The first press asks to confirm, a second one within <see cref="ConfirmWindow"/> buys.
    /// </summary>
    private void OnListingPressed(ListingRow row)
    {
        if (_confirming == row)
        {
            row.SetConfirming(false);
            _confirming = null;

            OnBuy?.Invoke(row.Listing.ID);
            ShowNotice(Loc.GetString("gs-balanceui-shop-purchased", ("item", Loc.GetString(row.Listing.Label))));
            return;
        }

        _confirming?.SetConfirming(false);
        _confirming = row;
        _confirmUntil = _timing.RealTime + ConfirmWindow;
        row.SetConfirming(true);
    }

    private void ShowNotice(string text)
    {
        _notice.Text = text;
        _notice.Visible = true;
        _noticeUntil = _timing.RealTime + NoticeTime;
    }

    private void UpdateBalance()
    {
        var balance = _currency.GetBalance();
        _balance.Text = _currency.Stringify(balance);

        foreach (var row in _rows)
        {
            row.SetAffordable(balance >= row.Listing.Price);
        }
    }

    private Control CreateTransferRow(string title, string player, string amount, Action<string, int> send)
    {
        var label = new Label { Text = Loc.GetString(title), MinWidth = 150, VerticalAlignment = VAlignment.Center };
        label.AddStyleClass(EridaStyleClass.TextMuted);

        var playerEdit = new LineEdit { PlaceHolder = Loc.GetString(player), HorizontalExpand = true };
        var amountEdit = new LineEdit { PlaceHolder = Loc.GetString(amount), MinWidth = 100 };

        var confirm = new ShapedButton
        {
            Text = Loc.GetString("gs-balanceui-confirm"),
            MinWidth = 120,
            MinHeight = 32,
        };
        confirm.AddStyleClass(EridaStyleClass.ButtonFilled);
        confirm.OnPressed += _ =>
        {
            var target = playerEdit.Text.Trim();

            if (target.Length == 0 || !int.TryParse(amountEdit.Text, out var value) || value == 0)
                return;

            send(target, value);
            amountEdit.Text = string.Empty;
        };

        return new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 6,
            Children = { label, playerEdit, amountEdit, confirm },
        };
    }

    private static ShapedPanel CreateSection(Control content)
    {
        var section = new ShapedPanel { Orientation = BoxContainer.LayoutOrientation.Vertical };
        section.AddStyleClass(EridaStyleClass.Section);
        content.Margin = new Thickness(14, 10);
        section.AddChild(content);
        return section;
    }

    private static TextureRect CreateIcon(string name)
    {
        var icon = new TextureRect
        {
            Texture = EridaIcons.Get(name),
            Stretch = TextureRect.StretchMode.KeepAspectCentered,
            SetSize = new Vector2(22, 22),
            VerticalAlignment = VAlignment.Center,
        };
        icon.AddStyleClass(EridaStyleClass.Icon);
        return icon;
    }

    private sealed class ListingRow
    {
        private const float DisabledAlpha = 0.45f;

        public readonly TokenListingPrototype Listing;
        public readonly ShapedButton Button;

        private readonly Label _price;
        private readonly string _priceText;

        public ListingRow(TokenListingPrototype listing, string price)
        {
            Listing = listing;
            _priceText = price;

            Button = new ShapedButton { HorizontalExpand = true, MinHeight = 44 };
            Button.AddStyleClass(EridaStyleClass.ButtonListItem);
            Button.Content.HorizontalAlignment = HAlignment.Stretch;
            Button.Content.Margin = new Thickness(10, 4);

            var name = new Label { Text = Loc.GetString(listing.Label), ClipText = true };
            name.AddStyleClass(EridaStyleClass.TextValue);

            var info = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                HorizontalExpand = true,
                VerticalAlignment = VAlignment.Center,
                Children = { name },
            };

            if (!string.IsNullOrEmpty(listing.Description))
            {
                var description = new Label { Text = Loc.GetString(listing.Description), ClipText = true };
                description.AddStyleClass(EridaStyleClass.TextMuted);
                info.AddChild(description);
                Button.ToolTip = description.Text;
            }

            _price = new Label { Text = price, VerticalAlignment = VAlignment.Center };
            _price.AddStyleClass(EridaStyleClass.TextValue);

            Button.Content.AddChild(info);
            Button.Content.AddChild(_price);
        }

        public void SetConfirming(bool confirming)
        {
            Button.IsActive = confirming;
            _price.Text = confirming ? Loc.GetString("gs-balanceui-shop-click-confirm") : _priceText;
        }

        public void SetAffordable(bool affordable)
        {
            Button.Disabled = !affordable;
            Button.Content.Modulate = affordable ? Color.White : Color.White.WithAlpha(DisabledAlpha);
        }
    }
}
