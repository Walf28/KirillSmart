namespace Smart
{
    public class Route
    {
        #region Поля
        private int? id; // Номер маршрута на всякий случай
        private int? idRequest; // Номер заказа, исполняемого по данному маршруту
        private string _Route = ""; // Маршрут (последовательные id через точку с запятой)
        private int MaxPower = 0; // Мощность маршрута
        private int size = 0; // Сколько товара надо произвести
        private double timeLead = double.PositiveInfinity; // Время прохождения маршрута
        #endregion

        #region Свойства
        public int? getId;
        public int? IdRequest { get => idRequest; set => idRequest = value; }
        public string route
        {
            get => _Route;
            set
            {
                _Route = value;
                MaxPower = UpdatePower();
            }
        }
        public int? getMaxPower => MaxPower;
        public int Size
        {
            get => size;
            set
            {
                if (value < 0)
                    throw new Exception("Размер не может быть меньше 0");
                size = value;
                timeLead = CalculateTheTime();
            }
        }
        public double GetTimeLead => timeLead;
        #endregion

        #region Конструкторы
        public Route(string Route)
        {
            this._Route = Route;
            MaxPower = UpdatePower();
        }
        public Route(string id, string idRequest, string Route, string MaxPower)
        {
            this.id = int.Parse(id);
            this.idRequest = int.Parse(idRequest);
            this._Route = Route;
            this.MaxPower = int.Parse(MaxPower);
        }
        #endregion

        #region Методы
        // Сохранить объект или его изменения в БД
        public bool Save()
        {
            // Проверка
            if (idRequest == null)
                throw new Exception("Маршрут должен выполнять заявку");

            // Если объект ещё не создан, то его надо добавить
            if (id == null)
            {
                if (DB.Insert("Routes", new string[] { idRequest.ToString()!, _Route, MaxPower.ToString() }, out int? returnID))
                {
                    id = returnID;
                    return true;
                }
                else
                    return false;
            }

            // Если объект уже создан, то его надо просто обновить
            return DB.Replace("Routes", "id", id.ToString()!,
                new string[] { "idRequest", "MaxPower", "route", "size", "timeLead" },
                new string[] { idRequest.ToString()!, MaxPower.ToString(), _Route, size.ToString(), timeLead.ToString() });
        }

        // Удалить объект из БД
        public bool Delete()
        {
            // Надо подтверждение существования этого заказа
            if (id == null)
                return false;

            // А теперь можно и сам завод удалить
            return DB.Delete("Routes", "id", id.ToString()!);
        }

        // Нахождение мощности маршрута
        private int UpdatePower()
        {
            int power = 0;
            if (route != "")
                foreach (string IdRegion in _Route.Split(';'))
                {
                    var result = DB.SelectWhere("Region", "id", IdRegion)![0];
                    if (int.TryParse(result[4].ToString(), out int ResPower))
                        power += ResPower;
                }
            return power;
        }

        // Сколько времени понадобится данному маршруту, чтоб выполнить заказ (в минутах)
        private double CalculateTheTime()
        {
            double time = 0;

            foreach (string IdRegion in _Route.Split(";"))
            {
                var region = DB.SelectWhere("Region", "id", IdRegion)![0];
                int power = int.Parse(region[4].ToString()!); // Мощность линии (граммы/час)
                if (power == 0)
                    return double.PositiveInfinity;
                double timeForWorkload = double.Parse(region[6].ToString()!) / (power / 60.0); // Время, затрачиваемое на имеющиеся заказы
                double timeForItRegion = size / (power / 60.0); // Время, затрачиваемое на данный регион
                double timeForTransit = double.Parse(region[5].ToString()!); // Время прохождения линии (не учитывает время обработки)
                time += timeForWorkload + timeForItRegion + timeForTransit;
            }

            return time;
        }

        // Выбрать лучший маршрут
        public static Route SelectFastestRoute(List<Route> routes)
        {
            Route route = routes[0];

            foreach (var item in routes)
            {
                if (item.GetTimeLead < route.GetTimeLead)
                    route = item;
            }

            return route;
        }
        #endregion
    }
}