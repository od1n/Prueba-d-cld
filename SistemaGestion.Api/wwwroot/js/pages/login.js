// login.js — página de inicio de sesión
if (Auth.isLoggedIn()) {
    window.location.replace('/pages/dashboard.html');
}

const formLogin  = document.getElementById('formLogin');
const btnLogin   = document.getElementById('btnLogin');
const inpUsuario = document.getElementById('usuario');
const inpPass    = document.getElementById('contrasena');
const btnVerPass = document.getElementById('btnVerPass');

btnVerPass.addEventListener('click', () => {
    const visible = inpPass.type === 'text';
    inpPass.type  = visible ? 'password' : 'text';
    btnVerPass.setAttribute('aria-pressed', String(!visible));
    btnVerPass.setAttribute('aria-label',
        visible ? 'Mostrar contraseña' : 'Ocultar contraseña');
});

formLogin.addEventListener('submit', async e => {
    e.preventDefault();
    const u = inpUsuario.value.trim();
    const p = inpPass.value;
    inpUsuario.removeAttribute('aria-invalid');
    inpPass.removeAttribute('aria-invalid');
    if (!u || !p) {
        showMsg('Ingresa usuario y contraseña.', 'error', '#msg');
        if (!u) inpUsuario.setAttribute('aria-invalid', 'true');
        if (!p) inpPass.setAttribute('aria-invalid', 'true');
        (u ? inpPass : inpUsuario).focus();
        return;
    }
    setLoading(btnLogin, true);
    hideMsg('#msg');
    try {
        await Auth.login(u, p);
        window.location.replace('/pages/dashboard.html');
    } catch (err) {
        showMsg(esh(err.message || 'Error desconocido.'), 'error', '#msg');
        inpPass.value = '';
        inpPass.setAttribute('aria-invalid', 'true');
        inpPass.focus();
    } finally {
        setLoading(btnLogin, false);
    }
});
