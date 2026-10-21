import { useCallback, useEffect, useState } from "react";
import { api, leerSesion } from "./api";
import { Boton, BotonIcono, Campo, Cargando, Icono, Modal } from "./componentes";
import { usePanel } from "./contexto";
import { guardarPreferencias, leerPreferencias, TAMANOS } from "./preferencias";
import { iniciales, ROLES_EQUIPO } from "./utilidades";

// Equipo de cuidado de una persona: quién la cuida (registra) y quién solo mira

export function ModalEquipo({ persona, alCerrar, alCambiar }) {
  const { avisar, ejecutar, confirmar, cambiarVista } = usePanel();
  const [datos, setDatos] = useState(null);
  const [nuevo, setNuevo] = useState({ email: "", rol: "Cuidador" });
  const [enviando, setEnviando] = useState(false);

  const ruta = `/PersonasAsistidas/${persona.id}/equipo`;

  const cargar = useCallback(
    () =>
      api
        .obtener(ruta, "No se pudo cargar el equipo.")
        .then(setDatos)
        .catch((error) => {
          avisar(error.message, "error");
          alCerrar();
        }),
    [ruta, avisar, alCerrar]
  );

  useEffect(() => {
    let activo = true;
    api
      .obtener(ruta, "No se pudo cargar el equipo.")
      .then((d) => activo && setDatos(d))
      .catch((error) => {
        if (!activo) return;
        avisar(error.message, "error");
        alCerrar();
      });
    return () => {
      activo = false;
    };
  }, [ruta, avisar, alCerrar]);

  const sumar = async (e) => {
    e.preventDefault();
    setEnviando(true);
    try {
      const resultado = await api.crear(ruta, { email: nuevo.email.trim(), rol: nuevo.rol });
      avisar(resultado.message);
      setNuevo((n) => ({ ...n, email: "" }));
      await cargar();
    } catch (error) {
      avisar(error.message, "error");
    } finally {
      setEnviando(false);
    }
  };

  const cambiarRol = async (miembro, rol) => {
    const ok = await ejecutar(() => api.actualizar(`${ruta}/${miembro.usuarioId}`, { rol }), "Se actualizó el rol.");
    if (ok) await cargar();
  };

  const cambiarResponsableCuida = async (responsableCuida) => {
    const ok = await ejecutar(
      () => api.actualizar(`/PersonasAsistidas/${persona.id}/responsable-cuida`, { responsableCuida }),
      responsableCuida ? "Vas a poder registrar los cuidados." : "Pasaste a modo observador."
    );
    if (ok) {
      await cargar();
      alCambiar?.();
    }
  };

  const quitar = (miembro, esUnoMismo) =>
    confirmar({
      titulo: esUnoMismo ? `¿Salir del equipo de ${persona.nombreCompleto}?` : `¿Quitar a ${miembro.nombreCompleto}?`,
      mensaje: esUnoMismo
        ? "Vas a dejar de ver sus datos, turnos y medicación. Lo que registraste queda guardado."
        : "Va a dejar de ver los datos de esta persona. Lo que registró queda guardado.",
      accion: async () => {
        const ok = await ejecutar(
          () => api.eliminar(`${ruta}/${miembro.usuarioId}`),
          esUnoMismo ? "Saliste del equipo." : "Se quitó a la persona del equipo."
        );
        if (!ok) return;
        if (esUnoMismo) {
          alCerrar();
          alCambiar?.();
        } else {
          await cargar();
        }
      },
    });

  const sesion = leerSesion() || {};
  const plan = datos?.plan;
  const sinEquipo = plan && !plan.grupo;

  return (
    <Modal
      abierto
      alCerrar={alCerrar}
      titulo={`Equipo de cuidado de ${persona.nombreCompleto}`}
      descripcion="Quién la cuida y quién mira lo que se registra. Cada registro queda con el nombre de quien lo hizo."
      pie={
        <Boton variante="secundario" onClick={alCerrar}>
          Cerrar
        </Boton>
      }
    >
      {!datos ? (
        <Cargando />
      ) : (
        <>
          <ul className="lista lista-cuidadores">
            <li>
              <div className="avatar" data-color="pino">
                {iniciales(datos.responsable.nombre)}
              </div>
              <div className="fila-datos">
                <strong>
                  {datos.responsable.nombre}
                  {datos.soyResponsable ? " (vos)" : ""}
                </strong>
                <span className="secundario">
                  {datos.responsableCuida
                    ? "Responsable: administra y también la cuida"
                    : "Responsable: administra y mira lo que registran"}
                </span>
              </div>
              <span className="etiqueta etiqueta-verde">Responsable</span>
            </li>

            {datos.miembros.map((m) => {
              const soyYo = m.email === sesion.email;
              return (
                <li key={m.usuarioId}>
                  <div className="avatar">{iniciales(m.nombreCompleto)}</div>
                  <div className="fila-datos">
                    <strong>
                      {m.nombreCompleto}
                      {soyYo ? " (vos)" : ""}
                    </strong>
                    <span className="secundario">{m.email}</span>
                  </div>
                  {datos.soyResponsable ? (
                    <select
                      className="selector-chico"
                      value={m.rol}
                      onChange={(e) => cambiarRol(m, e.target.value)}
                      aria-label={`Rol de ${m.nombreCompleto}`}
                    >
                      {Object.entries(ROLES_EQUIPO).map(([valor, r]) => (
                        <option key={valor} value={valor}>
                          {r.texto}
                        </option>
                      ))}
                    </select>
                  ) : (
                    <span className={`etiqueta ${m.rol === "Cuidador" ? "etiqueta-azul" : "etiqueta-suave"}`}>
                      {ROLES_EQUIPO[m.rol]?.texto || m.rol}
                    </span>
                  )}
                  {(datos.soyResponsable || soyYo) && (
                    <BotonIcono
                      icono={soyYo ? "salir" : "cerrar"}
                      etiqueta={soyYo ? "Salir del equipo" : `Quitar a ${m.nombreCompleto}`}
                      variante="peligro"
                      onClick={() => quitar(m, soyYo)}
                    />
                  )}
                </li>
              );
            })}
          </ul>

          {datos.soyResponsable && (
            <label className="opcion-equipo">
              <input
                type="checkbox"
                checked={datos.responsableCuida}
                disabled={!plan.grupo && datos.responsableCuida}
                onChange={(e) => cambiarResponsableCuida(e.target.checked)}
              />
              <span>
                <strong>Yo también la cuido</strong>
                <small>
                  {plan.grupo
                    ? "Si lo desactivás, quedás en modo observador: ves lo que registran los cuidadores sin poder cambiarlo."
                    : "El modo observador está desde el plan Equipo."}
                </small>
              </span>
            </label>
          )}

          {datos.soyResponsable && sinEquipo && (
            <div className="aviso-plan">
              <Icono nombre="info" tamano={16} />
              <p>
                Con el plan Gratis la persona la cuidás vos. Para sumar a un cuidador contratado o a un familiar que mire,
                pasate al plan Equipo.
              </p>
              <Boton
                variante="secundario"
                onClick={() => {
                  alCerrar();
                  cambiarVista("plan");
                }}
              >
                Ver planes
              </Boton>
            </div>
          )}

          {datos.soyResponsable && !sinEquipo && (
            <form className="formulario formulario-equipo" onSubmit={sumar}>
              <div className="grilla-campos">
                <Campo etiqueta="Correo" ayuda="Tiene que tener una cuenta en Acompañar.">
                  <input
                    type="email"
                    value={nuevo.email}
                    onChange={(e) => setNuevo((n) => ({ ...n, email: e.target.value }))}
                    placeholder="correo@ejemplo.com"
                    autoComplete="off"
                    required
                  />
                </Campo>
                <Campo etiqueta="Rol" ayuda={ROLES_EQUIPO[nuevo.rol].ayuda}>
                  <select value={nuevo.rol} onChange={(e) => setNuevo({ email: "", rol: e.target.value })}>
                    {Object.entries(ROLES_EQUIPO).map(([valor, r]) => (
                      <option key={valor} value={valor}>
                        {r.texto}
                      </option>
                    ))}
                  </select>
                </Campo>
              </div>
              <Boton type="submit" icono="mas" cargando={enviando}>
                Sumar al equipo
              </Boton>
              {plan.integrantes != null && (
                <p className="nota-pie">
                  <Icono nombre="info" tamano={14} />
                  Plan {plan.nombre}: {plan.usados} de {plan.integrantes} personas en tu grupo.
                </p>
              )}
            </form>
          )}

          {!datos.soyResponsable && (
            <p className="nota-pie">
              <Icono nombre="info" tamano={14} />
              El equipo lo arma quien es responsable de la persona.
            </p>
          )}
        </>
      )}
    </Modal>
  );
}

