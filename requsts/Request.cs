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
        private string? IdRoute; // ID маршрута, по которому дання заявка выполняется
        #endregion

        #region Свойства
        // Обычные
        public int GetId => id;
        public DateTime GetDateOfReceipt => DateOfReceipt;
        public string GetProduct => product;
        public int GetSize => size;
        public int? GetFactory => _Factory;
        public DateTime? GetDateOfAcceptance => _DateOfAcceptance;
        public DateTime? GetDateOfCompletion => _DateOfCompletion;
        public Route? GetRoute
        {
            get
            {
                if (IdRoute == null)
                    return null;
                var resRoute = DB.SelectWhere("Routes", "id", IdRoute)![0];
                Route route = new Route(
                    resRoute[0].ToString()!,
                    resRoute[1].ToString()!,
                    resRoute[2].ToString()!,
                    resRoute[3].ToString()!);
                return route;
            }
        }

        // Дополнительные
        public List<Technology> getNeedTechnologyProcessing
        {
            get
            {
                List<Technology> technologies = new List<Technology>();
                string res = DB.SelectWhere("Products", "name", product)![0][2].ToString()!; // Будет всего лишь один объект - строка из столбца TechnologyProcessing

                if (res != "") // Проверка на наличие технологии вообще, на всякий случай
                    foreach (string idTechnology in res.Split(';'))
                        technologies.Add((Technology)int.Parse(idTechnology));

                return technologies;
            }
        }
        #endregion

        #region Конструкторы
        public Request(string id, string DateOfReceipt, string product, string size, string? Factory, string? DateOfAcceptance, string? DateOfCompletion, string? IdRoute)
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
            this.IdRoute = IdRoute;
        }
        #endregion

        #region Методы
        // Сохранить
        public bool Save()
        {
            return DB.Replace("Requests", "id", id.ToString(),
                new string[] { "IdFactory", "DateOfAcceptance", "DateOfCompletion", "IdRoute" },
                new string[] { _Factory.ToString()!, _DateOfAcceptance.ToString()!, _DateOfCompletion.ToString()!, IdRoute!.ToString() });
        }

        // Выбрать завод, который выполнит заявку / означает принятие заявки / автосохранение в БД
        public bool SetFactory(Zavod zavod, Route route)
        {
            if (route.getId == null)
                if (!route.Save())
                    return false;
            
            _Factory = zavod.getId;
            _DateOfAcceptance = DateTime.Now;
            _DateOfCompletion = route.GetTimeLead == double.PositiveInfinity ? DateTime.MaxValue : ((DateTime)_DateOfAcceptance).AddMinutes(route.GetTimeLead);
            this.IdRoute = route.getId.ToString();

            return Save();
        }
        #endregion
    }
}