import { useEffect, useState } from "react";
import { api } from "../api";
import { Boton, Cargando, EncabezadoVista, Icono, Tarjeta } from "../componentes";
import { usePanel } from "../contexto";
import { formatearFechaCorta, planQueIncluye, primerNombre, sinTitulo } from "../utilidades";
import { AvisoPlan } from "./comunes";

// a qué vista del panel lleva cada sugerencia
const DESTINOS = {
  tramites: { vista: "tramites", texto: "Ver trámites" },
  diario: { vista: "diario", texto: "Abrir el diario" },
  grupo: { vista: "grupo", texto: "Ir a tu grupo" },
};

function Resultado({ resultado, alRepetir }) {
  const { cambiarVista } = usePanel();

  return (
    <Tarjeta className={`resultado-esfuerzo ${resultado.elevado ? "elevado" : ""}`}>
      <div className="resultado-cabecera">
        <div className="puntaje" aria-label={`${resultado.puntaje} de ${resultado.maximo}`}>
          <strong>{resultado.puntaje}</strong>
          <span>de {resultado.maximo}</span>
        </div>
        <div>
          <span className="secundario">Tu resultado del {formatearFechaCorta(resultado.fecha)}</span>
          <h2>{resultado.nivel}</h2>
          <p>{resultado.mensaje}</p>
        </div>
      </div>

      {resultado.sugerencias.length > 0 && (
        <div className="sugerencias">
          <h3>Algunas ideas según tus respuestas</h3>
          <ul>
            {resultado.sugerencias.map((s) => (
              <li key={s.titulo}>
                <strong>{s.titulo}</strong>
                <p>{s.texto}</p>
                {s.seccion && DESTINOS[s.seccion] && (
                  <button type="button" className="enlace-accion" onClick={() => cambiarVista(DESTINOS[s.seccion].vista)}>
                    {DESTINOS[s.seccion].texto} <Icono nombre="derecha" tamano={14} />
                  </button>
                )}
              </li>
            ))}
          </ul>
        </div>
      )}

      <div className="resultado-pie">
        <Boton variante="secundario" icono="editar" onClick={alRepetir}>
          Volver a responder
        </Boton>
      </div>
    </Tarjeta>
  );
}

function Cuestionario({ preguntas, alEnviar, alCancelar }) {
  const [respuestas, setRespuestas] = useState(() => Array(preguntas.length).fill(null));
  const [enviando, setEnviando] = useState(false);
  const respondidas = respuestas.filter((r) => r !== null).length;

  const responder = (i, valor) => setRespuestas((r) => r.map((x, j) => (j === i ? valor : x)));

  const enviar = async (e) => {
    e.preventDefault();
    setEnviando(true);
    try {
      await alEnviar(respuestas);
    } finally {
      setEnviando(false);
    }
  };

  return (
    <Tarjeta
      titulo="¿Cómo te está afectando cuidar?"
      descripcion="13 preguntas de sí o no, sobre cómo te sentiste en las últimas semanas. Nadie más ve tus respuestas."
    >
      <form className="cuestionario" onSubmit={enviar}>
        <ol>
          {preguntas.map((p, i) => (
            <li key={p.codigo}>
              <div className="pregunta">
                <strong>{p.texto}</strong>
                {p.ejemplo && <span className="secundario">{p.ejemplo}</span>}
              </div>
              <div className="si-no" role="radiogroup" aria-label={p.texto}>
                {[
                  [true, "Sí"],
                  [false, "No"],
                ].map(([valor, texto]) => (
                  <button
                    key={texto}
                    type="button"
                    role="radio"
                    aria-checked={respuestas[i] === valor}
                    className={`chip ${respuestas[i] === valor ? "activo" : ""}`}
                    onClick={() => responder(i, valor)}
                  >
                    {texto}
                  </button>
                ))}
              </div>
            </li>
          ))}
        </ol>

        <div className="cuestionario-pie">
          <span className="secundario">
            {respondidas} de {preguntas.length} respondidas
          </span>
          {alCancelar && (
            <Boton variante="secundario" onClick={alCancelar}>
              Cancelar
            </Boton>
          )}
          <Boton type="submit" icono="check" cargando={enviando} disabled={respondidas < preguntas.length}>
            Ver resultado
          </Boton>
        </div>
      </form>
    </Tarjeta>
  );
}