function CambiarContrasena() {
  const { avisar } = usePanel();
  const [valores, setValores] = useState({ actual: "", nueva: "", repetida: "" });
  const [errores, setErrores] = useState([]);
  const [guardando, setGuardando] = useState(false);
  const alCambiar = (e) => setValores((v) => ({ ...v, [e.target.name]: e.target.value }));

  const enviar = async (e) => {
    e.preventDefault();
    setErrores([]);
    if (valores.nueva !== valores.repetida) {
      setErrores(["Las contraseñas nuevas no coinciden."]);
      return;
    }
    setGuardando(true);
    try {
      const r = await api.crear("/Auth/cambiar-password", { actual: valores.actual, nueva: valores.nueva });
      avisar(r.message);
      setValores({ actual: "", nueva: "", repetida: "" });
    } catch (error) {
      setErrores(error.errores?.length ? error.errores : [error.message]);
    } finally {
      setGuardando(false);
    }
  };

  return (
    <form className="formulario" onSubmit={enviar}>
      <div className="grilla-campos">
        <Campo etiqueta="Contraseña actual" ancho="completo">
          <input type="password" name="actual" value={valores.actual} onChange={alCambiar} autoComplete="current-password" required />
        </Campo>
        <Campo etiqueta="Nueva" ayuda="Mínimo 6 caracteres, con mayúscula, número y símbolo.">
          <input type="password" name="nueva" value={valores.nueva} onChange={alCambiar} autoComplete="new-password" minLength={6} required />
        </Campo>
        <Campo etiqueta="Repetila">
          <input type="password" name="repetida" value={valores.repetida} onChange={alCambiar} autoComplete="new-password" required />
        </Campo>
      </div>
      {errores.length > 0 && (
        <ul className="errores-formulario" role="alert">
          {errores.map((e) => (
            <li key={e}>{e}</li>
          ))}
        </ul>
      )}
      <div>
        <Boton type="submit" variante="secundario" icono="check" cargando={guardando}>
          Cambiar contraseña
        </Boton>
      </div>
    </form>
  );
}

