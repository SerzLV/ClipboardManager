using System.Windows;
using System.Windows.Input;

namespace ClipboardManager.Helper;

public static class MouseWheelCommandBehavior
{
    public static readonly DependencyProperty WheelUpCommandProperty =
        DependencyProperty.RegisterAttached(
            "WheelUpCommand",
            typeof(ICommand),
            typeof(MouseWheelCommandBehavior),
            new PropertyMetadata(null, OnCommandChanged));

    public static readonly DependencyProperty WheelDownCommandProperty =
        DependencyProperty.RegisterAttached(
            "WheelDownCommand",
            typeof(ICommand),
            typeof(MouseWheelCommandBehavior),
            new PropertyMetadata(null, OnCommandChanged));

    public static ICommand? GetWheelUpCommand(DependencyObject obj)
    {
        return (ICommand?)obj.GetValue(WheelUpCommandProperty);
    }

    public static void SetWheelUpCommand(DependencyObject obj, ICommand? value)
    {
        obj.SetValue(WheelUpCommandProperty, value);
    }

    public static ICommand? GetWheelDownCommand(DependencyObject obj)
    {
        return (ICommand?)obj.GetValue(WheelDownCommandProperty);
    }

    public static void SetWheelDownCommand(DependencyObject obj, ICommand? value)
    {
        obj.SetValue(WheelDownCommandProperty, value);
    }

    private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element)
        {
            return;
        }

        element.PreviewMouseWheel -= HandlePreviewMouseWheel;

        if (GetWheelUpCommand(element) is not null || GetWheelDownCommand(element) is not null)
        {
            element.PreviewMouseWheel += HandlePreviewMouseWheel;
        }
    }

    private static void HandlePreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not DependencyObject dependencyObject)
        {
            return;
        }

        var command = e.Delta > 0
            ? GetWheelUpCommand(dependencyObject)
            : GetWheelDownCommand(dependencyObject);

        if (command?.CanExecute(null) != true)
        {
            return;
        }

        command.Execute(null);
        e.Handled = true;
    }
}
