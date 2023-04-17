using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ClipboardManager.Helper;

public static class LoadMoreOnScrollBehavior
{
    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.RegisterAttached(
            "Command",
            typeof(ICommand),
            typeof(LoadMoreOnScrollBehavior),
            new PropertyMetadata(null, OnCommandChanged));

    public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.RegisterAttached(
            "CommandParameter",
            typeof(object),
            typeof(LoadMoreOnScrollBehavior));

    public static readonly DependencyProperty ThresholdProperty =
        DependencyProperty.RegisterAttached(
            "Threshold",
            typeof(double),
            typeof(LoadMoreOnScrollBehavior),
            new PropertyMetadata(420.0));

    private static readonly DependencyProperty AttachedScrollViewerProperty =
        DependencyProperty.RegisterAttached(
            "AttachedScrollViewer",
            typeof(ScrollViewer),
            typeof(LoadMoreOnScrollBehavior));

    public static ICommand? GetCommand(DependencyObject obj)
    {
        return (ICommand?)obj.GetValue(CommandProperty);
    }

    public static void SetCommand(DependencyObject obj, ICommand? value)
    {
        obj.SetValue(CommandProperty, value);
    }

    public static object? GetCommandParameter(DependencyObject obj)
    {
        return obj.GetValue(CommandParameterProperty);
    }

    public static void SetCommandParameter(DependencyObject obj, object? value)
    {
        obj.SetValue(CommandParameterProperty, value);
    }

    public static double GetThreshold(DependencyObject obj)
    {
        return (double)obj.GetValue(ThresholdProperty);
    }

    public static void SetThreshold(DependencyObject obj, double value)
    {
        obj.SetValue(ThresholdProperty, value);
    }

    private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
        {
            return;
        }

        Detach(element);
        element.Loaded -= HandleLoaded;
        element.Unloaded -= HandleUnloaded;

        if (e.NewValue is not null)
        {
            element.Loaded += HandleLoaded;
            element.Unloaded += HandleUnloaded;

            if (element.IsLoaded)
            {
                Attach(element);
            }
        }
    }

    private static void HandleLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            Attach(element);
        }
    }

    private static void HandleUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            Detach(element);
        }
    }

    private static void Attach(FrameworkElement element)
    {
        Detach(element);

        var scrollViewer = FindChild<ScrollViewer>(element);
        if (scrollViewer is null)
        {
            return;
        }

        element.SetValue(AttachedScrollViewerProperty, scrollViewer);
        scrollViewer.ScrollChanged += HandleScrollChanged;
    }

    private static void Detach(FrameworkElement element)
    {
        if (element.GetValue(AttachedScrollViewerProperty) is ScrollViewer scrollViewer)
        {
            scrollViewer.ScrollChanged -= HandleScrollChanged;
            element.ClearValue(AttachedScrollViewerProperty);
        }
    }

    private static void HandleScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (sender is not ScrollViewer scrollViewer)
        {
            return;
        }

        var owner = FindOwner(scrollViewer);
        if (owner is null)
        {
            return;
        }

        var threshold = Math.Max(0, GetThreshold(owner));
        if (scrollViewer.VerticalOffset + scrollViewer.ViewportHeight < scrollViewer.ExtentHeight - threshold)
        {
            return;
        }

        var command = GetCommand(owner);
        var parameter = GetCommandParameter(owner);
        if (command?.CanExecute(parameter) == true)
        {
            command.Execute(parameter);
        }
    }

    private static FrameworkElement? FindOwner(DependencyObject scrollViewer)
    {
        var current = scrollViewer;
        while (current is not null)
        {
            if (current is FrameworkElement element && GetCommand(element) is not null)
            {
                return element;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private static T? FindChild<T>(DependencyObject parent)
        where T : DependencyObject
    {
        var childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (var index = 0; index < childCount; index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T typedChild)
            {
                return typedChild;
            }

            var nestedChild = FindChild<T>(child);
            if (nestedChild is not null)
            {
                return nestedChild;
            }
        }

        return null;
    }
}
