using System.Windows;
using System.Windows.Controls;

namespace Smart
{
    public partial class SelectPTView : Window
    {
        public SelectPTView(string? ThisPT)
        {
            InitializeComponent();

            // Загрузка вариантов
            EnumToListbox();

            // Загрузка нынешней технологии
            if (ThisPT != null && ThisPT != "")
            {
                foreach (var item in ThisPT.Split(';'))
                {
                    Technology i = (Technology)int.Parse(item);
                    ListBoxItem lbi = new ListBoxItem() 
                    {
                        Content = HelpingFuctions.getEnumDescription(i),
                        Tag = (int)i
                    };
                    lbSelect.Items.Add(lbi);
                }
            }
        }

        private void EnumToListbox()
        {
            Array array = Enum.GetValues(typeof(Technology));
            foreach (var ItTechnology in array)
            {
                lbExist.Items.Add(new ListBoxItem()
                {
                    Content = HelpingFuctions.getEnumDescription((ItTechnology as Enum)!),
                    Tag = (int)((Technology)ItTechnology)
                });
            }
        }

        public string getProcessingTechnology
        {
            get
            {
                string s = "";
                if (lbSelect.Items.Count > 0)
                    foreach (ListBoxItem lbi in lbSelect.Items)
                        s += $"{lbi.Tag};";
                return s.Length == 0 ? s : s.Remove(s.Length - 1);
            }
        }

        private void bAdd_Click(object sender, RoutedEventArgs e)
        {
            ListBoxItem? lbi = (lbExist.SelectedItem as ListBoxItem)!;
            if (lbi == null)
            {
                MessageBox.Show("Сначала выделите объект справа, который хотите добавить");
                return;
            }

            lbSelect.Items.Add(new ListBoxItem() { Content = lbi.Content, Tag = lbi.Tag });
            return;
        }

        private void bDelete_Click(object sender, RoutedEventArgs e)
        {
            ListBoxItem? lbi = (lbSelect.SelectedItem as ListBoxItem)!;
            if (lbi == null)
            {
                MessageBox.Show("Сначала выделите объект слева, который хотите удалить");
                return;
            }

            lbSelect.Items.Remove(lbi);
            return;
        }

        private void bPrioritetUp_Click(object sender, RoutedEventArgs e)
        {
            // Проверки
            ListBoxItem? lbi = (lbSelect.SelectedItem as ListBoxItem)!;
            if (lbi == null)
            {
                MessageBox.Show("Сначала выделите объект слева, приоритет которого хотите изменить");
                return;
            }
            if (lbSelect.SelectedIndex == 0)
            {
                MessageBox.Show("Уже в первой очереди!");
                return;
            }

            // Изменение приоритета
            int si = lbSelect.SelectedIndex;
            lbSelect.Items.RemoveAt(si);
            lbSelect.Items.Insert(si - 1, lbi);
            lbSelect.SelectedIndex = si - 1;
            return;
        }

        private void bPrioritetDown_Click(object sender, RoutedEventArgs e)
        {
            // Проверки
            ListBoxItem? lbi = (lbSelect.SelectedItem as ListBoxItem)!;
            if (lbi == null)
            {
                MessageBox.Show("Сначала выделите объект слева, приоритет которого хотите изменить");
                return;
            }
            if (lbSelect.SelectedIndex +1 == lbSelect.Items.Count)
            {
                MessageBox.Show("Уже в последней очереди!");
                return;
            }

            // Изменение приоритета
            int si = lbSelect.SelectedIndex;
            lbSelect.Items.RemoveAt(si);
            lbSelect.Items.Insert(si + 1, lbi);
            lbSelect.SelectedIndex = si + 1;
            return;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }
    }
}