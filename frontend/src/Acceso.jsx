import { useState } from "react";
import { api, guardarSesion } from "./api";
import { Boton, Campo, Icono, Marca } from "./componentes";

const FORMULARIO_VACIO = { nombreCompleto: "", email: "", password: "" };

// el enlace del mail trae ?restablecer=<token>&email=<correo>
function leerEnlaceRestablecer() {
  const parametros = new URLSearchParams(window.location.search);
  const token = parametros.get("restablecer");
  return token ? { token, email: parametros.get("email") || "" } : null;
}

const TITULOS = {
  login: ["Bienvenido/a de nuevo", "Ingresá para ver el resumen de tu día."],
  registro: ["Creá tu cuenta", "Cargá a quienes cuidás y, si querés, sumá a quienes te ayudan, cada uno con su cuenta."],
  olvide: ["Recuperá tu contraseña", "Te mandamos un enlace al correo para elegir una nueva."],
  restablecer: ["Elegí una contraseña nueva", "Después vas a poder ingresar con ella."],
};

export default function Acceso({ alIngresar, avisoInicial = "" }) {
  const [enlace] = useState(leerEnlaceRestablecer);
  const [modo, setModo] = useState(enlace ? "restablecer" : "login");
  const [formulario, setFormulario] = useState(FORMULARIO_VACIO);
  const [verPassword, setVerPassword] = useState(false);
  const [mensaje, setMensaje] = useState(
    avisoInicial ? { tipo: "info", texto: avisoInicial } : null
  );
  const [cargando, setCargando] = useState(false);

  const esLogin = modo === "login";
  const conPestanas = modo === "login" || modo === "registro";
  const pideEmail = modo !== "restablecer";
  const pidePassword = modo !== "olvide";

  const manejarCambio = (e) =>
    setFormulario((f) => ({ ...f, [e.target.name]: e.target.value }));

  const cambiarModo = (nuevoModo) => {
    setModo(nuevoModo);
    setMensaje(null);
    setVerPassword(false);
  };

  const enviar = async (e) => {
    e.preventDefault();
    setMensaje(null);
    setCargando(true);

    try {
      if (modo === "login") {
        const resultado = await api.iniciarSesion(formulario.email, formulario.password);
        guardarSesion(resultado);
        alIngresar();
        return;
      }

      if (modo === "olvide") {
        const resultado = await api.crear("/Auth/olvide", { email: formulario.email.trim() });
        setMensaje({ tipo: "exito", texto: resultado.message });
        return;
      }

      if (modo === "restablecer") {
        const resultado = await api.crear("/Auth/restablecer", {
          email: enlace.email,
          token: enlace.token,
          nuevaPassword: formulario.password,
        });
        // que el enlace no quede en la barra ni en el historial
        window.history.replaceState(null, "", window.location.pathname);
        setModo("login");
        setFormulario({ ...FORMULARIO_VACIO, email: enlace.email });
        setMensaje({ tipo: "exito", texto: resultado.message });
        return;
      }

      await api.registrarse(formulario.nombreCompleto, formulario.email, formulario.password);

      setModo("login");
      setFormulario({ ...FORMULARIO_VACIO, email: formulario.email });
      setMensaje({
        tipo: "exito",
        texto: "Cuenta creada. Ya podés iniciar sesión.",
      });
    } catch (error) {
      const detalle = error.errores?.length ? ` ${error.errores.join(" ")}` : "";
      setMensaje({ tipo: "error", texto: error.message + detalle });
    } finally {
      setCargando(false);
    }
  };

  const [titulo, subtitulo] = TITULOS[modo];
  const textoBoton = {
    login: "Ingresar",
    registro: "Crear cuenta",
    olvide: "Enviar enlace",
    restablecer: "Guardar contraseña",
  }[modo];

  return (
    <div className="acceso">
      <main className="acceso-principal">
        <div className="acceso-tarjeta">
          <Marca className="acceso-marca" />

          {conPestanas && (
            <div className="pestanas" role="tablist">
              <button
                type="button"
                role="tab"
                aria-selected={esLogin}
                className={esLogin ? "activa" : ""}
                onClick={() => cambiarModo("login")}
              >
                Iniciar sesión
              </button>
              <button
                type="button"
                role="tab"
                aria-selected={!esLogin}
                className={!esLogin ? "activa" : ""}
                onClick={() => cambiarModo("registro")}
              >
                Crear cuenta
              </button>
            </div>
          )}

          <div className="acceso-titulo">
            <h1>{titulo}</h1>
            <p>{modo === "restablecer" && enlace?.email ? `Para ${enlace.email}.` : subtitulo}</p>
          </div>

          {mensaje && (
            <div className={`alerta alerta-${mensaje.tipo}`} role="alert">
              <Icono
                nombre={mensaje.tipo === "error" ? "alerta" : mensaje.tipo === "exito" ? "check" : "info"}
                tamano={18}
              />
              <span>{mensaje.texto}</span>
            </div>
          )}

          <form className="formulario-acceso" onSubmit={enviar} noValidate={false}>
            {modo === "registro" && (
              <Campo etiqueta="Nombre completo">
                <input
                  type="text"
                  name="nombreCompleto"
                  value={formulario.nombreCompleto}
                  onChange={manejarCambio}
                  placeholder="Ej. Valentina Quiroz"
                  autoComplete="name"
                  required
                />
              </Campo>
            )}

            {pideEmail && (
              <Campo etiqueta="Correo electrónico">
                <input
                  type="email"
                  name="email"
                  value={formulario.email}
                  onChange={manejarCambio}
                  placeholder="correo@ejemplo.com"
                  autoComplete="email"
                  required
                />
              </Campo>
            )}

            {pidePassword && (
              <Campo
                etiqueta={modo === "restablecer" ? "Contraseña nueva" : "Contraseña"}
                htmlFor="acceso-password"
                ayuda={
                  esLogin
                    ? null
                    : "Mínimo 6 caracteres, con mayúscula, minúscula, número y símbolo."
                }
              >
                <div className="campo-con-boton">
                  <input
                    id="acceso-password"
                    type={verPassword ? "text" : "password"}
                    name="password"
                    value={formulario.password}
                    onChange={manejarCambio}
                    placeholder={esLogin ? "Tu contraseña" : "Creá una contraseña segura"}
                    autoComplete={esLogin ? "current-password" : "new-password"}
                    required
                  />
                  <button
                    type="button"
                    className="campo-boton"
                    onClick={() => setVerPassword((v) => !v)}
                    aria-label={verPassword ? "Ocultar contraseña" : "Mostrar contraseña"}
                    title={verPassword ? "Ocultar contraseña" : "Mostrar contraseña"}
                  >
                    <Icono nombre={verPassword ? "ojoTachado" : "ojo"} tamano={18} />
                  </button>
                </div>
              </Campo>
            )}

            {esLogin && (
              <button type="button" className="enlace enlace-olvide" onClick={() => cambiarModo("olvide")}>
                ¿Olvidaste tu contraseña?
              </button>
            )}

            <Boton type="submit" className="ancho-total boton-grande" cargando={cargando}>
              {cargando ? "Procesando..." : textoBoton}
            </Boton>
          </form>

          <p className="acceso-cambio">
            {conPestanas ? (
              <>
                {esLogin ? "¿Todavía no tenés cuenta?" : "¿Ya tenés cuenta?"}{" "}
                <button
                  type="button"
                  className="enlace"
                  onClick={() => cambiarModo(esLogin ? "registro" : "login")}
                >
                  {esLogin ? "Registrate" : "Iniciá sesión"}
                </button>
              </>
            ) : (
              <button
                type="button"
                className="enlace"
                onClick={() => {
                  window.history.replaceState(null, "", window.location.pathname);
                  cambiarModo("login");
                }}
              >
                Volver a iniciar sesión
              </button>
            )}
          </p>
        </div>
      </main>
    </div>
  );
}
