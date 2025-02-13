using System.Windows.Threading;

namespace Smart
{
    public class Region : ZavodStructure
    {
        #region Поля
        private int? idParent; // id завода
        private Technology type = Technology.NONE; // Тип участка
        private int? power = 0; // Производительность участка (г/час)
        private int? transitTime; // Время перерыва
        private List<int> workload = []; // Нагрузка (последовательный список маршрутов из их id)
        private string childrens = ""; // Список подчиннных участков, куда направляется изготовленная продукция, в порядке приоритета
        private double? SizeToCompleteFirstRoute; // Сколько необходимо произвести товара для завершения первого заказа
        public DateTime? DowntimeStart { get; set; } // Начало простоя
        public int? DowntimeDuration { get; set; } // Продолжительность простоя
        public string? DowntimeReason { get; set; } // Причина простоя
        #endregion

        #region Переменные для имитации работы участка
        private DispatcherTimer timer = new();
        private double powerInMinute = 0;
        Route? NowRoute;
        public bool IsOn => timer.IsEnabled;
        #endregion

        #region Свойства
        public int? GetIdParent => idParent;
        public Technology Type { get => type; set => type = value; }
        public int? Power
        {
            get => power;
            set
            {
                if (value == null && value < 0)
                    return;
                else if (value == 0)
                {
                    power = 0;
                    return;
                }
                power = value;
                powerInMinute = (double)(power / 60.0)!;
            }
        }
        public int? TransitTime { get => transitTime; set => transitTime = value; }
        public List<int> GetWorkload => workload;
        public double GetSummWorkload
        {
            get
            {
                if (workload == null)
                    return 0;
                double sum = 0;
                foreach (var w in workload)
                    sum += new Route(w).Size;
                return sum;
            }
        }
        public string Childrens { get => childrens; set => childrens = value; }
        public List<Region> GetListChildrenRegions
        {
            get
            {
                List<Region> regions = [];
                if (childrens != "")
                    foreach (string item in childrens.Split(';'))
                    {
                        try // На случай, если такого региона больше не существует в БД
                        {
                            regions.Add(new Region(int.Parse(item)));
                        }
                        catch { }
                    }
                return regions;
            }
        }
        public double? GetSizeToCompleteFirstRoute => SizeToCompleteFirstRoute;
        public DateTime? GetDowntimeFinish
        {
            get
            {
                if (DowntimeStart == null)
                    return null;
                return ((DateTime)DowntimeStart).AddMinutes((double)DowntimeDuration!);
            }
        }
        public bool IsDowntime
        {
            get
            {
                if (DowntimeStart == null)
                    return false;
                DateTime dtFinish = ((DateTime)DowntimeStart).AddMinutes((double)DowntimeDuration!);
                return dtFinish > DateTime.Now;
            }
        }
        public double DowntimeRemaining // Остаток времени простоя в минутах
        {
            get
            {
                if (DowntimeStart == null)
                    return 0;

                DateTime downtimeFinish = GetDowntimeFinish!.Value;
                if (downtimeFinish > DateTime.Now)
                    return downtimeFinish.Subtract(DateTime.Now).TotalMinutes;
                return 0;
            }
        }
        #endregion

        #region Конструкторы
        public Region(int id)
        {
            this.id = id;
            Refresh();
        }
        public Region(int? idParent, string name, Technology type, int? power, int? transitTime, string childrens,
            DateTime? downtimeStart = null, int? downtimeDuration = null, string? downtimeReason = null) : base(name) // Когда ещё только создаётся Участок
        {
            this.name = name;
            this.idParent = idParent;
            this.type = type;
            this.Power = power;
            this.transitTime = transitTime;
            this.childrens = childrens;
            this.DowntimeStart = downtimeStart;
            this.DowntimeDuration = downtimeDuration;
            this.DowntimeReason = downtimeReason;
            UpdateDowntime();
        }
        public Region(int id, int? idParent, string name, Technology type, int? power, int? transitTime, List<int>? workload, string childrens,
            double? SizeToCompleteFirstRoute, DateTime? downtimeStart, int? downtimeDuration, string? downtimeReason) : base(id, name) // Когда всё известно и надо загрузить
        {
            this.idParent = idParent;
            this.type = type;
            this.Power = power;
            this.transitTime = transitTime;
            this.workload = workload ?? [];
            this.childrens = childrens;
            this.SizeToCompleteFirstRoute = SizeToCompleteFirstRoute;
            this.DowntimeStart = downtimeStart;
            this.DowntimeDuration = downtimeDuration;
            this.DowntimeReason = downtimeReason;
            UpdateDowntime();
        }
        public Region(string id, string idParent, string name, string? type, string? power, string? transitTime, string? workload, string childrens, string? SizeToCompleteFirstRoute,
            string? downtimeStart, string? downtimeDuration, string? downtimeReason) // Когда всё известно и надо загрузить
        {
            this.id = int.Parse(id);
            this.idParent = int.Parse(idParent);
            this.type = int.TryParse(type, out int rType) ? (Technology)rType : Technology.NONE;
            this.name = name;
            this.Power = power == null ? null : int.Parse(power);
            this.transitTime = transitTime == null ? null : int.Parse(transitTime);
            if (workload != null && workload != "")
                foreach (var w in workload.Split(';'))
                    this.workload.Add(int.Parse(w));
            this.childrens = childrens;
            if (double.TryParse(SizeToCompleteFirstRoute, out double _SizeToCompleteFirstRoute))
                this.SizeToCompleteFirstRoute = _SizeToCompleteFirstRoute;
            if (DateTime.TryParse(downtimeStart, out DateTime _downtimeStart))
                this.DowntimeStart = _downtimeStart;
            if (int.TryParse(downtimeDuration, out int _downtimeDuration))
                this.DowntimeDuration = _downtimeDuration;
            this.DowntimeReason = downtimeReason;
            UpdateDowntime();
        }
        #endregion

