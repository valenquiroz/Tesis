import { useCallback, useEffect, useState } from "react";
import { api } from "../api";
import { Boton, BotonIcono, Campo, Cargando, EncabezadoVista, EstadoVacio, Modal, Tarjeta } from "../componentes";
import { usePanel } from "../contexto";
import { NECESIDADES, separarLista, TIPOS_DISCAPACIDAD, vacioANull } from "../utilidades";

// Contenido curado de la plataforma: artículos de especialistas y recursos recomendables.
// Antes de cargar algo, revisar que el enlace lleve a la página que describe la tarjeta.

const ARTICULO_VACIO = { titulo: "", autores: "", fuente: "", anio: "", resumen: "", tipoDiscapacidad: "", enlace: "", activo: true };
const RECURSO_VACIO = {
  titulo: "",
  descripcion: "",
  categoria: "",
  tipoDiscapacidad: "",
  obraSocial: "",
  localidad: "",
  necesidades: "",
  enlace: "",
  activo: true,
};

// uno o más tipos, guardados como "Motora, Múltiple"
function SelectorDiscapacidad({ valor, alCambiar, ayuda }) {
  const elegidos = separarLista(valor);
  const alternar = (t) =>
    alCambiar(TIPOS_DISCAPACIDAD.filter((x) => (x === t ? !elegidos.includes(t) : elegidos.includes(x))).join(", "));

  return (
    <fieldset className="elegir-tipos">
      <legend>Tipo de discapacidad</legend>
      <div className="elegir-personas-lista">
        {TIPOS_DISCAPACIDAD.map((t) => (
          <label key={t} className="opcion-persona">
            <input type="checkbox" checked={elegidos.includes(t)} onChange={() => alternar(t)} />
            <span>{t}</span>
          </label>
        ))}
      </div>
      {ayuda && <small className="ayuda-campo">{ayuda}</small>}
    </fieldset>
  );
}

function FormularioArticulo({ articulo, alGuardar, alCerrar }) {
  const [v, setV] = useState(() => ({ ...ARTICULO_VACIO, ...articulo, anio: articulo?.anio ?? "" }));
  const [guardando, setGuardando] = useState(false);
  const cambiar = (campo) => (e) => setV((x) => ({ ...x, [campo]: e.target.value }));

  const enviar = async (e) => {
    e.preventDefault();
    setGuardando(true);
    await alGuardar(
      { ...v, anio: v.anio === "" ? null : Number(v.anio), tipoDiscapacidad: vacioANull(v.tipoDiscapacidad) },
      articulo?.id
    );
    setGuardando(false);
  };

  return (
    <Modal
      abierto
      alCerrar={alCerrar}
      titulo={articulo ? "Editar artículo" : "Nuevo artículo"}
      descripcion="Un escrito ya publicado por especialistas. El resumen lo escribís vos: no copies el texto original."
      pie={
        <>
          <Boton variante="secundario" onClick={alCerrar}>
            Cancelar
          </Boton>
          <Boton type="submit" form="form-articulo" icono="check" cargando={guardando}>
            Guardar
          </Boton>
        </>
      }
    >
      <form id="form-articulo" className="formulario" onSubmit={enviar}>
        <Campo etiqueta="Título" obligatorio>
          <input value={v.titulo} onChange={cambiar("titulo")} required maxLength={250} />
        </Campo>
        <div className="grilla-campos">
          <Campo etiqueta="Autores" obligatorio>
            <input value={v.autores} onChange={cambiar("autores")} required maxLength={250} />
          </Campo>
          <Campo etiqueta="Año">
            <input type="number" min="1900" max="2100" value={v.anio} onChange={cambiar("anio")} />
          </Campo>
        </div>
        <Campo etiqueta="Dónde se publicó" obligatorio ayuda="Revista, organismo o sitio.">
          <input value={v.fuente} onChange={cambiar("fuente")} required maxLength={200} />
        </Campo>
        <Campo etiqueta="De qué trata" obligatorio>
          <textarea rows={3} value={v.resumen} onChange={cambiar("resumen")} required maxLength={600} />
        </Campo>
        <SelectorDiscapacidad
          valor={v.tipoDiscapacidad}
          alCambiar={(t) => setV((x) => ({ ...x, tipoDiscapacidad: t }))}
          ayuda="Sin ninguno: es un artículo general sobre el cuidado y lo ve todo el mundo."
        />
        <div className="grilla-campos">
          <Campo etiqueta="Enlace" obligatorio>
            <input type="url" value={v.enlace} onChange={cambiar("enlace")} required maxLength={500} placeholder="https://" />
          </Campo>
        </div>
        <label className="opcion-equipo">
          <input type="checkbox" checked={v.activo} onChange={(e) => setV((x) => ({ ...x, activo: e.target.checked }))} />
          <span>
            <strong>Visible</strong>
            <small>Si lo desactivás, deja de aparecer en Artículos.</small>
          </span>
        </label>
      </form>
    </Modal>
  );
}