export default function VistaBienestar() {
  const { nombreUsuario, avisar, miPlanIncluye } = usePanel();
  const conBienestar = miPlanIncluye("bienestar");
  const [datos, setDatos] = useState(null);
  const [respondiendo, setRespondiendo] = useState(false);

  useEffect(() => {
    if (!conBienestar) return;
    let activo = true;
    api
      .obtener("/Bienestar", "No se pudo cargar tu bienestar.")
      .then((d) => activo && setDatos(d))
      .catch((error) => activo && avisar(error.message, "error"));
    return () => {
      activo = false;
    };
  }, [conBienestar, avisar]);

  const encabezado = (
    <EncabezadoVista
      titulo={`¿Y vos cómo estás, ${primerNombre(sinTitulo(nombreUsuario))}?`}
      descripcion="Cuidar cansa. Medirlo cada tanto ayuda a pedir ayuda a tiempo."
    />
  );

  if (!conBienestar) {
    return (
      <>
        {encabezado}
        <AvisoPlan
          titulo={`Tu bienestar está desde el plan ${planQueIncluye("bienestar")}`}
          texto="Una autoevaluación breve del esfuerzo que te lleva cuidar, con orientación y líneas de ayuda si lo necesitás."
        />
      </>
    );
  }

  if (!datos) return <Cargando />;

  const enviar = async (respuestas) => {
    try {
      const resultado = await api.crear("/Bienestar", { respuestas });
      const actualizado = await api.obtener("/Bienestar");
      setDatos({ ...actualizado, ultimo: resultado });
      setRespondiendo(false);
      window.scrollTo({ top: 0, behavior: "smooth" });
    } catch (error) {
      avisar(error.message, "error");
    }
  };

  const mostrarCuestionario = respondiendo || !datos.ultimo;
  const historial = [...datos.historial].reverse();

  return (
    <>
      {encabezado}

      <div className="rejilla-bienestar">
        <div className="columna-principal">
          {mostrarCuestionario ? (
            <Cuestionario
              preguntas={datos.preguntas}
              alEnviar={enviar}
              alCancelar={datos.ultimo ? () => setRespondiendo(false) : null}
            />
          ) : (
            <Resultado resultado={datos.ultimo} alRepetir={() => setRespondiendo(true)} />
          )}
        </div>

        <div className="columna-lateral">
          {historial.length > 1 && (
            <Tarjeta titulo="Cómo fue cambiando" descripcion="La línea marca 7 puntos: desde ahí el esfuerzo es elevado.">
              <div className="grafico-esfuerzo" role="img" aria-label={`Evolución: ${historial.map((h) => h.puntaje).join(", ")} puntos`}>
                {historial.map((h) => (
                  <div key={h.id} className="barra-esfuerzo">
                    <span className={`barra ${h.elevado ? "elevado" : ""}`} style={{ height: `${(h.puntaje / 13) * 100}%` }}>
                      <b>{h.puntaje}</b>
                    </span>
                    <small>{formatearFechaCorta(h.fecha).replace(/ de \d{4}$/, "")}</small>
                  </div>
                ))}
                <span className="linea-corte" style={{ bottom: `calc(22px + ${datos.puntoDeCorte / 13} * (100% - 42px))` }} aria-hidden="true" />
              </div>
            </Tarjeta>
          )}

          <Tarjeta titulo="Si necesitás hablar con alguien" className="tarjeta-ayuda">
            <ul className="lineas-ayuda">
              <li>
                <a href="tel:08009990091">
                  <strong>0800-999-0091</strong>
                </a>
                <span>Línea nacional de salud mental. Gratuita, las 24 horas. También atiende a familiares.</span>
              </li>
              <li>
                <a href="tel:135">
                  <strong>135</strong>
                </a>
                <span>
                  Centro de Asistencia al Suicida, desde CABA y GBA. Desde el resto del país: (011) 5275-1135. Todos los días, de 8 a 2 h.
                </span>
              </li>
              <li>
                <a href="tel:107">
                  <strong>107</strong>
                </a>
                <span>SAME, ante una emergencia.</span>
              </li>
            </ul>
          </Tarjeta>
        </div>
      </div>
    </>
  );
}
