import { useCallback, useEffect, useState } from "react";
import { api } from "../api";
import { Boton, BotonIcono, Cargando, EncabezadoVista, EstadoVacio, Icono, Tarjeta } from "../componentes";
import { usePanel } from "../contexto";
import {
  colorPersona,
  diasHasta,
  fechaISO,
  formatearFechaCorta,
  formatearFechaLarga,
  iniciales,
  momentoDelDia,
  puedeGestionar,
  puedeRegistrar,
} from "../utilidades";

function avisoReceta(medicamento, hoy) {
  const dias = diasHasta(medicamento.vencimientoReceta, hoy);
  if (dias === null || !medicamento.activo) return null;
  if (dias < 0) return { clase: "etiqueta-roja", texto: "Receta vencida" };
  if (dias <= 15) return { clase: "etiqueta-naranja", texto: dias === 0 ? "La receta vence hoy" : `Receta: vence en ${dias} días` };
  return null;
}

function AgendaDelDia({ hoy }) {
  const { avisar, personas } = usePanel();
  const registraEn = (personaId) => puedeRegistrar(personas.find((p) => p.id === personaId));
  const [dia, setDia] = useState(() => fechaISO(hoy));
  const [agenda, setAgenda] = useState(null);

  const cargar = useCallback(
    (fecha) =>
      api
        .obtener(`/Medicamentos/agenda?fecha=${fecha}`, "No se pudo cargar la medicación del día.")
        .then((datos) => setAgenda({ ...datos, fechaPedida: fecha }))
        .catch((error) => avisar(error.message, "error")),
    [avisar]
  );

  useEffect(() => {
    let activo = true;
    api
      .obtener(`/Medicamentos/agenda?fecha=${dia}`, "No se pudo cargar la medicación del día.")
      .then((datos) => activo && setAgenda({ ...datos, fechaPedida: dia }))
      .catch((error) => activo && avisar(error.message, "error"));
    return () => {
      activo = false;
    };
  }, [dia, avisar]);

  const esHoy = dia === fechaISO(hoy);
  const moverDia = (delta) => {
    const f = new Date(dia + "T12:00:00");
    f.setDate(f.getDate() + delta);
    setDia(fechaISO(f));
  };

  const registrar = async (toma, estado) => {
    const nuevoEstado = toma.estado === estado ? null : estado;
    // se actualiza en pantalla antes de que conteste el servidor
    setAgenda((a) => ({
      ...a,
      tomas: a.tomas.map((t) =>
        t.medicamentoId === toma.medicamentoId && t.horario === toma.horario
          ? { ...t, estado: nuevoEstado, registradoPor: nuevoEstado ? "vos" : null }
          : t
      ),
    }));
    try {
      await api.actualizar(`/Medicamentos/${toma.medicamentoId}/tomas`, {
        fecha: `${dia}T00:00:00`,
        horario: toma.horario,
        estado: nuevoEstado,
      });
    } catch (error) {
      avisar(error.message, "error");
      cargar(dia);
    }
  };

  const tomas = agenda?.fechaPedida === dia ? agenda.tomas : null;
  const ahora = `${String(hoy.getHours()).padStart(2, "0")}:${String(hoy.getMinutes()).padStart(2, "0")}`;
  const pendientes = tomas?.filter((t) => !t.estado).length ?? 0;
  const horarios = tomas ? [...new Set(tomas.map((t) => t.horario))] : [];

  return (
    <Tarjeta
      className="tarjeta-agenda"
      titulo={esHoy ? "Hoy" : formatearFechaLarga(new Date(dia + "T12:00:00"))}
      descripcion={
        tomas && tomas.length > 0
          ? pendientes === 0
            ? "Todas las tomas registradas."
            : `${pendientes} ${pendientes === 1 ? "toma sin registrar" : "tomas sin registrar"}.`
          : undefined
      }
      accion={
        <div className="navegacion-dia">
          <BotonIcono icono="izquierda" etiqueta="Día anterior" onClick={() => moverDia(-1)} />
          {!esHoy && (
            <button type="button" className="enlace-accion" onClick={() => setDia(fechaISO(hoy))}>
              Hoy
            </button>
          )}
          <BotonIcono icono="derecha" etiqueta="Día siguiente" onClick={() => moverDia(1)} disabled={esHoy} />
        </div>
      }
    >
      {!tomas ? (
        <Cargando />
      ) : tomas.length === 0 ? (
        <EstadoVacio icono="pastilla" titulo="No hay medicación programada">
          Cargá los medicamentos de cada persona y acá vas a ver qué toca en cada horario.
        </EstadoVacio>
      ) : (
        <ol className="agenda-tomas">
          {horarios.flatMap((horario, i) => {
            const delHorario = tomas.filter((t) => t.horario === horario);
            const atrasado = esHoy && horario < ahora && delHorario.some((t) => !t.estado);
            // separador al cambiar de momento del día, como en un pastillero
            const momento = momentoDelDia(horario);
            const separador =
              i === 0 || momentoDelDia(horarios[i - 1]) !== momento ? (
                <li key={`m-${momento}`} className="momento-separador rotulo">
                  {momento}
                </li>
              ) : null;
            const bloque = (
              <li key={horario} className={`bloque-horario ${atrasado ? "atrasado" : ""}`}>
                <span className="hora">{horario}</span>
                <ul>
                  {delHorario.map((toma) => (
                    <li
                      key={toma.medicamentoId}
                      className={`toma ${toma.estado ? `toma-${toma.estado.toLowerCase()}` : ""}`}
                      data-color={colorPersona(toma.personaAsistidaId)}
                    >
                      <div className="toma-datos">
                        <strong>{toma.nombre}</strong>
                        <span className="secundario">
                          {[toma.dosis, toma.personaAsistida, toma.indicaciones].filter(Boolean).join(" · ")}
                        </span>
                        {toma.estado && toma.registradoPor && (
                          <span className="toma-registro">
                            {toma.estado === "Tomada" ? "Tomada" : "No se tomó"} · lo registró {toma.registradoPor}
                          </span>
                        )}
                      </div>
                      {registraEn(toma.personaAsistidaId) ? (
                      <div className="toma-acciones" role="group" aria-label={`Registrar ${toma.nombre} de las ${horario}`}>
                        <button
                          type="button"
                          className={`boton-toma ${toma.estado === "Tomada" ? "activo" : ""}`}
                          aria-pressed={toma.estado === "Tomada"}
                          onClick={() => registrar(toma, "Tomada")}
                        >
                          <Icono nombre="check" tamano={15} /> Tomada
                        </button>
                        <button
                          type="button"
                          className={`boton-toma omitir ${toma.estado === "Omitida" ? "activo" : ""}`}
                          aria-pressed={toma.estado === "Omitida"}
                          onClick={() => registrar(toma, "Omitida")}
                        >
                          No se tomó
                        </button>
                      </div>
                      ) : (
                        !toma.estado && <span className="toma-observador">Sin registrar</span>
                      )}
                    </li>
                  ))}
                </ul>
              </li>
            );
            return separador ? [separador, bloque] : [bloque];
          })}
        </ol>
      )}
    </Tarjeta>
  );
}

