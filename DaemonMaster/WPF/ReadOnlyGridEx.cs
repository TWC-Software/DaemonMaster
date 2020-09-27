using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace DaemonMaster.WPF
{
    public class ReadOnlyGridEx : Grid
    {
        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(ReadOnlyGridEx), new PropertyMetadata(false, OnIsReadOnlyChanged));

        public ReadOnlyGridEx()
        {
            Loaded += IsReadOnlyGrid_Loaded;
        }

        private static void OnIsReadOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SetChildren(d, (bool)e.NewValue);
        }
        private static void SetChildren(DependencyObject parent, bool isReadonly)
        {
            var children = LogicalTreeHelper.GetChildren(parent);
            foreach (object obj in LogicalTreeHelper.GetChildren(parent))
            {
                if (obj is DependencyObject dependencyObject)
                {
                    Type type = dependencyObject.GetType();
                    if (type == typeof(CheckBox) || 
                        type == typeof(Button) || 
                        type == typeof(RadioButton) ||
                        type == typeof(ComboBox))
                    {
                        PropertyInfo propertyInfo = type.GetProperties().FirstOrDefault(p => p.Name == "IsEnabled");
                        propertyInfo?.SetValue(dependencyObject, !isReadonly, null);
                    }
                    else
                    {
                        PropertyInfo propertyInfo = type.GetProperties().FirstOrDefault(p => p.Name == "IsReadOnly");
                        propertyInfo?.SetValue(dependencyObject, isReadonly, null);
                    }

                    SetChildren(dependencyObject, isReadonly);
                }
            }
        }
        
        void IsReadOnlyGrid_Loaded(object sender, RoutedEventArgs e)
        {
            SetChildren(this, IsReadOnly);
        }
    }
}
