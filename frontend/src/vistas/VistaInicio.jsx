import { useCallback, useEffect, useState } from "react";
import { api } from "../api";
import { Boton, Cargando, EstadoVacio, Icono } from "../componentes";
import { usePanel } from "../contexto";
import {
  colorPersona,
  diasHasta,
  etiquetaDia,
  extensionArchivo,
  fechaISO,
  formatearFechaCorta,
  formatearHora,
  iniciales,
  plural,
  primerNombre,
  saludoDelDia,
  planIncluye,
  puedeGestionar,
  puedeRegistrar,
} from "../utilidades";
import { TarjetaPersona } from "./comunes";

const horaDe = (fecha) => {
  const f = new Date(fecha);
  return `${String(f.getHours()).padStart(2, "0")}:${String(f.getMinutes()).padStart(2, "0")}`;
};

// Lo que conviene atender: tomas atrasadas, recetas y CUD por vencer
// el último turno de cada control que se repite, si no hay uno nuevo agendado
function controlesPendientes(turnos, hoy) {
  const ultimos = new Map();
  turnos
    .filter((t) => t.repetirCadaMeses)
    .forEach((t) => {
      const clave = `${t.personaAsistidaId}|${t.titulo.trim().toLowerCase()}`;
      const actual = ultimos.get(clave);
      if (!actual || new Date(t.fechaHora) > new Date(actual.fechaHora)) ultimos.set(clave, t);
    });

  return [...ultimos.values()]
    .filter((t) => new Date(t.fechaHora) < hoy)
    .map((t) => {
      const proximo = new Date(t.fechaHora);
      proximo.setMonth(proximo.getMonth() + t.repetirCadaMeses);
      return { turno: t, proximo, dias: diasHasta(fechaISO(proximo), hoy) };
    })
    .filter((c) => c.dias <= 30);
}

function ParaAtender({ atrasadas, soloAdministra }) {
  const { personas, medicamentos, turnosOrdenados, hoy, cambiarVista, acciones } = usePanel();
  const avisos = [];

  if (atrasadas > 0) {
    avisos.push({
      clave: "tomas",
      urgente: true,
      texto: soloAdministra
        ? `${plural(atrasadas, "toma")} de hoy sin registrar por tu equipo`
        : `${plural(atrasadas, "toma")} de hoy sin registrar`,
      vista: soloAdministra ? "grupo" : "medicacion",
    });
  }

  medicamentos
    .filter((m) => m.activo && m.vencimientoReceta)
    .forEach((m) => {
      const dias = diasHasta(m.vencimientoReceta, hoy);
      if (dias > 15) return;
      avisos.push({
        clave: `receta-${m.id}`,
        urgente: dias <= 3,
        texto:
          dias < 0
            ? `Receta vencida: ${m.nombre} (${primerNombre(m.personaAsistida)})`
            : `La receta de ${m.nombre} (${primerNombre(m.personaAsistida)}) vence ${dias === 0 ? "hoy" : dias === 1 ? "mañana" : `en ${dias} días`}`,
        vista: "medicacion",
      });
    });

  personas
    .filter((p) => p.vencimientoCud)
    .forEach((p) => {
      const dias = diasHasta(p.vencimientoCud, hoy);
      if (dias > 90) return;
      avisos.push({
        clave: `cud-${p.id}`,
        urgente: dias <= 30,
        texto:
          dias < 0
            ? `El CUD de ${primerNombre(p.nombreCompleto)} está vencido`
            : `El CUD de ${primerNombre(p.nombreCompleto)} vence el ${formatearFechaCorta(p.vencimientoCud)}`,
        vista: "tramites",
      });
    });

  controlesPendientes(turnosOrdenados, hoy).forEach(({ turno: t, proximo, dias }) => {
    const quien = primerNombre(t.personaAsistida?.nombreCompleto ?? "");
    avisos.push({
      clave: `control-${t.id}`,
      urgente: dias < 0,
      texto:
        dias < 0
          ? `Ya tocaba repetir «${t.titulo}» de ${quien}: pedí turno`
          : `El ${formatearFechaCorta(proximo)} toca repetir «${t.titulo}» de ${quien}: pedí turno`,
      accion: () =>
        acciones.nuevoTurno("", {
          titulo: t.titulo,
          especialidad: t.especialidad || "",
          profesional: t.profesional || "",
          lugar: t.lugar || "",
          repetirCadaMeses: String(t.repetirCadaMeses),
          personaAsistidaId: String(t.personaAsistidaId),
        }),
    });
  });

  if (avisos.length === 0) return null;

  return (
    <section className="para-atender" aria-labelledby="titulo-atender">
      <h2 id="titulo-atender" className="rotulo">Para atender</h2>
      <ul>
        {avisos.map((a) => (
          <li key={a.clave}>
            <button type="button" className={`aviso-dia ${a.urgente ? "urgente" : ""}`} onClick={() => (a.accion ? a.accion() : cambiarVista(a.vista))}>
              <span className="aviso-marca" aria-hidden="true" />
              <span>{a.texto}</span>
              <Icono nombre="derecha" tamano={16} />
            </button>
          </li>
        ))}
      </ul>
    </section>
  );
}

