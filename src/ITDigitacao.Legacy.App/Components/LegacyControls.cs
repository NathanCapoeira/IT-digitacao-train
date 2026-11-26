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
        internal static RadioButton SelectableCard(UIElement content, string group, bool selected)
        {
            RadioButton radio=new RadioButton { Content=content, GroupName=group, IsChecked=selected, Padding=new Thickness(4), Margin=new Thickness(5), MinHeight=88, Background=LegacyTheme.Surface, Foreground=LegacyTheme.Text, BorderBrush=LegacyTheme.Border, BorderThickness=new Thickness(2), Cursor=System.Windows.Input.Cursors.Hand, Focusable=true };
            ControlTemplate template=new ControlTemplate(typeof(RadioButton)); FrameworkElementFactory frame=new FrameworkElementFactory(typeof(Border)); frame.Name="Frame";
            frame.SetValue(Border.CornerRadiusProperty,new CornerRadius(12)); frame.SetValue(Border.BackgroundProperty,new TemplateBindingExtension(Control.BackgroundProperty)); frame.SetValue(Border.BorderBrushProperty,new TemplateBindingExtension(Control.BorderBrushProperty)); frame.SetValue(Border.BorderThicknessProperty,new TemplateBindingExtension(Control.BorderThicknessProperty)); frame.SetValue(Border.PaddingProperty,new TemplateBindingExtension(Control.PaddingProperty));
            FrameworkElementFactory presenter=new FrameworkElementFactory(typeof(ContentPresenter)); presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty,HorizontalAlignment.Center); presenter.SetValue(ContentPresenter.VerticalAlignmentProperty,VerticalAlignment.Center); frame.AppendChild(presenter); template.VisualTree=frame;
            Trigger hover=new Trigger { Property=UIElement.IsMouseOverProperty, Value=true }; hover.Setters.Add(new Setter(Border.BorderBrushProperty,LegacyTheme.BrandRed,"Frame")); template.Triggers.Add(hover);
            Trigger selectedTrigger=new Trigger { Property=RadioButton.IsCheckedProperty, Value=true }; selectedTrigger.Setters.Add(new Setter(Border.BackgroundProperty,LegacyTheme.BrandTint,"Frame")); selectedTrigger.Setters.Add(new Setter(Border.BorderBrushProperty,LegacyTheme.BrandRed,"Frame")); template.Triggers.Add(selectedTrigger);
            Trigger pressed=new Trigger { Property=ButtonBase.IsPressedProperty, Value=true }; pressed.Setters.Add(new Setter(UIElement.OpacityProperty,0.78,"Frame")); template.Triggers.Add(pressed);
            Trigger disabled=new Trigger { Property=UIElement.IsEnabledProperty, Value=false }; disabled.Setters.Add(new Setter(UIElement.OpacityProperty,0.48,"Frame")); template.Triggers.Add(disabled);
            radio.Template=template; return radio;
        }
        internal static RadioButton Navigation(string label)
        {
            return Navigation(label,"LegacyNavigation");
        }
        internal static RadioButton Navigation(string label, string group)
        {
            RadioButton radio=new RadioButton { Content=label, GroupName=group, Padding=new Thickness(12,6,12,6), Margin=new Thickness(0,2,4,2), MinHeight=34, Background=Brushes.Transparent, Foreground=LegacyTheme.Ink, BorderBrush=Brushes.Transparent, BorderThickness=new Thickness(1), FontSize=13, FontWeight=FontWeights.SemiBold, Cursor=System.Windows.Input.Cursors.Hand, Focusable=true };
            ControlTemplate template=new ControlTemplate(typeof(RadioButton)); FrameworkElementFactory frame=new FrameworkElementFactory(typeof(Border)); frame.Name="Frame";
            frame.SetValue(Border.CornerRadiusProperty,new CornerRadius(8)); frame.SetValue(Border.BackgroundProperty,new TemplateBindingExtension(Control.BackgroundProperty)); frame.SetValue(Border.BorderBrushProperty,new TemplateBindingExtension(Control.BorderBrushProperty)); frame.SetValue(Border.BorderThicknessProperty,new TemplateBindingExtension(Control.BorderThicknessProperty)); frame.SetValue(Border.PaddingProperty,new TemplateBindingExtension(Control.PaddingProperty));
            FrameworkElementFactory presenter=new FrameworkElementFactory(typeof(ContentPresenter)); presenter.SetValue(ContentPresenter.VerticalAlignmentProperty,VerticalAlignment.Center); presenter.SetValue(ContentPresenter.RecognizesAccessKeyProperty,true); frame.AppendChild(presenter); template.VisualTree=frame;
            Trigger hover=new Trigger { Property=UIElement.IsMouseOverProperty, Value=true }; hover.Setters.Add(new Setter(Border.BackgroundProperty,LegacyTheme.Canvas,"Frame")); template.Triggers.Add(hover);
            Trigger selected=new Trigger { Property=RadioButton.IsCheckedProperty, Value=true }; selected.Setters.Add(new Setter(Border.BackgroundProperty,LegacyTheme.BrandTint,"Frame")); selected.Setters.Add(new Setter(Border.BorderBrushProperty,LegacyTheme.BrandRed,"Frame")); selected.Setters.Add(new Setter(Control.ForegroundProperty,LegacyTheme.BrandDark)); selected.Setters.Add(new Setter(Control.FontWeightProperty,FontWeights.Bold)); template.Triggers.Add(selected);
            Trigger pressed=new Trigger { Property=ButtonBase.IsPressedProperty, Value=true }; pressed.Setters.Add(new Setter(UIElement.OpacityProperty,0.75,"Frame")); template.Triggers.Add(pressed);
            Trigger disabled=new Trigger { Property=UIElement.IsEnabledProperty, Value=false }; disabled.Setters.Add(new Setter(UIElement.OpacityProperty,0.48,"Frame")); template.Triggers.Add(disabled);
            radio.Template=template; return radio;
        }
        internal static CheckBox PreferenceCheckBox(string label, bool checkedValue)
        {
            CheckBox check=new CheckBox { Content=label, IsChecked=checkedValue, Margin=new Thickness(0,5,0,5), Focusable=true };
            ControlTemplate template=new ControlTemplate(typeof(CheckBox)); FrameworkElementFactory row=new FrameworkElementFactory(typeof(Border)); row.Name="Row"; row.SetValue(Border.BackgroundProperty,Brushes.Transparent); row.SetValue(Border.CornerRadiusProperty,new CornerRadius(8)); row.SetValue(Border.PaddingProperty,new Thickness(6));
            FrameworkElementFactory dock=new FrameworkElementFactory(typeof(DockPanel)); FrameworkElementFactory box=new FrameworkElementFactory(typeof(Border)); box.Name="Box"; box.SetValue(DockPanel.DockProperty,Dock.Left); box.SetValue(Border.CornerRadiusProperty,new CornerRadius(5)); box.SetValue(Border.BorderBrushProperty,LegacyTheme.Muted); box.SetValue(Border.BorderThicknessProperty,new Thickness(1)); box.SetValue(FrameworkElement.WidthProperty,20.0); box.SetValue(FrameworkElement.HeightProperty,20.0); box.SetValue(FrameworkElement.MarginProperty,new Thickness(0,0,9,0)); box.SetValue(FrameworkElement.VerticalAlignmentProperty,VerticalAlignment.Center);
            FrameworkElementFactory mark=new FrameworkElementFactory(typeof(System.Windows.Shapes.Path)); mark.Name="Mark"; mark.SetValue(System.Windows.Shapes.Path.DataProperty,Geometry.Parse("M1,5 L4,8 L10,1")); mark.SetValue(System.Windows.Shapes.Path.StrokeProperty,Brushes.White); mark.SetValue(System.Windows.Shapes.Path.StrokeThicknessProperty,2.0); mark.SetValue(UIElement.VisibilityProperty,Visibility.Collapsed); box.AppendChild(mark); dock.AppendChild(box);
            FrameworkElementFactory presenter=new FrameworkElementFactory(typeof(ContentPresenter)); presenter.SetValue(FrameworkElement.VerticalAlignmentProperty,VerticalAlignment.Center); presenter.SetValue(ContentPresenter.ContentSourceProperty,"Content"); dock.AppendChild(presenter); row.AppendChild(dock); template.VisualTree=row;
            Trigger hover=new Trigger { Property=UIElement.IsMouseOverProperty, Value=true }; hover.Setters.Add(new Setter(Border.BackgroundProperty,LegacyTheme.Canvas,"Row")); template.Triggers.Add(hover);
            Trigger checkedTrigger=new Trigger { Property=ToggleButton.IsCheckedProperty, Value=true }; checkedTrigger.Setters.Add(new Setter(Border.BackgroundProperty,LegacyTheme.BrandRed,"Box")); checkedTrigger.Setters.Add(new Setter(Border.BorderBrushProperty,LegacyTheme.BrandRed,"Box")); checkedTrigger.Setters.Add(new Setter(UIElement.VisibilityProperty,Visibility.Visible,"Mark")); template.Triggers.Add(checkedTrigger);
            check.Template=template; return check;
        }
        internal static Border Card(UIElement content)
        {
            return new Border { Background=LegacyTheme.Surface, BorderBrush=LegacyTheme.Border, BorderThickness=new Thickness(1), CornerRadius=new CornerRadius(13), Padding=new Thickness(16), Margin=new Thickness(5), Child=content };
        }
    }
}
