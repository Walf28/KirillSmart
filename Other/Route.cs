using System.Windows.Threading;

namespace Smart
{
    public class Route
    {
        #region Поля
        private int? id; // Номер маршрута на всякий случай
        private int? idRequest; // Номер заказа, исполняемого по данному маршруту
        private List<string> _route = []; // Маршрут (последовательные id через точку с запятой)
        private int MaxPower = 0; // Мощность маршрута
        private int size = 0; // Сколько товара надо произвести
        private double timeLead = double.PositiveInfinity; // Суммарное время прохождения маршрута (в минутах)
        private List<double> timeLeadOnRegions = []; // Время прохождения по участкам (в минутах)
        private int regionQueue = 0; // номер региона по счёту, который должен обрабатываться на данный момент.

        private Region? SelectedRegion; // Регион, которым сейчас управляет маршрут
        #endregion

        #region Свойства
        public int? GetId => id;
        public int? IdRequest { get => idRequest; set => idRequest = value; }
        public List<string> ItRoute
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
        public Request GetRequest
        {
            get
            {
                var r = DB.SelectWhere("Requests", "id", idRequest.ToString()!)![0];
                Request request = new(
                    r[0].ToString()!,
                    r[1].ToString()!,
                    r[2].ToString()!,
                    r[3].ToString()!,
                    r[4].ToString(),
                    r[5].ToString(),
                    r[6].ToString(),
                    r[7].ToString(),
                    r[8].ToString(),
                    r[9].ToString());
                return request;
            }
        }
        #endregion

        #region Поля для "оживления" класса
        private DispatcherTimer timer = new();
        #endregion

        #region Конструкторы
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
                    [idRequest.ToString()!, RouteListToString(), MaxPower.ToString(), size.ToString(), timeLead.ToString(), TimeLeadOnRegionsToString()],
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
                ["idRequest", "MaxPower", "ItRoute", "size", "timeLead", "timeLeadOnRegions"],
                [idRequest.ToString()!, MaxPower.ToString(), RouteListToString(), size.ToString(), timeLead.ToString(), TimeLeadOnRegionsToString()]);
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
            if (ItRoute.Count > 0)
                foreach (string IdRegion in _route)
                {
                    var result = DB.SelectWhere("Region", "id", IdRegion)![0];
                    if (int.TryParse(result[4].ToString(), out int ResPower))
                        power += ResPower;
                }
            return power;
        }

        // Сколько времени понадобится данному маршруту, чтоб выполнить заказ (в минутах).
        // При подсчёте времени считается, что маршрут ещё не запущен.
        private double CalculateTheTime()
        {
            double time = 0;
            timeLeadOnRegions = [];

            foreach (string IdRegion in _route)
            {
                var region = SelectRegionById(IdRegion);
                int power = (int)region.Power!; // Мощность линии (граммы/час)
                if (power == 0)
                    return double.PositiveInfinity;
                double timeForWorkload = region.GetSummWorkload / (power / 60.0); // Время, затрачиваемое на имеющиеся заказы
                double timeForItRegion = size / (power / 60.0); // Время, затрачиваемое на данный регион
                double timeForTransit = (int)region.TransitTime!; // Время прохождения линии (не учитывает время обработки)
                timeLeadOnRegions.Add(timeForWorkload + timeForItRegion + timeForTransit);
                time += timeLeadOnRegions[^1];
            }

            return time;
        }

        // Конвертация времени прохождения по участкам в строку
        private string TimeLeadOnRegionsToString()
        {
            string strTimeLeadOnRegions = "";
            foreach (var TLoR in timeLeadOnRegions)
                strTimeLeadOnRegions += $"{TLoR};";
            return strTimeLeadOnRegions == "" ? strTimeLeadOnRegions : strTimeLeadOnRegions.Remove(strTimeLeadOnRegions.Length - 1);
        }

        // Конвертация строки маршрута в список
        private List<string> RouteStringToList(string route)
        {
            List<string> list = [];
            if (route != "")
                foreach(var r in route.Split(';'))
                    list.Add(r);
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
            if (regionQueue >= timeLeadOnRegions.Count || timeLead == double.PositiveInfinity)
                throw new Exception("Ошибка! Убедитесь, что маршрут ещё не завершён или что он способен выполнить заявку");
            timer.Interval = TimeSpan.FromMinutes(timeLeadOnRegions[regionQueue]);
            timer.Tick += UpdateRoute;
            timer.Start();
        }
        private void UpdateRoute(object? sender, EventArgs e)
        {
            // Тут как-бы надо активировать участок
            timeLead = CalculateTheTime();
            SelectedRegion = SelectRegionById(_route[regionQueue]);
            SelectedRegion.AddWorkload(this);

            // Обновить маршрут или закончить его
            if (regionQueue < timeLeadOnRegions.Count)
            {
                timer.Interval = TimeSpan.FromMinutes(timeLeadOnRegions[regionQueue]);
                ++regionQueue;
            }
            else
                DeactivateRoute();

            // Сохраняем данные
            Save();
        }
        public void DeactivateRoute()
        {
            if (regionQueue >= _route.Count)
            {

            }
            SelectedRegion = null;
            timer.Stop();
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
            this.Size = int.Parse(datas[4].ToString()!);
            this.timeLead = int.Parse(datas[5].ToString()!);
            foreach (var TLoR in datas[6].ToString()!.Split(';'))
                this.timeLeadOnRegions.Add(double.Parse(TLoR));
            this.regionQueue = int.Parse(datas[7].ToString()!);
        }

        // Выбрать регион по его Id
        private Region SelectRegionById(string regionId)
        {
            var r = DB.SelectWhere("Region", "id", regionId)![0];
            Region newRegion = new(
                        r[0].ToString()!, // id
                        r[1].ToString()!, // idParent
                        r[2].ToString()!, // name
                        r[3].ToString()!, // StringTechnology
                        r[4].ToString(), // MaxPower
                        r[5].ToString(), // transitTime
                        r[6].ToString(), // workload
                        r[7].ToString()! // childrens
                        );
            return newRegion;
        }

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
        #endregion
    }
}