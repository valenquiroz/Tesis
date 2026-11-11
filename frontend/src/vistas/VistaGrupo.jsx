import { useCallback, useEffect, useMemo, useState } from "react";
import { api } from "../api";
import { Boton, Campo, Cargando, EncabezadoVista, EstadoVacio, Icono, Modal, Tarjeta } from "../componentes";
import { usePanel } from "../contexto";
import { colorPersona, formatearHora, haceCuanto, iniciales, PLANES, planQueIncluye, ROLES_EQUIPO } from "../utilidades";
import { AvisoPlan } from "./comunes";

// El grupo del dueño de la cuenta (una familia o una clínica): sus personas, quiénes las
// cuidan o miran, y todo lo que registra cada uno. Se mira por persona o por quien ayuda,
// con buscador y selector para que sirva igual con dos personas que con treinta.
// Equipo: lo registrado y lo que quedó sin registrar en la última semana.
// Plus: además el cumplimiento de cada cuidador (tarde, porcentaje) y períodos más largos.

const PERIODOS = [7, 14, 30];

const TIPOS = [
  { id: "todo", texto: "Todo" },
  { id: "Toma", texto: "Medicación", icono: "pastilla" },
  { id: "Diario", texto: "Diario", icono: "diario" },
  { id: "Documento", texto: "Documentos", icono: "documento" },
];

const ICONO_TIPO = { Toma: "pastilla", Diario: "diario", Documento: "documento" };

// la lista se ve con buscador a partir de este tamaño
const CON_BUSCADOR = 6;

// "2026-10-19..." → "19/10"
const diaMes = (fecha) => `${String(fecha).slice(8, 10)}/${String(fecha).slice(5, 7)}`;

const DIAS_SEMANA = ["domingo", "lunes", "martes", "miércoles", "jueves", "viernes", "sábado"];
const nombreDia = (fecha) => {
  const [a, m, d] = String(fecha).slice(0, 10).split("-").map(Number);
  const dia = new Date(a, m - 1, d);
  const hoy = new Date();
  hoy.setHours(0, 0, 0, 0);
  const diferencia = Math.round((hoy - dia) / 86400000);
  if (diferencia === 0) return "Hoy";
  if (diferencia === 1) return "Ayer";
  return `${DIAS_SEMANA[dia.getDay()][0].toUpperCase()}${DIAS_SEMANA[dia.getDay()].slice(1)} ${diaMes(fecha)}`;
};

const porcentaje = (parte, total) => (total === 0 ? null : Math.round((parte / total) * 100));

const normalizar = (texto = "") =>
  texto
    .normalize("NFD")
    .replace(/[̀-ͯ]/g, "")
    .toLowerCase();

function Estado({ valor }) {
  if (valor === null) return <span className="etiqueta etiqueta-suave">Sin medicación</span>;
  if (valor >= 95) return <span className="etiqueta etiqueta-verde">Al día</span>;
  if (valor >= 80) return <span className="etiqueta etiqueta-naranja">Revisar</span>;
  return <span className="etiqueta etiqueta-roja">Atención</span>;
}

function Chips({ opciones, valor, alCambiar, etiqueta }) {
  return (
    <div className="chips" role="group" aria-label={etiqueta}>
      {opciones.map((o) => (
        <button key={o.id} type="button" className={`chip ${valor === o.id ? "activo" : ""}`} aria-pressed={valor === o.id} onClick={() => alCambiar(o.id)}>
          {o.texto}
        </button>
      ))}
    </div>
  );
}

// ------------------------------------------------------------------
// Lo registrado: tomas, diario y documentos, agrupado por día
// ------------------------------------------------------------------

