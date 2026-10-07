using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using WingetManager.Domain.Entities;

namespace WingetManager.UI.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool b)
        {
            return b ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is Visibility v && v == Visibility.Visible;
    }
}

public class NameToAvatarBrushConverter : IValueConverter
{
    private static readonly Windows.UI.Color[] Palette =
    [
        Windows.UI.Color.FromArgb(255, 0, 120, 212),   // Microsoft Blue
        Windows.UI.Color.FromArgb(255, 16, 124, 65),   // Forest Green
        Windows.UI.Color.FromArgb(255, 135, 100, 184), // Soft Purple
        Windows.UI.Color.FromArgb(255, 202, 80, 16),   // Warm Amber / Coral
        Windows.UI.Color.FromArgb(255, 0, 153, 188),   // Peacock Teal
        Windows.UI.Color.FromArgb(255, 180, 0, 158),   // Orchid / Magenta
        Windows.UI.Color.FromArgb(255, 232, 17, 35),   // Crimson
        Windows.UI.Color.FromArgb(255, 74, 85, 104)    // Slate
    ];

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        string text = value?.ToString() ?? string.Empty;
        if (string.IsNullOrEmpty(text))
        {
            return new Microsoft.UI.Xaml.Media.SolidColorBrush(Palette[0]);
        }
        uint hash = 2166136261;
        foreach (char c in text)
        {
            hash = (hash ^ c) * 16777619;
        }
        int index = (int)(hash % (uint)Palette.Length);
        return new Microsoft.UI.Xaml.Media.SolidColorBrush(Palette[index]);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool b)
        {
            return b ? Visibility.Collapsed : Visibility.Visible;
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public class IntToZeroVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is int count)
        {
            return count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public class StringFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (parameter is string format)
        {
            return string.Format(CultureInfo.CurrentCulture, format, value);
        }
        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public class PinButtonLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isPinned && isPinned)
        {
            return "Unpin";
        }
        return "Pin Version";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public class ExcludeButtonLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isExcluded && isExcluded)
        {
            return "Include";
        }
        return "Exclude";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public class ThemeToIndexConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is AppTheme theme)
        {
            return theme switch
            {
                AppTheme.System => 0,
                AppTheme.Light => 1,
                AppTheme.Dark => 2,
                _ => 0
            };
        }
        return 0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is int index)
        {
            return index switch
            {
                1 => AppTheme.Light,
                2 => AppTheme.Dark,
                _ => AppTheme.System
            };
        }
        return AppTheme.System;
    }
}

public class FilterToCheckedConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string current && parameter is string target)
        {
            return string.Equals(current, target, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isChecked && isChecked && parameter is string target)
        {
            return target;
        }
        return DependencyProperty.UnsetValue;
    }
}

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool hasValue = value != null && (value is not string s || !string.IsNullOrWhiteSpace(s));
        return hasValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

public class FilterToPillBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool isSelected = string.Equals(value?.ToString(), parameter?.ToString(), StringComparison.OrdinalIgnoreCase);
        if (isSelected && Microsoft.UI.Xaml.Application.Current.Resources.TryGetValue("CardBackgroundFillColorDefaultBrush", out var brush))
        {
            return brush;
        }
        return new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

public class FilterToPillBorderConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool isSelected = string.Equals(value?.ToString(), parameter?.ToString(), StringComparison.OrdinalIgnoreCase);
        if (isSelected && Microsoft.UI.Xaml.Application.Current.Resources.TryGetValue("CardStrokeColorDefaultBrush", out var brush))
        {
            return brush;
        }
        return new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

public class FilterToPillFontWeightConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool isSelected = string.Equals(value?.ToString(), parameter?.ToString(), StringComparison.OrdinalIgnoreCase);
        return isSelected ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}


