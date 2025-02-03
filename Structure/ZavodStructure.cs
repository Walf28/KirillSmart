namespace Smart
{
    public abstract class ZavodStructure
    {
        #region поля
        protected int? id; // Номер объекта в БД
        protected string name = ""; // Название объекта
        #endregion

        #region Поля
        public int? getId => id;
        public string Name { get => name; set => name = value; }
        #endregion

        #region Конструкторы
        public ZavodStructure() { }
        public ZavodStructure(int id, string name)
        {
            this.id = id;
            this.name = name;
        }
        public ZavodStructure(string name)
        {
            this.name = name;
        }
        #endregion

        #region Методы
        abstract public bool Save();
        abstract public bool Delete();
        #endregion
    }
}