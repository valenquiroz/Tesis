import { Boton, BotonIcono, Cargando, EncabezadoVista, EstadoVacio, Icono, Tarjeta } from "../componentes";
import { usePanel } from "../contexto";
import { calcularEdad, formatearFechaCorta, iniciales, puedeAdministrar, puedeGestionar } from "../utilidades";

const NUMEROS_UTILES = [
  { numero: "911", texto: "Emergencias y policía" },
  { numero: "100", texto: "Bomberos" },
];

function FichaEmergencia() {
  const { personas, contactos, medicamentos, idRecomendaciones, acciones, hoy } = usePanel();
  const persona = personas.find((p) => p.id === idRecomendaciones);
  if (!persona) return null;

  const edad = calcularEdad(persona.fechaNacimiento, hoy);
  const remedios = medicamentos.filter((m) => m.personaAsistidaId === persona.id && m.activo);
  const susContactos = contactos.filter((c) => c.personaAsistidaId === persona.id);
  const vacia = !persona.diagnostico && !persona.alergias && !persona.grupoSanguineo && !persona.informacionEmergencia;

  const dato = (titulo, valor, destacado) =>
    valor ? (
      <div className={`dato-ficha ${destacado ? "destacado" : ""}`}>
        <dt>{titulo}</dt>
        <dd>{valor}</dd>
      </div>
    ) : null;

  return (
    <Tarjeta
      className="tarjeta-ficha"
      titulo="Ficha de emergencia"
      descripcion="Para mostrar en una guardia o dejar impresa en la heladera."
      accion={
        <div className="acciones-fila">
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
          {puedeAdministrar(persona) && (
            <Boton variante="secundario" icono="editar" onClick={() => acciones.editarPersona(persona)}>
              Editar
            </Boton>
          )}
          <Boton icono="descargar" onClick={() => window.print()}>
            Imprimir
          </Boton>
        </div>
      }
    >
      {vacia && (
        <p className="aviso-tramites">
          <Icono nombre="info" />
          <span>
            Completá el diagnóstico, las alergias y el grupo sanguíneo para que la ficha sirva en una urgencia.
          </span>
        </p>
      )}
      <article className="hoja-imprimible ficha">
        <header>
          <div className="avatar avatar-grande">{iniciales(persona.nombreCompleto)}</div>
          <div>
            <h2>{persona.nombreCompleto}</h2>
            <p>
              {[edad !== null && `${edad} años`, persona.fechaNacimiento && `Nació el ${formatearFechaCorta(persona.fechaNacimiento)}`, persona.domicilio]
                .filter(Boolean)
                .join(" · ")}
            </p>
          </div>
          {persona.grupoSanguineo && (
            <span className="grupo-sanguineo" aria-label={`Grupo sanguíneo ${persona.grupoSanguineo}`}>
              {persona.grupoSanguineo}
            </span>
          )}
        </header>

        <dl>
          {dato("Alergias", persona.alergias, true)}
          {dato("Diagnóstico", persona.diagnostico)}
          {dato("Tipo de discapacidad", persona.tipoDiscapacidad)}
          {dato("Obra social", [persona.obraSocial, persona.numeroAfiliado].filter(Boolean).join(" · "))}
          {dato("Médico de cabecera", [persona.medicoCabecera, persona.telefonoMedico].filter(Boolean).join(" · "))}
          {dato("A tener en cuenta", persona.informacionEmergencia)}
        </dl>

        {remedios.length > 0 && (
          <section>
            <h3>Medicación</h3>
            <ul>
              {remedios.map((m) => (
                <li key={m.id}>
                  <strong>{m.nombre}</strong> {[m.dosis, m.horarios.split(",").join(", ") + " h"].filter(Boolean).join(" · ")}
                </li>
              ))}
            </ul>
          </section>
        )}

        {susContactos.length > 0 && (
          <section>
            <h3>Avisar a</h3>
            <ul>
              {susContactos.map((c) => (
                <li key={c.id}>
                  <strong>{c.nombre}</strong> {[c.relacion, c.telefono].filter(Boolean).join(" · ")}
                </li>
              ))}
            </ul>
          </section>
        )}
      </article>
    </Tarjeta>
  );
}

