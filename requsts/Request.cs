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
        private DateTime? _DateOfCompletion; // Дата завершения выполнения заявки
        private int? IdRoute; // ID маршрута, по которому дання заявка выполняется
        private List<DateTime>? DateOfCompletionOnRegion; // Даты завершения выполнения заявки по участкам
        public bool isFinish = false; // Завершено выполнение заявки или нет
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
                var resRoute = DB.SelectWhere("Routes", "id", IdRoute.ToString()!)![0];
                Route route = new(
                    resRoute[0].ToString()!,
                    resRoute[1].ToString()!,
                    resRoute[2].ToString()!,
                    resRoute[3].ToString()!,
                    resRoute[4].ToString()!,
                    resRoute[5].ToString()!,
                    resRoute[6].ToString()!,
                    resRoute[7].ToString()!);
                return route;
            }
        }
        public List<DateTime>? GetDateOfCompletionOnRegion => DateOfCompletionOnRegion;
        public bool IsFinish => isFinish;

        // Дополнительные
        public List<Technology> GetNeedTechnologyProcessing
        {
            get
            {
                List<Technology> technologies = [];
                string res = DB.SelectWhere("Products", "name", product)![0][2].ToString()!; // Будет всего лишь один объект - строка из столбца TechnologyProcessing

                if (res != "") // Проверка на наличие технологии вообще, на всякий случай
                    foreach (string idTechnology in res.Split(';'))
                        technologies.Add((Technology)int.Parse(idTechnology));

                return technologies;
            }
        }
        #endregion

        #region Конструкторы
        public Request(int id)
        {
            this.id = id;
            this.product = "";
            Refresh();
        }
        public Request(string id, string DateOfReceipt, string product, string size, string? Factory, string? DateOfAcceptance, string? DateOfCompletion, string? IdRoute, string? DateOfCompletionOnRegion, string? IsFinish)
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
            if (int.TryParse(Factory, out int intIdRoute))
                this.IdRoute = intIdRoute;
            if (DateOfCompletionOnRegion != null)
                this.DateOfCompletionOnRegion = StringToDoCoR(DateOfCompletionOnRegion);
            bool.TryParse(IsFinish, out this.isFinish);
        }
        #endregion

        #region Методы
        // Сохранить
        public bool Save()
        {
            return DB.Replace("Requests", "id", id.ToString(),
                ["Factory", "DateOfAcceptance", "DateOfCompletion", "IdRoute", "DateOfCompletionOnRegion", "IsFinish"],
                [_Factory.ToString()!, _DateOfAcceptance.ToString()!, _DateOfCompletion.ToString()!, IdRoute!.ToString()!, DoCoRToString(), isFinish ? "1" : "0"]);
        }

        // Завершить заказ
        public bool SetFinish()
        {
            _DateOfCompletion = DateTime.Now;
            isFinish = true;
            return Save();
        }

        // Выбрать завод, который выполнит заявку / означает принятие заявки / автосохранение в БД
        public bool SetFactory(Zavod zavod, ref Route route)
        {
            if (route.GetId == null)
                if (!route.Save())
                    return false;

            _Factory = zavod.getId;
            _DateOfAcceptance = DateTime.Now;
            _DateOfCompletion = route.GetTimeLead == double.PositiveInfinity ? DateTime.MaxValue : ((DateTime)_DateOfAcceptance).AddMinutes(route.GetTimeLead);
            this.IdRoute = route.GetId;
            List<double> LeadOnRegions = route.GetTimeLeadOnRegions;
            if (LeadOnRegions.Count > 0)
            {
                DateOfCompletionOnRegion = [((DateTime)_DateOfAcceptance)!.AddMinutes(LeadOnRegions[0])];
                for (int i = 1; i < LeadOnRegions.Count; ++i)
                    DateOfCompletionOnRegion.Add(DateOfCompletionOnRegion[^1].AddMinutes(LeadOnRegions[i]));
            }

            return Save();
        }

        // Обновить объект
        public void Refresh()
        {
            var res = DB.SelectWhere("Requests", "id", id.ToString())![0];
            this.DateOfReceipt = DateTime.Parse(res[1].ToString()!);
            this.product = res[2].ToString()!;
            this.size = int.Parse(res[3].ToString()!);
            if (int.TryParse(res[4].ToString(), out int Factory))
                this._Factory = Factory;
            if (DateTime.TryParse(res[5].ToString(), out DateTime DateOfAcceptance))
                this._DateOfAcceptance = DateOfAcceptance;
            if (DateTime.TryParse(res[6].ToString(), out DateTime DateOfCompletion))
                this._DateOfCompletion = DateOfCompletion;
            if (int.TryParse(res[7].ToString(), out int IdRoute))
                this.IdRoute = IdRoute;
            this.DateOfCompletionOnRegion = StringToDoCoR(res[8].ToString()!);
            if (bool.TryParse(res[9].ToString(), out bool IsFinish))
                this.isFinish = IsFinish;
        }

        // Обновить время выполнения заявки по определённому номеру
        public void UpdateDateOfCompletionOnRegion(int NumberRegion, DateTime date, bool Save = true)
        {
            if (DateOfCompletionOnRegion != null && NumberRegion < DateOfCompletionOnRegion.Count)
            {
                DateOfCompletionOnRegion[NumberRegion] = date;
                if (Save)
                    if (!this.Save())
                        throw new Exception("Не удалось обновить данные в БД");
            }
        }

        // Конвертация дат в строку и строки в даты
        private string DoCoRToString()
        {
            string str = "";
            foreach (var DoCoR in DateOfCompletionOnRegion!)
                str += $"{DoCoR};";
            return str == "" ? str : str.Remove(str.Length - 1);
        }
        private static List<DateTime> StringToDoCoR(string str)
        {
            List<DateTime> list = [];
            if (str != "")
                foreach (var date in str.Split(";"))
                    list.Add(DateTime.Parse(date));
            return list;
        }
        #endregion
    }
}