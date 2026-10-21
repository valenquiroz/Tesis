import { useState } from "react";
import { Boton, Campo, Icono, Modal } from "./componentes";
import {
  aInputFecha,
  aInputFechaHora,
  CONTACTO_VACIO,
  GRUPOS_SANGUINEOS,
  MEDICAMENTO_VACIO,
  LOCALIDADES_SUGERIDAS,
  NECESIDADES,
  OBRAS_SOCIALES_SUGERIDAS,
  PERSONA_VACIA,
  separarLista,
  TIPOS_CONTACTO,
  TIPOS_DISCAPACIDAD,
  REPETICIONES,
  TURNO_VACIO,
  vacioANull,
} from "./utilidades";

const HOY_ISO = new Date().toISOString().slice(0, 10);

function useFormulario(inicial) {
  const [valores, setValores] = useState(inicial);
  const [guardando, setGuardando] = useState(false);

  const alCambiar = (e) => {
    const { name, value } = e.target;
    setValores((v) => ({ ...v, [name]: value }));
  };

  const enviarCon = (guardar) => async (e) => {
    e.preventDefault();
    setGuardando(true);
    try {
      await guardar(valores);
    } finally {
      setGuardando(false);
    }
  };

  return { valores, setValores, alCambiar, guardando, enviarCon };
}

function PieFormulario({ idFormulario, alCerrar, guardando, texto }) {
  return (
    <>
      <Boton variante="secundario" onClick={alCerrar}>
        Cancelar
      </Boton>
      <Boton type="submit" form={idFormulario} icono="check" cargando={guardando}>
        {guardando ? "Guardando..." : texto}
      </Boton>
    </>
  );
}

function SelectorPersona({ personas, value, onChange }) {
  return (
    <select name="personaAsistidaId" value={value} onChange={onChange} required>
      <option value="">Seleccioná una persona</option>
      {personas.map((p) => (
        <option key={p.id} value={p.id}>
          {p.nombreCompleto}
        </option>
      ))}
    </select>
  );
}

