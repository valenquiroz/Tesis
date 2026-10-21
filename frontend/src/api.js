export const API_URL = (
  import.meta.env.VITE_API_URL || "http://localhost:5282/api"
).replace(/\/$/, "");

const CLAVE_SESION = "sesion";

export const EVENTO_SESION_VENCIDA = "acompanar:sesion-vencida";

export function leerSesion() {
  try {
    const sesion = JSON.parse(localStorage.getItem(CLAVE_SESION));
    return sesion?.token ? sesion : null;
  } catch {
    return null;
  }
}

export function guardarSesion({ token, nombreCompleto, email, rol, roles, plan, nombreGrupo }) {
  localStorage.setItem(
    CLAVE_SESION,
    JSON.stringify({ token, nombreCompleto, email, rol, roles: roles || [rol], plan, nombreGrupo })
  );
}

// sesiones viejas: solo tenían "rol"
export const tieneRol = (sesion, rol) =>
  (sesion?.roles || [sesion?.rol]).includes(rol);

export function borrarSesion() {
  localStorage.removeItem(CLAVE_SESION);
  // claves de la versión anterior
  ["token", "nombreCompleto", "rol"].forEach((c) => localStorage.removeItem(c));
}

export class ErrorApi extends Error {
  constructor(mensaje, estado, errores = []) {
    super(mensaje);
    this.estado = estado;
    this.errores = errores;
  }
}

async function leerCuerpo(respuesta) {
  const texto = await respuesta.text();
  if (!texto) return null;

  try {
    return JSON.parse(texto);
  } catch {
    return null;
  }
}

function mensajeDeError(datos, porDefecto) {
  if (!datos) return porDefecto;

  if (Array.isArray(datos.errors) && datos.errors.length > 0) {
    return datos.errors.join(" ");
  }

  // ProblemDetails de ASP.NET: { errors: { campo: [] } }
  if (datos.errors && typeof datos.errors === "object") {
    return Object.values(datos.errors).flat().join(" ");
  }

  return datos.message || datos.title || porDefecto;
}

async function solicitar(ruta, { metodo = "GET", cuerpo, formulario, autenticado = true } = {}) {
  const headers = {};

  if (autenticado) {
    const sesion = leerSesion();
    if (sesion) headers.Authorization = `Bearer ${sesion.token}`;
  }

  let body;
  if (formulario) {
    body = formulario;
  } else if (cuerpo !== undefined) {
    headers["Content-Type"] = "application/json";
    body = JSON.stringify(cuerpo);
  }

  let respuesta;
  try {
    respuesta = await fetch(`${API_URL}${ruta}`, { method: metodo, headers, body });
  } catch {
    throw new ErrorApi(
      "No se pudo conectar con el servidor. Revisá que la API esté en funcionamiento.",
      0
    );
  }

  if (respuesta.status === 401 && autenticado) {
    window.dispatchEvent(new Event(EVENTO_SESION_VENCIDA));
    throw new ErrorApi("Tu sesión venció. Volvé a iniciar sesión.", 401);
  }

  return respuesta;
}

async function solicitarJson(ruta, opciones, errorPorDefecto) {
  const respuesta = await solicitar(ruta, opciones);
  const datos = await leerCuerpo(respuesta);

  if (respuesta.status >= 500) {
    throw new ErrorApi(
      "El servidor tuvo un problema y no pudo completar la operación. Probá de nuevo en unos minutos.",
      respuesta.status
    );
  }

  if (!respuesta.ok) {
    throw new ErrorApi(
      mensajeDeError(datos, errorPorDefecto),
      respuesta.status,
      Array.isArray(datos?.errors) ? datos.errors : []
    );
  }

  return datos;
}

export const api = {
  obtener: (ruta, error = "No se pudieron cargar los datos.") =>
    solicitarJson(ruta, {}, error),

  crear: (ruta, cuerpo, error = "No se pudo guardar.") =>
    solicitarJson(ruta, { metodo: "POST", cuerpo }, error),

  actualizar: (ruta, cuerpo, error = "No se pudieron guardar los cambios.") =>
    solicitarJson(ruta, { metodo: "PUT", cuerpo }, error),

  eliminar: (ruta, error = "No se pudo eliminar.") =>
    solicitarJson(ruta, { metodo: "DELETE" }, error),

  subir: (ruta, formulario, error = "No se pudo subir el archivo.") =>
    solicitarJson(ruta, { metodo: "POST", formulario }, error),

  async descargar(ruta, nombreArchivo) {
    const respuesta = await solicitar(ruta);

    if (!respuesta.ok) {
      const datos = await leerCuerpo(respuesta);
      throw new ErrorApi(
        mensajeDeError(datos, "No se pudo descargar el documento."),
        respuesta.status
      );
    }

    const blob = await respuesta.blob();
    const url = URL.createObjectURL(blob);

    const enlace = document.createElement("a");
    enlace.href = url;
    enlace.download = nombreArchivo;
    document.body.appendChild(enlace);
    enlace.click();
    enlace.remove();

    setTimeout(() => URL.revokeObjectURL(url), 1000);
  },

  iniciarSesion: (email, password) =>
    solicitarJson(
      "/Auth/login",
      { metodo: "POST", cuerpo: { email, password }, autenticado: false },
      "Correo o contraseña incorrectos."
    ),

  registrarse: (nombreCompleto, email, password) =>
    solicitarJson(
      "/Auth/register",
      { metodo: "POST", cuerpo: { nombreCompleto, email, password }, autenticado: false },
      "No se pudo crear la cuenta."
    ),
};
