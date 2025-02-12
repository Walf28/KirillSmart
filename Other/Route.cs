namespace Smart
{
    public class Route
    {
        #region Поля
        private int? id; // Номер маршрута на всякий случай
        private int? idRequest; // Номер заказа, исполняемого по данному маршруту
        private List<int> _route = []; // Маршрут (последовательные id через точку с запятой)
        private int MaxPower = 0; // Мощность маршрута
        private int size = 0; // Сколько товара надо произвести
        private double timeLead = double.PositiveInfinity; // Суммарное время прохождения маршрута (в минутах)
        private List<double> timeLeadOnRegions = []; // Время прохождения по участкам (в минутах)
        private int regionQueue = 0; // номер региона по счёту, который должен обрабатываться на данный момент.
        #endregion

        #region Свойства
        public int? GetId => id;
        public int? IdRequest { get => idRequest; set => idRequest = value; }
        public List<int> ItRoute
        {
            get => _route;
            set
            {
                _route = value;
                MaxPower = UpdatePower();
                if (Size != 0)
                    timeLead = CalculateTheTime();
            }
        }
        public int? GetMaxPower => MaxPower;
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
        public List<double> GetTimeLeadOnRegions => timeLeadOnRegions;
        public Request GetRequest => new Request((int)idRequest!);
        #endregion

        #region Конструкторы
        public Route(int id)
        {
            this.id = id;
            Refresh();
        }
        public Route(string Route)
        {
            this._route = RouteStringToList(Route);
            MaxPower = UpdatePower();
        }
        public Route(string id, string idRequest, string Route, string MaxPower, string size, string timeLead, string timeLeadOnRegions, string regionQueue)
        {
            this.id = int.Parse(id);
            this.idRequest = int.Parse(idRequest);
            this._route = RouteStringToList(Route);
            this.MaxPower = int.Parse(MaxPower);
            this.size = int.Parse(size);
            this.timeLead = double.Parse(timeLead);
            foreach (var TLoR in timeLeadOnRegions.Split(';'))
                this.timeLeadOnRegions.Add(double.Parse(TLoR));
            this.regionQueue = int.Parse(regionQueue);
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
                if (DB.Insert("Routes",
                    [idRequest.ToString()!, RouteListToString(), MaxPower.ToString(), size.ToString(), timeLead.ToString().Replace(',', '.'), TimeLeadOnRegionsToString(), regionQueue.ToString()],
                    out int? returnID))
                {
                    id = returnID;
                    return true;
                }
                else
                    return false;
            }

            // Если объект уже создан, то его надо просто обновить
            return DB.Replace("Routes", "id", id.ToString()!,
                ["idRequest", "MaxPower", "route", "size", "timeLead", "timeLeadOnRegions", "regionQueue"],
                [idRequest.ToString()!, MaxPower.ToString(), RouteListToString(), size.ToString(), timeLead.ToString().Replace(',', '.'), TimeLeadOnRegionsToString(), regionQueue.ToString()]);
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
            int power = int.MaxValue;
            if (ItRoute.Count > 0)
                foreach (int IdRegion in _route)
                {
                    var result = new Region(IdRegion);
                    if (result.Power < power)
                        power = (int)result.Power;
                }
            return power;
        }

        // Сколько времени понадобится данному маршруту, чтоб выполнить заказ (в минутах).
        // При подсчёте времени считается, что данный маршрут ещё не привязан к участку.
        private double CalculateTheTime()
        {
            double time = 0;
            timeLeadOnRegions = [];
            DateTime? downtimeOnRoute = null;

            foreach (int IdRegion in _route)
            {
                Region region = new(IdRegion);
                int power = (int)region.Power!; // Мощность линии (граммы/час)
                if (power == 0)
                    return double.PositiveInfinity;
                double timeForWorkload = (region.GetSummWorkload - (id == null ? 0 : region.IsRouteExist((int)id!) ? size : 0)) / (power / 60.0); // Время, затрачиваемое на имеющиеся заказы
                double timeForItRegion = size / (power / 60.0); // Время, затрачиваемое на данный регион
                double timeForTransit = (int)region.TransitTime!; // Время прохождения линии (не учитывает время обработки)
                double timeForDowntime = 0; // Расчёт времени по простою
                if (region.IsDowntime)
                {
                    DateTime DowntimeFinish = region.GetDowntimeFinish!.Value;
                    if (downtimeOnRoute == null)
                    {
                        timeForDowntime = DowntimeFinish.Subtract(DateTime.Now).TotalMinutes;
                        downtimeOnRoute = DowntimeFinish;
                    }
                    else if (downtimeOnRoute < DowntimeFinish)
                    {
                        timeForDowntime = DowntimeFinish.Subtract(downtimeOnRoute.Value).TotalMinutes;
                        downtimeOnRoute = DowntimeFinish;
                    }
                }

                // Всё, теперь осталось только суммировать всё
                timeLeadOnRegions.Add(timeForWorkload + timeForItRegion + timeForTransit + timeForDowntime);
                time += timeLeadOnRegions[^1];
            }

            return time;
        }

        // Конвертация времени прохождения по участкам в строку
        private string TimeLeadOnRegionsToString()
        {
            string strTimeLeadOnRegions = "";
            if (timeLeadOnRegions.Count > 0)
                foreach (var TLoR in timeLeadOnRegions)
                    strTimeLeadOnRegions += $"{TLoR};";
            return strTimeLeadOnRegions == "" ? strTimeLeadOnRegions : strTimeLeadOnRegions.Remove(strTimeLeadOnRegions.Length - 1);
        }

        // Конвертация строки маршрута в список
        private List<int> RouteStringToList(string route)
        {
            List<int> list = [];
            if (route != "")
                foreach (var r in route.Split(';'))
                    list.Add(int.Parse(r));
            return list;
        }

        // Конвертация списка маршрута в строку
        private string RouteListToString()
        {
            string str = "";
            if (_route.Count > 0)
            {
                foreach (var i in _route)
                    str += $"{i};";
                str = str.Remove(str.Length - 1);
            }
            return str;
        }

        // Активация/обновление/деактивация маршрута
        public void ActivateRoute()
        {
            if (id == null || regionQueue >= timeLeadOnRegions.Count || timeLead == double.PositiveInfinity)
                throw new Exception("Ошибка! Убедитесь, что маршрут существует, ещё не завершён и что он способен выполнить заявку");
            if (!Save())
                throw new Exception("Не удалось обновить данные в БД");
            for (int i = regionQueue; i < _route.Count; ++i)
            {
                Region r = new(_route[i]);
                r.AddWorkload(this);
            }
            StreamsByRegions.ActivateOnId(_route[regionQueue]);
        }
        public void NextRegion()
        {
            try
            {
                ++regionQueue;
                if (!Save())
                    throw new Exception("Не удалось обновить БД при смене участка");
                if (regionQueue >= _route.Count)
                    DeactivateRoute();
                else
                {
                    GetRequest.UpdateDateOfCompletionOnRegion(regionQueue, DateTime.Now);
                    StreamsByRegions.ActivateOnId(_route[regionQueue]);
                }
            }
            catch (Exception e)
            {
                --regionQueue;
                throw new Exception(e.Message);
            }
        }
        public void DeactivateRoute()
        {
            /*А что тут делать?
             Пока до конца не ясно.*/
            if (!GetRequest.SetFinish())
                throw new Exception("Не удалось обозначить заявку завершённой");
            if (!Save())
                throw new Exception("Не удалось сохранить маршрут после его завершения");
        }

        // Обновить объект
        public void Refresh()
        {
            if (id == null)
                return;
            object[] datas = DB.SelectWhere("Routes", "id", id.ToString()!)![0];
            this.idRequest = int.Parse(datas[1].ToString()!);
            this._route = RouteStringToList(datas[2].ToString()!)!;
            this.MaxPower = int.Parse(datas[3].ToString()!);
            this.size = int.Parse(datas[4].ToString()!);
            this.timeLead = double.Parse(datas[5].ToString()!);
            foreach (var TLoR in datas[6].ToString()!.Split(';'))
                this.timeLeadOnRegions.Add(double.Parse(TLoR));
            this.regionQueue = int.Parse(datas[7].ToString()!);
        }

        // Посмотреть готовность данного маршрута продолжить обработку
        public bool RegionIsReady(int regionId)
        {
            if (regionQueue < _route.Count)
                if (_route[regionQueue] == regionId)
                    return true;
            return false;
        }

        /*// Обновить время прохождения маршрута ---------------
        public void UpdateTimeLead()
        {

        }*/

        // Выбрать лучший маршрут
        public static Route? SelectFastestRoute(List<Route> routes)
        {
            if (routes.Count == 0)
                return null;

            Route route = routes[0];

            foreach (var item in routes)
            {
                if (item.GetTimeLead < route.GetTimeLead)
                    route = item;
            }

            return route;
        }

        // Проверка, что следующий участок доступен (проверка нужна участку, чтоб он менял приоритеты)
        public bool NextRegionIsAvailable(int IdItRegion)
        {
            if (id == null)
                return false;
            for (int i = 0; i < _route.Count; ++i)
                if (_route[i] == IdItRegion)
                {
                    // Если это последний участок, то однозначно можно доделывать участку свою работу
                    if (i == _route.Last())
                        return true;
                    // В ином случае надо удостовериться, что следующий участок - рабочий
                    if (new Region(i + 1).IsDowntime)
                        return false;
                    else
                        return true;
                }
            throw new Exception($"Участок №{IdItRegion} не найден в маршруте №{id}");
        }
        #endregion
    }
}