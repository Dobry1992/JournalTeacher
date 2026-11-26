// Тултипы и флажки комментариев о курсантах.
// Подключается в _Layout только для SuperAdmin и ANB-UMCH.
// Элементы с атрибутом data-student-id получают флажок слева от фамилии
// и всплывающую подсказку с текстами комментариев.
// Работает и с таблицами DataTables: при перелистывании/поиске строки
// возвращаются в DOM и подвязываются через MutationObserver.
$(function () {
    // Адрес эндпоинта: из layout (Url.Action), чтобы работало при размещении в виртуальном каталоге IIS
    var forStudentsUrl = window.__STUDENT_COMMENTS_URL || '/StudentComments/ForStudents';

    var commentsById = {};   // StudentID -> html текста комментариев
    var requestedIds = {};   // ID, уже запрошенные с сервера
    var scanTimer = null;

    function attachTo($el, html) {
        $el.attr('data-comment-bound', '1');

        // Флажок слева от фамилии. Блочные элементы (td, h3, ссылки nav-link) —
        // вставляем внутрь перед текстом, иначе флажок уедет на отдельную строку;
        // для строчных ссылок/кнопок/подписей — перед элементом
        var $flag = $('<i/>', { 'class': 'fas fa-flag student-comment-flag', 'aria-hidden': 'true' });
        var attachedToFlag = false;
        if (($el.is('a') || $el.is('button') || $el.is('label')) && $el.css('display') !== 'block') {
            $flag.insertBefore($el);
            attachedToFlag = true;
        } else {
            $el.prepend($flag);
        }

        var tooltipOptions = {
            html: true,
            sanitize: false,
            title: html,
            container: 'body',
            trigger: 'hover',
            template: '<div class="tooltip student-comment-tooltip" role="tooltip"><div class="arrow"></div><div class="tooltip-inner"></div></div>'
        };

        $el.tooltip(tooltipOptions);

        // Для отдельного стоящего флажка подсказку включаем тоже на него
        if (attachedToFlag) {
            $flag.tooltip(tooltipOptions);
        }
    }

    function attachAll() {
        $('[data-student-id]').each(function () {
            var $el = $(this);
            if ($el.attr('data-comment-bound')) {
                return;
            }

            var id = parseInt($el.attr('data-student-id'), 10);
            var html = commentsById[id];
            if (!html) {
                return;
            }

            attachTo($el, html);
        });
    }

    // ID на странице, которые ещё не запрашивались с сервера
    function newIds() {
        var ids = [];
        $('[data-student-id]').each(function () {
            var id = parseInt($(this).attr('data-student-id'), 10);
            if (id > 0 && !requestedIds[id] && ids.indexOf(id) === -1) {
                ids.push(id);
            }
        });
        return ids;
    }

    function fetchIds(ids) {
        if (!ids.length) {
            return;
        }

        $.each(ids, function (_, id) {
            requestedIds[id] = true;
        });

        $.getJSON(forStudentsUrl, { ids: ids.join(',') })
            .done(function (data) {
                if (!data) {
                    return;
                }

                $.each(data, function (key, value) {
                    commentsById[parseInt(key, 10)] = value;
                });

                attachAll();
            })
            .fail(function (jqXHR) {
                console.warn('student-comments: не удалось загрузить комментарии, HTTP ' + jqXHR.status);
            });
    }

    function scan() {
        // DataTables пересоздаёт узлы строк при перелистывании/поиске/сортировке —
        // привязываемся заново даже когда все ID уже запрошены (тексты лежат в кэше commentsById)
        attachAll();
        fetchIds(newIds());
    }

    scan();

    if (window.MutationObserver) {
        // DataTables при пагинации/поиске возвращает строки обратно в DOM —
        // реагируем на появление новых элементов с data-student-id
        new MutationObserver(function () {
            if (scanTimer) {
                return;
            }
            scanTimer = setTimeout(function () {
                scanTimer = null;
                scan();
            }, 100);
        }).observe(document.body, { childList: true, subtree: true });
    } else {
        // запасной вариант для старых браузеров: сканирование по взаимодействию со страницей
        $(document).on('click keyup', function () {
            setTimeout(scan, 200);
        });
    }
});
