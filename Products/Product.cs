namespace Smart
{
    public class Product
    {
        #region Поля
        private int? id;
        private string name;
        private string processing_technology = "";
        #endregion

        #region Свойства
        public int? getId => id;
        public string Name { get => name; set => name = value; }
        public string TechnologyProcessing { get => processing_technology; set => processing_technology = value; }
        #endregion


        #region Конструкторы
        public Product(string name, string processing_technology)
        {
            this.name = name;
            this.processing_technology = processing_technology;
        }
        public Product(string id, string name, string processing_technology) // Для БД
        {
            this.id = int.Parse(id);
            this.name = name;
            this.processing_technology = processing_technology;
        }
        #endregion

        #region Методы
        public bool Save()

        {
            // Если объект ещё не создан, то его надо добавить
            if (id == null)
            {
                if (DB.Insert("Products", new string[] { name, TechnologyProcessing }, out int? returnID))
                {
                    id = returnID;
                    return true;
                }
                else
                    return false;
            }

            // Если объект уже создан, то его надо просто обновить
            return DB.Replace("Products", "id", id.ToString()!,
                new string[] { "name", "TechnologyProcessing" },
                new string[] { name, processing_technology });
        }

        public bool Delete()
        {
            // Надо подтверждение существования этого продукта
            if (id == null)
                return false;

            // А теперь можно и сам завод удалить
            return DB.Delete("Products", "id", id.ToString()!);
        }
        #endregion
    }
}