using Microsoft.VisualBasic;
using Smart.Products;
using Smart.requsts;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Smart
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private TreeViewItem? selectedObject;
        private ZavodView? zavodView;
        private RegionView? regionView;

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                // Установка событий из древа
                tvAll.bPlus.Click += ZavodCreate_Click;
                tvAll.bDelete.Click += ZavodStructureDelete_Click;
                tvAll.bPlusRegion.Click += RegionCreate_Click;

                // Загрузка имеющих заводов
                var list = DB.SelectAll("Zavod");
                if (list == null || list.Count == 0) // Если ничего нет, то и загружать ничего не будем
                    return;
                foreach (object[] o in list)
                {
                    // Добавление завода в список
                    Zavod z = new Zavod(int.Parse(o[0].ToString()!), o[1].ToString()!, o[2].ToString()!, o[3].ToString()!);

                    // Вывод завода в список
                    TreeViewItem tvi = new TreeViewItem() { Header = z.Name, Tag = z }; // Общий узел
                    tvi.MouseDoubleClick += Select_DoubleClick;
                    tvAll.tvTree.Items.Add(tvi); // Добавление в дерево

                    // Добавление участков
                    var LoadedRegions = z.LoadRegions();
                    foreach (Region region in LoadedRegions)
                    {
                        TreeViewItem tviRegion = new TreeViewItem() { Header = region.Name, Tag = region };
                        tviRegion.MouseDoubleClick += Select_DoubleClick;
                        tvi.Items.Add(tviRegion);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ZavodCreate_Click(object sender, RoutedEventArgs e)
        {
            // Обнуляем участки и маршруты, а также выделение
            regionView = null;
            selectedObject = null;

            // Открываем интерфейс завода
            zavodView = new ZavodView();
            zavodView.bSave.Click += ZavodStructureSave_Click;
            ccSelect.Content = zavodView;
        }

        private void ZavodStructureDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверка на выбор узла
                TreeViewItem? tvi = (tvAll.tvTree.SelectedItem as TreeViewItem);
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
                    if ((tvi.Tag as ZavodStructure)!.Delete())
                    {
                        if (tvi.Items.Count > 0)
                            for (int i = 0; i < tvi.Items.Count; ++i)
                                tvi.Items.RemoveAt(i);
                        tvAll.tvTree.Items.Remove(tvi);
                    }
                    else
                        throw new Exception("Не удалось удалить элемент");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Select_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Исключение ошибок при обработке события
            if (!(sender as TreeViewItem)!.IsSelected)
                return;
            if ((sender as TreeViewItem)!.Tag ==  null)
                throw new Exception("Ошибка! Объект без значения!");
            try
            {
                selectedObject = (sender as TreeViewItem);
                ZavodStructure o = ((sender as TreeViewItem)!.Tag as ZavodStructure)!; // Смотрим, что таит в себе этот объект
                zavodView = null;
                regionView = null;

                if (o is Zavod)
                {
                    zavodView = new ZavodView((o as Zavod)!);
                    zavodView.bSave.Click += ZavodStructureSave_Click;
                    ccSelect.Content = zavodView;
                }
                else if (o is Region)
                {
                    Zavod z = ((selectedObject!.Parent as TreeViewItem)!.Tag as Zavod)!;
                    Region r = (o as Region)!;
                    regionView = new RegionView(ref z, ref r);
                    regionView.bSave.Click += ZavodStructureSave_Click;
                    ccSelect.Content = regionView;
                    return;
                }
                else
                    throw new Exception("Неизвестный тип");

                // Безопаснее будет переключить этот объект в самом конце, когда никаких проблем ранее не возникло
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                selectedObject = null;
            }
        }

        private void ZavodStructureSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Создаём объект, который необходимо сохранить
                ZavodStructure zs;
                if (zavodView != null)
                    zs = zavodView.ToZavod();
                else if (regionView != null)
                    zs = regionView.ToRegion();
                else
                    throw new Exception("Неизвестный тип данных");

                // Сохраняем этот объект, если получится
                if (zs!.Save() == false)
                {
                    MessageBox.Show("Не удалось сохранить данные в БД");
                    return;
                }

                // Создаём объект в дереве, либо обновляем его
                if (selectedObject == null)
                {
                    TreeViewItem tvi = new TreeViewItem() { Header = zs.Name, Tag = zs, IsSelected = true };
                    selectedObject = tvi;
                    tvAll.tvTree.Items.Add(tvi);
                }
                else
                {
                    if (selectedObject.Tag is Zavod && regionView != null) // Условия, при которых распознаётся создание участка
                    {
                        TreeViewItem tviRegion = new TreeViewItem() { Header = zs.Name, Tag = zs, IsSelected = true };
                        tviRegion.MouseDoubleClick += Select_DoubleClick;
                        selectedObject.Items.Add(tviRegion);
                    }
                    else // В остальных случаях считаем, что надо просто обновить ветку
                    {
                        selectedObject.Header = zs.Name;
                        selectedObject.Tag = zs;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void RegionCreate_Click(object sender, RoutedEventArgs e)
        {
            // Проверка на то, в каком заводе создавать участок
            if (tvAll.tvTree.Items.Count == 0)
            {
                MessageBox.Show("Сначала необходимо создать хотя бы один завод");
                return;
            }

            // Проверка на то, в каком заводе надо создать участок
            if ((tvAll.tvTree.SelectedItem as TreeViewItem) == null || !((tvAll.tvTree.SelectedItem as TreeViewItem)!.Tag is Zavod))
            {
                MessageBox.Show("Сначала необходимо выбрать, на каком заводе создавать участок");
                return;
            }

            // Если всё норм, то можно открыть окно по созданию участка
            zavodView = null;
            selectedObject = tvAll.tvTree.SelectedItem as TreeViewItem;
            Zavod z = (selectedObject!.Tag as Zavod)!;
            regionView = new RegionView(ref z);
            regionView.bSave.Click += ZavodStructureSave_Click;
            ccSelect.Content = regionView;
        }

        private void ProductsShow_Click(object sender, RoutedEventArgs e)
        {
            ProductView pv = new ProductView();
            pv.ShowDialog();
        }

        private void RequestsShow_Click(object sender, RoutedEventArgs e)
        {
            RequestView rv = new RequestView();
            rv.ShowDialog();
        }
    }
}