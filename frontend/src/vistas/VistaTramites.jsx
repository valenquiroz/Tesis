import { useEffect, useRef, useState } from "react";
import { api } from "../api";
import { Boton, Cargando, EncabezadoVista, EstadoVacio, Icono, Tarjeta } from "../componentes";
import { usePanel } from "../contexto";
import { AvisoPlan } from "./comunes";
import { colorPersona, diasHasta, formatearFechaCorta, iniciales, planIncluye, planQueIncluye, puedeAdministrar, puedeGestionar } from "../utilidades";

const ESTADOS = [
  { valor: "Pendiente", texto: "Pendiente" },
  { valor: "EnCurso", texto: "En curso" },
  { valor: "Hecho", texto: "Hecho" },
  { valor: "NoCorresponde", texto: "No corresponde" },
];

function AdjuntosTramite({ paso, persona, alCambiar }) {
  const { acciones } = usePanel();
  const entrada = useRef(null);
  const [subiendo, setSubiendo] = useState(false);

  const subir = async (e) => {
    const archivo = e.target.files?.[0];
    e.target.value = "";
    if (!archivo) return;
    setSubiendo(true);
    const ok = await acciones.subirDocumento(persona.id, archivo, paso.codigo);
    setSubiendo(false);
    if (ok) alCambiar();
  };

  return (
    <div className="tramite-adjuntos">
      <strong>Tus archivos para este paso</strong>
      {paso.documentos.length > 0 && (
        <ul>
          {paso.documentos.map((d) => (
            <li key={d.id}>
              <button type="button" className="enlace-accion" onClick={() => acciones.descargarDocumento(d)}>
                <Icono nombre="documento" tamano={14} /> {d.nombreOriginal}
              </button>
            </li>
          ))}
        </ul>
      )}
      {puedeGestionar(persona) && (
      <div className="tramite-botones">
        <input ref={entrada} type="file" hidden accept=".pdf,.jpg,.jpeg,.png,.doc,.docx" onChange={subir} />
        <Boton variante="secundario" icono="subir" cargando={subiendo} onClick={() => entrada.current?.click()}>
          Adjuntar archivo
        </Boton>
        <Boton
          variante="secundario"
          icono="calendario"
          onClick={() =>
            acciones.nuevoTurno("", {
              titulo: paso.titulo,
              especialidad: "Trámite",
              lugar: paso.organismo,
              personaAsistidaId: String(persona.id),
            })
          }
        >
          Agendar
        </Boton>
      </div>
      )}
    </div>
  );
}

function PasoTramite({ paso, persona, abierto, alAbrir, alCambiarEstado, alCambiarAdjuntos }) {
  const { acciones, hoy } = usePanel();
  const hecho = paso.estado === "Hecho";
  const descartado = paso.estado === "NoCorresponde";
  const tieneDetalle = paso.pasos.length > 0 || paso.documentacion.length > 0 || paso.aviso;
  const editable = puedeGestionar(persona);

  return (
    <li className={`tramite estado-${paso.estado.toLowerCase()}`} id={`tramite-${paso.codigo}`}>
      <button
        type="button"
        className="tramite-marca"
        aria-pressed={hecho}
        disabled={!editable}
        aria-label={hecho ? `Marcar "${paso.titulo}" como pendiente` : `Marcar "${paso.titulo}" como hecho`}
        onClick={() => alCambiarEstado(paso, hecho ? "Pendiente" : "Hecho")}
      >
        {hecho && <Icono nombre="check" tamano={16} />}
      </button>

      <div className="tramite-cuerpo">
        <div className="tramite-cabecera">
          <div>
            <h3>{paso.titulo}</h3>
            <div className="tramite-datos">
              <span className={`etiqueta ${paso.imprescindible ? "etiqueta-naranja" : "etiqueta-suave"}`}>
                {paso.imprescindible ? "Imprescindible" : "Si corresponde"}
              </span>
              <span className="secundario">{paso.organismo}</span>
              {paso.fechaActualizacion && paso.estado !== "Pendiente" && (
                <span className="secundario">· Actualizado el {formatearFechaCorta(paso.fechaActualizacion)}</span>
              )}
            </div>
          </div>

          <select
            className="tramite-estado"
            value={paso.estado}
            onChange={(e) => alCambiarEstado(paso, e.target.value)}
            disabled={!editable}
            aria-label={`Estado de "${paso.titulo}"`}
          >
            {ESTADOS.map((e) => (
              <option key={e.valor} value={e.valor}>
                {e.texto}
              </option>
            ))}
          </select>
        </div>

        {!descartado && <p className="tramite-resumen">{paso.resumen}</p>}

        {!descartado && tieneDetalle && (
          <button type="button" className="enlace-accion" aria-expanded={abierto} onClick={alAbrir}>
            {abierto ? "Ocultar detalle" : paso.documentacion.length > 0 ? "Qué hay que llevar" : "Cómo se hace"}
            <Icono nombre="derecha" tamano={14} className={abierto ? "girado" : ""} />
          </button>
        )}

        {abierto && !descartado && (
          <div className="tramite-detalle">
            {paso.pasos.length > 0 && (
              <ol className="tramite-pasos">
                {paso.pasos.map((p) => (
                  <li key={p}>{p}</li>
                ))}
              </ol>
            )}

            {paso.documentacion.length > 0 && (
              <ul className="tramite-documentacion">
                {paso.documentacion.map((d) => (
                  <li key={d}>
                    <Icono nombre="documento" tamano={15} />
                    <span>{d}</span>
                  </li>
                ))}
              </ul>
            )}

            {paso.codigo === "cud-vencimiento" && (
              <p className="tramite-aviso">
                <Icono nombre="calendario" tamano={15} />
                {persona.vencimientoCud ? (
                  <span>
                    El CUD de {persona.nombreCompleto.split(" ")[0]} vence el {formatearFechaCorta(persona.vencimientoCud)}
                    {diasHasta(persona.vencimientoCud, hoy) >= 0 ? "" : " (ya venció)"}. Hay un aviso agendado en el calendario.
                  </span>
                ) : (
                  <span>
                    Todavía no se cargó la fecha.{" "}
                    {puedeAdministrar(persona) && (
                      <button type="button" className="enlace" onClick={() => acciones.editarPersona(persona)}>
                        Cargarla en el perfil
                      </button>
                    )}
                  </span>
                )}
              </p>
            )}

            {paso.aviso && (
              <p className="tramite-aviso">
                <Icono nombre="info" tamano={15} />
                {paso.aviso}
              </p>
            )}

            <AdjuntosTramite paso={paso} persona={persona} alCambiar={alCambiarAdjuntos} />

            <a className="tramite-fuente" href={paso.enlace} target="_blank" rel="noopener noreferrer">
              <Icono nombre="externo" tamano={14} />
              <span>
                Fuente oficial: <strong>{paso.fuente}</strong>
              </span>
            </a>
          </div>
        )}
      </div>
    </li>
  );
}

