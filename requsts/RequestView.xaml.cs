using System.Windows;
using System.Windows.Controls;

namespace Smart.requsts
{
    /// <summary>
    /// Логика взаимодействия для RequestView.xaml
    /// </summary>
    public partial class RequestView : Window
    {
        private Request? SelectRequest;

        public RequestView()
        {
            InitializeComponent();

            // Загрузка древа
            var res = DB.SelectAll("Requests");
            foreach (var r in res!)
            {
                Request request = new Request(r[0].ToString()!, r[1].ToString()!, r[2].ToString()!, r[3].ToString()!, r[4].ToString(), r[5].ToString(), r[6].ToString());
                TreeViewItem tvi = new TreeViewItem() { Header = $"{request.getId}_{request.getProduct}", Tag = request };
                tvi.Selected += TreeViewItem_Selected;
                tvTree.Items.Add(tvi);
            }

            // Список заводов будем загружать полностью, хотя было бы неплохо провести отсеивание
            res = DB.SelectAll("Zavod");
            foreach (var r in res!)
            {
                Zavod z = new Zavod(int.Parse(r[0].ToString()!), r[1].ToString()!, r[2].ToString()!, r[3].ToString()!);
                ComboBoxItem cbi = new ComboBoxItem() { Content = r[1], Tag = z };
                cbFactory.Items.Add(cbi);
            }
        }

        // Выбор узла в древе
        private void TreeViewItem_Selected(object sender, RoutedEventArgs e)
        {
            GridMain.Visibility = Visibility.Visible;
            SelectRequest = ((sender as TreeViewItem)!.Tag as Request)!;

            // Вывод данных
            tbNumber.Text = SelectRequest.getId.ToString();
            tbDateOfReceipt.Text = SelectRequest.getDateOfReceipt.ToString();
            tbName.Text = SelectRequest.getProduct;
            tbSize.Text = SelectRequest.getSize.ToString();
            tbDateOfAcceptance.Text = SelectRequest.DateOfAcceptance == null ? DateTime.Now.ToString() : SelectRequest.DateOfAcceptance.ToString();
            tbDateOfCompletion.Text = SelectRequest.DateOfCompletion.ToString();

            // Поиск выбранного завода
            if (SelectRequest.Factory.ToString()! != "")
                foreach (ComboBoxItem cbi in cbFactory.Items)
                    if (cbi.Tag.ToString() == SelectRequest.Factory.ToString())
                    {
                        cbFactory.SelectedItem = cbi;
                        break;
                    }
        }

        // Принять заявку
        private void bAccept_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверка на выбранность завода
                if (cbFactory.SelectedItem == null)
                    throw new Exception("Сначала выберите, какой завод будет выполнять данный заказ");
                if (SelectRequest == null)
                    throw new Exception("Неизвестная ошибка!");

                // Нахождение маршрутов
                List<Route> routes = (cbFactory.Tag as Zavod)!.GetRoutes(SelectRequest!.getId, SelectRequest.getProduct);
                routes.ForEach(r =>
                {
                    tbDateOfCompletion.Text += $"{r.route}\n";
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void cbFactory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                Zavod z = ((cbFactory.SelectedItem as ComboBoxItem)!.Tag as Zavod)!;
                List<Route> routes = z.GetRoutes(SelectRequest!.getId, SelectRequest.getProduct);
                routes.ForEach(r =>
                {
                    tbDateOfCompletion.Text += $"{r.route}\n";
                });

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}