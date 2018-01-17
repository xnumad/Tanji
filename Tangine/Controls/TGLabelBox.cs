using System.Windows;
using System.Windows.Controls;

namespace Tangine.Controls
{
    public class TGLabelBox : TextBox
    {
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title),
            typeof(string), typeof(TGLabelBox), new UIPropertyMetadata("Title"));

        public static readonly DependencyProperty TitleWidthProperty = DependencyProperty.Register(nameof(TitleWidth),
            typeof(double), typeof(TGLabelBox), new UIPropertyMetadata(double.NaN));

        public string Title
        {
            get => (GetValue(TitleProperty) as string);
            set => SetValue(TitleProperty, value);
        }

        public double TitleWidth
        {
            get => (double)GetValue(TitleWidthProperty);
            set => SetValue(TitleWidthProperty, value);
        }
    }
}