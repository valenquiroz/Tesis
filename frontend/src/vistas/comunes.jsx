import { BotonIcono, Icono } from "../componentes";
import { usePanel } from "../contexto";
import {
  calcularEdad,
  colorPersona,
  CAMPOS_PERFIL,
  etiquetaDia,
  formatearHora,
  iniciales,
  partesFecha,
  perfilCompleto,
  puedeGestionar,
} from "../utilidades";

export function FilaTurno({ turno, hoy, acciones, compacta = false }) {
  const { personas } = usePanel();
  const { dia, mes } = partesFecha(turno.fechaHora);
  const editable = puedeGestionar(personas.find((p) => p.id === turno.personaAsistidaId));
  const pasado = new Date(turno.fechaHora) < hoy;

  return (
    <li className={`fila-turno ${pasado ? "pasado" : ""}`}>
      <div className="bloque-fecha">
        <strong>{dia}</strong>
        <span>{mes}</span>
      </div>

      <div className="fila-datos">
        <strong>{turno.titulo}</strong>
        <span className="secundario">
          <Icono nombre="reloj" tamano={13} />
          <span className="capitalizar">
            {`${etiquetaDia(turno.fechaHora, hoy)}, ${formatearHora(turno.fechaHora)} hs`}
            {turno.personaAsistida?.nombreCompleto && ` · ${turno.personaAsistida.nombreCompleto}`}
          </span>
        </span>

        {!compacta && (turno.especialidad || turno.profesional || turno.lugar) && (
          <div className="etiquetas">
            {turno.especialidad && <span className="etiqueta">{turno.especialidad}</span>}
            {turno.profesional && (
              <span className="etiqueta etiqueta-suave">
                <Icono nombre="persona" tamano={12} /> {turno.profesional}
              </span>
            )}
            {turno.lugar && (
              <span className="etiqueta etiqueta-suave">
                <Icono nombre="ubicacion" tamano={12} /> {turno.lugar}
              </span>
            )}
          </div>
        )}

        {!compacta && turno.observaciones && (
          <p className="nota-turno">{turno.observaciones}</p>
        )}
      </div>

      {acciones && editable && (
        <div className="acciones-fila">
          <BotonIcono icono="editar" etiqueta="Editar turno" onClick={() => acciones.editarTurno(turno)} />
          <BotonIcono
            icono="papelera"
            etiqueta="Eliminar turno"
            variante="peligro"
            onClick={() => acciones.eliminarTurno(turno)}
          />
        </div>
      )}
    </li>
  );
}

export function TarjetaPersona({ persona, hoy, acciones }) {
  const edad = calcularEdad(persona.fechaNacimiento, hoy);
  const completos = perfilCompleto(persona);

  return (
    <li className="tarjeta-persona">
      <div className="avatar avatar-grande" data-color={colorPersona(persona.id)}>{iniciales(persona.nombreCompleto)}</div>

      <div className="fila-datos">
        <strong>{persona.nombreCompleto}</strong>
        <span className="secundario">
          {[persona.relacionConCuidador, edad !== null ? `${edad} años` : null]
            .filter(Boolean)
            .join(" · ") || "Persona asistida"}
        </span>

        {persona.domicilio && (
          <span className="secundario">
            <Icono nombre="lugar" tamano={13} />
            {persona.domicilio}
          </span>
        )}

        <div className="etiquetas">
          {persona.esTitular === false && (
            <span className="etiqueta etiqueta-azul" title={`Responsable: ${persona.nombreTitular}`}>
              <Icono nombre="comunidad" tamano={12} />{" "}
              {persona.acceso === "Observador" ? "Mirás" : "Cuidás"} · {persona.nombreTitular}
            </span>
          )}
          {persona.esTitular !== false && persona.puedeRegistrar === false && (
            <span className="etiqueta etiqueta-suave" title="Vos la administrás y ves lo que registran sus cuidadores, sin registrar tomas">
              <Icono nombre="ojo" tamano={12} /> La cuida tu equipo
            </span>
          )}
          {persona.tipoDiscapacidad && (
            <span className="etiqueta etiqueta-verde">{persona.tipoDiscapacidad}</span>
          )}
          {persona.obraSocial && <span className="etiqueta">{persona.obraSocial}</span>}
          {persona.localidad && (
            <span className="etiqueta etiqueta-suave">
              <Icono nombre="ubicacion" tamano={12} /> {persona.localidad}
            </span>
          )}
        </div>

        {persona.puedeAdministrar !== false && completos < CAMPOS_PERFIL.length && (
          <button
            type="button"
            className="aviso-perfil"
            onClick={() => acciones.editarPersona(persona)}
          >
            <span className="medidor" aria-hidden="true">
              <span style={{ width: `${(completos / CAMPOS_PERFIL.length) * 100}%` }} />
            </span>
            Perfil {completos}/{CAMPOS_PERFIL.length} · Completalo para mejores recomendaciones
          </button>
        )}
      </div>

      <div className="acciones-fila">
        <BotonIcono
          icono="comunidad"
          etiqueta={`Equipo de cuidado de ${persona.nombreCompleto}`}
          onClick={() => acciones.abrirEquipo(persona)}
        />
        {persona.puedeAdministrar !== false && (
          <BotonIcono
            icono="editar"
            etiqueta={`Editar a ${persona.nombreCompleto}`}
            onClick={() => acciones.editarPersona(persona)}
          />
        )}
        {persona.puedeAdministrar !== false && (
          <BotonIcono
            icono="papelera"
            etiqueta={`Eliminar a ${persona.nombreCompleto}`}
            variante="peligro"
            onClick={() => acciones.eliminarPersona(persona)}
          />
        )}
      </div>
    </li>
  );
}

// Una función que el plan de la persona no incluye: se explica y se invita a ver los planes
export function AvisoPlan({ titulo, texto, puedeCambiar = true }) {
  const { cambiarVista } = usePanel();
  return (
    <section className="aviso-plan aviso-plan-grande">
      <Icono nombre="bandera" tamano={20} />
      <div>
        <h2>{titulo}</h2>
        <p>{texto}</p>
        {!puedeCambiar && <p className="texto-tenue">El plan lo elige quien es responsable de la persona.</p>}
      </div>
      {puedeCambiar && (
        <button type="button" className="boton boton-secundario" onClick={() => cambiarVista("plan")}>
          Ver planes
        </button>
      )}
    </section>
  );
}