        #region Методы
        // Сохранить изменения в БД
        public override bool Save()
        {
            // Проверка
            UpdateDowntime();

            // Если объект ещё не создан, то его надо добавить
            string[] arguments = [idParent.ToString()!, name, ((int)type).ToString(), power.ToString()!, transitTime.ToString()!, WorkloadToString(), childrens, SizeToCompleteFirstRoute.ToString()!,
                DowntimeStart == null ? "" : DowntimeStart.ToString()!, DowntimeDuration.ToString()!, DowntimeReason!];
            if (id == null)
            {
                if (DB.Insert("Region", arguments, out int? returnID))
                {
                    id = returnID;
                    return true;
                }
                else
                    return false;
            }

            // Если объект уже создан, то его надо просто обновить
            return DB.Replace("Region", "id", id.ToString()!,
                ["idParent", "name", "type", "power", "transitTime", "workload", "childrens", "SizeToCompleteFirstRoute", "downtimeStart", "downtimeDuration", "downtimeReason"],
                arguments);
        }

        // Удалить объект из БД
        public override bool Delete()
        {
            // Надо подтверждение существования этого участка
            if (id == null)
                return false;

            // А теперь можно и сам участок удалить
            return DB.Delete("Region", "id", id.ToString()!);
        }

        // Обновить объект
        public void Refresh()
        {
            if (id == null)
                return;
            object[] datas = DB.SelectWhere("Region", "id", id.ToString()!)![0];
            this.idParent = int.Parse(datas[1].ToString()!);
            this.name = datas[2].ToString()!;
            this.type = (Technology)int.Parse(datas[3].ToString()!);
            this.power = int.Parse(datas[4].ToString()!);
            this.transitTime = int.Parse(datas[5].ToString()!);
            this.workload = StringToWorkload(datas[6].ToString()!);
            this.childrens = datas[7].ToString()!;
            if (double.TryParse(datas[8].ToString()!, out double SizeToCompleteFirstRoute))
                this.SizeToCompleteFirstRoute = SizeToCompleteFirstRoute;
            if (DateTime.TryParse(datas[9].ToString(), out DateTime _downtimeStart))
                this.DowntimeStart = _downtimeStart;
            if (int.TryParse(datas[10].ToString(), out int _downtimeDuration))
                this.DowntimeDuration = _downtimeDuration;
            this.DowntimeReason = datas[11].ToString();
            UpdateDowntime();
        }

        // Конвертацию нагрузки в строку и наоборот
        private string WorkloadToString()
        {
            string str = "";
            foreach (var item in workload)
                str += $"{item};";
            return str == "" ? str : str.Remove(str.Length - 1);
        }
        private static List<int> StringToWorkload(string str = "")
        {
            List<int> list = [];
            if (str != "")
                foreach (var item in str.Split(';'))
                    list.Add(int.Parse(item));
            return list;
        }

        // Добавить очередь
        public void AddWorkload(Route route, bool start = false)
        {
            // Проверка на то, что маршрут можно запустить и на то, что маршрута ещё нет здесь
            Refresh();
            if (route.GetId == null)
                throw new Exception("Номер маршрута неизвестен");

            // Если маршрута ещё нет в списке, то его надо добавить.
            if (!IsRouteExist((int)route.GetId))
                workload.Add((int)route.GetId);

            // Если значение ещё не задано, то, по идее, и нагрузки нет никакой всё ещё.
            if (SizeToCompleteFirstRoute == null || SizeToCompleteFirstRoute == 0)
            {
                // Для ускорения надо сделать проверку
                if (route.GetId == workload[0])
                    SizeToCompleteFirstRoute = route.Size;
                else
                    SizeToCompleteFirstRoute = new Route(workload[0]).Size;
            }

            // Ну и обязательно сохраняем изменения.
            if (!Save())
                throw new Exception("Не удалось нагрузить участок");

            // Если надо запустить участок сразу, то запускаем.
            if (start && !timer.IsEnabled)
                StreamsByRegions.AddRegion(this);
        }

