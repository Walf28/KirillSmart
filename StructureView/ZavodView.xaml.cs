using System.Windows.Controls;

namespace Smart
{
    public partial class ZavodView : UserControl
    {
        Zavod? z;

        public ZavodView(string NewZavodName = "Новый завод")
        {
            InitializeComponent();
            tbName.Text = NewZavodName.Trim();
        }

        public ZavodView(Zavod z)
        {
            InitializeComponent();
            this.z = z;
            tbName.Text = z.Name.Trim();
        }

        public Zavod ToZavod()
        {
            // Сначала - проверка
            string errors = "";
            if (tbName.Text == "")
                errors += "Необходимо заполнить имя завода";
            if (errors != "")
                throw new Exception(errors);

            // Далее - заполнение или обновление данных о заводе
            if (z == null)
                z = new Zavod(tbName.Text);
            else
            {
                z.Name = tbName.Text;
            }

            // На этом закончили
            return z;
        }
    }
}