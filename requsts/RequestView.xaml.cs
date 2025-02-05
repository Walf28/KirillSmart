using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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
                Request request = new Request(
                    r[0].ToString()!, // id
                    r[1].ToString()!, // DateOfReceipt
                    r[2].ToString()!, // Product
                    r[3].ToString()!, // Size
                    r[4].ToString(), // Factory
                    r[5].ToString(), // GetDateOfAcceptance
                    r[6].ToString(), // GetDateOfCompletion
                    r[7].ToString() // IdRoute
                    );
                TreeViewItem tvi = new TreeViewItem() { Header = $"{request.GetId}_{request.GetProduct}", Tag = request };
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
            tbNumber.Text = SelectRequest.GetId.ToString();
            tbDateOfReceipt.Text = SelectRequest.GetDateOfReceipt.ToString();
            tbName.Text = SelectRequest.GetProduct;
            tbSize.Text = SelectRequest.GetSize.ToString();
            tbDateOfAcceptance.Text = SelectRequest.GetDateOfAcceptance == null ? DateTime.Now.ToString() : SelectRequest.GetDateOfAcceptance.ToString();
            tbDateOfCompletion.Text = SelectRequest.GetDateOfCompletion == null ? "" : SelectRequest.GetDateOfCompletion.ToString();

            // Поиск выбранного завода
            if (SelectRequest.GetFactory != null)
                foreach (ComboBoxItem cbi in cbFactory.Items)
                    if ((int)cbi.Tag == SelectRequest.GetFactory)
                    {
                        cbFactory.SelectedItem = cbi;
                        break;
                    }

            // Выделение заводов, которые могут справиться с заказом
            foreach (ComboBoxItem cbi in cbFactory.Items)
            {
                Zavod z = (cbi.Tag as Zavod)!;
                cbi.Background = z.ItCanMakeRequest(SelectRequest) ? Brushes.Lime : Brushes.Red;
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

                // Принятие заявки
                //(cbFactory.Tag as Zavod)!.AddRequest(ref SelectRequest);

                // Нахождение маршрутов
                /*List<Route> routes = (cbFactory.Tag as Zavod)!.GetRoutes(SelectRequest!.getId, SelectRequest.getProduct);
                routes.ForEach(r =>
                {
                    tbDateOfCompletion.Text += $"{r.route}\n";
                });*/
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // Выбор завода
        private void cbFactory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((cbFactory.SelectedItem as ComboBoxItem)!.Background == Brushes.Red)
            {
                tbDateOfCompletion.Text = "Данный завод не сможет выполнить заказ";
                return;
            }
            try
            {
                Zavod z = ((cbFactory.SelectedItem as ComboBoxItem)!.Tag as Zavod)!;
                List<Route> AllRoutes = z.GetRoutes(tbName.Text, int.Parse(tbSize.Text));
                Route r = Route.SelectFastestRoute(AllRoutes);
                double TimeLead = r.GetTimeLead;
                tbDateOfCompletion.Text = double.IsInfinity(TimeLead) ? "Заказ никогда не будет выполнен" : DateTime.Now.AddMinutes(r.GetTimeLead).ToString();

                // Эти настройки для отладки
                tbTimeLeadRoute.Text = TimeLead.ToString();
                tbRoute.Text = "";
                foreach (var id in r.route.Split(';'))
                {
                    var res = DB.SelectWhere("Region", "id", id)![0];
                    tbRoute.Text += $"{res[2]} -> ";
                }
                tbRoute.Text = tbRoute.Text.Remove(tbRoute.Text.Length - 4);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}