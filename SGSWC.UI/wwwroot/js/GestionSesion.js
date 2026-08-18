const minutosSesion = parseInt(window.configSesion.minutosSesion);
const tiempoTotal = minutosSesion * 60;
const avisoAntes = 30;

let temporizadorAviso;
let temporizadorCerrar;
let actividadBloqueada = false;

iniciarControlSesion();

document.addEventListener("click", detectarActividad);
document.addEventListener("scroll", detectarActividad);
document.addEventListener("keydown", detectarActividad);

function detectarActividad() {
    if (actividadBloqueada) return;
    actividadBloqueada = true;

    fetch(window.configSesion.urlRenovar)
        .then(() => {
            iniciarControlSesion();
            setTimeout(() => actividadBloqueada = false, 10000);
        })
        .catch(() => cerrarSesion());
}

function iniciarControlSesion() {
    clearTimeout(temporizadorAviso);
    clearTimeout(temporizadorCerrar);

    temporizadorAviso = setTimeout(mostrarAviso, (tiempoTotal - avisoAntes) * 1000);
    temporizadorCerrar = setTimeout(cerrarSesion, tiempoTotal * 1000);
}

function mostrarAviso() {
    let segundos = avisoAntes;

    Swal.fire({
        title: 'Sesión próxima a expirar',
        html: `Su sesión expirará en <b>${segundos}</b> segundos.`,
        timer: avisoAntes * 1000,
        timerProgressBar: true,
        icon: 'warning',
        allowOutsideClick: false,
        allowEscapeKey: false,
        showCancelButton: true,
        confirmButtonText: 'Continuar sesión',
        cancelButtonText: 'Cerrar sesión',
        didOpen: () => {                                    // ← fix #2
            const barra = document.querySelector('.swal2-timer-progress-bar');
            if (barra) {
                barra.style.backgroundColor = '#FFAB00';
                barra.style.height = '9px';
            }

            document.querySelector('.swal2-confirm')
                ?.addEventListener('click', renovarSesion);
            document.querySelector('.swal2-cancel')
                ?.addEventListener('click', cerrarSesion);

            const intervalo = setInterval(() => {
                segundos--;
                const b = Swal.getHtmlContainer()?.querySelector('b');
                if (b) b.textContent = segundos;
                if (segundos <= 0) clearInterval(intervalo);
            }, 1000);
        }
    }).then((result) => {
        // ← fix #3: timer expiró sin interacción del usuario
        if (result.dismiss === Swal.DismissReason.timer) {
            cerrarSesion();
        }
    });
}

function renovarSesion() {
    fetch(window.configSesion.urlRenovar)
        .then(() => {
            Swal.close();
            iniciarControlSesion();
        })
        .catch(() => cerrarSesion());
}

function cerrarSesion() {
    window.location.href = window.configSesion.urlCerrar;
}