export default function VistaTramites() {
  const { personas, idRecomendaciones, acciones, cargando, avisar } = usePanel();
  const persona = personas.find((p) => p.id === idRecomendaciones);
  const personaId = persona?.id;
  const conTramites = planIncluye(persona, "tramites");

  const [guiaGuardada, setGuia] = useState(null);
  const [abierto, setAbierto] = useState(null);

  // se recarga si cambia algo del perfil que afecta a la guía
  const claveGuia = persona
    ? [persona.id, persona.tipoDiscapacidad, persona.fechaNacimiento, persona.relacionConCuidador].join("|")
    : null;
  // mientras llega la de otra persona se muestra el cargando
  const guia = guiaGuardada?.clave === claveGuia ? guiaGuardada : null;

  useEffect(() => {
    if (!personaId || !conTramites) return;
    let activo = true;
    api
      .obtener(`/Tramites/${personaId}`, "No se pudo cargar la guía de trámites.")
      .then((datos) => {
        if (!activo) return;
        setGuia({ ...datos, clave: claveGuia });
        // se abre el primer paso que falta hacer
        const siguiente = datos.pasos.find((p) => p.imprescindible && p.estado !== "Hecho" && p.estado !== "NoCorresponde");
        setAbierto(siguiente?.codigo ?? null);
      })
      .catch((error) => {
        if (!activo) return;
        avisar(error.message, "error");
        setGuia({ error: true, clave: claveGuia });
      });
    return () => {
      activo = false;
    };
  }, [personaId, conTramites, claveGuia, avisar]);

  if (cargando) return <Cargando />;

  if (personas.length === 0) {
    return (
      <>
        <EncabezadoVista titulo="Trámites y estudios" />
        <Tarjeta>
          <EstadoVacio
            icono="lista"
            titulo="Primero agregá una persona asistida"
            accion={
              <Boton icono="mas" onClick={acciones.nuevaPersona}>
                Agregar persona
              </Boton>
            }
          >
            La guía se arma según el tipo de discapacidad, la edad y la relación con vos.
          </EstadoVacio>
        </Tarjeta>
      </>
    );
  }

  const recargarGuia = () =>
    api
      .obtener(`/Tramites/${persona.id}`)
      .then((datos) => setGuia({ ...datos, clave: claveGuia }))
      .catch((error) => avisar(error.message, "error"));

  const cambiarEstado = async (paso, estado) => {
    const anterior = paso.estado;
    const actualizar = (cambios) =>
      setGuia((g) => ({
        ...g,
        pasos: g.pasos.map((p) => (p.codigo === paso.codigo ? { ...p, ...cambios } : p)),
      }));

    actualizar({ estado });
    try {
      const resultado = await api.actualizar(`/Tramites/${persona.id}/${paso.codigo}`, { estado, nota: paso.nota });
      actualizar({ fechaActualizacion: resultado.fechaActualizacion });
      if (estado === "Hecho" && abierto === paso.codigo) {
        const siguiente = guia.pasos.find(
          (p) => p.codigo !== paso.codigo && p.imprescindible && p.estado !== "Hecho" && p.estado !== "NoCorresponde"
        );
        setAbierto(siguiente?.codigo ?? null);
      }
    } catch (error) {
      actualizar({ estado: anterior });
      avisar(error.message, "error");
    }
  };

  const pasos = guia?.pasos ?? [];
  const imprescindibles = pasos.filter((p) => p.imprescindible);
  const hechos = imprescindibles.filter((p) => p.estado === "Hecho").length;
  const siguiente = imprescindibles.find((p) => p.estado !== "Hecho" && p.estado !== "NoCorresponde");

  const irA = (codigo) => {
    setAbierto(codigo);
    requestAnimationFrame(() =>
      document.getElementById(`tramite-${codigo}`)?.scrollIntoView({ behavior: "smooth", block: "center" })
    );
  };

  return (
    <>
      <EncabezadoVista
        titulo="Trámites y estudios"
        descripcion="Lo que hay que hacer, en orden, según el perfil de la persona."
      />

      <section className="banner-persona">
        <div className="banner-persona-datos">
          <div className="avatar avatar-grande" data-color={colorPersona(persona?.id)}>{iniciales(persona?.nombreCompleto)}</div>
          <div>
            <small>Hoja de ruta de</small>
            {personas.length > 1 ? (
              <select
                className="selector-persona"
                value={idRecomendaciones ?? ""}
                onChange={(e) => acciones.elegirPersonaRecomendaciones(e.target.value)}
                aria-label="Elegir persona asistida"
              >
                {personas.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.nombreCompleto}
                  </option>
                ))}
              </select>
            ) : (
              <strong>{persona?.nombreCompleto}</strong>
            )}
            <div className="etiquetas">
              {persona?.tipoDiscapacidad ? (
                <span className="etiqueta etiqueta-clara">
                  Discapacidad {persona.tipoDiscapacidad.toLowerCase()}
                </span>
              ) : (
                <span className="texto-claro">Sin tipo de discapacidad cargado</span>
              )}
            </div>
          </div>
        </div>

        {conTramites && guia && !guia.error && (
          <div className="banner-persona-perfil">
            <span>
              {hechos} de {imprescindibles.length} pasos imprescindibles
            </span>
            <span className="medidor medidor-claro" aria-hidden="true">
              <span style={{ width: `${imprescindibles.length ? (hechos / imprescindibles.length) * 100 : 0}%` }} />
            </span>
            {siguiente ? (
              <Boton variante="claro" icono="derecha" onClick={() => irA(siguiente.codigo)}>
                Próximo paso
              </Boton>
            ) : (
              <span className="tramite-listo">
                <Icono nombre="check" tamano={15} /> Lo imprescindible está hecho
              </span>
            )}
          </div>
        )}
      </section>

      {persona && !persona.tipoDiscapacidad && (
        <div className="aviso-tramites">
          <Icono nombre="info" />
          <p>
            Cargá el <strong>tipo de discapacidad</strong> para ver qué estudios pide la junta evaluadora en su caso.
          </p>
          {puedeAdministrar(persona) && (
            <Boton variante="secundario" icono="editar" onClick={() => acciones.editarPersona(persona)}>
              Completar perfil
            </Boton>
          )}
        </div>
      )}

      {!conTramites ? (
        <AvisoPlan
          titulo={`La guía de trámites está desde el plan ${planQueIncluye("tramites")}`}
          texto="Una hoja de ruta del CUD y de los derechos que habilita, armada según el tipo de discapacidad de la persona, con el seguimiento de cada paso."
          puedeCambiar={puedeAdministrar(persona)}
        />
      ) : !guia ? (
        <Cargando texto="Armando la hoja de ruta..." />
      ) : guia.error ? (
        <Tarjeta>
          <EstadoVacio icono="alerta" titulo="No pudimos cargar la guía">
            Probá de nuevo en unos minutos.
          </EstadoVacio>
        </Tarjeta>
      ) : (
        <>
          <ol className="etapas-tramites">
            {guia.etapas.map((etapa) => {
              const deEtapa = pasos.filter((p) => p.etapa === etapa.numero);
              if (deEtapa.length === 0) return null;
              const completa = deEtapa.every((p) => p.estado === "Hecho" || p.estado === "NoCorresponde");
              return (
                <li key={etapa.numero} className={`etapa ${completa ? "etapa-completa" : ""}`}>
                  <div className="etapa-cabecera">
                    <span className="etapa-numero">
                      {completa ? <Icono nombre="check" tamano={16} /> : etapa.numero}
                    </span>
                    <div>
                      <h2>{etapa.titulo}</h2>
                      <p>{etapa.descripcion}</p>
                    </div>
                  </div>
                  <ul className="lista-tramites">
                    {deEtapa.map((paso) => (
                      <PasoTramite
                        key={paso.codigo}
                        paso={paso}
                        persona={persona}
                        alCambiarAdjuntos={recargarGuia}
                        abierto={abierto === paso.codigo}
                        alAbrir={() => setAbierto((a) => (a === paso.codigo ? null : paso.codigo))}
                        alCambiarEstado={cambiarEstado}
                      />
                    ))}
                  </ul>
                </li>
              );
            })}
          </ol>
        </>
      )}
    </>
  );
}
