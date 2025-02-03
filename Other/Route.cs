namespace Smart
{
    public class Route
    {
        #region Поля
        private int? id; // Номер маршрута на всякий случай
        //private int idFactory; // Номер завода, который выполняет данный маршрут
        private int idRequest; // Номер заказа, исполняемого по данному маршруту
        private string _Route; // Маршрут (последовательные id через точку с запятой)
        private int power; // Мощность маршрута
        #endregion

        #region Свойства
        public int? getId;
        //public int IdFactory { get => idFactory; set => idFactory = value; }
        public int IdRequest { get=>idRequest; set => idRequest = value; }
        public string route { get=>_Route; set => _Route = value; }
        public int getPower => power;
        #endregion

        #region Конструкторы
        public Route(/*int IdFactory,*/ int IdRequest, string Route)
        {
            //this.idFactory = IdFactory;
            this.idRequest = IdRequest;
            this._Route = Route;
        }
        #endregion

        #region Методы
        // Сохранить объект или его изменения в БД
        public bool Save()
        {
            // Если объект ещё не создан, то его надо добавить
            if (id == null)
            {
                if (DB.Insert("Routes", new string[] { idRequest.ToString(), _Route, power.ToString() }, out int? returnID))
                {
                    id = returnID;
                    return true;
                }
                else
                    return false;
            }

            // Если объект уже создан, то его надо просто обновить
            return DB.Replace("Routes", "id", id.ToString()!,
                new string[] { "idRequest", "power", "route" },
                new string[] { idRequest.ToString(), power.ToString(), _Route });
        }

        // Удалить объект из БД
        public bool Delete()
        {
            // Надо подтверждение существования этого завода
            if (id == null)
                return false;

            // А теперь можно и сам завод удалить
            return DB.Delete("Routes", "id", id.ToString()!);
        }
        #endregion
    }
}