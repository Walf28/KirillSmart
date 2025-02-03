using System.Windows;
using System.Windows.Controls;

namespace Smart
{
    /// <summary>
    /// Логика взаимодействия для Tree.xaml
    /// </summary>
    public partial class Tree : UserControl
    {
        public Tree()
        {
            InitializeComponent();
        }

        private void tvTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (tvTree.SelectedItem != null)
                bDelete.IsEnabled = true;
            else
                bDelete.IsEnabled = false;
        }

        private void tvTree_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            e.Handled = true;
        }
    }
}
