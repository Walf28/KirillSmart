using System.Windows;
using System.Windows.Controls;

namespace Smart
{
    public partial class RegionView : UserControl
    {
        private Zavod z;
        private Region? region;

        public Zavod getZavod => z;

        #region Конструкторы
        public RegionView(ref Zavod z, string NewRegionName = "Новый участок")
        {
            InitializeComponent();
            EnumToCombobox();
            this.z = z;
        }
        public RegionView(ref Zavod z, ref Region region)
        {
            // Обычное копирование данных
            InitializeComponent();
            EnumToCombobox();
            this.z = z;
            this.region = region;

            // Перевод имеющихся данных на экран
            tbName.Text = region.Name.Trim();
            cbType.SelectedIndex = (int)(region.Type);
            tbPower.Text = region.Power.ToString();
            tbTransitTime.Text = region.TransitTime.ToString();
            tbWorkload.Text = region.GetSummWorkload.ToString();
            if (region.Childrens != null && region.Childrens != "")
                foreach (string id in region.Childrens!.Split(';'))
                {
                    var res = DB.SelectWhere("Region", "id", id);
                    ListOfChildrens.Items.Add(new ListBoxItem()
                    {
                        Content = res![0][2].ToString(),
                        Tag = res![0][0].ToString()
                    });
                }
            if (region!.DowntimeStart != null)
            {
                tbDowntimeStart.Text = region!.DowntimeStart.ToString();
                tbDowntimeDuration.Text = region!.DowntimeDuration.ToString();
                tbDowntimeReason.Text = region!.DowntimeReason!.ToString();
            }
            else
                tbDowntimeStart.Text = "Сейчас";
        }
        #endregion

        // Добавление подчинённых участков
        private void AddChildrenRegions_Click(object sender, RoutedEventArgs e)
        {
            // Сначала берём данные
            SelectRegionView srv = new SelectRegionView(z, region == null ? null : region.getId);
            srv.ShowDialog();
            string[][] SelectedRegions = srv.getChildrenRegions();
            srv.Close(); // Теперь нам это окно не нужно

            // Добавляем эти данные
            ListOfChildrens.Items.Clear();
            foreach (string[] SelectRegion in SelectedRegions)
            {
                ListOfChildrens.Items.Add(new ListBoxItem() { Tag = SelectRegion[0], Content = SelectRegion[1] });
            }
        }

        // Конвертация данной страницы в переменную
        public Region ToRegion()
        {
            // Сначала проверка
            string errors = "";
            if (tbName.Text == "")
                errors += "Необходимо заполнить имя";
            if (errors != "")
                throw new Exception(errors);

            // Получаем необходимые переменные, описывающие новые данные
            Technology technology = (Technology)cbType.SelectedIndex;
            _ = int.TryParse(tbPower.Text, out int Power);
            _ = int.TryParse(tbTransitTime.Text, out int TransitTime);
            string idChildrens = "";
            if (ListOfChildrens.Items.Count > 0)
            {
                foreach (ListBoxItem lbi in ListOfChildrens.Items)
                    idChildrens += $"{lbi.Tag};";
                idChildrens = idChildrens.Remove(idChildrens.Length - 1);
            }
            bool DowntimeExist = int.TryParse(tbDowntimeDuration.Text, out int _DownTimeDuration);

            // Решаем, обновлять или создавать новый регион
            if (region == null)
                region = new Region(z.getId, tbName.Text, technology, Power, TransitTime, idChildrens,
                    DowntimeExist ? DateTime.Now : null, DowntimeExist ? _DownTimeDuration : null, DowntimeExist ? tbDowntimeReason.Text : null);
            else
            {
                region.Name = tbName.Text;
                region.Type = technology;
                region.Power = Power;
                region.TransitTime = TransitTime;
                region.Childrens = idChildrens;
                if (DowntimeExist)
                {
                    region.DowntimeStart = DateTime.Now;
                    region.DowntimeDuration = _DownTimeDuration;
                    region.DowntimeReason = tbDowntimeReason.Text;
                }
            }
            return region;
        }

        // Конвертация перечисления в комбобокс - надо осуществить конвертацию описания в адекватные имена
        private void EnumToCombobox()
        {
            Array array = Enum.GetValues(typeof(Technology));
            foreach (var ItTechnology in array)
            {
                cbType.Items.Add(new ComboBoxItem()
                {
                    Content = HelpingFuctions.getEnumDescription((ItTechnology as Enum)!),
                    Tag = ItTechnology
                });
            }
            cbType.SelectedIndex = 0;
        }
    }
}