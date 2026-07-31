using System.Collections.Specialized;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Arbiter.App.Behaviors;

public class ScrollToEndBehavior : AvaloniaObject
{
    private static readonly ConditionalWeakTable<Interactive, AutoScrollSubscription> AutoScrollSubscriptions = new();

    #region AutoScrollToEnd Property

    public static readonly AttachedProperty<bool> AutoScrollToEndProperty =
        AvaloniaProperty.RegisterAttached<ScrollToEndBehavior, Interactive, bool>("AutoScrollToEnd");

    public static void SetAutoScrollToEnd(Interactive element, bool value) =>
        element.SetValue(AutoScrollToEndProperty, value);

    public static bool GetAutoScrollToEnd(Interactive element) => element.GetValue(AutoScrollToEndProperty);

    #endregion

    #region ScrollToEnd Property

    public static readonly AttachedProperty<bool> ScrollToEndProperty =
        AvaloniaProperty.RegisterAttached<ScrollToEndBehavior, Interactive, bool>("ScrollToEnd", false, false,
            BindingMode.TwoWay);

    public static void SetScrollToEnd(Interactive element, bool value) => element.SetValue(ScrollToEndProperty, value);
    public static bool GetScrollToEnd(Interactive element) => element.GetValue(ScrollToEndProperty);

    #endregion

    static ScrollToEndBehavior()
    {
        AutoScrollToEndProperty.Changed.AddClassHandler<Interactive>(HandleAutoScrollToEndChanged);
        ScrollToEndProperty.Changed.AddClassHandler<Interactive>(HandleScrollToEndChanged);
    }

    private static void HandleAutoScrollToEndChanged(Interactive element, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is false)
        {
            if (AutoScrollSubscriptions.TryGetValue(element, out var existingSubscription))
            {
                existingSubscription.Dispose();
                AutoScrollSubscriptions.Remove(element);
            }

            return;
        }

        if (e.NewValue is not true)
        {
            return;
        }

        var itemsControl = element as ItemsControl ?? element.FindDescendantOfType<ItemsControl>();
        if (itemsControl is null)
        {
            return;
        }

        if (AutoScrollSubscriptions.TryGetValue(element, out _))
        {
            return;
        }

        var subscription = new AutoScrollSubscription(element, itemsControl);
        AutoScrollSubscriptions.Add(element, subscription);
        subscription.Start();
    }

    private static void HandleScrollToEndChanged(Interactive element, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is not true)
        {
            return;
        }

        AutoScrollSubscriptions.TryGetValue(element, out var subscription);
        TryScrollToEnd(element, force: true, subscription);
        SetScrollToEnd(element, false);
    }

    private static void TryScrollToEnd(Interactive element, bool force = false,
        AutoScrollSubscription? subscription = null)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => TryScrollToEnd(element, force, subscription),
                DispatcherPriority.Background);
            return;
        }
        
        var scrollViewer = element as ScrollViewer ?? element.FindDescendantOfType<ScrollViewer>();
        if (scrollViewer is null)
        {
            return;
        }

        var maxY = scrollViewer.Extent.Height - scrollViewer.Viewport.Height;
        var currentY = scrollViewer.Offset.Y;

        if (force || currentY >= maxY - 1)
        {
            if (subscription is not null)
            {
                subscription.ScheduleScroll(scrollViewer.ScrollToEnd);
            }
            else
            {
                Dispatcher.UIThread.Post(scrollViewer.ScrollToEnd, DispatcherPriority.Background);
            }
        }
    }

    private sealed class AutoScrollSubscription : IDisposable
    {
        private readonly Interactive _element;
        private readonly ItemsControl _itemsControl;
        private bool _isSubscribed;
        private bool _isDisposed;
        private int _isScrollScheduled;

        public AutoScrollSubscription(Interactive element, ItemsControl itemsControl)
        {
            _element = element;
            _itemsControl = itemsControl;

            _itemsControl.AttachedToVisualTree += OnAttachedToVisualTree;
            _itemsControl.DetachedFromVisualTree += OnDetachedFromVisualTree;
        }

        public void Start()
        {
            if (TopLevel.GetTopLevel(_itemsControl) is not null)
            {
                Subscribe();
            }
        }

        public void ScheduleScroll(Action action)
        {
            if (_isDisposed || Interlocked.CompareExchange(ref _isScrollScheduled, 1, 0) != 0)
            {
                return;
            }

            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    if (!_isDisposed && TopLevel.GetTopLevel(_itemsControl) is not null)
                    {
                        action();
                    }
                }
                finally
                {
                    Volatile.Write(ref _isScrollScheduled, 0);
                }
            }, DispatcherPriority.Background);
        }

        private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e) => Subscribe();

        private void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e) => Unsubscribe();

        private void Subscribe()
        {
            if (_isDisposed || _isSubscribed)
            {
                return;
            }

            _itemsControl.Items.CollectionChanged += OnCollectionChanged;
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed)
            {
                return;
            }

            _itemsControl.Items.CollectionChanged -= OnCollectionChanged;
            _isSubscribed = false;
        }

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs _)
        {
            if (GetAutoScrollToEnd(_element))
            {
                TryScrollToEnd(_element, subscription: this);
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            Unsubscribe();
            _itemsControl.AttachedToVisualTree -= OnAttachedToVisualTree;
            _itemsControl.DetachedFromVisualTree -= OnDetachedFromVisualTree;
        }
    }
}
