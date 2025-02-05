using System.Windows;
using System.Windows.Controls;

namespace Smart.Products
{
    /// <summary>
    /// Логика взаимодействия для ProductView.xaml
    /// </summary>
    public partial class ProductView : Window
    {
        public ProductView()
        {
            InitializeComponent();

            try
            {
                List<object[]> l = DB.SelectAll("Products")!;
                foreach (var litem in l)
                {
                    Product product = new Product(litem[0].ToString()!, litem[1].ToString()!, litem[2].ToString()!);
                    TreeViewItem tvi = new TreeViewItem() { Header = product.Name, Tag = product };
                    tvi.Selected += TreeViewItem_Selected;
                    tvTree.Items.Add(tvi);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // Событие по выбора узла в древе
        private void TreeViewItem_Selected(object sender, RoutedEventArgs e)
        {
            Product product = ((sender as TreeViewItem)!.Tag as Product)!;
            tbName.Text = product.Name;
            tbTechnology.Text = ConvertIdToString(product.TechnologyProcessing); // Здесь описание технологии
            tbTechnology.Tag = product.TechnologyProcessing; // Здесь содержание технологии

            GridMain.Visibility = Visibility.Visible;
        }

        // Событие по сохранению данных
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Получаем данные
                Product product;
                if (tvTree.SelectedItem == null)
                    product = new Product(tbName.Text, tbTechnology.Tag.ToString()!);
                else
                {
                    product = ((tvTree.SelectedItem as TreeViewItem)!.Tag as Product)!;
                    product.Name = tbName.Text;
                    product.TechnologyProcessing = tbTechnology.Tag.ToString()!;
                }

                // Сохраняем данные
                if (!product.Save())
                {
                    MessageBox.Show("Сохранить данные не удалось");
                    return;
                }

                // Обновляем древо, если удалось сохранить данные
                if (tvTree.SelectedItem == null)
                {
                    TreeViewItem tvi = new TreeViewItem() { Header = product.Name, Tag = product };
                    tvi.Selected += TreeViewItem_Selected;
                    tvTree.Items.Add(tvi);
                }
                else
                {
                    TreeViewItem tvi = (tvTree.SelectedItem as TreeViewItem)!;
                    tvi.Header = product.Name;
                    tvi.Tag = product;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // Добавление продукта
        private void Add_Click(object sender, RoutedEventArgs e)
        {
            // Убираем выделение, если оно присутствует
            if (tvTree.SelectedItem != null)
            {
                TreeViewItem tvi = (tvTree.SelectedItem as TreeViewItem)!;
                tvi.IsSelected = false;
            }

            // Открываем окошко
            GridMain.Visibility = Visibility.Visible;
            tbName.Text = "";
            tbTechnology.Text = "";
        }

        // Удаление продукта
        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверка на выбор узла
                TreeViewItem? tvi = (tvTree.SelectedItem as TreeViewItem);
                if (tvi == null)
                {
                    MessageBox.Show("Сначала выберите объект в древе");
                    return;
                }

                // Получение подтверждения
                string str = $"объект \"{tvi.Header}\"";
                if (MessageBox.Show($"Вы действительно хотите удалить {str}?", "Удаление", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    // Удаление
                    if ((tvi.Tag as Product)!.Delete())
                        tvTree.Items.Remove(tvi);
                    else
                        throw new Exception("Не удалось удалить элемент");
                }
                GridMain.Visibility = Visibility.Hidden;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // Настройка технологии обработки
        private void SelectTechnology_Click(object sender, RoutedEventArgs e)
        {
            SelectPTView view = new SelectPTView(tbTechnology.Tag == null ? null : tbTechnology.Tag.ToString());
            view.ShowDialog();

            string pt = view.getProcessingTechnology;
            tbTechnology.Text = ConvertIdToString(pt);
            tbTechnology.Tag = pt;

            view.Close();
        }

        // Перевод id в нормальное отображение для пользователя
        private string ConvertIdToString(string id)
        {
            string str = "";
            if (id != "")
            {
                foreach (string id1 in id.Split(';'))
                    str += $"{HelpingFuctions.getEnumDescription((Technology)int.Parse(id1))} -> ";
                str = str.Remove(str.Length - 4);
            }
            return str;
        }
    }
}