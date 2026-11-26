using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Animation;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using ITDigitacao.Legacy.App.Themes;
namespace ITDigitacao.Legacy.App.Components
{
    internal static class LegacyControls
    {
        internal static TextBlock Text(string value, double size, bool bold, Brush foreground)
        {
            return new TextBlock { Text=value, FontSize=size, FontWeight=bold?FontWeights.Bold:FontWeights.Normal, Foreground=foreground??LegacyTheme.Ink, TextWrapping=TextWrapping.Wrap, Margin=new Thickness(6) };
        }
        internal static Button Button(string label, Action click, bool primary)
        {
            Button button=new Button { Content=label, Padding=new Thickness(14,9,14,9), Margin=new Thickness(5), FontSize=14, FontWeight=FontWeights.SemiBold, Background=primary?LegacyTheme.BrandRed:LegacyTheme.Surface, Foreground=primary?Brushes.White:LegacyTheme.Ink, BorderBrush=primary?LegacyTheme.BrandDark:LegacyTheme.Border, BorderThickness=new Thickness(1), Cursor=System.Windows.Input.Cursors.Hand, Focusable=true };
            ControlTemplate template=new ControlTemplate(typeof(Button)); FrameworkElementFactory frame=new FrameworkElementFactory(typeof(Border)); frame.Name="Frame";
            frame.SetValue(Border.CornerRadiusProperty,new CornerRadius(10)); frame.SetValue(Border.BackgroundProperty,new TemplateBindingExtension(Control.BackgroundProperty)); frame.SetValue(Border.BorderBrushProperty,new TemplateBindingExtension(Control.BorderBrushProperty)); frame.SetValue(Border.BorderThicknessProperty,new TemplateBindingExtension(Control.BorderThicknessProperty)); frame.SetValue(Border.PaddingProperty,new TemplateBindingExtension(Control.PaddingProperty));
            FrameworkElementFactory presenter=new FrameworkElementFactory(typeof(ContentPresenter)); presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty,HorizontalAlignment.Center); presenter.SetValue(ContentPresenter.VerticalAlignmentProperty,VerticalAlignment.Center); presenter.SetValue(ContentPresenter.RecognizesAccessKeyProperty,true); frame.AppendChild(presenter); template.VisualTree=frame;
            Trigger hover=new Trigger { Property=UIElement.IsMouseOverProperty, Value=true }; hover.Setters.Add(new Setter(Border.BorderBrushProperty,primary?LegacyTheme.BrandRed:LegacyTheme.BrandDark,"Frame")); template.Triggers.Add(hover);
            Trigger pressed=new Trigger { Property=ButtonBase.IsPressedProperty, Value=true }; pressed.Setters.Add(new Setter(UIElement.OpacityProperty,0.78,"Frame")); template.Triggers.Add(pressed);
            Trigger disabled=new Trigger { Property=UIElement.IsEnabledProperty, Value=false }; disabled.Setters.Add(new Setter(UIElement.OpacityProperty,0.48,"Frame")); template.Triggers.Add(disabled); button.Template=template;
            button.Click += delegate { if(click!=null) click(); }; return button;
        }
        internal static Border Card(UIElement content)
        {
            return new Border { Background=LegacyTheme.Surface, BorderBrush=LegacyTheme.Border, BorderThickness=new Thickness(1), CornerRadius=new CornerRadius(13), Padding=new Thickness(16), Margin=new Thickness(5), Child=content };
        }
    }
}
