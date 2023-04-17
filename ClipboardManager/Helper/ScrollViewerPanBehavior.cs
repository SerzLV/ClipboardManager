using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClipboardManager.Helper;

public static class ScrollViewerPanBehavior
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(ScrollViewerPanBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    private static readonly DependencyProperty StartPointProperty =
        DependencyProperty.RegisterAttached(
            "StartPoint",
            typeof(Point),
            typeof(ScrollViewerPanBehavior));

    private static readonly DependencyProperty StartHorizontalOffsetProperty =
        DependencyProperty.RegisterAttached(
            "StartHorizontalOffset",
            typeof(double),
            typeof(ScrollViewerPanBehavior));

    private static readonly DependencyProperty StartVerticalOffsetProperty =
        DependencyProperty.RegisterAttached(
            "StartVerticalOffset",
            typeof(double),
            typeof(ScrollViewerPanBehavior));

    private static readonly DependencyProperty OriginalCursorProperty =
        DependencyProperty.RegisterAttached(
            "OriginalCursor",
            typeof(Cursor),
            typeof(ScrollViewerPanBehavior));

    public static bool GetIsEnabled(DependencyObject obj)
    {
        return (bool)obj.GetValue(IsEnabledProperty);
    }

    public static void SetIsEnabled(DependencyObject obj, bool value)
    {
        obj.SetValue(IsEnabledProperty, value);
    }

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ScrollViewer scrollViewer)
        {
            return;
        }

        scrollViewer.PreviewMouseLeftButtonDown -= HandleMouseLeftButtonDown;
        scrollViewer.PreviewMouseMove -= HandleMouseMove;
        scrollViewer.PreviewMouseLeftButtonUp -= HandleMouseLeftButtonUp;
        scrollViewer.LostMouseCapture -= HandleLostMouseCapture;

        if (e.NewValue is true)
        {
            scrollViewer.PreviewMouseLeftButtonDown += HandleMouseLeftButtonDown;
            scrollViewer.PreviewMouseMove += HandleMouseMove;
            scrollViewer.PreviewMouseLeftButtonUp += HandleMouseLeftButtonUp;
            scrollViewer.LostMouseCapture += HandleLostMouseCapture;
        }
    }

    private static void HandleMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ScrollViewer scrollViewer || !CanPan(scrollViewer))
        {
            return;
        }

        scrollViewer.SetValue(StartPointProperty, e.GetPosition(scrollViewer));
        scrollViewer.SetValue(StartHorizontalOffsetProperty, scrollViewer.HorizontalOffset);
        scrollViewer.SetValue(StartVerticalOffsetProperty, scrollViewer.VerticalOffset);
        scrollViewer.SetValue(OriginalCursorProperty, scrollViewer.Cursor);
        scrollViewer.Cursor = Cursors.SizeAll;
        scrollViewer.CaptureMouse();
        e.Handled = true;
    }

    private static void HandleMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is not ScrollViewer scrollViewer || !scrollViewer.IsMouseCaptured)
        {
            return;
        }

        var startPoint = (Point)scrollViewer.GetValue(StartPointProperty);
        var currentPoint = e.GetPosition(scrollViewer);
        var startHorizontalOffset = (double)scrollViewer.GetValue(StartHorizontalOffsetProperty);
        var startVerticalOffset = (double)scrollViewer.GetValue(StartVerticalOffsetProperty);

        scrollViewer.ScrollToHorizontalOffset(startHorizontalOffset - (currentPoint.X - startPoint.X));
        scrollViewer.ScrollToVerticalOffset(startVerticalOffset - (currentPoint.Y - startPoint.Y));
        e.Handled = true;
    }

    private static void HandleMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ScrollViewer scrollViewer)
        {
            return;
        }

        ReleaseMouse(scrollViewer);
        e.Handled = true;
    }

    private static void HandleLostMouseCapture(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is ScrollViewer scrollViewer)
        {
            RestoreCursor(scrollViewer);
        }
    }

    private static bool CanPan(ScrollViewer scrollViewer)
    {
        return scrollViewer.ScrollableWidth > 0 || scrollViewer.ScrollableHeight > 0;
    }

    private static void ReleaseMouse(ScrollViewer scrollViewer)
    {
        if (scrollViewer.IsMouseCaptured)
        {
            scrollViewer.ReleaseMouseCapture();
        }

        RestoreCursor(scrollViewer);
    }

    private static void RestoreCursor(ScrollViewer scrollViewer)
    {
        scrollViewer.Cursor = (Cursor?)scrollViewer.GetValue(OriginalCursorProperty);
    }
}