export default function VistaMedicacion() {
  const { personas, medicamentos, cargando, hoy, acciones } = usePanel();
  const [filtro, setFiltro] = useState("");

  if (cargando) return <Cargando />;

  if (personas.length === 0) {
    return (
      <>
        <EncabezadoVista titulo="Medicación" />
        <Tarjeta>
          <EstadoVacio
            icono="pastilla"
            titulo="Primero agregá una persona asistida"
            accion={
              <Boton icono="mas" onClick={acciones.nuevaPersona}>
                Agregar persona
              </Boton>
            }
          />
        </Tarjeta>
      </>
    );
  }

  const visibles = medicamentos.filter((m) => !filtro || m.personaAsistidaId === Number(filtro));
  const activos = visibles.filter((m) => m.activo);
  const suspendidos = visibles.filter((m) => !m.activo);

  const gestiona = personas.some(puedeGestionar);
  const fila = (m) => {
    const aviso = avisoReceta(m, hoy);
    const editable = puedeGestionar(personas.find((p) => p.id === m.personaAsistidaId));
    return (
      <li key={m.id} className={`fila-medicamento ${m.activo ? "" : "suspendido"}`}>
        <div className="avatar" data-color={colorPersona(m.personaAsistidaId)}>{iniciales(m.personaAsistida)}</div>
        <div className="fila-datos">
          <strong>{m.nombre}</strong>
          <span className="secundario">{[m.dosis, m.personaAsistida].filter(Boolean).join(" · ")}</span>
          <span className="horarios-lista">
            {m.horarios.split(",").map((h) => (
              <span key={h} className="mono">
                {h}
              </span>
            ))}
            {aviso && <span className={`etiqueta ${aviso.clase}`}>{aviso.texto}</span>}
          </span>
          {(m.indicaciones || m.medico || m.vencimientoReceta) && (
            <span className="secundario">
              {[m.indicaciones, m.medico && `Indicó ${m.medico}`, m.vencimientoReceta && `Receta hasta el ${formatearFechaCorta(m.vencimientoReceta)}`]
                .filter(Boolean)
                .join(" · ")}
            </span>
          )}
        </div>
        {editable && (
          <div className="fila-acciones">
            <BotonIcono icono="editar" etiqueta={`Editar ${m.nombre}`} onClick={() => acciones.editarMedicamento(m)} />
            <BotonIcono icono="papelera" etiqueta={`Eliminar ${m.nombre}`} variante="peligro" onClick={() => acciones.eliminarMedicamento(m)} />
          </div>
        )}
      </li>
    );
  };

  return (
    <>
      <EncabezadoVista titulo="Medicación" descripcion="Qué toca hoy, quién lo dio y cuándo vencen las recetas.">
        {gestiona && (
          <Boton icono="mas" onClick={() => acciones.nuevoMedicamento(filtro ? Number(filtro) : null)}>
            Agregar medicamento
          </Boton>
        )}
      </EncabezadoVista>

      <div className="rejilla-medicacion">
        <AgendaDelDia hoy={hoy} />

        <Tarjeta
          titulo="Medicamentos"
          accion={
            personas.length > 1 && (
              <select className="selector-chico" value={filtro} onChange={(e) => setFiltro(e.target.value)} aria-label="Filtrar por persona">
                <option value="">Todas las personas</option>
                {personas.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.nombreCompleto}
                  </option>
                ))}
              </select>
            )
          }
        >
          {visibles.length === 0 ? (
            <EstadoVacio
              icono="pastilla"
              titulo="Todavía no cargaste medicamentos"
              accion={
                gestiona && (
                  <Boton variante="secundario" icono="mas" onClick={() => acciones.nuevoMedicamento(filtro ? Number(filtro) : null)}>
                    Agregar el primero
                  </Boton>
                )
              }
            />
          ) : (
            <>
              <ul className="lista">{activos.map(fila)}</ul>
              {suspendidos.length > 0 && (
                <>
                  <h3 className="subtitulo-lista">Suspendidos</h3>
                  <ul className="lista">{suspendidos.map(fila)}</ul>
                </>
              )}
            </>
          )}
        </Tarjeta>
      </div>

      <p className="nota-pie">
        <Icono nombre="info" tamano={14} />
        Esta lista es para organizarte: no cambies dosis ni horarios sin hablarlo con el médico tratante.
      </p>
    </>
  );
}
