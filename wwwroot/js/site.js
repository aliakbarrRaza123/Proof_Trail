// Show institution registration fields only for Issuer sign-up.
const role = document.getElementById('account-role');
if (role) {
    const update = () => { document.getElementById('institution-fields').hidden = role.value !== 'Issuer'; };
    role.addEventListener('change', update);
    update();
}