export function FormularioPersona({ persona, alGuardar, alCerrar }) {
  const esEdicion = !!persona;
  // la relación la define quien cargó a la persona
  const esCompartida = esEdicion && persona.esTitular === false;

  const { valores, setValores, alCambiar, guardando, enviarCon } = useFormulario(
    esEdicion
      ? {
          nombreCompleto: persona.nombreCompleto || "",
          fechaNacimiento: aInputFecha(persona.fechaNacimiento),
          relacionConCuidador: persona.relacionConCuidador || "",
          tipoDiscapacidad: persona.tipoDiscapacidad || "",
          obraSocial: persona.obraSocial || "",
          localidad: persona.localidad || "",
          domicilio: persona.domicilio || "",
          necesidades: separarLista(persona.necesidades),
          observaciones: persona.observaciones || "",
          grupoSanguineo: persona.grupoSanguineo || "",
          alergias: persona.alergias || "",
          diagnostico: persona.diagnostico || "",
          numeroAfiliado: persona.numeroAfiliado || "",
          medicoCabecera: persona.medicoCabecera || "",
          telefonoMedico: persona.telefonoMedico || "",
          informacionEmergencia: persona.informacionEmergencia || "",
          vencimientoCud: aInputFecha(persona.vencimientoCud),
        }
      : PERSONA_VACIA
  );

  // por si quedó un valor viejo que no está en la lista
  const opcionesDiscapacidad =
    valores.tipoDiscapacidad && !TIPOS_DISCAPACIDAD.includes(valores.tipoDiscapacidad)
      ? [...TIPOS_DISCAPACIDAD, valores.tipoDiscapacidad]
      : TIPOS_DISCAPACIDAD;

  const alternarNecesidad = (necesidad) =>
    setValores((v) => ({
      ...v,
      necesidades: v.necesidades.includes(necesidad)
        ? v.necesidades.filter((n) => n !== necesidad)
        : [...v.necesidades, necesidad],
    }));

  const guardar = (v) =>
    alGuardar(
      {
        nombreCompleto: v.nombreCompleto.trim(),
        fechaNacimiento: v.fechaNacimiento ? `${v.fechaNacimiento}T00:00:00` : null,
        relacionConCuidador: vacioANull(v.relacionConCuidador),
        tipoDiscapacidad: vacioANull(v.tipoDiscapacidad),
        obraSocial: vacioANull(v.obraSocial),
        localidad: vacioANull(v.localidad),
        domicilio: vacioANull(v.domicilio),
        necesidades: v.necesidades.length ? v.necesidades.join(", ") : null,
        observaciones: vacioANull(v.observaciones),
        grupoSanguineo: vacioANull(v.grupoSanguineo),
        alergias: vacioANull(v.alergias),
        diagnostico: vacioANull(v.diagnostico),
        numeroAfiliado: vacioANull(v.numeroAfiliado),
        medicoCabecera: vacioANull(v.medicoCabecera),
        telefonoMedico: vacioANull(v.telefonoMedico),
        informacionEmergencia: vacioANull(v.informacionEmergencia),
        vencimientoCud: v.vencimientoCud ? `${v.vencimientoCud}T00:00:00` : null,
      },
      persona?.id
    );

  return (
    <Modal
      abierto
      alCerrar={alCerrar}
      ancho="grande"
      titulo={esEdicion ? "Editar persona asistida" : "Nueva persona asistida"}
      pie={
        <PieFormulario
          idFormulario="form-persona"
          alCerrar={alCerrar}
          guardando={guardando}
          texto={esEdicion ? "Guardar cambios" : "Agregar persona"}
        />
      }
    >
      <form id="form-persona" className="formulario" onSubmit={enviarCon(guardar)}>
        <fieldset>
          <legend>Datos personales</legend>
          <div className="grilla-campos">
            <Campo etiqueta="Nombre completo" obligatorio ancho="completo">
              <input
                type="text"
                name="nombreCompleto"
                value={valores.nombreCompleto}
                onChange={alCambiar}
                placeholder="Ej. Rosa Fernández"
                maxLength={150}
                required
              />
            </Campo>

            <Campo etiqueta="Fecha de nacimiento">
              <input
                type="date"
                name="fechaNacimiento"
                value={valores.fechaNacimiento}
                onChange={alCambiar}
                max={HOY_ISO}
              />
            </Campo>

            <Campo
              etiqueta={esCompartida ? "Relación" : "Relación con vos"}
              ayuda={esCompartida ? `Es la relación con ${persona.nombreTitular}.` : undefined}
            >
              <input
                type="text"
                name="relacionConCuidador"
                value={valores.relacionConCuidador}
                onChange={alCambiar}
                placeholder="Ej. Madre, hijo, esposo"
                maxLength={80}
                disabled={esCompartida}
              />
            </Campo>
          </div>
        </fieldset>

        <fieldset>
          <legend>Su perfil</legend>
          <p className="ayuda-perfil">
            Con estos datos elegimos los recursos, los artículos y los estudios del CUD que le corresponden. Lo más importante es el
            tipo de discapacidad: figura en el certificado.
          </p>
          <div className="grilla-campos">
            <Campo
              etiqueta="Tipo de discapacidad"
              obligatorio
              ayuda="Si tiene más de uno, elegí Múltiple."
            >
              <select
                name="tipoDiscapacidad"
                value={valores.tipoDiscapacidad}
                onChange={alCambiar}
                required
              >
                <option value="" disabled>
                  Elegí uno
                </option>
                {opcionesDiscapacidad.map((t) => (
                  <option key={t} value={t}>
                    {t}
                  </option>
                ))}
              </select>
            </Campo>

            <Campo etiqueta="Obra social o prepaga" ayuda="Si no tiene, dejalo vacío.">
              <input
                type="text"
                name="obraSocial"
                list="lista-obras-sociales"
                value={valores.obraSocial}
                onChange={alCambiar}
                placeholder="Ej. PAMI"
                maxLength={80}
              />
            </Campo>
            <datalist id="lista-obras-sociales">
              {OBRAS_SOCIALES_SUGERIDAS.map((o) => (
                <option key={o} value={o} />
              ))}
            </datalist>

            <Campo etiqueta="Localidad" ayuda="Para mostrarte lugares cerca.">
              <input
                type="text"
                name="localidad"
                list="lista-localidades"
                value={valores.localidad}
                onChange={alCambiar}
                placeholder="Ej. Pilar"
                maxLength={100}
              />
            </Campo>
            <Campo etiqueta="Domicilio">
              <input
                type="text"
                name="domicilio"
                value={valores.domicilio}
                onChange={alCambiar}
                placeholder="Calle, número, piso"
                maxLength={200}
              />
            </Campo>
            <datalist id="lista-localidades">
              {LOCALIDADES_SUGERIDAS.map((l) => (
                <option key={l} value={l} />
              ))}
            </datalist>
          </div>

          <div className="campo">
            <span className="etiqueta-campo" id="etiqueta-necesidades">
              ¿En qué necesitás ayuda?
            </span>
            <div className="chips" role="group" aria-labelledby="etiqueta-necesidades">
              {NECESIDADES.map((n) => {
                const activa = valores.necesidades.includes(n);
                return (
                  <button
                    key={n}
                    type="button"
                    className={`chip ${activa ? "activo" : ""}`}
                    aria-pressed={activa}
                    onClick={() => alternarNecesidad(n)}
                  >
                    {activa && <Icono nombre="check" tamano={14} />}
                    {n}
                  </button>
                );
              })}
            </div>
            <small className="ayuda">Podés elegir varias.</small>
          </div>
        </fieldset>

        <fieldset>
          <legend>Ficha de emergencia</legend>
          <p className="ayuda-fieldset">
            Se muestra en la sección Emergencia, lista para enseñar en una guardia o imprimir.
          </p>
          <div className="grilla-campos">
            <Campo etiqueta="Diagnóstico" ancho="completo">
              <input
                type="text"
                name="diagnostico"
                value={valores.diagnostico}
                onChange={alCambiar}
                placeholder="Ej. Secuela de ACV con hemiparesia izquierda"
                maxLength={300}
              />
            </Campo>

            <Campo etiqueta="Grupo sanguíneo">
              <select name="grupoSanguineo" value={valores.grupoSanguineo} onChange={alCambiar}>
                <option value="">No sé</option>
                {GRUPOS_SANGUINEOS.map((g) => (
                  <option key={g} value={g}>
                    {g}
                  </option>
                ))}
              </select>
            </Campo>

            <Campo etiqueta="Alergias">
              <input
                type="text"
                name="alergias"
                value={valores.alergias}
                onChange={alCambiar}
                placeholder="Ej. Penicilina, látex"
                maxLength={300}
              />
            </Campo>

            <Campo etiqueta="N.º de afiliado">
              <input
                type="text"
                name="numeroAfiliado"
                value={valores.numeroAfiliado}
                onChange={alCambiar}
                maxLength={60}
              />
            </Campo>

            <Campo etiqueta="Médico de cabecera">
              <input
                type="text"
                name="medicoCabecera"
                value={valores.medicoCabecera}
                onChange={alCambiar}
                placeholder="Ej. Dra. Ferrari"
                maxLength={120}
              />
            </Campo>

            <Campo etiqueta="Teléfono del médico">
              <input
                type="tel"
                name="telefonoMedico"
                value={valores.telefonoMedico}
                onChange={alCambiar}
                pattern="[0-9+()\-\s]{6,40}"
                title="Solo números, espacios, guiones, paréntesis y +"
                maxLength={40}
              />
            </Campo>

            <Campo
              etiqueta="Vencimiento del CUD"
              ayuda="Si lo cargás, agendamos un aviso dos meses antes."
            >
              <input
                type="date"
                name="vencimientoCud"
                value={valores.vencimientoCud}
                onChange={alCambiar}
              />
            </Campo>

            <Campo etiqueta="Qué tiene que saber quien la atienda" ancho="completo">
              <textarea
                name="informacionEmergencia"
                value={valores.informacionEmergencia}
                onChange={alCambiar}
                placeholder="Cómo se comunica, qué la calma, qué evitar..."
                rows="2"
                maxLength={1000}
              />
            </Campo>
          </div>
        </fieldset>

        <Campo etiqueta="Observaciones">
          <textarea
            name="observaciones"
            value={valores.observaciones}
            onChange={alCambiar}
            placeholder="Rutinas, preferencias o cualquier dato útil..."
            rows="3"
            maxLength={1000}
          />
        </Campo>
      </form>
    </Modal>
  );
}

