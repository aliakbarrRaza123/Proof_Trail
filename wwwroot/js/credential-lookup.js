$(function () {
    const form = $('#credential-lookup');
    const button = form.find('button');
    const result = $('#credential-result');

    form.on('submit', function (event) {
        event.preventDefault();
        button.prop('disabled', true);
        result.text('Looking up credential…');

        $.ajax({
            url: form.data('lookup-url'),
            method: 'GET',
            data: { id: $('#id').val() },
            dataType: 'html'
        })
            .done(function (html) {
                result.html(html);
            })
            .fail(function () {
                result.text('Unable to look up the credential. Try again.');
            })
            .always(function () {
                button.prop('disabled', false);
            });
    });
});
