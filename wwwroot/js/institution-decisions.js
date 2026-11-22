$(function () {
    const message = $('#decision-message');

    $('.institution-decision').on('submit', function (event) {
        event.preventDefault();
        const submitter = event.originalEvent.submitter;
        if (!submitter || !submitter.value) return;

        const form = $(this);
        const buttons = form.find('button');
        const data = form.serializeArray();
        data.push({ name: submitter.name, value: submitter.value });

        buttons.prop('disabled', true);
        message.prop('hidden', true);

        $.post(form.attr('action'), data, null, 'json')
            .done(function (result) {
                form.closest('tr').find('.institution-status').text(result.status);
                form.remove();
            })
            .fail(function (response) {
                const text = response.status === 409
                    ? 'This application is no longer pending. Reload to see its current status.'
                    : 'Unable to save the decision. Reload the page and try again.';
                message.text(text).prop('hidden', false);
            })
            .always(function () {
                buttons.prop('disabled', false);
            });
    });
});
