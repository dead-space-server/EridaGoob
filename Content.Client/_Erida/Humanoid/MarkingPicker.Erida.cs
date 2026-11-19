// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared._Erida.CCVar;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Client.Humanoid;

public sealed partial class MarkingPicker
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private bool _populateQueued;

    /// <summary>
    /// Loading a profile changes the species, sex and markings one by one, so the tabs are rebuilt once on the next frame.
    /// </summary>
    private void QueuePopulate()
    {
        _populateQueued = true;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_populateQueued)
            Populate("");
    }

    /// <summary>
    /// Removes the tabs and returns the index of the selected one.
    /// The tab container keeps its current index after its children are removed but shows the first new tab,
    /// which is then never laid out and draws over the tab titles, so the selection is moved to the first tab beforehand.
    /// </summary>
    private int ClearTabs()
    {
        var current = CMarkingTabs.CurrentTab;
        _categoryTabs.Clear();
        if (CMarkingTabs.ChildCount > 0)
            CMarkingTabs.CurrentTab = 0;

        CMarkingTabs.RemoveAllChildren();
        return current;
    }

    private void RestoreTab(int index)
    {
        if (index < CMarkingTabs.ChildCount)
            CMarkingTabs.CurrentTab = index;

        BuildTab(CMarkingTabs.CurrentTab);
    }

    private void BuildTab(int index)
    {
        if (index >= 0 && index < _categoryTabs.Count)
            _categoryTabs[index].EnsureBuilt();
    }

    private sealed partial class CategoryTab
    {
        private const string FilterOwn = "own";
        private const string FilterAll = "all";
        private const string FilterCommon = "common";

        private readonly List<string> _filters = [];
        private BoxContainer _searchRow = default!;
        private OptionButton _filterButton = default!;
        private string _filter = FilterAll;

        private BoxContainer CreateSearchRow()
        {
            _searchRow = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                HorizontalExpand = true,
            };
            _searchRow.AddChild(_searchBar);

            _filterButton = new OptionButton
            {
                MinWidth = 180,
                Visible = false,
            };
            _filterButton.OnItemSelected += args =>
            {
                _picker._cfg.SetCVar(ECCVars.MarkingsFilter, _filters[args.Id]);
                _picker._cfg.SaveToFile();
                SelectFilter(args.Id);
            };
            _searchRow.AddChild(_filterButton);

            return _searchRow;
        }

        /// <summary>
        /// Fills the filter with the species of this category's markings and applies the remembered filter.
        /// </summary>
        private void SetupFilter()
        {
            var species = _picker._currentSpecies;
            var hasOwn = false;
            var hasCommon = false;
            HashSet<string> others = [];
            foreach (var marking in _markings.Values)
            {
                if (MarkingManager.IsCommon(marking))
                {
                    hasCommon = true;
                    continue;
                }

                foreach (var restriction in marking.SpeciesRestrictions!)
                {
                    if (restriction == species)
                        hasOwn = true;
                    else if (_picker._prototypeManager.HasIndex<SpeciesPrototype>(restriction))
                        others.Add(restriction);
                }
            }

            if (hasOwn)
                _filters.Add(FilterOwn);

            _filters.Add(FilterAll);

            if (hasCommon && (hasOwn || others.Count > 0))
                _filters.Add(FilterCommon);

            _filters.AddRange(others.OrderBy(SpeciesName));

            for (var i = 0; i < _filters.Count; i++)
            {
                _filterButton.AddItem(FilterName(_filters[i]), i);
            }

            _filterButton.Visible = _filters.Count > 1;

            var index = _filters.IndexOf(_picker._cfg.GetCVar(ECCVars.MarkingsFilter));
            if (index < 0)
                index = _filters.IndexOf(FilterOwn);

            SelectFilter(Math.Max(index, 0));
        }

        private void SelectFilter(int index)
        {
            _filterButton.SelectId(index);
            _filter = _filters[index];

            var position = 0;
            foreach (var item in _searchable.OrderByDescending(item => IsMarkingApplied(item.Prototype.ID)))
            {
                item.SetPositionInParent(position++);
            }

            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            var query = _searchBar.Text.Trim();
            foreach (var item in _searchable)
            {
                item.Visible = item.CheckMatchesSearch(query)
                    && (IsMarkingApplied(item.Prototype.ID) || MatchesFilter(item.Prototype));
            }
        }

        private bool MatchesFilter(MarkingPrototype marking)
        {
            return _filter switch
            {
                FilterAll => true,
                FilterOwn => marking.SpeciesRestrictions?.Contains(_picker._currentSpecies) == true,
                FilterCommon => MarkingManager.IsCommon(marking),
                _ => marking.SpeciesRestrictions?.Contains(_filter) == true,
            };
        }

        private string FilterName(string filter)
        {
            return filter switch
            {
                FilterOwn or FilterAll or FilterCommon => Loc.GetString($"markings-filter-{filter}"),
                _ => SpeciesName(filter),
            };
        }

        private string SpeciesName(string species)
        {
            return Loc.GetString(_picker._prototypeManager.Index<SpeciesPrototype>(species).Name);
        }
    }
}
