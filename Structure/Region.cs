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
        private List<double> workload = []; // Нагрузка
        private string childrens = ""; // Список подчиннных участков, куда направляется изготовленная продукция, в порядке приоритета
        #endregion

        #region Поля не для просмотра
        private DispatcherTimer timer = new();
        private double powerInMinute = 0;
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
        public List<double> GetWorkload => workload;
        public double GetSummWorkload
        {
            get
            {
                if (workload == null)
                    return 0;
                double sum = 0;
                foreach (var w in workload)
                    sum += w;
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
                        var res = DB.SelectWhere("Region", "id", item)![0];
                        try // На случай, если такого региона больше не существует в БД
                        {
                            regions.Add(new Region(
                                res![0].ToString()!,
                                res[1].ToString()!,
                                res[2].ToString()!,
                                res[3].ToString(),
                                res[4].ToString(),
                                res[5].ToString(),
                                res[6].ToString(),
                                res[7].ToString()!));
                        }
                        catch { }
                    }
                return regions;
            }
        }
        #endregion

        #region Конструкторы
        public Region(int? idParent, string name, Technology type, int? power, int? transitTime, string childrens) : base(name) // Когда ещё только создаётся Участок
        {
            this.name = name;
            this.idParent = idParent;
            this.type = type;
            this.Power = power;
            this.transitTime = transitTime;
            this.workload = workload ?? [];
            this.childrens = childrens;
        }
        public Region(int id, int? idParent, string name, Technology type, int? power, int? transitTime, List<double>? workload, string childrens) : base(id, name) // Когда всё известно и надо загрузить
        {
            this.idParent = idParent;
            this.type = type;
            this.Power = power;
            this.transitTime = transitTime;
            this.workload = workload ?? [];
            this.childrens = childrens;
        }
        public Region(string id, string idParent, string name, string? type, string? power, string? transitTime, string? workload, string childrens) // Когда всё известно и надо загрузить
        {
            this.id = int.Parse(id);
            this.idParent = int.Parse(idParent);
            this.type = int.TryParse(type, out int rType) ? (Technology)rType : Technology.NONE;
            this.name = name;
            this.Power = power == null ? null : int.Parse(power);
            this.transitTime = transitTime == null ? null : int.Parse(transitTime);
            if (workload != null && workload != "")
                foreach (var w in workload.Split(';'))
                    this.workload.Add(double.Parse(w));
            this.childrens = childrens;
        }
        #endregion

        #region Методы
        // Сохранить изменения в БД
        public override bool Save()
        {
            // Если объект ещё не создан, то его надо добавить
            if (id == null)
            {
                if (DB.Insert("Region",
                    [idParent.ToString()!, name, ((int)type).ToString(), power.ToString()!, transitTime.ToString()!, workload.ToString()!, childrens],
                    out int? returnID))
                {
                    id = returnID;
                    return true;
                }
                else
                    return false;
            }

            // Если объект уже создан, то его надо просто обновить
            return DB.Replace("Region", "id", id.ToString()!,
                ["name", "type", "idParent", "power", "transitTime", "workload", "childrens"],
                [name, ((int)type).ToString(), idParent.ToString()!, power.ToString()!, transitTime.ToString()!, WorkloadToString(), childrens]);
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
        }

        // Конвертацию нагрузки в строку и наоборот
        private string WorkloadToString()
        {
            string str = "";
            foreach (var item in workload)
                str += $"{item};";
            return str == "" ? str : str.Remove(str.Length - 1);
        }
        private static List<double> StringToWorkload(string str = "")
        {
            List<double> list = new List<double>();
            foreach (var item in str.Split(';'))
                list.Add(double.Parse(item));
            return list;
        }

        // Добавить очередь
        public void AddWorkload(Route route)
        {
            workload.Add(route.Size);
            if (!timer.IsEnabled)
                ActivateRegion();
        }

        // Активировать/обновить/выключить участок
        public void ActivateRegion()
        {
            timer.Tick += UpdateWorkload;
            timer.Interval = TimeSpan.FromSeconds(60);
            timer.Start();
        }
        private void UpdateWorkload(object? sender, EventArgs e)
        {
            // Проверка: если нет нагрузки, то нечего работать участку
            if (workload.Count == 0)
            {
                DeactivateRegion();
                return;
            }

            // Обрабатываем изменения
            double copyPowerInMinute = powerInMinute;
            while (copyPowerInMinute > 0 && workload.Count > 0)
            {
                double copyWorkload = workload[0];
                workload[0] -= copyPowerInMinute;
                if (workload[0] <= 0)
                    workload.Remove(0);
                copyPowerInMinute -= copyWorkload;
            }

            // Очередная проверка
            if (workload.Count == 0)
                DeactivateRegion();

            // Сохраняем изменения
            Save();
        }
        public void DeactivateRegion()
        {
            timer.Stop();
        }
        #endregion
    }
}