function FormularioRecurso({ recurso, alGuardar, alCerrar }) {
  const { avisar } = usePanel();
  const [v, setV] = useState(() => ({ ...RECURSO_VACIO, ...recurso }));
  const [guardando, setGuardando] = useState(false);
  const cambiar = (campo) => (e) => setV((x) => ({ ...x, [campo]: e.target.value }));
  const necesidades = separarLista(v.necesidades);
  const alternarNecesidad = (n) =>
    setV((x) => ({
      ...x,
      necesidades: (necesidades.includes(n) ? necesidades.filter((y) => y !== n) : [...necesidades, n]).join(", "),
    }));

  const enviar = async (e) => {
    e.preventDefault();
    if (!v.tipoDiscapacidad) {
      avisar("Elegí al menos un tipo de discapacidad.", "error");
      return;
    }
    setGuardando(true);
    await alGuardar(
      {
        ...v,
        tipoDiscapacidad: vacioANull(v.tipoDiscapacidad),
        obraSocial: vacioANull(v.obraSocial),
        localidad: vacioANull(v.localidad),
        necesidades: vacioANull(v.necesidades),
      },
      recurso?.id
    );
    setGuardando(false);
  };

  return (
    <Modal
      abierto
      alCerrar={alCerrar}
      titulo={recurso ? "Editar recurso" : "Nuevo recurso"}
      descripcion="Si el recurso pide una discapacidad, obra social o localidad, solo se recomienda a quien la cumpla."
      pie={
        <>
          <Boton variante="secundario" onClick={alCerrar}>
            Cancelar
          </Boton>
          <Boton type="submit" form="form-recurso" icono="check" cargando={guardando}>
            Guardar
          </Boton>
        </>
      }
    >
      <form id="form-recurso" className="formulario" onSubmit={enviar}>
        <Campo etiqueta="Título" obligatorio>
          <input value={v.titulo} onChange={cambiar("titulo")} required />
        </Campo>
        <Campo etiqueta="Descripción" obligatorio ayuda="Que diga exactamente lo que la persona va a encontrar en el enlace.">
          <textarea rows={3} value={v.descripcion} onChange={cambiar("descripcion")} required />
        </Campo>
        <div className="grilla-campos">
          <Campo etiqueta="Categoría" obligatorio>
            <input value={v.categoria} onChange={cambiar("categoria")} required placeholder="Trámites, Cobertura..." />
          </Campo>
        </div>
        <SelectorDiscapacidad
          valor={v.tipoDiscapacidad}
          alCambiar={(t) => setV((x) => ({ ...x, tipoDiscapacidad: t }))}
          ayuda="Elegí al menos uno: cada recurso se recomienda solo a las personas de esos tipos. Lo que sirve a todos va en la guía de trámites."
        />
        <div className="grilla-campos">
          <Campo etiqueta="Obra social" ayuda="Vacío: para cualquiera. «Sin obra social»: solo para quien no tiene.">
            <input value={v.obraSocial || ""} onChange={cambiar("obraSocial")} />
          </Campo>
          <Campo etiqueta="Localidad" ayuda="Vacío: alcance nacional.">
            <input value={v.localidad || ""} onChange={cambiar("localidad")} />
          </Campo>
        </div>
        <Campo etiqueta="Responde a">
          <div className="chips">
            {NECESIDADES.map((n) => (
              <button
                key={n}
                type="button"
                className={`chip ${necesidades.includes(n) ? "activo" : ""}`}
                aria-pressed={necesidades.includes(n)}
                onClick={() => alternarNecesidad(n)}
              >
                {n}
              </button>
            ))}
          </div>
        </Campo>
        <Campo etiqueta="Enlace" obligatorio>
          <input type="url" value={v.enlace || ""} onChange={cambiar("enlace")} required placeholder="https://" />
        </Campo>
      </form>
    </Modal>
  );
}

