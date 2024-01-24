using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;

namespace Charting
{
    public static class UIElementExtension
    {
        //根据子元素查找父元素
        public static T FindVisualParent<T>(this DependencyObject obj) where T : class
        {
            while (obj != null)
            {
                if (obj is T)
                    return obj as T;

                obj = VisualTreeHelper.GetParent(obj);
            }
            return null;
        }

        public static IEnumerable<T> FindVisualChildren<T>(this DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null)
            {
                yield break;
            }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                T val = (child as T)!;
                if (val != null)
                {
                    yield return val;
                }

                foreach (T item in FindVisualChildren<T>(child))
                {
                    yield return item;
                }
            }
        }
    }
}