// Para quien administra pero no registra (la dueña de una residencia): cómo está la toma y quién la tiene a cargo
function EstadoToma({ toma, pasado, cuidadores = [] }) {
  const aCargo = cuidadores.length ? cuidadores.map((n) => n.split(" ")[0]).join(", ") : null;
  let estado;
  if (toma.estado) {
    estado = (
      <span className="toma-registro">
        {toma.estado === "Tomada" ? "Tomada" : "No se tomó"}
        {toma.registradoPor ? ` · lo registró ${toma.registradoPor.split(" ")[0]}` : ""}
      </span>
    );
  } else if (pasado) {
    estado = <span className="toma-observador">Sin registrar{aCargo ? ` · a cargo de ${aCargo}` : ""}</span>;
  } else {
    estado = <span className="texto-tenue">Pendiente{aCargo ? ` · a cargo de ${aCargo}` : ""}</span>;
  }

  return (
    <div className="toma-ajena">
      <span className="toma-check" aria-hidden="true">
        {toma.estado === "Tomada" && <Icono nombre="check" tamano={13} />}
        {toma.estado === "Omitida" && <Icono nombre="cerrar" tamano={12} />}
      </span>
      <span>
        <strong>{toma.nombre}</strong> {toma.dosis && <span className="texto-tenue">{toma.dosis}</span>}
        <br />
        {estado}
      </span>
    </div>
  );
}

