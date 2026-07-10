using System;
using System.ComponentModel;
using Arbiter.App.ViewModels.Tracing;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Arbiter.App.Views;

public partial class TraceView : UserControl
{
    private TraceViewModel? _viewModel;

    public TraceView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (VisualRoot is not null)
        {
            AttachViewModel();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        AttachViewModel();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        DetachViewModel();
        base.OnDetachedFromVisualTree(e);
    }

    private void AttachViewModel()
    {
        var viewModel = DataContext as TraceViewModel;
        if (ReferenceEquals(viewModel, _viewModel))
        {
            return;
        }

        DetachViewModel();
        _viewModel = viewModel;
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void DetachViewModel()
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel = null;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TraceViewModel.ShowSearchBar) && _viewModel?.ShowSearchBar == true)
        {
            Dispatcher.UIThread.Post(() => SearchQueryTextBox.Focus(), DispatcherPriority.Input);
        }
    }
}