        // Активировать/обновить/выключить участок
        public void ActivateRegion()
        {
            // Проверка на то, что регион можно запустить
            Refresh();
            UpdateQueue();
            if (workload.Count > 0)
            {
                NowRoute = new(workload[0]);
                if (SizeToCompleteFirstRoute == null || SizeToCompleteFirstRoute == 0)
                    SizeToCompleteFirstRoute = NowRoute.Size;
            }
            else
                return;

            // Проверка на то, что регион ещё не запущен, а запустить его возможно
            // Таймер не запущен, необходимо произвести больше 0 грамм товара, простоя нет на данный момент, маршрут готов продолжать работу по этому участку, следующий регион маршрута доступен
            if (!timer.IsEnabled && SizeToCompleteFirstRoute > 0 && !IsDowntime && NowRoute.RegionIsReady((int)id!) && NowRoute.NextRegionIsAvailable(id.Value))
            {
                timer.Tick += UpdateWorkload;
                timer.Interval = TimeSpan.FromSeconds(60);
                timer.Start();
            }
        }
        private void UpdateWorkload(object? sender, EventArgs e)
        {
            // Проверка: если нет нагрузки или сейчас простой, то нечего работать участку
            Refresh();
            bool IsDowntimeLocal = IsDowntime;
            if (workload.Count == 0 || NowRoute == null || IsDowntimeLocal)
            {
                if (IsDowntimeLocal)
                    StreamsByRegions.RunAfter(id!.Value, DowntimeRemaining);
                DeactivateRegion();
                return;
            }

            // Обрабатываем изменения
            double copyPowerInMinute = powerInMinute;
            while (copyPowerInMinute > 0 && SizeToCompleteFirstRoute > 0)
            {
                double CopySTCFR = SizeToCompleteFirstRoute.Value;
                SizeToCompleteFirstRoute -= copyPowerInMinute;
                copyPowerInMinute -= CopySTCFR;
                if (SizeToCompleteFirstRoute <= 0)
                {
                    workload.RemoveAt(0);
                    NowRoute.NextRegion();

                    if (workload.Count > 0)
                    {
                        UpdateQueue();
                        NowRoute = new Route(workload[0]);
                        SizeToCompleteFirstRoute = NowRoute.Size;
                    }
                    else
                        break;
                }
            }

            // Очередная проверка
            if (workload.Count == 0 || !NowRoute.RegionIsReady((int)id!) || !NowRoute.NextRegionIsAvailable(id.Value))
            {
                NowRoute = null;
                SizeToCompleteFirstRoute = null;
                DeactivateRegion();
            }

            // Сохраняем изменения
            _ = Save();
        }
        public void DeactivateRegion()
        {
            timer.Stop();
            timer.IsEnabled = false; // На всякий случай ещё и так
        }

        // Проверка, есть ли в очереди маршрут с указанным номером
        public bool IsRouteExist(int IdRoute)
        {
            if (workload.Count > 0)
                foreach (var ItRoute in workload)
                    if (ItRoute == IdRoute)
                        return true;
            return false;
        }

        // Обновить состояние простоя
        private void UpdateDowntime()
        {
            // Надо ли вообще обновлять
            if (DowntimeStart == null)
                return;

            // Удостоверяемся, что надо обновить состояние
            if (DateTime.Now >= ((DateTime)DowntimeStart).AddMinutes((int)DowntimeDuration!))
            {
                DowntimeToNull();
                _ = Save();
            }
        }
        private void DowntimeToNull()
        {
            DowntimeStart = null;
            DowntimeDuration = null;
            DowntimeReason = null;
        }

        // Обновить очередь маршрутов
        private void UpdateQueue()
        {
            // Обязательные условия: существование участка и наличие очереди
            if (id == null)
                throw new Exception("Необходимо сохранить участок в БД");
            if (workload.Count == 0)
                return;

            // Сначала проверим необходимость изменения приоритоета вообще
            Route r = new(workload[0]);
            if (r.NextRegionIsAvailable((int)id))
                return;

            // Проверяем весь оставшийся список.
            // Цель: поставить на первое место маршрут, который способен выполнять работу
            for (int i = 1; i < workload.Count; ++i)
            {
                r = new(workload[i]);
                if (r.NextRegionIsAvailable((int)id))
                {
                    int Copy = workload[i];
                    workload.RemoveAt(i);
                    workload.Insert(0, Copy);
                    return;
                }
            }
        }
        #endregion
    }
}