export function ModalPreferencias({ alCerrar }) {
  const [preferencias, setPreferencias] = useState(leerPreferencias);

  const cambiar = (cambios) => {
    // se parte de lo guardado: dos clics seguidos no se pisan
    const nuevas = { ...leerPreferencias(), ...cambios };
    setPreferencias(nuevas);
    guardarPreferencias(nuevas);
  };

  return (
    <Modal
      abierto
      alCerrar={alCerrar}
      titulo="Preferencias"
      pie={
        <Boton variante="secundario" onClick={alCerrar}>
          Listo
        </Boton>
      }
    >
      <section className="bloque-preferencias">
        <h3>Accesibilidad</h3>
        <p className="secundario">Se guarda en este navegador.</p>

        <div className="campo">
          <span className="etiqueta-campo" id="etiqueta-tamano">
            Tamaño de la letra
          </span>
          <div className="chips" role="radiogroup" aria-labelledby="etiqueta-tamano">
            {TAMANOS.map((t) => (
              <button
                key={t.valor}
                type="button"
                role="radio"
                aria-checked={preferencias.tamano === t.valor}
                className={`chip ${preferencias.tamano === t.valor ? "activo" : ""}`}
                onClick={() => cambiar({ tamano: t.valor })}
              >
                {t.texto}
              </button>
            ))}
          </div>
        </div>

        <label className="check-linea">
          <input type="checkbox" checked={preferencias.contraste} onChange={(e) => cambiar({ contraste: e.target.checked })} />
          Alto contraste
        </label>
      </section>

      <section className="bloque-preferencias">
        <h3>Contraseña</h3>
        <CambiarContrasena />
      </section>
    </Modal>
  );
}
