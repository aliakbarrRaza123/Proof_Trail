const role = document.getElementById('account-role');
if (role) {
    const holderFields = document.getElementById('holder-fields');
    const institutionFields = document.getElementById('institution-fields');

    function showFields(group, visible) {
        group.hidden = !visible;
        group.querySelectorAll('input').forEach(input => {
            input.disabled = !visible;
            input.required = visible;
        });
    }

    function updateFields() {
        showFields(holderFields, role.value === 'Holder');
        showFields(institutionFields, role.value === 'Issuer');
    }

    role.addEventListener('change', updateFields);
    updateFields();
}