export function FormularioTurno({ turno, fechaSugerida, valoresIniciales, personas, alGuardar, alCerrar }) {
  const esEdicion = !!turno;

  const { valores, alCambiar, guardando, enviarCon } = useFormulario(
    esEdicion
      ? {
          titulo: turno.titulo || "",
          fechaHora: aInputFechaHora(turno.fechaHora),
          lugar: turno.lugar || "",
          profesional: turno.profesional || "",
          especialidad: turno.especialidad || "",
          observaciones: turno.observaciones || "",
          repetirCadaMeses: turno.repetirCadaMeses ? String(turno.repetirCadaMeses) : "",
          personaAsistidaId: String(turno.personaAsistidaId),
        }
      : {
          ...TURNO_VACIO,
          fechaHora: fechaSugerida || "",
          personaAsistidaId: personas.length === 1 ? String(personas[0].id) : "",
          ...valoresIniciales,
        }
  );

  const guardar = (v) =>
    alGuardar(
      {
        titulo: v.titulo.trim(),
        fechaHora: v.fechaHora,
        lugar: vacioANull(v.lugar),
        profesional: vacioANull(v.profesional),
        especialidad: vacioANull(v.especialidad),
        observaciones: vacioANull(v.observaciones),
        repetirCadaMeses: v.repetirCadaMeses ? Number(v.repetirCadaMeses) : null,
        personaAsistidaId: Number(v.personaAsistidaId),
      },
      turno?.id
    );

  return (
    <Modal
      abierto
      alCerrar={alCerrar}
      titulo={esEdicion ? "Editar turno" : "Nuevo turno"}
      pie={
        <PieFormulario
          idFormulario="form-turno"
          alCerrar={alCerrar}
          guardando={guardando}
          texto={esEdicion ? "Guardar cambios" : "Agendar turno"}
        />
      }
    >
      <form id="form-turno" className="formulario" onSubmit={enviarCon(guardar)}>
        <div className="grilla-campos">
          <Campo etiqueta="Persona asistida" obligatorio>
            <SelectorPersona
              personas={personas}
              value={valores.personaAsistidaId}
              onChange={alCambiar}
            />
          </Campo>

          <Campo etiqueta="Fecha y hora" obligatorio>
            <input
              type="datetime-local"
              name="fechaHora"
              value={valores.fechaHora}
              onChange={alCambiar}
              required
            />
          </Campo>

          <Campo etiqueta="Motivo" obligatorio ancho="completo">
            <input
              type="text"
              name="titulo"
              value={valores.titulo}
              onChange={alCambiar}
              placeholder="Ej. Control de kinesiología"
              maxLength={150}
              required
            />
          </Campo>

          <Campo etiqueta="Especialidad">
            <input
              type="text"
              name="especialidad"
              value={valores.especialidad}
              onChange={alCambiar}
              placeholder="Ej. Neurología"
              maxLength={100}
            />
          </Campo>

          <Campo etiqueta="Profesional">
            <input
              type="text"
              name="profesional"
              value={valores.profesional}
              onChange={alCambiar}
              placeholder="Ej. Dra. García"
              maxLength={150}
            />
          </Campo>

          <Campo etiqueta="¿Es un control que se repite?" ayuda="Te avisamos un mes antes de que toque pedir el próximo turno.">
            <select name="repetirCadaMeses" value={valores.repetirCadaMeses} onChange={alCambiar}>
              <option value="">No se repite</option>
              {REPETICIONES.map((r) => (
                <option key={r.meses} value={r.meses}>
                  {r.texto}
                </option>
              ))}
            </select>
          </Campo>

          <Campo etiqueta="Lugar" ancho="completo">
            <input
              type="text"
              name="lugar"
              value={valores.lugar}
              onChange={alCambiar}
              placeholder="Ej. Hospital Austral, consultorio 12"
              maxLength={150}
            />
          </Campo>

          <Campo etiqueta="Observaciones" ancho="completo">
            <textarea
              name="observaciones"
              value={valores.observaciones}
              onChange={alCambiar}
              placeholder="Llevar estudios previos, ir en ayunas..."
              rows="3"
              maxLength={1000}
            />
          </Campo>
        </div>
      </form>
    </Modal>
  );
}

