using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Markup.Xaml;

namespace Arbiter.App.Controls;

public partial class SpritePresenter : UserControl
{
    public static readonly StyledProperty<IImage?> SourceProperty =
        AvaloniaProperty.Register<SpritePresenter, IImage?>(nameof(Source));

    public static readonly StyledProperty<string> SlotTextProperty =
        AvaloniaProperty.Register<SpritePresenter, string>(nameof(SlotText), string.Empty);

    public static readonly StyledProperty<string> FallbackTextProperty =
        AvaloniaProperty.Register<SpritePresenter, string>(nameof(FallbackText), string.Empty);

    public static readonly StyledProperty<bool> IsEmptyProperty =
        AvaloniaProperty.Register<SpritePresenter, bool>(nameof(IsEmpty));

    public static readonly StyledProperty<bool> IsCooldownActiveProperty =
        AvaloniaProperty.Register<SpritePresenter, bool>(nameof(IsCooldownActive));

    public static readonly StyledProperty<string> CooldownTextProperty =
        AvaloniaProperty.Register<SpritePresenter, string>(nameof(CooldownText), string.Empty);

    public static readonly StyledProperty<string> DetailTextProperty =
        AvaloniaProperty.Register<SpritePresenter, string>(nameof(DetailText), string.Empty);

    public static readonly StyledProperty<StretchDirection> SpriteStretchDirectionProperty =
        AvaloniaProperty.Register<SpritePresenter, StretchDirection>(
            nameof(SpriteStretchDirection),
            StretchDirection.Both);

    public IImage? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public string SlotText
    {
        get => GetValue(SlotTextProperty);
        set => SetValue(SlotTextProperty, value);
    }

    public string FallbackText
    {
        get => GetValue(FallbackTextProperty);
        set => SetValue(FallbackTextProperty, value);
    }

    public bool IsEmpty
    {
        get => GetValue(IsEmptyProperty);
        set => SetValue(IsEmptyProperty, value);
    }

    public bool IsCooldownActive
    {
        get => GetValue(IsCooldownActiveProperty);
        set => SetValue(IsCooldownActiveProperty, value);
    }

    public string CooldownText
    {
        get => GetValue(CooldownTextProperty);
        set => SetValue(CooldownTextProperty, value);
    }

    public string DetailText
    {
        get => GetValue(DetailTextProperty);
        set => SetValue(DetailTextProperty, value);
    }

    public StretchDirection SpriteStretchDirection
    {
        get => GetValue(SpriteStretchDirectionProperty);
        set => SetValue(SpriteStretchDirectionProperty, value);
    }

    public SpritePresenter()
    {
        InitializeComponent();
    }
}
