using Portal.Models;
using System;
using System.Collections.Generic;

namespace Portal.Helpers
{
    // Правила редактирования отметок преподавателем в обычном журнале.
    // Отметки контрольных мероприятий выставляются один раз, после чего допускаются
    // только корректировки в день занятия и только ограниченное число раз.
    // Корректировка журнала (adjustment) этими правилами не ограничивается.
    public static class MarkEditingRules
    {
        public static readonly IReadOnlyList<string> ControlTypeNames = new[]
        {
            "Экзамен",
            "Зачёт",
            "Дифференцированный зачёт"
        };

        public const int MaxCorrections = 3;

        public static bool IsControlMark(Mark mark, ICollection<int> controlTypeIDs)
        {
            return mark != null && controlTypeIDs != null && controlTypeIDs.Contains(mark.TypeOfExerciseID);
        }

        public static bool IsIssued(Mark mark)
        {
            return mark != null && !string.IsNullOrWhiteSpace(mark.Value);
        }

        // Проверка выполняется по отметке, сохранённой в базе: значения из формы,
        // включая число изменений, преподаватель может передать любые
        public static bool CanEditMark(Mark mark, ICollection<int> controlTypeIDs, DateTime today,
            string controlTypeName, out string error)
        {
            error = null;

            if (!IsControlMark(mark, controlTypeIDs))
            {
                return true;
            }

            // пустая отметка ещё не выставлена, поэтому корректировок не было
            if (!IsIssued(mark))
            {
                return true;
            }

            string typeName = string.IsNullOrWhiteSpace(controlTypeName) ? "контрольного мероприятия" : controlTypeName;

            if (today.Date != mark.Date.Date)
            {
                error = $"Корректировать отметку «{typeName}» можно только в день занятия ({mark.Date:dd.MM.yyyy}). " +
                        "Для изменения отметки воспользуйтесь корректировкой журнала.";
                return false;
            }

            // первое сохранение — выставление отметки, последующие — корректировки
            int corrections = Math.Max(mark.ChangeCounter - 1, 0);

            if (corrections >= MaxCorrections)
            {
                error = $"Отметку «{typeName}» можно корректировать не более {MaxCorrections} раз. " +
                        "Возможность корректировки закрыта, воспользуйтесь корректировкой журнала.";
                return false;
            }

            return true;
        }
    }
}
