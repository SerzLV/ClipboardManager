using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using ClipboardManager.Interfaces;
using ClipboardManager.Localization;
using WpfButton = System.Windows.Controls.Button;
using WpfControl = System.Windows.Controls.Control;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace ClipboardManager.Services;

public sealed class SecretDialogService : ISecretDialogService
{
    private readonly LocalizationService _localization;

    public SecretDialogService(LocalizationService localization)
    {
        _localization = localization;
    }

    public string? ShowCreateSecretDialog(string suggestedName)
    {
        var nameBox = new WpfTextBox
        {
            MinWidth = 360,
            Text = suggestedName,
            FontSize = 14,
            Padding = new Thickness(12, 9, 12, 9),
            BorderBrush = Brush("#CBD5E1"),
            BorderThickness = new Thickness(1),
            Background = Brush("#F8FAFC"),
            Foreground = Brush("#182033"),
            SelectionBrush = Brush("#BFDBFE")
        };

        var validationText = new TextBlock
        {
            Text = _localization.SecretNameRequiredMessage,
            Foreground = Brush("#DC2626"),
            Margin = new Thickness(0, 6, 0, 0),
            Visibility = Visibility.Collapsed
        };

        var dialog = new Window
        {
            Title = _localization.SecretNameDialogTitle,
            Owner = System.Windows.Application.Current?.MainWindow,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            Background = Brushes.Transparent,
            ShowInTaskbar = false,
            Content = CreateContent(nameBox, validationText, out var saveButton, out var cancelButton)
        };

        string? result = null;
        saveButton.Click += (_, _) =>
        {
            var name = nameBox.Text.Trim();
            if (name.Length == 0)
            {
                validationText.Visibility = Visibility.Visible;
                nameBox.Focus();
                return;
            }

            result = name;
            dialog.DialogResult = true;
        };
        cancelButton.Click += (_, _) => dialog.DialogResult = false;

        dialog.Loaded += (_, _) =>
        {
            nameBox.Focus();
            nameBox.SelectAll();
        };

        return dialog.ShowDialog() == true
            ? result
            : null;
    }

    private FrameworkElement CreateContent(
        WpfTextBox nameBox,
        TextBlock validationText,
        out WpfButton saveButton,
        out WpfButton cancelButton)
    {
        var root = new Border
        {
            Width = 460,
            Background = Brushes.White,
            BorderBrush = Brush("#DDE5EF"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Effect = new DropShadowEffect
            {
                BlurRadius = 28,
                ShadowDepth = 0,
                Opacity = 0.16,
                Color = Color.FromRgb(17, 24, 39)
            }
        };

        var layout = new Grid();
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.Child = layout;

        var header = CreateHeader();
        Grid.SetRow(header, 0);
        layout.Children.Add(header);

        var panel = new StackPanel
        {
            Margin = new Thickness(24, 22, 24, 24)
        };
        Grid.SetRow(panel, 1);
        layout.Children.Add(panel);

        panel.Children.Add(new TextBlock
        {
            Text = _localization.SecretNameDialogDescription,
            Foreground = Brush("#687386"),
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 16)
        });

        panel.Children.Add(new TextBlock
        {
            Text = _localization.SecretNameLabel,
            Foreground = Brush("#182033"),
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 6)
        });
        panel.Children.Add(nameBox);
        panel.Children.Add(validationText);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 18, 0, 0)
        };

        cancelButton = CreateDialogButton(_localization.CancelButton);
        cancelButton.Margin = new Thickness(0, 0, 10, 0);
        cancelButton.IsCancel = true;

        saveButton = CreateDialogButton(_localization.SaveSecretButton);
        saveButton.IsDefault = true;
        saveButton.Background = Brush("#2563EB");
        saveButton.Foreground = Brushes.White;
        saveButton.BorderBrush = Brush("#2563EB");

        buttons.Children.Add(cancelButton);
        buttons.Children.Add(saveButton);
        panel.Children.Add(buttons);

        return root;
    }

    private Grid CreateHeader()
    {
        var header = new Grid
        {
            Background = Brush("#111827"),
            Height = 58
        };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.MouseLeftButtonDown += (_, e) =>
        {
            if (e.ButtonState == MouseButtonState.Pressed && !IsInsideButton(e.OriginalSource))
            {
                Window.GetWindow(header)?.DragMove();
            }
        };

        var icon = new Border
        {
            Width = 32,
            Height = 32,
            Margin = new Thickness(18, 0, 12, 0),
            CornerRadius = new CornerRadius(8),
            Background = Brush("#EEF2FF"),
            Child = new TextBlock
            {
                Text = "\uE72E",
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                Foreground = Brush("#4F46E5"),
                FontSize = 15,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            },
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(icon, 0);
        header.Children.Add(icon);

        var title = new TextBlock
        {
            Text = _localization.SecretNameDialogTitle,
            Foreground = Brushes.White,
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(title, 1);
        header.Children.Add(title);

        var closeButton = CreateChromeButton();
        closeButton.Click += (_, _) => Window.GetWindow(header)!.DialogResult = false;
        Grid.SetColumn(closeButton, 2);
        header.Children.Add(closeButton);

        return header;
    }

    private static WpfButton CreateDialogButton(string text)
    {
        return new WpfButton
        {
            Content = text,
            MinWidth = 96,
            Height = 38,
            Padding = new Thickness(14, 0, 14, 0),
            Background = Brushes.White,
            BorderBrush = Brush("#DDE5EF"),
            BorderThickness = new Thickness(1),
            Foreground = Brush("#182033"),
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Cursor = Cursors.Hand,
            Template = CreateButtonTemplate(new CornerRadius(8))
        };
    }

    private static WpfButton CreateChromeButton()
    {
        return new WpfButton
        {
            Content = new TextBlock
            {
                Text = "\uE711",
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            },
            Width = 38,
            Height = 38,
            Margin = new Thickness(0, 10, 10, 10),
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            Foreground = Brush("#CBD5E1"),
            Cursor = Cursors.Hand,
            Template = CreateButtonTemplate(new CornerRadius(8))
        };
    }

    private static ControlTemplate CreateButtonTemplate(CornerRadius cornerRadius)
    {
        var border = new FrameworkElementFactory(typeof(Border));
        border.Name = "Root";
        border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(WpfControl.BackgroundProperty));
        border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(WpfControl.BorderBrushProperty));
        border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(WpfControl.BorderThicknessProperty));
        border.SetValue(Border.CornerRadiusProperty, cornerRadius);

        var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
        contentPresenter.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        contentPresenter.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        contentPresenter.SetValue(ContentPresenter.RecognizesAccessKeyProperty, true);
        border.AppendChild(contentPresenter);

        var template = new ControlTemplate(typeof(WpfButton))
        {
            VisualTree = border
        };

        var hoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        hoverTrigger.Setters.Add(new Setter(Border.OpacityProperty, 0.88, "Root"));
        template.Triggers.Add(hoverTrigger);

        var pressedTrigger = new Trigger { Property = ButtonBase.IsPressedProperty, Value = true };
        pressedTrigger.Setters.Add(new Setter(Border.OpacityProperty, 0.76, "Root"));
        template.Triggers.Add(pressedTrigger);

        return template;
    }

    private static SolidColorBrush Brush(string hex)
    {
        return (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
    }

    private static bool IsInsideButton(object? source)
    {
        if (source is not DependencyObject current)
        {
            return false;
        }

        while (current is not null)
        {
            if (current is ButtonBase)
            {
                return true;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return false;
    }
}
