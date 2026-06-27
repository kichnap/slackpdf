using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SlackPDF.Converters;

[ValueConversion(typeof(bool), typeof(bool))]
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => value is bool b ? !b : value;

    public object ConvertBack(object value, Type t, object p, CultureInfo c)
        => value is bool b ? !b : value;
}

[ValueConversion(typeof(bool), typeof(Visibility))]
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type t, object p, CultureInfo c)
        => value is Visibility.Visible;
}

[ValueConversion(typeof(int), typeof(Visibility))]
public class IsNonZeroToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => value is int i && i != 0 ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type t, object p, CultureInfo c)
        => DependencyProperty.UnsetValue;
}

[ValueConversion(typeof(Enum), typeof(bool))]
public class EnumToBoolConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => value?.ToString() == p?.ToString();

    public object ConvertBack(object value, Type t, object p, CultureInfo c)
    {
        if (value is true && p != null)
            return Enum.Parse(t, p.ToString()!);
        return Binding.DoNothing;
    }
}

[ValueConversion(typeof(string), typeof(bool))]
public class StringEqualsToBoolConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => value?.ToString() == p?.ToString();

    public object ConvertBack(object value, Type t, object p, CultureInfo c)
        => value is true ? p?.ToString() ?? string.Empty : Binding.DoNothing;
}

[ValueConversion(typeof(bool), typeof(Thickness))]
public class BoolToThicknessConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => value is true ? new Thickness(2) : new Thickness(0);

    public object ConvertBack(object value, Type t, object p, CultureInfo c)
        => DependencyProperty.UnsetValue;
}

[ValueConversion(typeof(int), typeof(string))]
public class PageIndexToNumberConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => value is int i ? (i + 1).ToString() : string.Empty;

    public object ConvertBack(object value, Type t, object p, CultureInfo c)
        => DependencyProperty.UnsetValue;
}

[ValueConversion(typeof(object), typeof(Visibility))]
public class IsNullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => value == null ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type t, object p, CultureInfo c)
        => DependencyProperty.UnsetValue;
}

[ValueConversion(typeof(object), typeof(Visibility))]
public class IsNotNullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => value != null ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type t, object p, CultureInfo c)
        => DependencyProperty.UnsetValue;
}
