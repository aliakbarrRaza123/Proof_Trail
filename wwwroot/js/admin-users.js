$(function () {
    const form = $('#users-search');
    const input = $('#search');
    const message = $('#users-search-message');
    const results = $('#users-results');
    let timer;
    let request;

    function search(delay) {
        clearTimeout(timer);
        request?.abort();
        message.prop('hidden', true);

        const term = input.val().trim();

        function loadResults() {
            request = $.get(form.data('search-url'), { search: term })
                .done(function (html) {
                    results.html(html);
                })
                .fail(function (_, status) {
                    if (status !== 'abort') {
                        message.text('Unable to search users. Try again.').prop('hidden', false);
                    }
                })
                .always(function () {
                    request = null;
                });
        }

        if (term && delay > 0) {
            timer = setTimeout(loadResults, delay);
        } else {
            loadResults();
        }
    }

    input.on('input', function () {
        search(200);
    });

    form.on('submit', function (event) {
        event.preventDefault();
        search(0);
    });
});
