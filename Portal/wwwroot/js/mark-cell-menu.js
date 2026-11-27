// Контекстное меню выставления отметки в ячейках журнала (Journals/Journal, Journals/AdjustedJournal).
// Меню самодостаточное: выбор отметки, замечание преподавателя и кнопки "Выставить"/"Закрыть".
// Сохранение выполняется POST-ом на те же действия MarksController.Edit / MarksController.AdjustmentEdit
// с тем же набором полей, что и формы Edit / AdjustmentEdit, поэтому вся серверная логика
// (валидация, история, счётчик корректировок, пересчёт итоговых) идентична. Никаких переходов на Edit.
$(function () {
    var $cells = $('.mark-menu-cell');
    if (!$cells.length) {
        return;
    }

    var values = window.__MARK_VALUES || [];
    var $token = $('input[name="__RequestVerificationToken"]').first();

    // Клиентское правило из Edit.cshtml / AdjustmentEdit.cshtml: для этих отметок обязательно замечание.
    // В простых журналах (без МРСО) правило отключают установкой window.__MARK_MENU_COMMENT_RULE = false.
    var REQUIRED_COMMENT_VALUES = ['1', '2', '3', 'НЗ'];
    var requireCommentRule = window.__MARK_MENU_COMMENT_RULE !== false;

    var $menu = $('<div class="dropdown-menu mark-context-menu"></div>').appendTo('body').hide();
    var current = null;
    var $msg = null;
    var $saveBtn = null;

    function closeMenu() {
        $menu.hide();
        current = null;
    }

    function cellData(cell) {
        var data = {};
        try {
            data = JSON.parse($(cell).attr('data-mark') || '{}');
        } catch (err) {
            data = {};
        }
        return data;
    }

    // Сообщения об ошибках из возвращенного сервером HTML (валидация, правила редактирования)
    function extractErrors(html) {
        var msgs = [];
        try {
            var doc = new DOMParser().parseFromString(html, 'text/html');
            var nodes = doc.querySelectorAll(
                '.validation-summary-errors li, .validation-summary-valid li, [data-valmsg-summary="true"] li, .field-validation-error, .alert-danger');
            for (var i = 0; i < nodes.length; i++) {
                var t = $.trim(nodes[i].textContent);
                if (t) {
                    msgs.push(t);
                }
            }
        } catch (err) { }
        return msgs.join(' ');
    }

    // Есть ли в ответе сервера форма редактирования отметки (= отметка НЕ сохранена)
    function hasEditForm(html) {
        try {
            var doc = new DOMParser().parseFromString(html, 'text/html');
            return !!doc.querySelector('#markFormEdit, #markFormAdjustment');
        } catch (err) {
            return false;
        }
    }

    // Маркер страниц журналов (Journal/AdjustedJournal) - однозначный признак успешного сохранения
    function hasJournalMarker(html) {
        try {
            var doc = new DOMParser().parseFromString(html, 'text/html');
            return !!doc.querySelector('#markMenuJournalMarker');
        } catch (err) {
            return false;
        }
    }

    // Компактный список полей формы из ответа сервера (для диагностики в консоли)
    function formDigest(html) {
        try {
            var doc = new DOMParser().parseFromString(html, 'text/html');
            var form = doc.querySelector('#markFormEdit, #markFormAdjustment');
            if (!form) {
                return '(формы нет)';
            }
            var parts = [];
            var inputs = form.querySelectorAll('input, select, textarea');
            for (var i = 0; i < inputs.length; i++) {
                var el = inputs[i];
                var name = el.getAttribute('name') || el.id || '';
                var val = (el.value || '').substring(0, 40);
                var invalid = /input-validation-error|is-invalid/.test(el.getAttribute('class') || '');
                parts.push(name + '=' + val + (invalid ? ' [INVALID]' : ''));
            }
            return parts.join('; ');
        } catch (err) {
            return String(err);
        }
    }

    function showError(text) {
        if ($msg) {
            $msg.removeClass('d-none').text(text);
        }
        if ($saveBtn) {
            $saveBtn.prop('disabled', false);
        }
    }

    // Состав полей идентичен форме Edit/AdjustmentEdit (FlagX формами не отправляется)
    function submitValue(value, comment, signature) {
        if (!current) {
            return;
        }

        var $c = $(current);
        var data = cellData(current);
        var url = $c.attr('data-menu-url') || $c.attr('href');
        if (!url) {
            return;
        }

        // Защита от поврежденных/отсутствующих данных ячейки (устаревшая страница, битый JSON):
        // без числовых id сервер ответит "The value '' is invalid."
        if (data.markID === undefined || data.markID === null || data.markID === '' ||
            data.lessonID === undefined || data.themeID === undefined) {
            console.error('mark-cell-menu: повреждены данные ячейки (data-mark):', $c.attr('data-mark'));
            showError('Данные ячейки повреждены. Обновите страницу (F5) и повторите.');
            return;
        }

        var $form = $('<form></form>', { method: 'post', action: url, css: { display: 'none' } });

        if ($token.length) {
            $form.append($('<input/>', {
                type: 'hidden',
                name: '__RequestVerificationToken',
                value: $token.val()
            }));
        }

        var fields = {
            MarkID: data.markID,
            Value: value,
            Date: data.date,
            Comment: comment === undefined ? data.comment : comment,
            SignatureOfTeacher: signature === undefined ? data.signature : signature,
            HistoryOfMark: data.history,
            SubjectID: data.subjectID,
            GroupID: data.groupID,
            LessonID: data.lessonID,
            TypeOfExerciseID: data.typeOfExerciseID,
            DepartmentID: data.departmentID,
            SpecialityID: data.specialityID,
            ThemeID: data.themeID,
            StudentID: data.studentID,
            FlagF: data.flagF,
            InstituteID: data.instituteID,
            ChangeCounter: data.changeCounter,
            subgroup: data.subgroup
        };

        $.each(fields, function (name, val) {
            $form.append($('<input/>', {
                type: 'hidden',
                name: name,
                value: (val === null || val === undefined) ? '' : val
            }));
        });

        if (!window.fetch) {
            // Устаревший браузер: обычная отправка формы (с навигацией)
            closeMenu();
            $form.appendTo('body').trigger('submit');
            return;
        }

        if ($saveBtn) {
            $saveBtn.prop('disabled', true);
        }

        // redirect: 'manual' - редирект не разыменовывается запросом, поэтому TempData-сообщения
        // доживают до перезагрузки страницы журнала
        fetch(url, {
            method: 'POST',
            body: $form.serialize(),
            headers: { 'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8' },
            redirect: 'manual',
            credentials: 'same-origin'
        })
            .then(function (res) {
                // 302: отметка сохранена (или служебное сообщение) - закрываем меню и обновляем журнал
                if (res.type === 'opaqueredirect' || res.status === 0) {
                    closeMenu();
                    window.location.reload();
                    return;
                }
                if (res.status >= 400) {
                    showError('Не удалось сохранить отметку (ошибка сервера ' + res.status + ').');
                    return;
                }
                // 200 без принудительного редиректа: разбираем содержимое ответа
                return res.text().then(function (html) {
                    var isForm = hasEditForm(html);
                    if (!isForm || hasJournalMarker(html)) {
                        // Страница журнала: отметка сохранена, редирект разыграл сам браузер
                        // (устаревшие браузеры игнорируют redirect: 'manual'), либо редкий
                        // случай ответа 200 без перенаправления
                        closeMenu();
                        window.location.reload();
                        return;
                    }
                    var msg = extractErrors(html);
                    if (msg) {
                        console.warn('mark-cell-menu: отправленные данные:', $form.serialize());
                        showError(msg);
                    } else {
                        console.error('mark-cell-menu: сервер вернул форму редактирования без сообщения об ошибке. Отправленные данные:', $form.serialize());
                        console.error('mark-cell-menu: поля формы из ответа сервера:', formDigest(html));
                        showError('Отметка не сохранена. Подробности - в консоли браузера (F12).');
                    }
                });
            })
            .catch(function () {
                showError('Соединение с сервером потеряно, отметка не сохранена.');
            });
    }

    function openMenu(cell) {
        var $c = $(cell);
        var data = cellData(cell);

        $menu.empty();

        // Выбор отметки
        var $sel = $('<select class="form-control form-control-sm"></select>');
        $.each(values, function (_, v) {
            $sel.append($('<option></option>').val(v.value).text(v.label));
        });
        var cur = (data.value === null || data.value === undefined) ? '' : String(data.value);
        $sel.val(cur);
        if ($sel.val() === null) {
            $sel.val($sel.find('option').first().val());
        }
        $menu.append($('<div class="px-2 pt-2"></div>').append($sel));

        // Поле подписи - как в Edit/AdjustmentEdit: зависит от роли пользователя.
        // Шаблоны рендерятся представлением: #markMenuTeacherTpl (select, ICDA-writer/K-8Writer
        // в обычном журнале), #markMenuSignatureTpl (текст, SuperAdmin в журнале УМЧ).
        // Для остальных ролей поля нет - сервер подставит текущего преподавателя.
        var $sign = null;
        var $teacherTpl = $('#markMenuTeacherTpl');
        var $signatureTpl = $('#markMenuSignatureTpl');
        if ($teacherTpl.length) {
            $sign = $('<select class="form-control form-control-sm"></select>').append($teacherTpl.children().clone());
            $sign.val(data.signature || '');
            // текущей подписи может не быть в списке - добавим, чтобы не менять её незаметно
            if ($sign.val() === null && data.signature) {
                $sign.prepend($('<option></option>').val(data.signature).text(data.signature));
                $sign.val(data.signature);
            }
            $menu.append($('<div class="small text-muted px-2 pt-2"></div>').text('Преподаватель'));
            $menu.append($('<div class="px-2"></div>').append($sign));
        } else if ($signatureTpl.length) {
            $sign = $('<input type="text" class="form-control form-control-sm" />');
            $sign.val(data.signature || '');
            $menu.append($('<div class="small text-muted px-2 pt-2"></div>').text('Подпись'));
            $menu.append($('<div class="px-2"></div>').append($sign));
        }

        // Замечание преподавателя
        var $cmt = $('<textarea class="form-control form-control-sm" rows="2" placeholder="Замечание преподавателя"></textarea>');
        $cmt.val(data.comment || '');
        $menu.append($('<div class="px-2 pt-2"></div>').append($cmt));

        // Сообщение о ошибке заполнения
        var $err = $('<div class="text-danger small px-2 d-none"></div>');
        $menu.append($err);

        // Кнопки
        var $buttons = $('<div class="d-flex justify-content-between px-2 py-2"></div>');
        var $save = $('<button type="button" class="btn btn-primary btn-sm">Выставить</button>');
        var $close = $('<button type="button" class="btn btn-outline-secondary btn-sm ml-2">Закрыть</button>');

        $save.on('click', function () {
            var value = $sel.val();
            var comment = $cmt.val() || '';
            if (requireCommentRule && $.inArray($.trim(value || ''), REQUIRED_COMMENT_VALUES) !== -1 && !$.trim(comment)) {
                $err.removeClass('d-none').text('Поле "Замечание преподавателя" обязательно для заполнения.');
                $cmt.focus();
                return;
            }
            $err.addClass('d-none').text('');
            submitValue(value, comment, $sign ? $sign.val() : undefined);
        });

        $close.on('click', function () {
            closeMenu();
        });

        $buttons.append($save).append($close);
        $menu.append($buttons);

        $msg = $err;
        $saveBtn = $save;

        // Позиционирование рядом с ячейкой, при нехватке места - вверх/влево
        $menu.css({ top: 0, left: 0 }).show();
        var rect = cell.getBoundingClientRect();
        var menuWidth = $menu.outerWidth();
        var menuHeight = $menu.outerHeight();
        var top = rect.bottom + 2;
        var left = rect.left;

        if (top + menuHeight > $(window).height() - 8) {
            top = Math.max(8, rect.top - menuHeight - 2);
        }
        if (left + menuWidth > $(window).width() - 8) {
            left = Math.max(8, $(window).width() - menuWidth - 8);
        }

        $menu.css({ top: top, left: left });
    }

    $cells.on('click', function (e) {
        e.preventDefault();
        e.stopPropagation();

        if (current === this && $menu.is(':visible')) {
            closeMenu();
            return;
        }

        // подсказку ячейки прячем, чтобы не перекрывать меню
        try { $(this).tooltip('hide'); } catch (err) { }

        current = this;
        openMenu(this);
    });

    $(document).on('click', function (e) {
        if (!$(e.target).closest('.mark-context-menu').length) {
            closeMenu();
        }
    });

    $(document).on('keydown', function (e) {
        if (e.key === 'Escape') {
            closeMenu();
        }
    });

    // При прокрутке журнала меню удобнее закрыть
    $(window).on('scroll resize', closeMenu);
});
