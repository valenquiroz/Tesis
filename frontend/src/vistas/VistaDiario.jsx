import { useCallback, useEffect, useState } from "react";
import { api } from "../api";
import { Boton, BotonIcono, Campo, Cargando, EncabezadoVista, EstadoVacio, Modal, Tarjeta } from "../componentes";
import { usePanel } from "../contexto";
import { AvisoPlan } from "./comunes";
import {
  aInputFechaHora,
  calcularEdad,
  formatearFechaCorta,
  formatearHora,
  planIncluye,
  planQueIncluye,
  puedeRegistrar,
} from "../utilidades";

const COLORES_TIPO = {
  Crisis: "tipo-crisis",
  Síntoma: "tipo-sintoma",
  Ánimo: "tipo-animo",
  Sueño: "tipo-sueno",
  Alimentación: "tipo-alimentacion",
  Avance: "tipo-avance",
  Otro: "tipo-otro",
};

const PERIODOS = [
  { dias: 7, texto: "7 días" },
  { dias: 30, texto: "30 días" },
  { dias: 90, texto: "3 meses" },
];

const ahoraInput = () => aInputFechaHora(new Date(Date.now() - new Date().getTimezoneOffset() * 60000).toISOString());

function FormularioEntrada({ entrada, tipos, personaId, alGuardar, alCerrar }) {
  const { avisar } = usePanel();
  const [valores, setValores] = useState(
    entrada
      ? { fecha: aInputFechaHora(entrada.fecha), tipo: entrada.tipo, intensidad: entrada.intensidad ?? "", texto: entrada.texto }
      : { fecha: ahoraInput(), tipo: "", intensidad: "", texto: "" }
  );
  const [guardando, setGuardando] = useState(false);
  const alCambiar = (e) => setValores((v) => ({ ...v, [e.target.name]: e.target.value }));

  const enviar = async (e) => {
    e.preventDefault();
    if (!valores.tipo) {
      avisar("Elegí qué querés registrar.", "error");
      return;
    }
    setGuardando(true);
    try {
      await alGuardar(
        {
          personaAsistidaId: personaId,
          fecha: valores.fecha,
          tipo: valores.tipo,
          intensidad: valores.intensidad ? Number(valores.intensidad) : null,
          texto: valores.texto.trim(),
        },
        entrada?.id
      );
    } finally {
      setGuardando(false);
    }
  };

  return (
    <Modal
      abierto
      alCerrar={alCerrar}
      titulo={entrada ? "Editar registro" : "Nuevo registro"}
      descripcion="Anotá lo que pasó con tus palabras: después te sirve para contárselo al médico."
      pie={
        <>
          <Boton variante="secundario" onClick={alCerrar}>
            Cancelar
          </Boton>
          <Boton type="submit" form="form-diario" icono="check" cargando={guardando}>
            Guardar
          </Boton>
        </>
      }
    >
      <form id="form-diario" className="formulario" onSubmit={enviar}>
        <div className="campo">
          <span className="etiqueta-campo" id="etiqueta-tipo">
            ¿Qué querés registrar? <span className="obligatorio">*</span>
          </span>
          <div className="chips" role="radiogroup" aria-labelledby="etiqueta-tipo">
            {tipos.map((t) => (
              <button
                key={t}
                type="button"
                role="radio"
                aria-checked={valores.tipo === t}
                className={`chip ${valores.tipo === t ? "activo" : ""}`}
                onClick={() => setValores((v) => ({ ...v, tipo: t }))}
              >
                {t}
              </button>
            ))}
          </div>
        </div>

        <div className="grilla-campos">
          <Campo etiqueta="Cuándo" obligatorio>
            <input type="datetime-local" name="fecha" value={valores.fecha} onChange={alCambiar} max={ahoraInput()} required />
          </Campo>

          <Campo etiqueta="Intensidad" ayuda="Opcional. 1 es leve, 5 muy fuerte.">
            <select name="intensidad" value={valores.intensidad} onChange={alCambiar}>
              <option value="">Sin indicar</option>
              {[1, 2, 3, 4, 5].map((n) => (
                <option key={n} value={n}>
                  {n}
                </option>
              ))}
            </select>
          </Campo>

          <Campo etiqueta="Qué pasó" obligatorio ancho="completo">
            <textarea
              name="texto"
              value={valores.texto}
              onChange={alCambiar}
              rows="4"
              minLength={3}
              maxLength={2000}
              placeholder="Ej. Se despertó dos veces a la noche con dolor en el hombro."
              required
            />
          </Campo>
        </div>
      </form>
    </Modal>
  );
}

