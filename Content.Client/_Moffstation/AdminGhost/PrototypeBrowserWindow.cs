using System;
using System.Collections.Generic;
using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client._Moffstation.AdminGhost;

public sealed class PrototypeBrowserWindow : DefaultWindow
{
    [Dependency] private IPrototypeManager _prototypes = default!;

    public event Action<string>? OnPrototypeSelected;

    private readonly LineEdit _searchBar;
    private readonly Button _clearButton;
    private readonly ScrollContainer _scrollContainer;
    private readonly PrototypeListContainer _prototypeList;

    private string? _currentSearch;
    private readonly List<EntityPrototype> _shownPrototypes = new();
    private (int start, int end) _lastVisibleIndices;
    private float _rowHeight;

    public PrototypeBrowserWindow()
    {
        IoCManager.InjectDependencies(this);

        Title = "Browse Prototypes";
        SetSize = new Vector2(400, 500);
        MinSize = new Vector2(300, 300);

        var root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical
        };

        // Search row
        var searchRow = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal
        };
        _searchBar = new LineEdit
        {
            HorizontalExpand = true,
            PlaceHolder = "Search prototypes..."
        };
        _clearButton = new Button
        {
            Text = "Clear",
            Disabled = true,
            MinSize = new Vector2(60, 0)
        };
        searchRow.AddChild(_searchBar);
        searchRow.AddChild(_clearButton);
        root.AddChild(searchRow);

        // Scrollable prototype list
        _scrollContainer = new ScrollContainer
        {
            VerticalExpand = true,
            MinSize = new Vector2(200, 0)
        };
        _prototypeList = new PrototypeListContainer();
        _scrollContainer.AddChild(_prototypeList);
        root.AddChild(_scrollContainer);

        Contents.AddChild(root);

        // Measure row height with a temporary button
        var measureButton = new PrototypeBrowserButton();
        root.AddChild(measureButton);
        measureButton.Measure(Vector2Helpers.Infinity);
        _rowHeight = measureButton.DesiredSize.Y + PrototypeListContainer.Separation;
        root.RemoveChild(measureButton);

        _searchBar.OnTextChanged += OnSearchChanged;
        _clearButton.OnPressed += OnClearPressed;
        _scrollContainer.OnScrolled += OnScrolled;
        OnResized += OnScrolled;

        BuildEntityList();
    }

    private void OnSearchChanged(LineEdit.LineEditEventArgs args)
    {
        _currentSearch = args.Text;
        _clearButton.Disabled = string.IsNullOrEmpty(args.Text);
        BuildEntityList(args.Text);
    }

    private void OnClearPressed(ButtonEventArgs args)
    {
        _searchBar.Clear();
        _currentSearch = null;
        _clearButton.Disabled = true;
        BuildEntityList();
    }

    private void OnScrolled()
    {
        UpdateVisiblePrototypes();
    }

    private void BuildEntityList(string? searchStr = null)
    {
        _shownPrototypes.Clear();
        _prototypeList.RemoveAllChildren();
        _lastVisibleIndices = (0, -1);
        searchStr = searchStr?.ToLowerInvariant();

        foreach (var prototype in _prototypes.EnumeratePrototypes<EntityPrototype>())
        {
            if (prototype.Abstract || prototype.HideSpawnMenu)
                continue;

            if (searchStr != null && !DoesPrototypeMatchSearch(prototype, searchStr))
                continue;

            _shownPrototypes.Add(prototype);
        }

        _shownPrototypes.Sort((a, b) =>
        {
            var nameCompare = string.Compare(a.Name, b.Name, StringComparison.Ordinal);
            if (nameCompare == 0)
                return string.Compare(a.EditorSuffix, b.EditorSuffix, StringComparison.Ordinal);
            return nameCompare;
        });

        _prototypeList.TotalItemCount = _shownPrototypes.Count;
        _scrollContainer.SetScrollValue(new Vector2(0, 0));
        UpdateVisiblePrototypes();
    }

    private static bool DoesPrototypeMatchSearch(EntityPrototype prototype, string searchStr)
    {
        if (string.IsNullOrEmpty(searchStr))
            return true;

        if (prototype.ID.Contains(searchStr, StringComparison.InvariantCultureIgnoreCase))
            return true;

        if (prototype.EditorSuffix != null &&
            prototype.EditorSuffix.Contains(searchStr, StringComparison.InvariantCultureIgnoreCase))
            return true;

        if (string.IsNullOrEmpty(prototype.Name))
            return false;

        if (prototype.Name.Contains(searchStr, StringComparison.InvariantCultureIgnoreCase))
            return true;

        return false;
    }

    private void UpdateVisiblePrototypes()
    {
        var height = _rowHeight;
        var offset = Math.Max(-_prototypeList.Position.Y, 0);
        var startIndex = (int)Math.Floor(offset / height);
        _prototypeList.ItemOffset = startIndex;

        var (prevStart, prevEnd) = _lastVisibleIndices;

        var endIndex = startIndex - 1;
        var spaceUsed = -height;

        while (spaceUsed < _scrollContainer.Height)
        {
            spaceUsed += height;
            endIndex += 1;
        }

        endIndex = Math.Min(endIndex, _shownPrototypes.Count - 1);

        if (endIndex == prevEnd && startIndex == prevStart)
            return;

        _lastVisibleIndices = (startIndex, endIndex);

        // Remove buttons scrolled out of view
        for (var i = prevStart; i < startIndex && i <= prevEnd; i++)
        {
            var control = (PrototypeBrowserButton)_prototypeList.GetChild(0);
            _prototypeList.RemoveChild(control);
        }

        for (var i = prevEnd; i > endIndex && i >= prevStart; i--)
        {
            var control = (PrototypeBrowserButton)_prototypeList.GetChild(_prototypeList.ChildCount - 1);
            _prototypeList.RemoveChild(control);
        }

        // Add buttons scrolled into view
        for (var i = Math.Min(prevStart - 1, endIndex); i >= startIndex; i--)
        {
            InsertBrowserButton(_shownPrototypes[i], true, i);
        }

        for (var i = Math.Max(prevEnd + 1, startIndex); i <= endIndex; i++)
        {
            InsertBrowserButton(_shownPrototypes[i], false, i);
        }
    }

    private void InsertBrowserButton(EntityPrototype prototype, bool insertFirst, int index)
    {
        var button = new PrototypeBrowserButton
        {
            Prototype = prototype,
            Index = index
        };

        var labelText = string.IsNullOrEmpty(prototype.Name) ? prototype.ID : prototype.Name;
        if (!string.IsNullOrWhiteSpace(prototype.EditorSuffix))
            labelText += $" [{prototype.EditorSuffix}]";

        button.EntityLabel.Text = labelText;
        button.ActualButton.ToolTip = prototype.ID;

        button.EntityTextureRects.SetPrototype(prototype.ID);

        button.ActualButton.OnPressed += _ => OnButtonPressed(button);

        _prototypeList.AddChild(button);
        if (insertFirst)
            button.SetPositionInParent(0);
    }

    private void OnButtonPressed(PrototypeBrowserButton button)
    {
        OnPrototypeSelected?.Invoke(button.PrototypeID);
        Close();
    }
}