function ListaRegistros({ registros, mostrar, supervision }) {
  const [tipo, setTipo] = useState("todo");
  const [filtro, setFiltro] = useState("todos");

  // los filtros salen de lo que hay: personas (si se mira a un cuidador) o autores (si se mira a una persona)
  const opcionesFiltro = useMemo(() => {
    const clave = mostrar === "persona" ? "personaId" : "autorId";
    const nombre = mostrar === "persona" ? "persona" : "autor";
    const vistos = new Map();
    registros.forEach((r) => vistos.set(String(r[clave]), r[nombre]));
    return [...vistos].map(([id, texto]) => ({ id, texto }));
  }, [registros, mostrar]);

  const filtroActivo = opcionesFiltro.some((o) => o.id === filtro) ? filtro : "todos";
  const visibles = registros.filter(
    (r) =>
      (tipo === "todo" || r.tipo === tipo) &&
      (filtroActivo === "todos" || String(mostrar === "persona" ? r.personaId : r.autorId) === filtroActivo)
  );

  const porDia = [];
  visibles.forEach((r) => {
    const dia = r.momento.slice(0, 10);
    const grupo = porDia.at(-1);
    if (grupo?.dia === dia) grupo.items.push(r);
    else porDia.push({ dia, items: [r] });
  });

  return (
    <>
      <div className="filtros-registros">
        <Chips opciones={TIPOS} valor={tipo} alCambiar={setTipo} etiqueta="Tipo de registro" />
        {opcionesFiltro.length > 1 && (
          <select value={filtroActivo} onChange={(e) => setFiltro(e.target.value)} aria-label={mostrar === "persona" ? "Filtrar por persona" : "Filtrar por quién lo registró"}>
            <option value="todos">{mostrar === "persona" ? "Todas las personas" : "Todos los que registraron"}</option>
            {opcionesFiltro.map((o) => (
              <option key={o.id} value={o.id}>
                {o.texto}
              </option>
            ))}
          </select>
        )}
      </div>

      {porDia.length === 0 ? (
        <p className="secundario">No hay registros {tipo === "todo" ? "" : "de este tipo "}en este período.</p>
      ) : (
        <ol className="registros-dias">
          {porDia.map((grupo) => (
            <li key={grupo.dia}>
              <h4 className="registros-dia">{nombreDia(grupo.dia)}</h4>
              <ul className="registros">
                {grupo.items.map((r, i) => (
                  <li key={`${r.tipo}-${r.momento}-${r.personaId}-${i}`} className={`registro registro-${r.tipo.toLowerCase()}`}>
                    <span className="registro-hora mono">{r.momento.slice(11, 16)}</span>
                    <span className="registro-icono" aria-hidden="true">
                      <Icono nombre={ICONO_TIPO[r.tipo]} tamano={15} />
                    </span>
                    <div className="registro-texto">
                      <strong>
                        {r.titulo}
                        {r.tipo === "Toma" && (
                          <span className={`etiqueta ${r.estado === "Tomada" ? "etiqueta-verde" : "etiqueta-naranja"}`}>
                            {r.estado === "Tomada" ? "Tomada" : "No se tomó"}
                          </span>
                        )}
                      </strong>
                      {r.detalle && <span className={r.tipo === "Diario" ? "registro-cita" : "secundario"}>{r.detalle}</span>}
                      <span className="registro-autor">
                        {mostrar === "persona" ? r.persona : r.autor} · anotado {formatearHora(r.anotado)}
                        {supervision && r.tipo === "Toma" && r.demoraMinutos > 60 && (
                          <span className="texto-alerta"> ({Math.round(r.demoraMinutos / 60)} h tarde)</span>
                        )}
                      </span>
                    </div>
                  </li>
                ))}
              </ul>
            </li>
          ))}
        </ol>
      )}
    </>
  );
}

function SinRegistrar({ tomas, mostrarPersona }) {
  const [todas, setTodas] = useState(false);
  if (tomas.length === 0) return null;
  const visibles = todas ? tomas : tomas.slice(0, 5);

  return (
    <section className="sin-registrar">
      <h3 className="rotulo">
        <Icono nombre="alerta" tamano={14} /> {tomas.length === 1 ? "1 toma sin registrar" : `${tomas.length} tomas sin registrar`}
      </h3>
      <ul className="lista-actividad">
        {visibles.map((t) => (
          <li key={`${t.fecha}-${t.horario}-${t.medicamento}-${t.personaId}`}>
            <span className="mono">
              {diaMes(t.fecha)} · {t.horario}
            </span>
            <span>
              {t.medicamento}
              {mostrarPersona ? ` — ${t.persona}` : ""}
            </span>
          </li>
        ))}
      </ul>
      {tomas.length > 5 && (
        <button type="button" className="enlace-accion" onClick={() => setTodas((v) => !v)}>
          {todas ? "Ver menos" : `Ver las ${tomas.length}`}
        </button>
      )}
    </section>
  );
}

// ------------------------------------------------------------------
// Detalle de una persona a cargo
// ------------------------------------------------------------------

