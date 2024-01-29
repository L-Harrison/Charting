using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Media;

namespace Charting.Extensions
{
    public static class ToggleButtonHelper
    {


        public static Brush GetToggleBtnTxtForeground(DependencyObject obj)
        {
            return (Brush)obj.GetValue(ToggleBtnTxtForegroundProperty);
        }
        public static void SetToggleBtnTxtForeground(DependencyObject obj, Brush value)
        {
            obj.SetValue(ToggleBtnTxtForegroundProperty, value);
        }
        // Using a DependencyProperty as the backing store for ToggleBtnTxtForeground.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ToggleBtnTxtForegroundProperty =
            DependencyProperty.RegisterAttached("ToggleBtnTxtForeground", typeof(Brush), typeof(ToggleButtonHelper), new PropertyMetadata(default));



        public static Brush GetToggleBtnBorderBrush(DependencyObject obj)
        {
            return (Brush)obj.GetValue(ToggleBtnBorderBrushProperty);
        }

        public static void SetToggleBtnBorderBrush(DependencyObject obj, Brush value)
        {
            obj.SetValue(ToggleBtnBorderBrushProperty, value);
        }

        // Using a DependencyProperty as the backing store for ToggleBtnBorderBrush.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ToggleBtnBorderBrushProperty =
            DependencyProperty.RegisterAttached("ToggleBtnBorderBrush", typeof(Brush), typeof(ToggleButtonHelper), new PropertyMetadata(default));




        public static Brush GetTextForeGround(DependencyObject obj)
        {
            return (Brush)obj.GetValue(TextForeGroundProperty);
        }

        public static void SetTextForeGround(DependencyObject obj, Brush value)
        {
            obj.SetValue(TextForeGroundProperty, value);
        }

        // Using a DependencyProperty as the backing store for TextForeGround.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextForeGroundProperty =
            DependencyProperty.RegisterAttached("TextForeGround", typeof(Brush), typeof(ToggleButtonHelper), new PropertyMetadata(default));



        public static Brush GetDefaultForeGround(DependencyObject obj)
        {
            return (Brush)obj.GetValue(DefaultForeGroundProperty);
        }

        public static void SetDefaultForeGround(DependencyObject obj, Brush value)
        {
            obj.SetValue(DefaultForeGroundProperty, value);
        }

        // Using a DependencyProperty as the backing store for DefaultForeGround.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DefaultForeGroundProperty =
            DependencyProperty.RegisterAttached("DefaultForeGround", typeof(Brush), typeof(ToggleButtonHelper), new PropertyMetadata(default,onv));

        private static void onv(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }
    }
}
