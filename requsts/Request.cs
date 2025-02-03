namespace Smart
{
    public class Request
    {
        #region Поля
        private int id; // Номер заявки
        private DateTime DateOfReceipt; // Дата поступления заявки
        private string product; // Название заказанного товара
        private int size; // Размер заказа
        private int? _Factory; // Номер завода, выполняющего заявку
        private DateTime? _DateOfAcceptance; // Дата принятия заявки
        private DateTime? _DateOfCompletion; // Дата завершения заявки
        #endregion

        #region Свойства
        public int getId => id;
        public DateTime getDateOfReceipt => DateOfReceipt;
        public string getProduct => product;
        public int getSize => size;
        public int? Factory { get => _Factory; set => _Factory = value; }
        public DateTime? DateOfAcceptance { get => _DateOfAcceptance; set => _DateOfAcceptance = value; }
        public DateTime? DateOfCompletion { get => _DateOfCompletion; set => _DateOfCompletion = value; }
        #endregion

        #region Конструкторы
        public Request(string id, string DateOfReceipt, string product, string size, string? Factory, string? DateOfAcceptance, string? DateOfCompletion)
        {
            this.id = int.Parse(id);
            this.DateOfReceipt = DateTime.Parse(DateOfReceipt);
            this.product = product;
            this.size = int.Parse(size);
            if (int.TryParse(Factory, out int res))
                this._Factory = res;
            if (DateTime.TryParse(DateOfAcceptance, out DateTime dt))
                this._DateOfAcceptance = dt;
            if (DateTime.TryParse(DateOfCompletion, out dt))
                this._DateOfCompletion = dt;
        }
        #endregion

        #region Методы
        public bool Save()
        {
            return DB.Replace("Requests", "id", id.ToString(),
                new string[] { "IdFactory", "DateOfAcceptance", "DateOfCompletion" },
                new string[] { _Factory.ToString()!, _DateOfAcceptance.ToString()!, _DateOfCompletion.ToString()! });
        }
        #endregion
    }
}