function DetallePersona({ personaId, personas, dias, alElegir, alSumar, alVolver }) {
  const { avisar } = usePanel();
  const [datos, setDatos] = useState(null);
  const clave = `${personaId}-${dias}`;

  useEffect(() => {
    let activo = true;
    api
      .obtener(`/Grupo/personas/${personaId}?dias=${dias}`, "No se pudo cargar lo que se registró.")
      .then((d) => activo && setDatos({ ...d, clave: `${personaId}-${dias}` }))
      .catch((error) => activo && avisar(error.message, "error"));
    return () => {
      activo = false;
    };
  }, [personaId, dias, avisar]);

  const listo = datos?.clave === clave;
  const p = listo ? datos : null;

  return (
    <section className="grupo-detalle" aria-live="polite">
      <div className="grupo-detalle-cabecera">
        <button type="button" className="enlace-accion solo-movil" onClick={alVolver}>
          <Icono nombre="izquierda" tamano={14} /> Todas las personas
        </button>
        <div className="grupo-detalle-titulo">
          <div className="avatar avatar-grande" data-color={colorPersona(personaId)}>
            {iniciales(personas.find((x) => x.id === personaId)?.nombreCompleto)}
          </div>
          <div>
            <small className="rotulo">Lo que se registró de</small>
            <select className="selector-persona" value={personaId} onChange={(e) => alElegir(Number(e.target.value))} aria-label="Elegir persona">
              {personas.map((x) => (
                <option key={x.id} value={x.id}>
                  {x.nombreCompleto}
                </option>
              ))}
            </select>
          </div>
        </div>
      </div>

      {!p ? (
        <Cargando />
      ) : (
        <>
          <div className="grupo-equipo">
            {p.integrantes.length === 0 && !p.persona.teCuida ? (
              <span className="secundario">Todavía nadie la cuida.</span>
            ) : (
              <>
                {p.persona.teCuida && <span className="etiqueta etiqueta-verde">Vos la cuidás</span>}
                {p.integrantes.map((i) => (
                  <span key={i.id} className={`etiqueta ${i.rol === "Cuidador" ? "etiqueta-azul" : "etiqueta-suave"}`}>
                    {i.nombre} · {i.rol === "Cuidador" ? "cuida" : "mira"}
                  </span>
                ))}
              </>
            )}
            <button type="button" className="enlace-accion" onClick={() => alSumar(personaId)}>
              <Icono nombre="mas" tamano={14} /> Sumar a alguien
            </button>
          </div>

          <div className="grupo-numeros">
            <div>
              <strong>
                {p.registradas}/{p.programadas}
              </strong>
              <span>tomas registradas</span>
            </div>
            <div>
              <strong>{p.sinRegistrar.length}</strong>
              <span>sin registrar</span>
            </div>
            <div>
              <strong>{p.registros.filter((r) => r.tipo === "Diario").length}</strong>
              <span>en el diario</span>
            </div>
          </div>

          <SinRegistrar tomas={p.sinRegistrar} />

          {p.porCuidador?.length > 0 && (
            <section>
              <h3 className="rotulo">Cada cuidador con esta persona</h3>
              <table className="tabla tabla-compacta">
                <thead>
                  <tr>
                    <th>Cuidador</th>
                    <th>Tomas que anotó</th>
                    <th>Tarde</th>
                    <th>Diario</th>
                  </tr>
                </thead>
                <tbody>
                  {p.porCuidador.map((c) => (
                    <tr key={c.id}>
                      <td data-etiqueta="Cuidador">{c.nombre}</td>
                      <td data-etiqueta="Tomas que anotó">{c.registradas}</td>
                      <td data-etiqueta="Tarde">{c.tarde}</td>
                      <td data-etiqueta="Diario">{c.diario}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </section>
          )}

          <section>
            <h3 className="rotulo">Lo que se registró</h3>
            <ListaRegistros key={personaId} registros={p.registros} mostrar="autor" supervision={p.supervision} />
          </section>
        </>
      )}
    </section>
  );
}

// ------------------------------------------------------------------
// Detalle de quien ayuda
// ------------------------------------------------------------------

function DetalleIntegrante({ integranteId, integrantes, dias, alElegir, alEditar, alQuitar, alVolver }) {
  const { avisar } = usePanel();
  const [datos, setDatos] = useState(null);
  const clave = `${integranteId}-${dias}`;

  useEffect(() => {
    let activo = true;
    api
      .obtener(`/Grupo/integrantes/${integranteId}?dias=${dias}`, "No se pudo cargar lo que registró.")
      .then((d) => activo && setDatos({ ...d, clave: `${integranteId}-${dias}` }))
      .catch((error) => activo && avisar(error.message, "error"));
    return () => {
      activo = false;
    };
  }, [integranteId, dias, avisar]);

  const listo = datos?.clave === clave;
  const d = listo ? datos : null;
  const nombre = integrantes.find((i) => i.id === integranteId)?.nombreCompleto;

  return (
    <section className="grupo-detalle" aria-live="polite">
      <div className="grupo-detalle-cabecera">
        <button type="button" className="enlace-accion solo-movil" onClick={alVolver}>
          <Icono nombre="izquierda" tamano={14} /> Todos los que ayudan
        </button>
        <div className="grupo-detalle-titulo">
          <div className="avatar avatar-grande">{iniciales(nombre)}</div>
          <div>
            <small className="rotulo">Lo que registró</small>
            <select className="selector-persona" value={integranteId} onChange={(e) => alElegir(e.target.value)} aria-label="Elegir quién ayuda">
              {integrantes.map((i) => (
                <option key={i.id} value={i.id}>
                  {i.nombreCompleto}
                </option>
              ))}
            </select>
          </div>
        </div>
      </div>

      {!d ? (
        <Cargando />
      ) : (
        <>
          <div className="grupo-equipo">
            {d.personas.map((p) => (
              <span key={p.id} className={`etiqueta ${p.rol === "Cuidador" ? "etiqueta-azul" : "etiqueta-suave"}`}>
                {p.nombre} · {p.rol === "Cuidador" ? "cuida" : "mira"}
              </span>
            ))}
            <button type="button" className="enlace-accion" onClick={() => alEditar({ ...d.integrante, personas: d.personas })}>
              <Icono nombre="editar" tamano={14} /> Cambiar a quiénes acompaña
            </button>
            <button type="button" className="enlace-accion enlace-peligro" onClick={() => alQuitar(d.integrante)}>
              <Icono nombre="salir" tamano={14} /> Quitar del grupo
            </button>
          </div>

          {d.porPersona?.length > 0 && (
            <section>
              <h3 className="rotulo">Por persona</h3>
              <table className="tabla tabla-compacta">
                <thead>
                  <tr>
                    <th>Persona</th>
                    <th>Registró</th>
                    <th>Sin registrar</th>
                    <th>Tarde</th>
                    <th />
                  </tr>
                </thead>
                <tbody>
                  {d.porPersona.map((p) => (
                    <tr key={p.id}>
                      <td data-etiqueta="Persona">{p.nombre}</td>
                      <td data-etiqueta="Registró">
                        {p.registradasPorCuidador} de {p.programadas}
                        {p.registradasPorOtros > 0 && <span className="texto-tenue"> (+{p.registradasPorOtros} otros)</span>}
                      </td>
                      <td data-etiqueta="Sin registrar">{p.sinRegistrar}</td>
                      <td data-etiqueta="Tarde">{p.tarde}</td>
                      <td>
                        <Estado valor={porcentaje(p.registradasPorCuidador + p.registradasPorOtros, p.programadas)} />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </section>
          )}

          <SinRegistrar tomas={d.sinRegistrar} mostrarPersona />

          <section>
            <h3 className="rotulo">Lo que registró</h3>
            {d.registros.length === 0 && d.personas.every((p) => p.rol === "Observador") ? (
              <p className="secundario">Es observador: mira lo que se registra sin poder cambiarlo.</p>
            ) : (
              <ListaRegistros key={integranteId} registros={d.registros} mostrar="persona" supervision={d.supervision} />
            )}
          </section>
        </>
      )}
    </section>
  );
}

// ------------------------------------------------------------------
// Sumar a alguien, cambiar a quiénes acompaña y el nombre del grupo
// ------------------------------------------------------------------

function ModalAsignacion({ personas, inicial, alCerrar, alGuardar }) {
  const { avisar } = usePanel();
  const editando = Boolean(inicial?.id);
  const [valores, setValores] = useState(() => ({
    email: inicial?.email || "",
    rol: inicial?.personas?.[0]?.rol || "Cuidador",
    personasIds: inicial?.personas?.map((p) => p.id) || inicial?.personasIds || (personas.length === 1 ? [personas[0].id] : []),
  }));
  const [busqueda, setBusqueda] = useState("");
  const [enviando, setEnviando] = useState(false);

  const alternar = (id) =>
    setValores((v) => ({
      ...v,
      personasIds: v.personasIds.includes(id) ? v.personasIds.filter((p) => p !== id) : [...v.personasIds, id],
    }));

  const visibles = personas.filter((p) => normalizar(p.nombreCompleto).includes(normalizar(busqueda)));

  const guardar = async (e) => {
    e.preventDefault();
    if (valores.personasIds.length === 0) {
      avisar("Elegí al menos una persona.", "error");
      return;
    }
    setEnviando(true);
    try {
      const r = editando
        ? await api.actualizar(`/Grupo/integrantes/${inicial.id}`, { rol: valores.rol, personasIds: valores.personasIds })
        : await api.crear("/Grupo/integrantes", { ...valores, email: valores.email.trim() });
      avisar(r.message);
      await alGuardar();
      alCerrar();
    } catch (error) {
      avisar(error.message, "error");
    } finally {
      setEnviando(false);
    }
  };

  return (
    <Modal
      abierto
      alCerrar={alCerrar}
      titulo={editando ? `A quiénes acompaña ${inicial.nombreCompleto}` : "Sumar a alguien al grupo"}
      descripcion={
        editando
          ? "Deja de ver a las personas que destildes. Lo que ya registró queda guardado."
          : "Cada uno entra con su propia cuenta y ve solo a las personas que le asignes."
      }
      pie={
        <>
          <Boton variante="secundario" onClick={alCerrar}>
            Cancelar
          </Boton>
          <Boton type="submit" form="form-asignacion" icono={editando ? "check" : "mas"} cargando={enviando}>
            {editando ? "Guardar" : "Sumar al grupo"}
          </Boton>
        </>
      }
    >
      <form id="form-asignacion" className="formulario" onSubmit={guardar}>
        <div className="grilla-campos">
          {!editando && (
            <Campo etiqueta="Correo" ayuda="Si no tiene cuenta, pedile que se registre primero.">
              <input
                type="email"
                value={valores.email}
                onChange={(e) => setValores((v) => ({ ...v, email: e.target.value }))}
                placeholder="correo@ejemplo.com"
                autoComplete="off"
                required
              />
            </Campo>
          )}
          <Campo etiqueta="Rol" ayuda={ROLES_EQUIPO[valores.rol].ayuda}>
            <select value={valores.rol} onChange={(e) => setValores((v) => ({ ...v, rol: e.target.value }))}>
              {Object.entries(ROLES_EQUIPO).map(([valor, r]) => (
                <option key={valor} value={valor}>
                  {r.texto}
                </option>
              ))}
            </select>
          </Campo>
        </div>

        <fieldset className="elegir-personas">
          <legend>
            ¿A quiénes va a {valores.rol === "Cuidador" ? "cuidar" : "ver"}? ({valores.personasIds.length} de {personas.length})
          </legend>
          {personas.length >= CON_BUSCADOR && (
            <div className="elegir-personas-barra">
              <input type="search" value={busqueda} onChange={(e) => setBusqueda(e.target.value)} placeholder="Buscar persona..." aria-label="Buscar persona" />
              <button type="button" className="enlace-accion" onClick={() => setValores((v) => ({ ...v, personasIds: personas.map((p) => p.id) }))}>
                Todas
              </button>
              <button type="button" className="enlace-accion" onClick={() => setValores((v) => ({ ...v, personasIds: [] }))}>
                Ninguna
              </button>
            </div>
          )}
          <div className="elegir-personas-lista">
            {visibles.map((p) => (
              <label key={p.id} className="opcion-persona">
                <input type="checkbox" checked={valores.personasIds.includes(p.id)} onChange={() => alternar(p.id)} />
                <span>{p.nombreCompleto}</span>
              </label>
            ))}
          </div>
        </fieldset>
      </form>
    </Modal>
  );
}

function ModalNombre({ actual, alCerrar, alGuardar }) {
  const { ejecutar } = usePanel();
  const [nombre, setNombre] = useState(actual || "");
  const [guardando, setGuardando] = useState(false);

  const guardar = async (e) => {
    e.preventDefault();
    setGuardando(true);
    const ok = await ejecutar(() => api.actualizar("/Cuenta/grupo", { nombreGrupo: nombre.trim() }), "Se guardó el nombre del grupo.");
    setGuardando(false);
    if (ok) {
      alGuardar(nombre.trim() || null);
      alCerrar();
    }
  };

  return (
    <Modal
      abierto
      alCerrar={alCerrar}
      titulo="Nombre del grupo"
      descripcion="Es lo que ven quienes ayudan, por ejemplo el nombre de tu clínica o residencia. Si lo dejás vacío ven tu nombre."
      pie={
        <>
          <Boton variante="secundario" onClick={alCerrar}>
            Cancelar
          </Boton>
          <Boton type="submit" form="form-nombre-grupo" cargando={guardando}>
            Guardar
          </Boton>
        </>
      }
    >
      <form id="form-nombre-grupo" className="formulario" onSubmit={guardar}>
        <Campo etiqueta="Nombre (opcional)">
          <input value={nombre} onChange={(e) => setNombre(e.target.value)} maxLength={150} placeholder="Ej.: Residencia Las Glicinas" />
        </Campo>
      </form>
    </Modal>
  );
}

// ------------------------------------------------------------------
// Grupos de otros dueños en los que ayuda (por ejemplo, una cuidadora de una residencia)
// ------------------------------------------------------------------

function DondeAyudas({ grupos, titulo }) {
  const { cambiarVista } = usePanel();
  return (
    <section className="donde-ayudas">
      {titulo && <h2 className="rotulo">{titulo}</h2>}
      {grupos.map((g) => (
        <Tarjeta key={g.duenoId}>
          <div className="donde-ayudas-cabecera">
            <div className="avatar avatar-grande">{iniciales(g.nombre)}</div>
            <div>
              <h3>{g.nombre}</h3>
              <p className="secundario">
                {g.nombre === g.dueno ? "" : `Dueño/a del grupo: ${g.dueno} · `}Plan {PLANES[g.plan]?.nombre ?? g.plan}, lo paga {g.dueno}
              </p>
            </div>
          </div>
          <ul className="donde-ayudas-personas">
            {g.personas.map((p) => (
              <li key={p.id}>
                <div className="avatar" data-color={colorPersona(p.id)}>
                  {iniciales(p.nombre)}
                </div>
                <strong>{p.nombre}</strong>
                <span className={`etiqueta ${p.rol === "Cuidador" ? "etiqueta-azul" : "etiqueta-suave"}`}>
                  {p.rol === "Cuidador" ? "La cuidás" : "La mirás"}
                </span>
              </li>
            ))}
          </ul>
          <p className="nota-pie">
            <Icono nombre="info" tamano={14} />
            Lo que registres queda con tu nombre y {g.dueno} lo ve. Quién más está en el grupo y a quién acompañás lo decide {g.dueno}.
          </p>
          <Boton variante="secundario" icono="inicio" onClick={() => cambiarVista("inicio")}>
            Ir a tu día
          </Boton>
        </Tarjeta>
      ))}
    </section>
  );
}

// ------------------------------------------------------------------

export default function VistaGrupo() {
  const { avisar, ejecutar, confirmar, acciones, nombreGrupo, cambiarNombreGrupo } = usePanel();
  const [grupo, setGrupo] = useState(null);
  const [mirar, setMirar] = useState("personas");
  const [dias, setDias] = useState(7);
  const [personas, setPersonas] = useState(null);
  const [integrantes, setIntegrantes] = useState(null);
  const [personaElegida, setPersonaElegida] = useState(null);
  const [integranteElegido, setIntegranteElegido] = useState(null);
  const [busqueda, setBusqueda] = useState("");
  const [modal, setModal] = useState(null); // { tipo: "asignacion" | "nombre", datos }
  const [version, setVersion] = useState(0);

  const plan = grupo?.plan;
  const conGrupo = Boolean(plan?.grupo);

  useEffect(() => {
    let activo = true;
    api
      .obtener("/Grupo", "No se pudo cargar tu grupo.")
      .then((g) => activo && setGrupo(g))
      .catch((error) => activo && avisar(error.message, "error"));
    return () => {
      activo = false;
    };
  }, [avisar, version]);

  useEffect(() => {
    if (!conGrupo) return;
    let activo = true;
    Promise.all([api.obtener(`/Grupo/personas?dias=${dias}`), api.obtener(`/Grupo/integrantes?dias=${dias}`)])
      .then(([p, i]) => {
        if (!activo) return;
        setPersonas({ ...p, dias });
        setIntegrantes({ ...i, dias });
      })
      .catch((error) => activo && avisar(error.message, "error"));
    return () => {
      activo = false;
    };
  }, [conGrupo, dias, avisar, version]);

  const recargar = useCallback(async () => setVersion((v) => v + 1), []);

  const quitar = (integrante) =>
    confirmar({
      titulo: `¿Quitar a ${integrante.nombreCompleto} del grupo?`,
      mensaje: "Deja de ver a todas las personas del grupo. Lo que registró queda guardado.",
      textoBoton: "Quitar",
      icono: "salir",
      accion: async () => {
        const ok = await ejecutar(() => api.eliminar(`/Grupo/integrantes/${integrante.id}`), "Se quitó a la persona del grupo.");
        if (ok) {
          setIntegranteElegido(null);
          await recargar();
        }
      },
    });

  const encabezado = (
    <EncabezadoVista
      antetitulo={nombreGrupo || undefined}
      titulo="Tu grupo"
      descripcion="Las personas que tenés a cargo, quiénes te ayudan y todo lo que registra cada uno."
    >
      {conGrupo && (
        <>
          <Boton variante="secundario" icono="editar" onClick={() => setModal({ tipo: "nombre" })}>
            Nombre del grupo
          </Boton>
          <Boton icono="mas" onClick={() => setModal({ tipo: "asignacion", datos: null })}>
            Sumar a alguien
          </Boton>
        </>
      )}
    </EncabezadoVista>
  );

  if (!grupo) {
    return (
      <>
        {encabezado}
        <Cargando />
      </>
    );
  }

  if (grupo.personas.length === 0 && grupo.dondeAyuda.length > 0) {
    return (
      <>
        <EncabezadoVista
          titulo={grupo.dondeAyuda.length === 1 ? "Tu grupo" : "Tus grupos"}
          descripcion="Los grupos en los que ayudás y a quiénes cuidás en cada uno. Su plan lo paga quien es dueño del grupo."
        />
        <DondeAyudas grupos={grupo.dondeAyuda} />
      </>
    );
  }

  if (grupo.personas.length === 0) {
    return (
      <>
        {encabezado}
        <Tarjeta>
          <EstadoVacio
            icono="comunidad"
            titulo="Todavía no tenés personas a cargo"
            accion={
              <Boton icono="mas" onClick={acciones.nuevaPersona}>
                Agregar persona
              </Boton>
            }
          >
            Cuando cargues a alguien vas a poder sumar a quienes te ayudan a cuidarlo. Si cuidás en el grupo de otra persona, eso lo
            administra ella.
          </EstadoVacio>
        </Tarjeta>
      </>
    );
  }

  if (!conGrupo) {
    return (
      <>
        {encabezado}
        <AvisoPlan
          titulo={`Para sumar cuidadores, pasate al plan ${planQueIncluye("grupo")}`}
          texto="Con un plan pago sumás a un cuidador contratado, a un familiar que mire o a los empleados de tu clínica, cada uno con su cuenta, y ves todo lo que registra cada uno."
        />
      </>
    );
  }

  const listos = personas?.dias === dias && integrantes?.dias === dias;
  const supervision = plan.supervision;
  const filtrar = (lista, campo) => lista.filter((x) => normalizar(x[campo]).includes(normalizar(busqueda)));

  const listaPersonas = listos ? filtrar(personas.personas, "nombreCompleto") : [];
  const listaIntegrantes = listos ? filtrar(integrantes.integrantes, "nombreCompleto") : [];
  const total = mirar === "personas" ? personas?.personas.length ?? 0 : integrantes?.integrantes.length ?? 0;

  // en computadora siempre hay algo elegido; en el celular, la lista hasta que se toca una fila
  const elegidaPersona = listos && (personas.personas.some((p) => p.id === personaElegida) ? personaElegida : personas.personas[0]?.id);
  const elegidoIntegrante = listos && (integrantes.integrantes.some((i) => i.id === integranteElegido) ? integranteElegido : integrantes.integrantes[0]?.id);
  const hayElegido = mirar === "personas" ? personaElegida != null : integranteElegido != null;

  return (
    <>
      {encabezado}

      <section className="resumen-grupo">
        <div>
          <strong>{grupo.personas.length}</strong>
          <span>{plan.personas == null ? "personas a cargo" : `de ${plan.personas} ${plan.personas === 1 ? "persona" : "personas"} del plan`}</span>
        </div>
        <div>
          <strong>{grupo.integrantes}</strong>
          <span>{plan.integrantes == null ? "personas ayudan" : `de ${plan.integrantes} en el grupo`}</span>
        </div>
      </section>

      <div className="barra-grupo">
        <Chips
          etiqueta="Cómo mirar el grupo"
          opciones={[
            { id: "personas", texto: "Por persona a cargo" },
            { id: "integrantes", texto: "Por quien ayuda" },
          ]}
          valor={mirar}
          alCambiar={(v) => {
            setMirar(v);
            setBusqueda("");
          }}
        />
        {supervision ? (
          <Chips etiqueta="Período" opciones={PERIODOS.map((d) => ({ id: d, texto: `${d} días` }))} valor={dias} alCambiar={setDias} />
        ) : (
          <span className="texto-tenue">Últimos 7 días</span>
        )}
      </div>

      {!listos ? (
        <Cargando />
      ) : (
        <div className={`grupo-maestro ${hayElegido ? "con-elegido" : ""}`}>
          <div className="grupo-lista">
            {total >= CON_BUSCADOR && (
              <input
                type="search"
                value={busqueda}
                onChange={(e) => setBusqueda(e.target.value)}
                placeholder={mirar === "personas" ? "Buscar persona..." : "Buscar quién ayuda..."}
                aria-label="Buscar"
              />
            )}

            {mirar === "personas" ? (
              <ul className="lista-grupo">
                {listaPersonas.map((p) => (
                  <li key={p.id}>
                    <button type="button" className={`fila-grupo ${elegidaPersona === p.id ? "activa" : ""}`} onClick={() => setPersonaElegida(p.id)}>
                      <div className="avatar" data-color={colorPersona(p.id)}>
                        {iniciales(p.nombreCompleto)}
                      </div>
                      <div className="fila-datos">
                        <strong>{p.nombreCompleto}</strong>
                        <span className="secundario">
                          {p.integrantes.length === 0
                            ? p.teCuida
                              ? "La cuidás vos"
                              : "Nadie la cuida todavía"
                            : p.integrantes.map((i) => i.nombre.split(" ")[0]).join(", ")}
                        </span>
                        <span className="fila-grupo-pie">
                          {p.programadas > 0 && (
                            <span>
                              {p.registradas}/{p.programadas} tomas
                            </span>
                          )}
                          {p.ultimo ? <span>{`${p.ultimo.autor?.split(" ")[0]}, ${haceCuanto(p.ultimo.anotado).toLowerCase()}`}</span> : <span>Sin registros</span>}
                        </span>
                      </div>
                      {p.sinRegistrar > 0 && (
                        <span className="etiqueta etiqueta-roja" title="Tomas sin registrar">
                          {p.sinRegistrar}
                        </span>
                      )}
                    </button>
                  </li>
                ))}
                {listaPersonas.length === 0 && <li className="secundario">No hay personas con ese nombre.</li>}
              </ul>
            ) : integrantes.integrantes.length === 0 ? (
              <EstadoVacio icono="comunidad" titulo="Todavía no sumaste a nadie">
                Sumá a quienes cuidan o quieren ver lo que se registra con el botón “Sumar a alguien”.
              </EstadoVacio>
            ) : (
              <ul className="lista-grupo">
                {listaIntegrantes.map((i) => {
                  const a = i.actividad;
                  const valor = a ? porcentaje(a.registradas, a.programadas) : null;
                  const cuida = i.personas.some((p) => p.rol === "Cuidador");
                  return (
                    <li key={i.id}>
                      <button type="button" className={`fila-grupo ${elegidoIntegrante === i.id ? "activa" : ""}`} onClick={() => setIntegranteElegido(i.id)}>
                        <div className="avatar">{iniciales(i.nombreCompleto)}</div>
                        <div className="fila-datos">
                          <strong>{i.nombreCompleto}</strong>
                          <span className="secundario">
                            {cuida ? "Cuida a " : "Mira a "}
                            {i.personas.length === 1 ? i.personas[0].nombre : `${i.personas.length} personas`}
                          </span>
                          <span className="fila-grupo-pie">
                            <span>{i.registros === 1 ? "1 registro" : `${i.registros} registros`}</span>
                            <span>{i.ultimaActividad ? haceCuanto(i.ultimaActividad).toLowerCase() : "sin actividad"}</span>
                          </span>
                        </div>
                        {a && <Estado valor={valor} />}
                      </button>
                    </li>
                  );
                })}
                {listaIntegrantes.length === 0 && <li className="secundario">No hay nadie con ese nombre.</li>}
              </ul>
            )}
          </div>

          {mirar === "personas" && elegidaPersona && (
            <DetallePersona
              key={version}
              personaId={elegidaPersona}
              personas={personas.personas}
              dias={dias}
              alElegir={setPersonaElegida}
              alSumar={(id) => setModal({ tipo: "asignacion", datos: { personasIds: [id] } })}
              alVolver={() => setPersonaElegida(null)}
            />
          )}
          {mirar === "integrantes" && elegidoIntegrante && (
            <DetalleIntegrante
              key={version}
              integranteId={elegidoIntegrante}
              integrantes={integrantes.integrantes}
              dias={dias}
              alElegir={setIntegranteElegido}
              alEditar={(datos) => setModal({ tipo: "asignacion", datos })}
              alQuitar={quitar}
              alVolver={() => setIntegranteElegido(null)}
            />
          )}
        </div>
      )}

      {grupo.dondeAyuda.length > 0 && <DondeAyudas grupos={grupo.dondeAyuda} titulo="También ayudás en" />}

      {!supervision && (
        <p className="nota-pie">
          <Icono nombre="info" tamano={14} />
          Con el plan {planQueIncluye("supervision")} ves además el cumplimiento de cada cuidador (tomas anotadas a tiempo y tarde) y
          podés mirar hasta 30 días.
        </p>
      )}

      {modal?.tipo === "asignacion" && (
        <ModalAsignacion personas={grupo.personas} inicial={modal.datos} alCerrar={() => setModal(null)} alGuardar={recargar} />
      )}
      {modal?.tipo === "nombre" && (
        <ModalNombre
          actual={grupo.nombreGrupo}
          alCerrar={() => setModal(null)}
          alGuardar={(nuevo) => {
            cambiarNombreGrupo(nuevo);
            setGrupo((g) => ({ ...g, nombreGrupo: nuevo }));
          }}
        />
      )}
    </>
  );
}
