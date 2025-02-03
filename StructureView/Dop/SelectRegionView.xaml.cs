using System.Windows;
using System.Windows.Controls;

namespace Smart
{
    public partial class SelectRegionView : Window
    {
        #region Конструкторы
        public SelectRegionView(Zavod z, int? IdRegionException)
        {
            InitializeComponent();

            List<object[]> res = DB.SelectWhere("Region", "idParent", z.getId.ToString()!)!;
            foreach (object[] r in res!)
            {
                if (IdRegionException != null && IdRegionException.ToString() == r[0].ToString())
                    continue;
                lbVariant.Items.Add(new ListBoxItem() { Content = r[2], Tag = r[0] });
            }
        }
        #endregion

        #region События
        private void bAdd_Click(object sender, RoutedEventArgs e)
        {
            // Проверка на выбранность объекта
            ListBoxItem? item = lbVariant.SelectedItem as ListBoxItem;
            if (item == null)
                return;

            // Добавление участка
            lbVariant.Items.Remove(item);
            lbSelect.Items.Add(item);
        }

        private void bDelete_Click(object sender, RoutedEventArgs e)
        {
            // Проверка на выбранность объекта
            ListBoxItem? item = lbSelect.SelectedItem as ListBoxItem;
            if (item == null)
                return;

            // Удаление участка
            lbSelect.Items.Remove(item);
            lbVariant.Items.Add(item);
        }

        private void bClose_Click(object sender, RoutedEventArgs e)
        {
            this.Visibility = Visibility.Collapsed;
        }
        #endregion

        #region Методы
        // Взять id и имя выбранных участков
        public string[][] getChildrenRegions()
        {
            string[][] Regions = new string[lbSelect.Items.Count][];
            for (int i = 0; i < lbSelect.Items.Count; ++i)
            {
                ListBoxItem lbi = (lbSelect.Items[i] as ListBoxItem)!;
                Regions[i] = new string[] { lbi.Tag.ToString()!, lbi.Content.ToString()! };
            }
            return Regions;
        }
        #endregion
    }
}