// La línea de tiempo de hoy: turnos y tomas en orden, con una marca de "ahora"
function TuDia({ turnosHoy, tomas, alRegistrar, registraEn }) {
  const { hoy, cambiarVista, acciones, personas } = usePanel();
  const ahora = horaDe(hoy);

  const items = [
    ...turnosHoy.map((t) => ({ tipo: "turno", hora: horaDe(t.fechaHora), turno: t })),
    ...Object.values(
      tomas.reduce((grupos, t) => {
        (grupos[t.horario] ||= { tipo: "tomas", hora: t.horario, tomas: [] }).tomas.push(t);
        return grupos;
      }, {})
    ),
  ].sort((a, b) => a.hora.localeCompare(b.hora));

  const indiceAhora = items.findIndex((i) => i.hora > ahora);
  const posicionAhora = indiceAhora === -1 ? items.length : indiceAhora;

  if (items.length === 0) {
    return (
      <div className="dia-vacio">
        <p>No hay turnos ni medicación para hoy.</p>
        <div className="dia-vacio-acciones">
          <Boton variante="secundario" icono="calendario" onClick={() => acciones.nuevoTurno()}>
            Agendar un turno
          </Boton>
          <Boton variante="secundario" icono="pastilla" onClick={() => cambiarVista("medicacion")}>
            Cargar medicación
          </Boton>
        </div>
      </div>
    );
  }

  const marcaAhora = (
    <li className="ahora" key="ahora" aria-label={`Ahora, ${ahora}`}>
      <span className="ahora-hora mono">{ahora}</span>
      <span className="ahora-linea" aria-hidden="true" />
    </li>
  );

  return (
    <ol className="linea-dia">
      {items.flatMap((item, i) => {
        const pasado = i < posicionAhora;
        const nodo =
          item.tipo === "turno" ? (
            <li key={`t-${item.turno.id}`} className={`momento momento-turno ${pasado ? "pasado" : ""}`}>
              <span className="momento-hora mono">{item.hora}</span>
              <span className="momento-punto" data-color={colorPersona(item.turno.personaAsistidaId)} aria-hidden="true" />
              <div className="momento-cuerpo">
                <span className="momento-tipo">Turno · {item.turno.personaAsistida?.nombreCompleto}</span>
                <strong>{item.turno.titulo}</strong>
                {(item.turno.profesional || item.turno.lugar) && (
                  <span className="secundario">{[item.turno.profesional, item.turno.lugar].filter(Boolean).join(" · ")}</span>
                )}
              </div>
            </li>
          ) : (
            <li key={`m-${item.hora}`} className={`momento momento-tomas ${pasado ? "pasado" : ""}`}>
              <span className="momento-hora mono">{item.hora}</span>
              <span className="momento-punto momento-punto-medicacion" aria-hidden="true" />
              <div className="momento-cuerpo">
                <span className="momento-tipo">Medicación</span>
                <ul className="tomas-rapidas">
                  {item.tomas.map((t) => (
                    <li key={t.medicamentoId} className={t.estado ? `toma-${t.estado.toLowerCase()}` : ""}>
                      {!registraEn(t.personaAsistidaId) ? (
                        <EstadoToma toma={t} pasado={pasado} cuidadores={personas.find((p) => p.id === t.personaAsistidaId)?.cuidadores} />
                      ) : (
                      <button
                        type="button"
                        className="toma-rapida"
                        aria-pressed={t.estado === "Tomada"}
                        disabled={!registraEn(t.personaAsistidaId)}
                        onClick={() => alRegistrar(t)}
                        title={t.estado === "Tomada" ? "Marcar como pendiente" : "Marcar como tomada"}
                      >
                        <span className="toma-check" aria-hidden="true">
                          {t.estado === "Tomada" && <Icono nombre="check" tamano={13} />}
                          {t.estado === "Omitida" && <Icono nombre="cerrar" tamano={12} />}
                        </span>
                        <span>
                          <strong>{t.nombre}</strong> {t.dosis && <span className="texto-tenue">{t.dosis}</span>}
                        </span>
                      </button>
                      )}
                      <span className="avatar avatar-mini" data-color={colorPersona(t.personaAsistidaId)} title={t.personaAsistida}>
                        {iniciales(t.personaAsistida)}
                      </span>
                    </li>
                  ))}
                </ul>
              </div>
            </li>
          );
        return i === posicionAhora ? [marcaAhora, nodo] : [nodo];
      })}
      {posicionAhora === items.length && marcaAhora}
    </ol>
  );
}

