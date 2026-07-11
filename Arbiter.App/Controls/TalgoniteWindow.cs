using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace Arbiter.App.Controls;

[PseudoClasses(":maximized")]
public class TalgoniteWindow : Window
{
    private Button? _minimizeButton;
    private Button? _maximizeButton;
    private Button? _closeButton;
    
    protected override Type StyleKeyOverride { get; } = typeof(TalgoniteWindow);
    
    public static readonly StyledProperty<Control> TitleBarContentProperty = AvaloniaProperty.Register<TalgoniteWindow, Control>(
        nameof(TitleBarContent));

    public static readonly StyledProperty<IBrush?> TitleBarBorderBrushProperty = AvaloniaProperty.Register<TalgoniteWindow, IBrush?>(
        nameof(TitleBarBorderBrush), Brushes.Transparent);

    public static readonly StyledProperty<Thickness> TitleBarBorderThicknessProperty = AvaloniaProperty.Register<TalgoniteWindow, Thickness>(
        nameof(TitleBarBorderThickness), new Thickness(0, 0, 0, 1));
    
    public static readonly StyledProperty<TextAlignment> TitleAlignmentProperty = AvaloniaProperty.Register<TalgoniteWindow, TextAlignment>(
        nameof(TitleAlignment));

    public Control TitleBarContent
    {
        get => GetValue(TitleBarContentProperty);
        set => SetValue(TitleBarContentProperty, value);
    }
    
    public IBrush? TitleBarBorderBrush
    {
        get => GetValue(TitleBarBorderBrushProperty);
        set => SetValue(TitleBarBorderBrushProperty, value);
    }
    
    public Thickness TitleBarBorderThickness
    {
        get => GetValue(TitleBarBorderThicknessProperty);
        set => SetValue(TitleBarBorderThicknessProperty, value);
    }
    
    public TextAlignment TitleAlignment
    {
        get => GetValue(TitleAlignmentProperty);
        set => SetValue(TitleAlignmentProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == WindowStateProperty)
        {
            PseudoClasses.Set(":maximized", change.NewValue is WindowState.Maximized);
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _minimizeButton = e.NameScope.Find<Button>("PART_MinimizeButton")!;
        _minimizeButton.Click += (_, _) => WindowState = WindowState.Minimized;
        
        _maximizeButton = e.NameScope.Find<Button>("PART_MaximizeButton")!;
        _maximizeButton.Click += (_, _) => ToggleMaximizedState();
        
        _closeButton = e.NameScope.Find<Button>("PART_CloseButton")!;
        _closeButton.Click += (_, _) => Close();
    }

    private void ToggleMaximizedState()
    {
        if (!CanMaximize)
        {
            return;
        }
        
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }
}
