namespace Smart
{
    public class Zavod : ZavodStructure
    {
        #region Поля
        private string AcceptedRequests = "";
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
                if (DB.Insert("Zavod", [name, AcceptedRequests, ""], out int? returnID))
                {
                    id = returnID;
                    return true;
                }
                else
                    return false;
            }

            // Если объект уже создан, то его надо просто обновить
            return DB.Replace("Zavod", "id", id.ToString()!,
                ["name", "requests", "regions"],
                [name, AcceptedRequests, ""]);
        }

        // Удалить объект из БД
        public override bool Delete()
        {
            // Надо подтверждение существования этого завода
            if (id == null)
                return false;

            // Сначала надо удалить участки, если таковые имеются
            if (!DB.Delete("Region", "idParent", id.ToString()!))
                throw new Exception($"Не удалось удалить участок номер {id}");

            // А теперь можно и сам завод удалить
            return DB.Delete("Zavod", "id", id.ToString()!);
        }

        // Загрузить участки и маршруты
        public List<Region> LoadRegions()
        {
            if (id == null)
                throw new Exception("Произошла ошибка при загрузке участков! Удостоверьтесь, что завод существует!");

            List<Region> ListRegions = [];
            var res = DB.SelectWhere("Region", "idParent", id.ToString()!);
            if (res != null)
                foreach (var r in res)
                {
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
                    ListRegions.Add(newRegion);
                }

            return ListRegions;
        }

        // Проверка, может ли данный завод принять заказ
        public bool ItCanMakeRequest(Request request)
        {
            // Исходные данные
            List<Technology> TechnologyProcess = request.GetNeedTechnologyProcessing;
            List<Region> regions = LoadRegions();

            // Нужен хотя бы один путь
            foreach (var region in regions)
            {
                List<Route> routes = FindRoute(region, [.. TechnologyProcess]);
                if (routes.Count > 0) // Путь есть => заказ выполнить возможно
                    return true;
            }

            // Если пути не были найдены, то сообщаем, что данный завод не может выполнить заказ
            return false;
        }

        // Маршруты по определённой технологии
        public List<Route> GetRoutes(string Product, int size = 0)
        {
            // Находим инструкцию, как делается такой товар, и подчинённые участки
            List<string> StringTechnology = [.. DB.SelectWhere("Products", "name", Product)![0][2].ToString()!.Split(';')];
            List<Region> regions = LoadRegions();
            List<Technology> ListTechnology = [];
            foreach (var st in StringTechnology)
                ListTechnology.Add((Technology)int.Parse(st));

            // Находим маршрут(-ы)
            List<Route> Routes = [];
            foreach (Region reg in regions)
            {
                List<Route>? result = FindRoute(reg, [.. ListTechnology]);
                if (result != null)
                    Routes.AddRange(result);
            }

            // Добавляем размер, если нужно
            if (size > 0)
                foreach (Route route in Routes)
                    route.Size = size;

            // Конец
            return Routes;
        }
        private static List<Route> FindRoute(Region region, List<Technology> technology, string nowPath = "") // Проверяем соответствие участка на технологию для построения маршрута
        {
            List<Route> routes = [];
            // Сначала проверим, не является ли данная технология последней в списке
            nowPath += $"{region.getId};";
            if (technology.Count == 1)
            {
                // Если данная технология - единственная, регион последний, а тип региона и последняя технология совпадают, то маршрут найден
                if (region.Childrens == "" && region.Type == technology[0])
                {
                    routes.Add(new Route(nowPath.Remove(nowPath.Length - 1)));
                    return routes;
                }
                else
                    return routes;
            }

            // Если это не последняя технология, то нам надо будет проверить ещё следующие регионы
            if (region.Type == technology[0])
            {
                technology.RemoveAt(0);
                foreach (Region r in region.GetListChildrenRegions)
                {
                    List<Route> CheckRegion = FindRoute(r, [.. technology], nowPath);
                    if (CheckRegion.Count > 0)
                        routes.AddRange(CheckRegion);
                }
            }

            return routes;
        }

        // Добавить заказ. Если не выбрать маршрут, то он будет выбран автоматически. Автоматическое сохранение
        public void AddRequest(ref Request request, Route? route = null)
        {
            // Проверка на то, что данный завод существует
            if (id == null)
                throw new Exception("Данного завода не существует");

            // Проверка на то, добавлен этот заказ уже, или нет
            string[] AcceptedRequestsParts = AcceptedRequests.Split(';');
            foreach (string RequestPart in AcceptedRequestsParts)
                if (RequestPart == request.GetId.ToString())
                    throw new Exception("Заявка уже принята");

            // Проверка, что данный завод может выполнить заявку
            if (!ItCanMakeRequest(request))
                throw new Exception("Данный завод не может выполнить заявку");

            // Добавление заказа
            if (AcceptedRequests != "")
                AcceptedRequests += ';';
            AcceptedRequests += request.GetId;
            route ??= Route.SelectFastestRoute(GetRoutes(request.GetProduct))!;
            if (!request.SetFactory(this, ref route))
                throw new Exception("Не удалось передать заказ");

            // Сохранение
            if (!Save())
                throw new Exception("Сохранение на каком-то этапе не удалось");
        }
        #endregion
    }
}