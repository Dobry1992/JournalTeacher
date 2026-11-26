using System.Collections.Generic;

namespace Portal.Helpers
{
    // Значения для быстрого выставления отметки из ячейки журнала.
    // Полностью совпадают со списком выпадающего списка в формах
    // Views/Marks/Edit.cshtml и Views/Marks/AdjustmentEdit.cshtml
    public static class MarkValueOptions
    {
        public static readonly IReadOnlyList<object> All = new List<object>
        {
            new { value = " ", label = "Нет отметки" },
            new { value = "1", label = "1" },
            new { value = "2", label = "2" },
            new { value = "3", label = "3" },
            new { value = "4", label = "4" },
            new { value = "5", label = "5" },
            new { value = "6", label = "6" },
            new { value = "7", label = "7" },
            new { value = "8", label = "8" },
            new { value = "9", label = "9" },
            new { value = "10", label = "10" },
            new { value = "Б", label = "Б" },
            new { value = "НР", label = "НР" },
            new { value = "О", label = "О" },
            new { value = "КМ", label = "КМ" },
            new { value = "Р", label = "Р" },
            new { value = "НБ", label = "НБ" },
            new { value = "Н/Я", label = "Н/Я" },
            new { value = "НД/К", label = "НД/К" },
            new { value = "ОСВ", label = "ОСВ" },
            new { value = "З", label = "З" },
            new { value = "НЗ", label = "НЗ" }
        };
    }
}
