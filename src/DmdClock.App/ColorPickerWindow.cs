using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;

namespace DmdClock.App;

public sealed class ColorPickerWindow : Window
{
    private readonly Slider _red;
    private readonly Slider _green;
    private readonly Slider _blue;
    private readonly Border _preview;
    private readonly TextBox _hex;
    private bool _updating;

    public ColorPickerWindow(string title, Color initialColor, string okText, string cancelText)
    {
        Title = title;
        Width = 390;
        MinWidth = 390;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        _red = CreateSlider(initialColor.R);
        _green = CreateSlider(initialColor.G);
        _blue = CreateSlider(initialColor.B);
        _preview = new Border { Height = 58, CornerRadius = new CornerRadius(5) };
        _hex = new TextBox { HorizontalContentAlignment = HorizontalAlignment.Center };

        _red.PropertyChanged += ChannelChanged;
        _green.PropertyChanged += ChannelChanged;
        _blue.PropertyChanged += ChannelChanged;
        _hex.LostFocus += (_, _) => ApplyHexValue();

        var ok = new Button { Content = okText, MinWidth = 90 };
        var cancel = new Button { Content = cancelText, MinWidth = 90 };
        ok.Click += (_, _) => Close(SelectedColor);
        cancel.Click += (_, _) => Close(null);

        Content = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 10,
            Children =
            {
                ChannelRow("R", _red), ChannelRow("G", _green), ChannelRow("B", _blue),
                _hex,
                _preview,
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 6, 0, 2),
                    Spacing = 10,
                    Children = { cancel, ok }
                }
            }
        };
        UpdatePreview();
    }

    public Color SelectedColor => Color.FromRgb((byte)_red.Value, (byte)_green.Value, (byte)_blue.Value);

    private static Slider CreateSlider(byte value) => new()
    {
        Minimum = 0,
        Maximum = 255,
        Value = value,
        TickFrequency = 1,
        Width = 300
    };

    private static Control ChannelRow(string label, Slider slider) => new StackPanel
    {
        Orientation = Orientation.Horizontal,
        Spacing = 10,
        Children = { new TextBlock { Text = label, Width = 20, VerticalAlignment = VerticalAlignment.Center }, slider }
    };

    private void ChannelChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (!_updating && e.Property == RangeBase.ValueProperty) UpdatePreview();
    }

    private void UpdatePreview()
    {
        var color = SelectedColor;
        _preview.Background = new SolidColorBrush(color);
        _hex.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    private void ApplyHexValue()
    {
        try
        {
            var color = Color.Parse(_hex.Text ?? string.Empty);
            _updating = true;
            _red.Value = color.R;
            _green.Value = color.G;
            _blue.Value = color.B;
            _updating = false;
            UpdatePreview();
        }
        catch (FormatException) { UpdatePreview(); }
    }
}
