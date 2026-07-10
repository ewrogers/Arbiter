using System;
using System.Collections.ObjectModel;
using Arbiter.App.Services.Sprites;
using Arbiter.Net.Types;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Arbiter.App.ViewModels.Dialogs;

public partial class DialogViewModel : ViewModelBase
{
    private readonly IGameSpriteService _spriteService;

    [ObservableProperty] private string? _name;
    [ObservableProperty] private string? _content;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SpriteImage), nameof(SpriteFallbackText), nameof(HasSprite))]
    private int? _sprite;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SpriteImage))]
    private SpriteType _spriteType;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SpriteImage))]
    private byte? _color;

    [ObservableProperty] private long? _entityId;
    [ObservableProperty] private EntityTypeFlags _entityType;
    [ObservableProperty] private int? _pursuitId;
    [ObservableProperty] private int? _stepId;
    [ObservableProperty] private bool _isTextInput;
    [ObservableProperty] private string? _promptLine1;
    [ObservableProperty] private string? _promptLine2;

    [ObservableProperty] private int _inputMaxLength = 255;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(ConfirmTextInputCommand))]
    private string? _inputText;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(NavigatePreviousCommand))]
    private bool _canNavigatePrevious;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(NavigateNextCommand))]
    private bool _canNavigateNext;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(NavigateTopCommand))]
    private bool _canNavigateTop;

    public ObservableCollection<DialogMenuChoiceViewModel> MenuChoices { get; } = [];

    public bool HasMenuChoices => MenuChoices.Count > 0;
    public bool HasSprite => Sprite.HasValue;
    public string SpriteFallbackText => Sprite.HasValue ? $"#{Sprite.Value}" : "None";
    public IImage? SpriteImage
    {
        get
        {
            if (!Sprite.HasValue || Sprite.Value < ushort.MinValue || Sprite.Value > ushort.MaxValue)
            {
                return null;
            }

            var sprite = (ushort)Sprite.Value;
            return SpriteType switch
            {
                SpriteType.Monster => _spriteService.GetCreature(sprite),
                SpriteType.Item => _spriteService.GetItem(sprite, Color ?? 0),
                _ => null
            };
        }
    }

    public event EventHandler<DialogEventArgs>? RequestPrevious;
    public event EventHandler<DialogEventArgs>? RequestNext;
    public event EventHandler<DialogEventArgs>? RequestTop;
    public event EventHandler<DialogEventArgs>? RequestClose;
    public event EventHandler<DialogMenuEventArgs>? MenuChoiceSelected;
    public event EventHandler<DialogEventArgs>? TextInputConfirmed;

    public DialogViewModel()
        : this(NullGameSpriteService.Instance)
    {
    }

    public DialogViewModel(IGameSpriteService spriteService)
    {
        _spriteService = spriteService;
        MenuChoices.CollectionChanged += (_, _) => { OnPropertyChanged(nameof(HasMenuChoices)); };
    }

    public void RefreshSprite() => OnPropertyChanged(nameof(SpriteImage));

    [RelayCommand]
    private void SelectMenuChoice(DialogMenuChoiceViewModel viewModel)
    {
        MenuChoiceSelected?.Invoke(this, new DialogMenuEventArgs(viewModel));
    }

    [RelayCommand(CanExecute = nameof(CanNavigatePrevious))]
    private void NavigatePrevious()
    {
        if (!CanNavigatePrevious)
        {
            return;
        }

        RequestPrevious?.Invoke(this, new DialogEventArgs());
    }

    [RelayCommand(CanExecute = nameof(CanNavigateNext))]
    private void NavigateNext()
    {
        if (!CanNavigateNext)
        {
            return;
        }

        RequestNext?.Invoke(this, new DialogEventArgs());
    }

    [RelayCommand(CanExecute = nameof(CanNavigateTop))]
    private void NavigateTop()
    {
        if (!CanNavigateTop)
        {
            return;
        }

        RequestTop?.Invoke(this, new DialogEventArgs());
    }

    private bool CanConfirmTextInput() => IsTextInput && !string.IsNullOrWhiteSpace(InputText);

    [RelayCommand(CanExecute = nameof(CanConfirmTextInput))]
    private void ConfirmTextInput()
    {
        if (!IsTextInput)
        {
            return;
        }

        TextInputConfirmed?.Invoke(this, new DialogEventArgs());
    }

    [RelayCommand]
    private void CloseDialog()
    {
        RequestClose?.Invoke(this, new DialogEventArgs());
    }
}
