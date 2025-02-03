namespace Smart
{
    public class Zavod : ZavodStructure
    {
        #region Поля
        /*private string routes = ""; // Список линий этого завода - ИСПРАВИТЬ!!!
        private string regions = ""; // Список участков этого завода*/
        #endregion

        #region Свойства
        /*public string getRoutes => routes;
        public string Regions => regions;*/
        #endregion

        #region Конструкторы
        // Самый новенький
        public Zavod(string name) : base(name) { }
        // Самый не пустой
        public Zavod(int id, string name, string routes, string regions) : base(id, name)
        {
            /*this.routes = routes;
            this.regions = regions;*/
        }
        #endregion

        #region Методы
        // Сохранить объект или его изменения в БД
        public override bool Save()
        {
            // Если объект ещё не создан, то его надо добавить
            if (id == null)
            {
                if (DB.Insert("Zavod", new string[] { name, "", "" }, out int? returnID))
                {
                    id = returnID;
                    return true;
                }
                else
                    return false;
            }

            // Если объект уже создан, то его надо просто обновить
            return DB.Replace("Zavod", "id", id.ToString()!,
                new string[] { "name", "routes", "regions" },
                new string[] { name, "", "" });
        }

        // Удалить объект из БД
        public override bool Delete()
        {
            // Надо подтверждение существования этого завода
            if (id == null)
                return false;

            // Сначала надо удалить участки, если таковые имеются
            if (!DB.Delete("Region", "idParent", id.ToString()!))
                throw new Exception($"Не удалось удалить участок номер {id.ToString()}");

            // А теперь можно и сам завод удалить
            return DB.Delete("Zavod", "id", id.ToString()!);
        }

        // Загрузить участки и маршруты
        public List<Region> LoadRegions()
        {
            if (id == null)
                throw new Exception("Произошла ошибка при загрузке участков! Удостоверьтесь, что завод существует!");

            List<Region> ListRegions = new List<Region>();
            var res = DB.SelectWhere("Region", "idParent", id.ToString()!);
            if (res != null)
                foreach (var r in res)
                {
                    Region newRoute = new Region(
                        r[0].ToString()!, // id
                        r[1].ToString()!, // idParent
                        r[2].ToString()!, // name
                        r[3].ToString()!, // technology
                        r[4].ToString(), // power
                        r[5].ToString(), // downtime
                        r[6].ToString()! // childrens
                        );
                    ListRegions.Add(newRoute);
                }

            return ListRegions;
        }

        // Добавить подчинённый участок
        /*public bool AddRegion(Region region)
        {
            if (id == null || region.getId == null)
                return false;
            this.regions += this.regions == "" ? region.getId : $";{region}";
            if (!DB.Replace("Zavod", "id", id.ToString()!, "regions", regions))
                return false;
            return true;
        }*/

        // Маршруты для выбранного товара
        public List<Route> GetRoutes(int IdRequest, string Product)
        {
            // Находим инструкцию, как делается такой товар, и подчинённые участки
            List<string> technology = DB.SelectWhere("Products", "name", Product)![0][2].ToString()!.Split(';').ToList<string>();
            List<Region> regions = LoadRegions();

            // Находим маршрут(-ы)
            List<string> RoutesInString = new List<string>();
            foreach (Region reg in regions)
            {
                List<string>? result = FindRoute(reg, technology.ToList());
                if (result != null)
                    RoutesInString.AddRange(result);
            }

            // Создаём список этих маршрутов
            List<Route> routes = new List<Route>();
            if (RoutesInString.Count > 0)
                foreach (string route in RoutesInString)
                    routes.Add(new Route(IdRequest, route));

            // Конец
            return routes;
        }
        private List<string>? FindRoute(Region region, List<string> technology, string nowPath = "") // Проверяем соответствие участка на технологию для построения маршрута
        {
            List<string> routes = new List<string>();
            // Сначала проверим, не является ли данная технология последней в списке
            nowPath += $"{region.getId};";
            if (technology.Count == 1)
            {
                // Если данная технология - единственная, регион последний, а тип региона и последняя технология совпадают, то маршрут найден
                if (region.Childrens == "" && region.Type == (Technology)int.Parse(technology[0]))
                {
                    routes.Add(nowPath.Remove(nowPath.Length - 1));
                    return routes;
                }
                else
                    return null;
            }

            // Если это не последняя технология, то нам надо будет проверить ещё следующие регионы
            if (region.Type == (Technology)int.Parse(technology[0]))
            {
                technology.RemoveAt(0);
                foreach (Region r in region.getListRegions)
                {
                    List<string>? CheckRegion = FindRoute(r, technology, nowPath);
                    if (CheckRegion != null && CheckRegion.Count > 0)
                        routes.Add(CheckRegion[^1]);
                }
            }

            return routes;
        }
        #endregion
    }
}