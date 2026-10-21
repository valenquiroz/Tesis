import { useState } from "react";
import { Boton, Cargando, EncabezadoVista, EstadoVacio, Icono, Tarjeta } from "../componentes";
import { usePanel } from "../contexto";
import { CAMPOS_PERFIL, colorPersona, iniciales, perfilCompleto, planIncluye, planQueIncluye, separarLista, puedeAdministrar, puedeGestionar } from "../utilidades";
import { AvisoPlan } from "./comunes";


const NIVELES = {
  Alta: { clase: "nivel-alta", texto: "Muy relevante" },
  Media: { clase: "nivel-media", texto: "Relevante" },
  Baja: { clase: "nivel-baja", texto: "Puede servirte" },
};

export default function VistaRecomendaciones() {
  const { personas, recomendaciones, idRecomendaciones, acciones, cargando } = usePanel();
  const [categoria, setCategoria] = useState("Todas");

  if (cargando) return <Cargando />;

  if (personas.length === 0) {
    return (
      <>
        <EncabezadoVista
          titulo="Recursos para su situación"
        />
        <Tarjeta>
          <EstadoVacio
            icono="estrella"
            titulo="Primero agregá una persona asistida"
            accion={
              <Boton icono="mas" onClick={acciones.nuevaPersona}>
                Agregar persona
              </Boton>
            }
          >
            Las recomendaciones se calculan a partir de su perfil: tipo de
            discapacidad, obra social, localidad y necesidades.
          </EstadoVacio>
        </Tarjeta>
      </>
    );
  }

  const persona = personas.find((p) => p.id === idRecomendaciones);

  if (!planIncluye(persona, "recursos")) {
    return (
      <>
        <EncabezadoVista titulo="Recursos para su situación" />
        <AvisoPlan
          titulo={`Los recursos recomendados están desde el plan ${planQueIncluye("recursos")}`}
          texto="Programas, organismos y centros ordenados según el perfil de la persona, con la explicación de por qué se recomienda cada uno."
          puedeCambiar={puedeAdministrar(persona)}
        />
      </>
    );
  }

  const estado = recomendaciones[idRecomendaciones];
  const lista = estado?.estado === "listo" ? estado.lista : [];
  const completos = perfilCompleto(persona);

  const categorias = ["Todas", ...new Set(lista.map((r) => r.recurso.categoria))];
  const categoriaActiva = categorias.includes(categoria) ? categoria : "Todas";
  const visibles =
    categoriaActiva === "Todas"
      ? lista
      : lista.filter((r) => r.recurso.categoria === categoriaActiva);

  const datosPerfil = [
    persona?.tipoDiscapacidad && `Discapacidad ${persona.tipoDiscapacidad.toLowerCase()}`,
    persona?.obraSocial,
    persona?.localidad,
  ].filter(Boolean);

  return (
    <>
      <EncabezadoVista
        titulo="Recursos para su situación"
        descripcion="Ordenados según el perfil de la persona."
      />

      <section className="banner-persona">
        <div className="banner-persona-datos">
          <div className="avatar avatar-grande" data-color={colorPersona(persona?.id)}>
            {iniciales(persona?.nombreCompleto)}
          </div>
          <div>
            <small>Recomendaciones para</small>
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
              {datosPerfil.map((d) => (
                <span className="etiqueta etiqueta-clara" key={d}>
                  {d}
                </span>
              ))}
              {separarLista(persona?.necesidades).map((n) => (
                <span className="etiqueta etiqueta-clara etiqueta-borde" key={n}>
                  {n}
                </span>
              ))}
              {datosPerfil.length === 0 && !persona?.necesidades && (
                <span className="texto-claro">Sin datos de perfil cargados</span>
              )}
            </div>
          </div>
        </div>

        <div className="banner-persona-perfil">
          <span>
            Perfil {completos}/{CAMPOS_PERFIL.length}
          </span>
          <span className="medidor medidor-claro" aria-hidden="true">
            <span style={{ width: `${(completos / CAMPOS_PERFIL.length) * 100}%` }} />
          </span>
          {puedeAdministrar(persona) && (
            <Boton variante="claro" icono="editar" onClick={() => acciones.editarPersona(persona)}>
              {completos < CAMPOS_PERFIL.length ? "Completar perfil" : "Editar perfil"}
            </Boton>
          )}
        </div>
      </section>

      {!estado ? (
        <Cargando texto="Buscando recursos..." />
      ) : estado.estado === "error" ? (
        <Tarjeta>
          <EstadoVacio
            icono="alerta"
            titulo="No pudimos obtener las recomendaciones"
            accion={
              <Boton
                variante="secundario"
                onClick={() => acciones.reintentarRecomendaciones(idRecomendaciones)}
              >
                Reintentar
              </Boton>
            }
          >
            {estado.mensaje}
          </EstadoVacio>
        </Tarjeta>
      ) : lista.length === 0 ? (
        <Tarjeta>
          {persona?.tipoDiscapacidad ? (
            <EstadoVacio icono="estrella" titulo="No hay recomendaciones por el momento">
              Todavía no hay recursos para discapacidad {persona.tipoDiscapacidad.toLowerCase()} que se ajusten a su
              obra social y su localidad. Lo que sirve a cualquier persona (CUD, pensión, transporte) está en Trámites.
            </EstadoVacio>
          ) : (
            <EstadoVacio
              icono="estrella"
              titulo="Falta el tipo de discapacidad"
              accion={
                puedeAdministrar(persona) && (
                  <Boton icono="editar" onClick={() => acciones.editarPersona(persona)}>
                    Completar perfil
                  </Boton>
                )
              }
            >
              Los recursos son específicos de cada tipo de discapacidad. Cargalo en el perfil para ver los que le corresponden.
            </EstadoVacio>
          )}
        </Tarjeta>
      ) : (
        <>
          {categorias.length > 2 && (
            <div className="chips filtros-categorias" role="group" aria-label="Filtrar por categoría">
              {categorias.map((c) => (
                <button
                  key={c}
                  type="button"
                  className={`chip ${categoriaActiva === c ? "activo" : ""}`}
                  aria-pressed={categoriaActiva === c}
                  onClick={() => setCategoria(c)}
                >
                  {c}
                  <small>
                    {c === "Todas" ? lista.length : lista.filter((r) => r.recurso.categoria === c).length}
                  </small>
                </button>
              ))}
            </div>
          )}

          <div className="rejilla-recomendaciones">
            {visibles.map((recomendacion) => {
              const { recurso } = recomendacion;
              const nivel = NIVELES[recomendacion.nivel] || NIVELES.Baja;

              return (
                <article className="tarjeta-recomendacion" key={recurso.id}>
                  <div className="recomendacion-superior">
                    <span className="etiqueta etiqueta-naranja">{recurso.categoria}</span>
                    <span className={`nivel ${nivel.clase}`}>
                      <span className="nivel-punto" aria-hidden="true" />
                      {nivel.texto}
                    </span>
                  </div>

                  <h3>{recurso.titulo}</h3>
                  <p>{recurso.descripcion}</p>

                  {recomendacion.motivos?.length > 0 && (
                    <div className="motivos">
                      <span className="motivos-titulo">¿Por qué te lo recomendamos?</span>
                      <ul>
                        {recomendacion.motivos.map((motivo) => (
                          <li key={motivo}>
                            <Icono nombre="check" tamano={14} />
                            {motivo}
                          </li>
                        ))}
                      </ul>
                    </div>
                  )}

                  <div className="recomendacion-pie">
                    <div
                      className="coincidencia"
                      title={`${Math.round(recomendacion.puntuacion)} puntos de coincidencia`}
                    >
                      <span className="medidor" aria-hidden="true">
                        <span style={{ width: `${Math.max(recomendacion.porcentaje ?? 0, 6)}%` }} />
                      </span>
                      <span>{recomendacion.porcentaje ?? 0}% de coincidencia</span>
                    </div>

                    {recurso.enlace && (
                      <a
                        href={recurso.enlace}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="boton boton-secundario boton-chico"
                      >
                        Ver recurso <Icono nombre="externo" tamano={14} />
                      </a>
                    )}
                  </div>

                  {recurso.localidad && (
                    <span className="recurso-ubicacion">
                      <Icono nombre="ubicacion" tamano={13} /> {recurso.localidad}
                    </span>
                  )}

                  {puedeGestionar(persona) && (
                  <div className="valoracion" role="group" aria-label={`¿Te sirvió ${recurso.titulo}?`}>
                    <span className="valoracion-pregunta">
                      {recomendacion.valoracion === true
                        ? "Gracias: lo vamos a tener en cuenta."
                        : recomendacion.valoracion === false
                          ? "Lo vamos a mostrar más abajo."
                          : "¿Te sirvió?"}
                    </span>
                    <button
                      type="button"
                      className={`boton-valorar ${recomendacion.valoracion === true ? "activo" : ""}`}
                      aria-pressed={recomendacion.valoracion === true}
                      onClick={() => acciones.valorarRecurso(idRecomendaciones, recurso, true)}
                    >
                      <Icono nombre="check" tamano={14} /> Me sirvió
                    </button>
                    <button
                      type="button"
                      className={`boton-valorar no ${recomendacion.valoracion === false ? "activo" : ""}`}
                      aria-pressed={recomendacion.valoracion === false}
                      onClick={() => acciones.valorarRecurso(idRecomendaciones, recurso, false)}
                    >
                      No me sirvió
                    </button>
                  </div>
                  )}
                </article>
              );
            })}
          </div>

          <p className="nota-pie">
            <Icono nombre="info" tamano={14} />
            Los recursos son informativos y enlazan a fuentes oficiales. Verificá
            siempre los requisitos vigentes en cada organismo.
          </p>
        </>
      )}
    </>
  );
}