// el plan Equipo imprime hasta el último mes; Plus, el historial de un año
const PERIODO_ANIO = { dias: 365, texto: "12 meses" };

function ResumenConsulta({ personaId, historialCompleto, alCerrar }) {
  const { avisar } = usePanel();
  const [dias, setDias] = useState(30);
  const periodos = historialCompleto ? [...PERIODOS, PERIODO_ANIO] : PERIODOS.filter((o) => o.dias <= 30);
  const [resumen, setResumen] = useState(null);

  useEffect(() => {
    let activo = true;
    api
      .obtener(`/Diario/resumen?personaId=${personaId}&dias=${dias}`, "No se pudo armar el resumen.")
      .then((r) => activo && setResumen({ ...r, diasPedidos: dias }))
      .catch((error) => {
        if (!activo) return;
        avisar(error.message, "error");
        alCerrar();
      });
    return () => {
      activo = false;
    };
  }, [personaId, dias, avisar, alCerrar]);

  const listo = resumen?.diasPedidos === dias;
  const p = resumen?.persona;
  const edad = p ? calcularEdad(p.fechaNacimiento) : null;
  const totalTomas = listo ? resumen.tomas.tomadas + resumen.tomas.omitidas : 0;

  return (
    <Modal
      abierto
      alCerrar={alCerrar}
      ancho="grande"
      titulo="Resumen para la consulta"
      descripcion="Imprimilo o guardalo como PDF y llevalo al médico o a la junta."
      pie={
        <>
          <Boton variante="secundario" onClick={alCerrar}>
            Cerrar
          </Boton>
          <Boton icono="descargar" onClick={() => window.print()} disabled={!listo}>
            Imprimir o guardar PDF
          </Boton>
        </>
      }
    >
      <div className="chips periodos" role="group" aria-label="Período">
        {periodos.map((o) => (
          <button key={o.dias} type="button" className={`chip ${dias === o.dias ? "activo" : ""}`} aria-pressed={dias === o.dias} onClick={() => setDias(o.dias)}>
            Últimos {o.texto}
          </button>
        ))}
        {!historialCompleto && <span className="texto-tenue">El historial de hasta un año está en el plan Plus.</span>}
      </div>

      {!listo ? (
        <Cargando texto="Armando el resumen..." />
      ) : (
        <article className="hoja-imprimible" id="hoja-imprimible">
          <header>
            <h2>{p.nombreCompleto}</h2>
            <p>
              {[edad !== null && `${edad} años`, p.tipoDiscapacidad && `Discapacidad ${p.tipoDiscapacidad.toLowerCase()}`, p.obraSocial, p.numeroAfiliado && `Afiliado ${p.numeroAfiliado}`]
                .filter(Boolean)
                .join(" · ")}
            </p>
            {p.diagnostico && <p><strong>Diagnóstico:</strong> {p.diagnostico}</p>}
            {p.alergias && <p><strong>Alergias:</strong> {p.alergias}</p>}
            <p className="periodo">
              Período: {formatearFechaCorta(resumen.desde)} al {formatearFechaCorta(resumen.hasta)}
            </p>
          </header>

          <section>
            <h3>En números</h3>
            <div className="numeros-resumen">
              {resumen.porTipo.map((t) => (
                <span key={t.tipo}>
                  <strong>{t.cantidad}</strong> {t.tipo.toLowerCase()}
                </span>
              ))}
              {totalTomas > 0 && (
                <span>
                  <strong>{resumen.tomas.omitidas}</strong> de {totalTomas} tomas de medicación sin dar
                </span>
              )}
              {resumen.porTipo.length === 0 && totalTomas === 0 && <span>Sin registros en el período.</span>}
            </div>
          </section>

          {resumen.medicamentos.length > 0 && (
            <section>
              <h3>Medicación actual</h3>
              <ul>
                {resumen.medicamentos.map((m) => (
                  <li key={m.nombre}>
                    <strong>{m.nombre}</strong> {[m.dosis, m.horarios.split(",").join(", ") + " h", m.indicaciones].filter(Boolean).join(" · ")}
                    {m.tomadas + m.omitidas > 0 && (
                      <span className="resumen-adherencia">
                        {" "}
                        — se dieron {m.tomadas} de {m.tomadas + m.omitidas} tomas registradas
                      </span>
                    )}
                  </li>
                ))}
              </ul>
            </section>
          )}

          {resumen.noDadas.length > 0 && (
            <section>
              <h3>Tomas que no se dieron</h3>
              <table>
                <thead>
                  <tr>
                    <th>Fecha</th>
                    <th>Hora</th>
                    <th>Medicamento</th>
                    <th>Lo registró</th>
                  </tr>
                </thead>
                <tbody>
                  {resumen.noDadas.map((t) => (
                    <tr key={`${t.fecha}-${t.horario}-${t.medicamento}`}>
                      <td>{formatearFechaCorta(t.fecha + "T12:00:00")}</td>
                      <td>{t.horario}</td>
                      <td>{t.medicamento}</td>
                      <td>{t.registradoPor}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </section>
          )}

          <section>
            <h3>Registro del diario</h3>
            {resumen.entradas.length === 0 ? (
              <p>No hay registros en el período.</p>
            ) : (
              <table>
                <thead>
                  <tr>
                    <th>Fecha</th>
                    <th>Tipo</th>
                    <th>Int.</th>
                    <th>Qué pasó</th>
                  </tr>
                </thead>
                <tbody>
                  {resumen.entradas.map((e) => (
                    <tr key={e.id}>
                      <td>
                        {formatearFechaCorta(e.fecha)} {formatearHora(e.fecha)}
                      </td>
                      <td>{e.tipo}</td>
                      <td>{e.intensidad ?? "–"}</td>
                      <td>{e.texto}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </section>

          {resumen.turnos.length > 0 && (
            <section>
              <h3>Consultas del período y próximas</h3>
              <ul>
                {resumen.turnos.map((t, i) => (
                  <li key={i}>
                    {formatearFechaCorta(t.fechaHora)}: {t.titulo}
                    {t.profesional ? ` (${t.profesional})` : ""}
                  </li>
                ))}
              </ul>
            </section>
          )}

          <footer>Generado con Acompañar el {formatearFechaCorta(resumen.hasta + "T12:00:00")}.</footer>
        </article>
      )}
    </Modal>
  );
}

export default function VistaDiario() {
  const { personas, idRecomendaciones, acciones, cargando, avisar, ejecutar, confirmar } = usePanel();
  const persona = personas.find((p) => p.id === idRecomendaciones);
  const personaId = persona?.id;

  const [dias, setDias] = useState(30);
  const [tipo, setTipo] = useState("");
  const [datos, setDatos] = useState(null);
  const [editando, setEditando] = useState(undefined); // undefined: cerrado · null: nueva
  const [viendoResumen, setViendoResumen] = useState(false);
  const cerrarResumen = useCallback(() => setViendoResumen(false), []);

  const clave = `${personaId}|${dias}`;
  const conDiario = planIncluye(persona, "diario");
  const registra = puedeRegistrar(persona);

  const cargar = () =>
    api
      .obtener(`/Diario?personaId=${personaId}&dias=${dias}`, "No se pudo cargar el diario.")
      .then((d) => setDatos({ ...d, clave: `${personaId}|${dias}` }))
      .catch((error) => avisar(error.message, "error"));

  useEffect(() => {
    if (!personaId || !conDiario) return;
    let activo = true;
    api
      .obtener(`/Diario?personaId=${personaId}&dias=${dias}`, "No se pudo cargar el diario.")
      .then((d) => activo && setDatos({ ...d, clave: `${personaId}|${dias}` }))
      .catch((error) => activo && avisar(error.message, "error"));
    return () => {
      activo = false;
    };
  }, [personaId, dias, avisar, conDiario]);

  if (cargando) return <Cargando />;

  if (personas.length === 0) {
    return (
      <>
        <EncabezadoVista titulo="Diario de cuidado" />
        <Tarjeta>
          <EstadoVacio
            icono="diario"
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

  const actual = datos?.clave === clave ? datos : null;
  const entradas = (actual?.entradas ?? []).filter((e) => !tipo || e.tipo === tipo);
  const tipos = datos?.tipos ?? [];

  const guardar = async (cuerpo, id) => {
    const ok = await ejecutar(
      () => (id ? api.actualizar(`/Diario/${id}`, cuerpo) : api.crear("/Diario", cuerpo)),
      id ? "Registro actualizado." : "Registro guardado."
    );
    if (ok) {
      setEditando(undefined);
      await cargar();
    }
  };

  const eliminar = (entrada) =>
    confirmar({
      titulo: "¿Eliminar este registro?",
      mensaje: "Se va a borrar del diario de forma permanente.",
      accion: async () => {
        const ok = await ejecutar(() => api.eliminar(`/Diario/${entrada.id}`), "Registro eliminado.");
        if (ok) await cargar();
      },
    });

  // agrupado por día
  const porDia = entradas.reduce((grupos, e) => {
    const dia = String(e.fecha).slice(0, 10);
    (grupos[dia] ||= []).push(e);
    return grupos;
  }, {});

  return (
    <>
      <EncabezadoVista titulo="Diario de cuidado" descripcion="Lo que pasa en el día a día, para no olvidarlo en la consulta.">
        {conDiario && (
          <Boton variante="secundario" icono="documento" onClick={() => setViendoResumen(true)}>
            Resumen para la consulta
          </Boton>
        )}
        {conDiario && registra && (
          <Boton icono="mas" onClick={() => setEditando(null)}>
            Nuevo registro
          </Boton>
        )}
      </EncabezadoVista>

      <div className="barra-filtros">
        {personas.length > 1 && (
          <select
            className="selector-chico"
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
        )}
        <div className="chips" role="group" aria-label="Período">
          {PERIODOS.map((o) => (
            <button key={o.dias} type="button" className={`chip ${dias === o.dias ? "activo" : ""}`} aria-pressed={dias === o.dias} onClick={() => setDias(o.dias)}>
              {o.texto}
            </button>
          ))}
        </div>
        <select className="selector-chico" value={tipo} onChange={(e) => setTipo(e.target.value)} aria-label="Filtrar por tipo">
          <option value="">Todos los tipos</option>
          {tipos.map((t) => (
            <option key={t} value={t}>
              {t}
            </option>
          ))}
        </select>
      </div>

      {!conDiario ? (
        <AvisoPlan
          titulo={`El diario está desde el plan ${planQueIncluye("diario")}`}
          texto="Anotá crisis, cómo durmió, qué comió o un avance en la terapia, y llevá un resumen impreso a cada consulta."
          puedeCambiar={persona?.puedeAdministrar !== false}
        />
      ) : !actual ? (
        <Cargando />
      ) : entradas.length === 0 ? (
        <Tarjeta>
          <EstadoVacio
            icono="diario"
            titulo={tipo ? `No hay registros de "${tipo}" en el período` : "Todavía no hay registros"}
            accion={
              registra && (
                <Boton variante="secundario" icono="mas" onClick={() => setEditando(null)}>
                  Escribir el primero
                </Boton>
              )
            }
          >
            Crisis, cómo durmió, qué comió, un avance en la terapia: todo sirve.
          </EstadoVacio>
        </Tarjeta>
      ) : (
        <div className="diario">
          {Object.entries(porDia).map(([dia, lista]) => (
            <section key={dia} className="diario-dia">
              <h2>{formatearFechaCorta(dia + "T12:00:00")}</h2>
              <ul>
                {lista.map((e) => (
                  <li key={e.id} className="entrada-diario">
                    <span className={`marca-tipo ${COLORES_TIPO[e.tipo] ?? ""}`}>{e.tipo}</span>
                    <div className="entrada-cuerpo">
                      <p>{e.texto}</p>
                      <span className="secundario">
                        {formatearHora(e.fecha)} · {e.esMia ? "Lo escribiste vos" : e.autor}
                        {e.intensidad ? ` · Intensidad ${e.intensidad}/5` : ""}
                      </span>
                    </div>
                    {e.esMia && (
                      <div className="fila-acciones">
                        <BotonIcono icono="editar" etiqueta="Editar registro" onClick={() => setEditando(e)} />
                        <BotonIcono icono="papelera" etiqueta="Eliminar registro" variante="peligro" onClick={() => eliminar(e)} />
                      </div>
                    )}
                  </li>
                ))}
              </ul>
            </section>
          ))}
        </div>
      )}

      {editando !== undefined && (
        <FormularioEntrada
          entrada={editando}
          tipos={tipos}
          personaId={personaId}
          alGuardar={guardar}
          alCerrar={() => setEditando(undefined)}
        />
      )}

      {viendoResumen && (
        <ResumenConsulta personaId={personaId} historialCompleto={planIncluye(persona, "informe")} alCerrar={cerrarResumen} />
      )}

    </>
  );
}