export function FormularioContacto({ contacto, personas, alGuardar, alCerrar }) {
  const esEdicion = !!contacto;

  const { valores, alCambiar, guardando, enviarCon } = useFormulario(
    esEdicion
      ? {
          nombre: contacto.nombre || "",
          relacion: contacto.relacion || "",
          telefono: contacto.telefono || "",
          tipoContacto: contacto.tipoContacto || "",
          personaAsistidaId: String(contacto.personaAsistidaId),
        }
      : {
          ...CONTACTO_VACIO,
          personaAsistidaId: personas.length === 1 ? String(personas[0].id) : "",
        }
  );

  const guardar = (v) =>
    alGuardar(
      {
        nombre: v.nombre.trim(),
        relacion: vacioANull(v.relacion),
        telefono: v.telefono.trim(),
        tipoContacto: vacioANull(v.tipoContacto),
        personaAsistidaId: Number(v.personaAsistidaId),
      },
      contacto?.id
    );

  return (
    <Modal
      abierto
      alCerrar={alCerrar}
      titulo={esEdicion ? "Editar contacto" : "Nuevo contacto de emergencia"}
      pie={
        <PieFormulario
          idFormulario="form-contacto"
          alCerrar={alCerrar}
          guardando={guardando}
          texto={esEdicion ? "Guardar cambios" : "Agregar contacto"}
        />
      }
    >
      <form id="form-contacto" className="formulario" onSubmit={enviarCon(guardar)}>
        <div className="grilla-campos">
          <Campo etiqueta="Nombre" obligatorio ancho="completo">
            <input
              type="text"
              name="nombre"
              value={valores.nombre}
              onChange={alCambiar}
              placeholder="Ej. María Quiroz"
              maxLength={150}
              required
            />
          </Campo>

          <Campo etiqueta="Teléfono" obligatorio>
            <input
              type="tel"
              name="telefono"
              value={valores.telefono}
              onChange={alCambiar}
              placeholder="Ej. 11 5555-5555"
              pattern="[0-9+()\-\s]{6,25}"
              title="Solo números, espacios, guiones, paréntesis y +"
              required
            />
          </Campo>

          <Campo etiqueta="Relación">
            <input
              type="text"
              name="relacion"
              value={valores.relacion}
              onChange={alCambiar}
              placeholder="Ej. Hermana, médica de cabecera"
              maxLength={80}
            />
          </Campo>

          <Campo etiqueta="Tipo de contacto">
            <select name="tipoContacto" value={valores.tipoContacto} onChange={alCambiar}>
              <option value="">Sin especificar</option>
              {TIPOS_CONTACTO.map((t) => (
                <option key={t} value={t}>
                  {t}
                </option>
              ))}
            </select>
          </Campo>

          <Campo etiqueta="Persona asistida" obligatorio>
            <SelectorPersona
              personas={personas}
              value={valores.personaAsistidaId}
              onChange={alCambiar}
            />
          </Campo>
        </div>
      </form>
    </Modal>
  );
}

