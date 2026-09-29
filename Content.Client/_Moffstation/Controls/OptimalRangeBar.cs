using System.Numerics;
using Content.Shared.Destructible.Thresholds;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client._Moffstation.Controls;

/// <summary>
/// Vertical gauge plotting a good, acceptable, and bad (inferred) range, as well as a current value
/// </summary>
public sealed partial class OptimalRangeBar : Control
{
    private const float DisplayPadding = 0.2f;

    private const float LabelMargin = 2f;

    private readonly Color _backgroundColor = new(0.1f, 0.1f, 0.1f);
    private readonly Color _acceptableColor = Color.FromHex("#20304a");
    private readonly Color _optimalFillColor = Color.FromHex("#2fbf4c").WithAlpha(0.3f);
    private readonly Color _optimalColor = Color.FromHex("#3ee85f");
    private readonly Color _badColor = Color.FromHex("#e03050");
    private readonly Color _currentColor = Color.FromHex("#4499ff");

    private MinMax _acceptable;
    private MinMax _optimal;
    private float? _targetCurrent;
    private float? _displayedCurrent;

    private string _optimalMaxText = string.Empty;
    private string _optimalMinText = string.Empty;
    private string _currentText = string.Empty;
    private float? _currentTextValue;

    public string ValueFormat { get; set; } = "F0";

    public string NoDataText { get; set; } = string.Empty;

    private Font ActiveFont => TryGetStyleProperty<Font>(Label.StylePropertyFont, out var font)
        ? font
        : UserInterfaceManager.ThemeDefaults.LabelFont;

    public void SetRanges(MinMax acceptable, MinMax optimal)
    {
        _acceptable = acceptable;
        _optimal = optimal;
        _optimalMaxText = optimal.Max.ToString(ValueFormat);
        _optimalMinText = optimal.Min.ToString(ValueFormat);
    }

    public void SetCurrent(float? current)
    {
        _targetCurrent = current;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        if (_targetCurrent is not { } target)
        {
            _displayedCurrent = null;
            return;
        }

        if (_displayedCurrent is not { } displayed)
        {
            _displayedCurrent = target;
            return;
        }

        _displayedCurrent = MathHelper.Lerp(displayed, target, MathHelper.Clamp01(8f * args.DeltaSeconds));
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        handle.DrawRect(PixelSizeBox, _backgroundColor);

        var span = _acceptable.Max - _acceptable.Min;
        if (span <= 0f)
            return;

        var displayMin = _acceptable.Min - span * DisplayPadding;
        var displayMax = _acceptable.Max + span * DisplayPadding;

        var minAcceptableY = ValueToY(_acceptable.Min, displayMin, displayMax);
        var maxAcceptableY = ValueToY(_acceptable.Max, displayMin, displayMax);
        var minOptimalY = ValueToY(_optimal.Min, displayMin, displayMax);
        var maxOptimalY = ValueToY(_optimal.Max, displayMin, displayMax);

        handle.DrawRect(new UIBox2(0, maxAcceptableY, PixelWidth, minAcceptableY), _acceptableColor);
        handle.DrawRect(new UIBox2(0, maxOptimalY, PixelWidth, minOptimalY), _optimalFillColor);

        DrawThreshold(handle, maxAcceptableY, _badColor);
        DrawThreshold(handle, minAcceptableY, _badColor);
        DrawThreshold(handle, maxOptimalY, _optimalColor);
        DrawThreshold(handle, minOptimalY, _optimalColor);

        DrawLabel(handle, _optimalMaxText, maxOptimalY, _optimalColor, above: true, right: false);
        DrawLabel(handle, _optimalMinText, minOptimalY, _optimalColor, above: false, right: false);

        if (_displayedCurrent is not { } current)
        {
            DrawLabel(handle, NoDataText, PixelHeight / 2f, _currentColor, above: false, right: true);
            return;
        }

        var currentY = ValueToY(current, displayMin, displayMax);
        DrawThreshold(handle, currentY, _currentColor);

        if (_currentTextValue != current)
        {
            _currentTextValue = current;
            _currentText = current.ToString(ValueFormat);
        }

        DrawLabel(handle, _currentText, currentY, _currentColor, above: currentY > PixelHeight / 2f, right: true);
    }

    private void DrawThreshold(DrawingHandleScreen handle, float y, Color color)
    {
        var thickness = MathF.Max(2f, MathF.Round(2f * UIScale));
        var top = MathF.Round(y - thickness / 2f);
        handle.DrawRect(new UIBox2(0, top, PixelWidth, top + thickness), color);
    }

    private void DrawLabel(DrawingHandleScreen handle, string text, float y, Color color, bool above, bool right)
    {
        var font = ActiveFont;
        var dimensions = handle.GetDimensions(font, text, UIScale);
        var inset = LabelMargin * UIScale;
        var x = right ? PixelWidth - dimensions.X - inset : inset;
        var top = above ? y - dimensions.Y - LabelMargin * UIScale : y + LabelMargin * UIScale;

        handle.DrawString(
            font,
            new Vector2(x, Math.Clamp(top, 0f, MathF.Max(0f, PixelHeight - dimensions.Y))),
            text,
            UIScale,
            color);
    }

    private float ValueToY(float value, float displayMin, float displayMax)
    {
        var fraction = MathHelper.Clamp01((value - displayMin) / (displayMax - displayMin));
        return PixelHeight * (1f - fraction);
    }
}