export default function VistaAdministracion() {
  const { avisar, ejecutar, confirmar } = usePanel();
  const [articulos, setArticulos] = useState(null);
  const [recursos, setRecursos] = useState(null);
  const [editando, setEditando] = useState(null); // { tipo, datos }

  const cargar = useCallback(
    () =>
      Promise.all([api.obtener("/Articulos/todos"), api.obtener("/RecursosRecomendables")])
        .then(([a, r]) => {
          setArticulos(a);
          setRecursos(r);
        })
        .catch((error) => avisar(error.message, "error")),
    [avisar]
  );

  useEffect(() => {
    let activo = true;
    Promise.all([api.obtener("/Articulos/todos"), api.obtener("/RecursosRecomendables")])
      .then(([a, r]) => {
        if (!activo) return;
        setArticulos(a);
        setRecursos(r);
      })
      .catch((error) => activo && avisar(error.message, "error"));
    return () => {
      activo = false;
    };
  }, [avisar]);

  const guardar = (ruta, texto) => async (cuerpo, id) => {
    const ok = await ejecutar(() => (id ? api.actualizar(`${ruta}/${id}`, cuerpo) : api.crear(ruta, cuerpo)), texto);
    if (ok) {
      setEditando(null);
      await cargar();
    }
  };

  const borrar = (titulo, mensaje, ruta, texto) =>
    confirmar({
      titulo,
      mensaje,
      accion: async () => {
        if (await ejecutar(() => api.eliminar(ruta), texto)) await cargar();
      },
    });

  return (
    <>
      <EncabezadoVista
        titulo="Administración"
        descripcion="El contenido que ve todo el mundo: artículos de especialistas y recursos recomendables."
      />

      <Tarjeta
        titulo="Artículos"
        descripcion="Escritos ya publicados, con enlace a la fuente."
        accion={
          <Boton icono="mas" onClick={() => setEditando({ tipo: "articulo", datos: null })}>
            Nuevo artículo
          </Boton>
        }
      >
        {!articulos ? (
          <Cargando />
        ) : articulos.length === 0 ? (
          <EstadoVacio icono="libro" titulo="Todavía no hay artículos" />
        ) : (
          <ul className="lista">
            {articulos.map((a) => (
              <li key={a.id} className={`fila-admin ${a.activo ? "" : "suspendido"}`}>
                <div className="fila-datos">
                  <strong>{a.titulo}</strong>
                  <span className="secundario">
                    {[a.autores, a.fuente, a.anio, a.tipoDiscapacidad].filter(Boolean).join(" · ")}
                    {!a.activo && " · Oculto"}
                  </span>
                </div>
                <div className="fila-acciones">
                  <BotonIcono icono="editar" etiqueta={`Editar ${a.titulo}`} onClick={() => setEditando({ tipo: "articulo", datos: a })} />
                  <BotonIcono
                    icono="papelera"
                    etiqueta={`Eliminar ${a.titulo}`}
                    variante="peligro"
                    onClick={() => borrar(`¿Eliminar "${a.titulo}"?`, "Deja de aparecer en Artículos.", `/Articulos/${a.id}`, "Artículo eliminado.")}
                  />
                </div>
              </li>
            ))}
          </ul>
        )}
      </Tarjeta>

      <Tarjeta
        titulo="Recursos recomendables"
        descripcion="Lo que el recomendador elige para cada persona."
        accion={
          <Boton icono="mas" onClick={() => setEditando({ tipo: "recurso", datos: null })}>
            Nuevo recurso
          </Boton>
        }
      >
        {!recursos ? (
          <Cargando />
        ) : (
          <ul className="lista">
            {recursos.map((r) => (
              <li key={r.id} className="fila-admin">
                <div className="fila-datos">
                  <strong>{r.titulo}</strong>
                  <span className="secundario">
                    {[r.categoria, r.tipoDiscapacidad, r.obraSocial, r.localidad].filter(Boolean).join(" · ")}
                  </span>
                </div>
                <div className="fila-acciones">
                  <BotonIcono icono="editar" etiqueta={`Editar ${r.titulo}`} onClick={() => setEditando({ tipo: "recurso", datos: r })} />
                  <BotonIcono
                    icono="papelera"
                    etiqueta={`Desactivar ${r.titulo}`}
                    variante="peligro"
                    onClick={() =>
                      borrar(`¿Desactivar "${r.titulo}"?`, "Deja de recomendarse. Las valoraciones se conservan.", `/RecursosRecomendables/${r.id}`, "Recurso desactivado.")
                    }
                  />
                </div>
              </li>
            ))}
          </ul>
        )}
      </Tarjeta>

      {editando?.tipo === "articulo" && (
        <FormularioArticulo
          articulo={editando.datos}
          alGuardar={guardar("/Articulos", "Artículo guardado.")}
          alCerrar={() => setEditando(null)}
        />
      )}
      {editando?.tipo === "recurso" && (
        <FormularioRecurso
          recurso={editando.datos}
          alGuardar={guardar("/RecursosRecomendables", "Recurso guardado.")}
          alCerrar={() => setEditando(null)}
        />
      )}
    </>
  );
}