export default function VistaEmergencia() {
  const { personas, contactos, cargando, acciones } = usePanel();

  return (
    <>
      <EncabezadoVista
        titulo="Emergencia"
      />

      <div className="rejilla-emergencia">
        <div className="columna">
          <section className="bloque-emergencia">
            <span className="bloque-emergencia-etiqueta">
              <Icono nombre="alerta" tamano={14} /> Emergencia médica
            </span>
            <h2>SAME · 107</h2>
            <p>Si hay riesgo para la vida, llamá de inmediato.</p>

            <a href="tel:107" className="boton-emergencia">
              <Icono nombre="telefono" tamano={20} /> Llamar al 107
            </a>
          </section>

          <Tarjeta titulo="Otros números útiles" className="tarjeta-numeros">
            <ul className="lista lista-numeros">
              {NUMEROS_UTILES.map((n) => (
                <li key={n.numero}>
                  <a href={`tel:${n.numero}`}>
                    <strong>{n.numero}</strong>
                    <span>{n.texto}</span>
                    <Icono nombre="telefono" tamano={16} />
                  </a>
                </li>
              ))}
            </ul>
          </Tarjeta>
        </div>

        <Tarjeta
          titulo="Contactos de emergencia"
          className="tarjeta-contactos"
          accion={
            personas.some(puedeGestionar) && (
              <Boton variante="secundario" icono="mas" onClick={acciones.nuevoContacto}>
                Agregar
              </Boton>
            )
          }
        >
          {cargando ? (
            <Cargando texto="Cargando contactos..." />
          ) : personas.length === 0 ? (
            <EstadoVacio icono="corazon" titulo="Primero agregá una persona asistida">
              Los contactos de emergencia se asocian a la persona que cuidás.
            </EstadoVacio>
          ) : contactos.length === 0 ? (
            <EstadoVacio
              icono="corazon"
              titulo="Todavía no hay contactos"
              accion={
                personas.some(puedeGestionar) && (
                  <Boton icono="mas" onClick={acciones.nuevoContacto}>
                    Agregar el primero
                  </Boton>
                )
              }
            >
              Agregá familiares, médicos o instituciones para llamarlos desde acá.
            </EstadoVacio>
          ) : (
            <ul className="lista">
              {contactos.map((contacto) => (
                <li className="fila-contacto" key={contacto.id}>
                  <div className="avatar avatar-verde">{iniciales(contacto.nombre)}</div>

                  <div className="fila-datos">
                    <strong>{contacto.nombre}</strong>
                    <span className="secundario">
                      {[contacto.relacion, contacto.tipoContacto].filter(Boolean).join(" · ") ||
                        "Contacto de emergencia"}
                      {personas.length > 1 && contacto.personaAsistida?.nombreCompleto
                        ? ` · Para ${contacto.personaAsistida.nombreCompleto}`
                        : ""}
                    </span>
                    <span className="telefono">{contacto.telefono}</span>
                  </div>

                  <div className="acciones-fila">
                    <a
                      className="boton-llamar"
                      href={`tel:${contacto.telefono.replace(/[^\d+]/g, "")}`}
                      aria-label={`Llamar a ${contacto.nombre}`}
                    >
                      <Icono nombre="telefono" tamano={15} />
                      <span>Llamar</span>
                    </a>
                    {puedeGestionar(personas.find((p) => p.id === contacto.personaAsistidaId)) && (
                      <>
                        <BotonIcono
                          icono="editar"
                          etiqueta={`Editar a ${contacto.nombre}`}
                          onClick={() => acciones.editarContacto(contacto)}
                        />
                        <BotonIcono
                          icono="papelera"
                          etiqueta={`Eliminar a ${contacto.nombre}`}
                          variante="peligro"
                          onClick={() => acciones.eliminarContacto(contacto)}
                        />
                      </>
                    )}
                  </div>
                </li>
              ))}
            </ul>
          )}
        </Tarjeta>
      </div>

      {!cargando && <FichaEmergencia />}
    </>
  );
}
