using System.ComponentModel;

namespace Smart
{
    public enum Technology
    {
        [Description("Отсутствует")] NONE = 0,
        [Description("Отгрузочный терминал")] SHIPPING_TERMINAL = 1,
        [Description("Терминал сырья")] RAW_TERMINAL = 2,
        [Description("Сортировка")] SORTING = 3,
        [Description("Мойка")] WASHING = 4,
        [Description("Засолка")] SALTING = 5,
        [Description("Обжарка")] ROASTING = 6,
        [Description("Упаковка")] PACKAGING = 7,
        [Description("Бункер")] SILO = 8,
        //[Description("Линия")] LINE = 9,
    }
}