export default function VistaInicio() {
  const { personas, turnosOrdenados, documentos, medicamentos, cargando, hoy, nombreUsuario, acciones, cambiarVista, avisar } =
    usePanel();

  const [agenda, setAgenda] = useState(null);
  const hayMedicacion = medicamentos.some((m) => m.activo);
  const dia = fechaISO(hoy);

  const cargarAgenda = useCallback(
    () =>
      api
        .obtener(`/Medicamentos/agenda?fecha=${dia}`)
        .then((a) => setAgenda({ ...a, dia }))
        .catch(() => setAgenda({ tomas: [], dia })),
    [dia]
  );

  useEffect(() => {
    if (!hayMedicacion) return;
    let activo = true;
    api
      .obtener(`/Medicamentos/agenda?fecha=${dia}`)
      .then((a) => activo && setAgenda({ ...a, dia }))
      .catch(() => activo && setAgenda({ tomas: [], dia }));
    return () => {
      activo = false;
    };
  }, [hayMedicacion, dia]);

  const tomas = hayMedicacion && agenda?.dia === dia ? agenda.tomas : [];

  const registrar = async (toma) => {
    const estado = toma.estado === "Tomada" ? null : "Tomada";
    setAgenda((a) => ({
      ...a,
      tomas: a.tomas.map((t) =>
        t.medicamentoId === toma.medicamentoId && t.horario === toma.horario ? { ...t, estado } : t
      ),
    }));
    try {
      await api.actualizar(`/Medicamentos/${toma.medicamentoId}/tomas`, { fecha: `${dia}T00:00:00`, horario: toma.horario, estado });
    } catch (error) {
      avisar(error.message, "error");
      cargarAgenda();
    }
  };

  const inicioDeHoy = new Date(hoy);
  inicioDeHoy.setHours(0, 0, 0, 0);
  const finDeHoy = new Date(inicioDeHoy);
  finDeHoy.setDate(finDeHoy.getDate() + 1);

  const turnosHoy = turnosOrdenados.filter((t) => {
    const f = new Date(t.fechaHora);
    return f >= inicioDeHoy && f < finDeHoy;
  });
  const proximos = turnosOrdenados.filter((t) => new Date(t.fechaHora) >= finDeHoy).slice(0, 4);

  const ahora = horaDe(hoy);
  // la dueña de una clínica administra a sus pacientes pero no los cuida ella
  const propias = personas.filter((p) => p.esTitular !== false);
  const soloAdministra = propias.length > 0 && propias.length === personas.length && propias.every((p) => p.puedeRegistrar === false);
  const conGrupo = propias.some((p) => planIncluye(p, "grupo"));

  const atrasadas = tomas.filter((t) => !t.estado && t.horario <= ahora).length;
  const pendientesHoy = tomas.filter((t) => !t.estado).length;

  const resumenDia = [
    turnosHoy.length > 0 && plural(turnosHoy.length, "turno"),
    tomas.length > 0 && `${plural(tomas.length, "toma")} de medicación`,
  ].filter(Boolean);

  const fecha = new Date(hoy);
  const diaSemana = fecha.toLocaleDateString("es-AR", { weekday: "long" });
  const mes = fecha.toLocaleDateString("es-AR", { month: "long" });

  return (
    <>
      <header className="portada-dia">
        <div className="portada-fecha" aria-hidden="true">
          <span>{diaSemana}</span>
          <strong>{fecha.getDate()}</strong>
          <span>{mes}</span>
        </div>
        <div className="portada-texto">
          <p className="rotulo">
            {saludoDelDia(hoy)}, {primerNombre(nombreUsuario)}
          </p>
          <h1>
            {cargando
              ? "Preparando tu día…"
              : resumenDia.length > 0
                ? `Hoy hay ${resumenDia.join(" y ")}.`
                : "Hoy no hay nada agendado."}
          </h1>
          {pendientesHoy > 0 && !cargando && (
            <p className="portada-detalle">
              {soloAdministra ? "Tu equipo tiene" : "Quedan"} {plural(pendientesHoy, "toma")} por registrar
              {atrasadas > 0 ? `, ${atrasadas} ya pasaron su horario` : ""}.
            </p>
          )}
        </div>
        <div className="encabezado-acciones">
          {personas.some((p) => puedeRegistrar(p) && planIncluye(p, "diario")) && (
            <Boton variante="secundario" icono="diario" onClick={() => cambiarVista("diario")}>
              Anotar en el diario
            </Boton>
          )}
          {personas.some(puedeGestionar) && (
            <Boton icono="mas" onClick={() => acciones.nuevoTurno()}>
              Nuevo turno
            </Boton>
          )}
        </div>
      </header>

      {cargando ? (
        <Cargando texto="Cargando tu información..." />
      ) : personas.length === 0 ? (
        <section className="bienvenida">
          <EstadoVacio
            icono="corazon"
            titulo="Empecemos por la persona que cuidás"
            accion={
              <Boton icono="mas" onClick={acciones.nuevaPersona}>
                Agregar persona
              </Boton>
            }
          >
            Con su perfil armamos la guía de trámites, las recomendaciones y la agenda de medicación.
          </EstadoVacio>
        </section>
      ) : (
        <div className="rejilla-inicio">
          <section className="columna-dia" aria-labelledby="titulo-dia">
            <div className="encabezado-seccion">
              <h2 id="titulo-dia">Tu día</h2>
              <button type="button" className="enlace-accion" onClick={() => cambiarVista("calendario")}>
                Calendario <Icono nombre="derecha" tamano={14} />
              </button>
            </div>
            {hayMedicacion && !agenda ? <Cargando /> : <TuDia
                turnosHoy={turnosHoy}
                tomas={tomas}
                alRegistrar={registrar}
                registraEn={(id) => puedeRegistrar(personas.find((p) => p.id === id))}
              />}

            <div className="encabezado-seccion">
              <h2>Próximos días</h2>
            </div>
            {proximos.length === 0 ? (
              <p className="texto-tenue">No hay más turnos agendados.</p>
            ) : (
              <ul className="proximos">
                {proximos.map((t) => (
                  <li key={t.id}>
                    <span className="proximo-dia capitalizar">{etiquetaDia(t.fechaHora, hoy)}</span>
                    <span className="mono texto-tenue">{formatearHora(t.fechaHora)}</span>
                    <span className="proximo-titulo">
                      <span className="punto" data-color={colorPersona(t.personaAsistidaId)} aria-hidden="true" />
                      {t.titulo}
                    </span>
                    <span className="texto-tenue proximo-persona">{primerNombre(t.personaAsistida?.nombreCompleto)}</span>
                  </li>
                ))}
              </ul>
            )}
          </section>

          <aside className="columna-lateral-inicio">
            <ParaAtender atrasadas={atrasadas} soloAdministra={soloAdministra} />

            <section aria-labelledby="titulo-personas">
              <div className="encabezado-seccion">
                <h2 id="titulo-personas" className="rotulo">
                  {soloAdministra ? "Personas a cargo" : personas.length === 1 ? "A quien cuidás" : "A quienes cuidás"}
                </h2>
                <button type="button" className="enlace-accion" onClick={acciones.nuevaPersona}>
                  <Icono nombre="mas" tamano={14} /> Agregar
                </button>
              </div>
              <ul className="lista lista-personas">
                {personas.map((persona) => (
                  <TarjetaPersona key={persona.id} persona={persona} hoy={hoy} acciones={acciones} />
                ))}
              </ul>
            </section>

            {conGrupo && (
              <button type="button" className="enlace-bloque" onClick={() => cambiarVista("grupo")}>
                <Icono nombre="comunidad" tamano={18} />
                <span>
                  <strong>Tu grupo</strong>
                  <span>Quién cuida a cada persona y qué registró cada uno.</span>
                </span>
                <Icono nombre="derecha" tamano={16} />
              </button>
            )}

            {personas.some((p) => planIncluye(p, "recursos")) && (
              <button type="button" className="enlace-bloque" onClick={() => cambiarVista("recomendaciones")}>
                <Icono nombre="estrella" tamano={18} />
                <span>
                  <strong>Recursos para su situación</strong>
                  <span>Según el perfil de cada persona.</span>
                </span>
                <Icono nombre="derecha" tamano={16} />
              </button>
            )}

            {documentos.length > 0 && (
              <section aria-labelledby="titulo-documentos">
                <div className="encabezado-seccion">
                  <h2 id="titulo-documentos" className="rotulo">Últimos documentos</h2>
                  <button type="button" className="enlace-accion" onClick={() => cambiarVista("documentos")}>
                    Ver todos
                  </button>
                </div>
                <ul className="lista">
                  {documentos.slice(0, 3).map((documento) => (
                    <li className="fila-documento" key={documento.id}>
                      <span className={`tipo-archivo tipo-${extensionArchivo(documento.nombreOriginal).toLowerCase()}`}>
                        {extensionArchivo(documento.nombreOriginal)}
                      </span>
                      <div className="fila-datos">
                        <strong className="truncar">{documento.nombreOriginal}</strong>
                        <span className="secundario">
                          {documento.personaAsistida?.nombreCompleto} · {formatearFechaCorta(documento.fechaCarga)}
                        </span>
                      </div>
                    </li>
                  ))}
                </ul>
              </section>
            )}
          </aside>
        </div>
      )}
    </>
  );
}