export function FormularioMedicamento({ medicamento, personas, personaSugerida, alGuardar, alCerrar }) {
  const esEdicion = !!medicamento;

  const { valores, setValores, alCambiar, guardando, enviarCon } = useFormulario(
    esEdicion
      ? {
          nombre: medicamento.nombre,
          dosis: medicamento.dosis || "",
          horarios: medicamento.horarios.split(","),
          indicaciones: medicamento.indicaciones || "",
          medico: medicamento.medico || "",
          vencimientoReceta: aInputFecha(medicamento.vencimientoReceta),
          activo: medicamento.activo,
          personaAsistidaId: String(medicamento.personaAsistidaId),
        }
      : {
          ...MEDICAMENTO_VACIO,
          personaAsistidaId: personaSugerida ? String(personaSugerida) : personas.length === 1 ? String(personas[0].id) : "",
        }
  );

  const cambiarHorario = (i, valor) =>
    setValores((v) => ({ ...v, horarios: v.horarios.map((h, j) => (j === i ? valor : h)) }));
  const quitarHorario = (i) => setValores((v) => ({ ...v, horarios: v.horarios.filter((_, j) => j !== i) }));
  const agregarHorario = () => setValores((v) => ({ ...v, horarios: [...v.horarios, ""] }));

  const guardar = (v) =>
    alGuardar(
      {
        personaAsistidaId: Number(v.personaAsistidaId),
        nombre: v.nombre.trim(),
        dosis: vacioANull(v.dosis),
        horarios: v.horarios.filter(Boolean).join(","),
        indicaciones: vacioANull(v.indicaciones),
        medico: vacioANull(v.medico),
        vencimientoReceta: v.vencimientoReceta ? `${v.vencimientoReceta}T00:00:00` : null,
        activo: v.activo,
      },
      medicamento?.id
    );

  return (
    <Modal
      abierto
      alCerrar={alCerrar}
      titulo={esEdicion ? "Editar medicamento" : "Nuevo medicamento"}
      pie={
        <PieFormulario
          idFormulario="form-medicamento"
          alCerrar={alCerrar}
          guardando={guardando}
          texto={esEdicion ? "Guardar cambios" : "Agregar medicamento"}
        />
      }
    >
      <form id="form-medicamento" className="formulario" onSubmit={enviarCon(guardar)}>
        <div className="grilla-campos">
          <Campo etiqueta="Persona asistida" obligatorio>
            <SelectorPersona personas={personas} value={valores.personaAsistidaId} onChange={alCambiar} />
          </Campo>

          <Campo etiqueta="Medicamento" obligatorio>
            <input
              type="text"
              name="nombre"
              value={valores.nombre}
              onChange={alCambiar}
              placeholder="Ej. Enalapril 10 mg"
              maxLength={120}
              required
            />
          </Campo>

          <Campo etiqueta="Dosis">
            <input
              type="text"
              name="dosis"
              value={valores.dosis}
              onChange={alCambiar}
              placeholder="Ej. 1 comprimido, 5 ml"
              maxLength={80}
            />
          </Campo>

          <Campo etiqueta="Indicó">
            <input
              type="text"
              name="medico"
              value={valores.medico}
              onChange={alCambiar}
              placeholder="Ej. Dr. Paredes"
              maxLength={120}
            />
          </Campo>

          <div className="campo campo-completo">
            <span className="etiqueta-campo" id="etiqueta-horarios">
              Horarios <span className="obligatorio">*</span>
            </span>
            <div className="horarios" role="group" aria-labelledby="etiqueta-horarios">
              {valores.horarios.map((h, i) => (
                <span className="horario" key={i}>
                  <input
                    type="time"
                    value={h}
                    onChange={(e) => cambiarHorario(i, e.target.value)}
                    aria-label={`Horario ${i + 1}`}
                    required
                  />
                  {valores.horarios.length > 1 && (
                    <button type="button" onClick={() => quitarHorario(i)} aria-label={`Quitar horario ${i + 1}`}>
                      <Icono nombre="cerrar" tamano={14} />
                    </button>
                  )}
                </span>
              ))}
              {valores.horarios.length < 8 && (
                <button type="button" className="chip" onClick={agregarHorario}>
                  <Icono nombre="mas" tamano={14} /> Otro horario
                </button>
              )}
            </div>
          </div>

          <Campo etiqueta="Indicaciones" ancho="completo">
            <input
              type="text"
              name="indicaciones"
              value={valores.indicaciones}
              onChange={alCambiar}
              placeholder="Ej. En ayunas, con las comidas, guardar en la heladera"
              maxLength={300}
            />
          </Campo>

          <Campo etiqueta="Vence la receta" ayuda="Te avisamos en el inicio cuando falten 15 días.">
            <input type="date" name="vencimientoReceta" value={valores.vencimientoReceta} onChange={alCambiar} />
          </Campo>

          {esEdicion && (
            <label className="check-linea">
              <input
                type="checkbox"
                checked={valores.activo}
                onChange={(e) => setValores((v) => ({ ...v, activo: e.target.checked }))}
              />
              Lo está tomando actualmente
            </label>
          )}
        </div>
      </form>
    </Modal>
  );
}
