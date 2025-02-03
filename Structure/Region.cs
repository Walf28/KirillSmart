using System.ComponentModel;

namespace Smart
{
    public class Region : ZavodStructure
    {
        #region Поля
        private int? idParent; // id завода
        private Technology type = Technology.NONE; // Тип участка
        private int? power; // Производительность участка
        private int? downtime; // Время перерыва
        private string childrens = ""; // Список подчиннных участков, куда направляется изготовленная продукция, в порядке приоритета
        #endregion

        #region Свойства
        public int? getIdParent => idParent;
        public Technology Type { get => type; set => type = value; }
        public int? Power { get => power; set => power = value; }
        public int? DownTime { get => downtime; set => downtime = value; }
        public string Childrens { get => childrens; set => childrens = value; }
        public List<Region> getListRegions
        {
            get
            {
                List<Region> regions = new List<Region>();
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
                                res[6].ToString()!));
                        }
                        catch { }
                    }
                return regions;
            }
        }
        #endregion

        #region Конструкторы
        public Region(int? idParent, string name, Technology type, int? power, int? downtime, string childrens) : base(name) // Когда ещё только создаётся Участок
        {
            this.name = name;
            this.idParent = idParent;
            this.type = type;
            this.power = power;
            this.downtime = downtime;
            this.childrens = childrens;
        }
        public Region(int id, int? idParent, string name, Technology type, int? power, int? downtime, string childrens) : base(id, name) // Когда всё известно и надо загрузить
        {
            this.idParent = idParent;
            this.type = type;
            this.power = power;
            this.downtime = downtime;
            this.childrens = childrens;
        }
        public Region(string id, string idParent, string name, string? type, string? power, string? downtime, string childrens) // Когда всё известно и надо загрузить
        {
            this.id = int.Parse(id);
            this.idParent = int.Parse(idParent);
            this.type = int.TryParse(type, out int rType) ? (Technology)rType : Technology.NONE;
            this.name = name;
            this.power = power == null ? null : int.Parse(power);
            this.downtime = downtime == null ? null : int.Parse(downtime);
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
                if (DB.Insert("Region", new string[] { idParent.ToString()!, name, ((int)type).ToString(), power.ToString()!, downtime.ToString()!, childrens }, out int? returnID))
                {
                    id = returnID;
                    return true;
                }
                else
                    return false;
            }

            // Если объект уже создан, то его надо просто обновить
            return DB.Replace("Region", "id", id.ToString()!,
                new string[] { "name", "type", "idParent", "power", "downtime", "childrens" },
                new string[] { name, ((int)type).ToString(), idParent.ToString()!, power.ToString()!, downtime.ToString()!, childrens });
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
        #endregion
    }
}