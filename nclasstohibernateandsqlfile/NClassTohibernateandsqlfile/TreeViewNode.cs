using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NClassTohibernateandsqlfile
{
    class TreeViewNode : DependencyObject
    {
        public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.Register("IsChecked", typeof(bool), typeof(TreeViewNode));

        public bool IsChecked { get { return (bool)this.GetValue(IsCheckedProperty); } set { this.SetValue(IsCheckedProperty, value); } }

        public string Title { get; set; }

        public bool IsClass { get; set; }

        public Type Type { get; set; }

        public List<TreeViewNode> SubNodes { get; set; }
    }
}
