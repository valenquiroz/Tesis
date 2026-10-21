import { useEffect, useState } from "react";
import { api } from "../api";
import { Cargando, EncabezadoVista, EstadoVacio, Icono, Tarjeta } from "../componentes";
import { usePanel } from "../contexto";
import { esDelTipo, nombrarTipos, planQueIncluye, TIPOS_DISCAPACIDAD } from "../utilidades";
import { AvisoPlan } from "./comunes";

function TarjetaArticulo({ articulo: a }) {
  return (
    <article className="tarjeta-articulo">
      <span className={`etiqueta ${a.tipoDiscapacidad ? "etiqueta-naranja" : "etiqueta-suave"}`}>
        {a.tipoDiscapacidad ? `Discapacidad ${nombrarTipos(a.tipoDiscapacidad)}` : "Sobre el cuidado"}
      </span>
      <h3>{a.titulo}</h3>
      <p className="articulo-autores">{a.autores}</p>
      <p>{a.resumen}</p>
      <div className="articulo-pie">
        <span className="secundario">
          {a.fuente}
          {a.anio ? ` · ${a.anio}` : ""}
        </span>
        <a className="boton boton-secundario boton-chico" href={a.enlace} target="_blank" rel="noopener noreferrer">
          Leer en la fuente <Icono nombre="externo" tamano={14} />
        </a>
      </div>
    </article>
  );
}

// Escritos de especialistas ya publicados: la plataforma cuenta de qué tratan y enlaza a la fuente.
// Se muestran los de la discapacidad de la persona y, aparte, los generales sobre el cuidado.
export default function VistaArticulos() {
  const { personas, avisar, miPlanIncluye } = usePanel();
  const conArticulos = miPlanIncluye("articulos");
  const [articulos, setArticulos] = useState(null);

  // arranca en la discapacidad de la primera persona a cargo
  const tiposDeMisPersonas = [...new Set(personas.map((p) => p.tipoDiscapacidad).filter(Boolean))];
  const [tipo, setTipo] = useState(tiposDeMisPersonas[0] ?? TIPOS_DISCAPACIDAD[0]);

  useEffect(() => {
    if (!conArticulos) return;
    let activo = true;
    api
      .obtener("/Articulos", "No se pudieron cargar los artículos.")
      .then((lista) => activo && setArticulos(lista))
      .catch((error) => activo && avisar(error.message, "error"));
    return () => {
      activo = false;
    };
  }, [conArticulos, avisar]);

  const encabezado = (
    <EncabezadoVista
      titulo="Artículos"
      descripcion="Escritos de especialistas ya publicados, según la discapacidad de cada persona. Cada uno se lee en su fuente original."
    />
  );

  if (!conArticulos) {
    return (
      <>
        {encabezado}
        <AvisoPlan
          titulo={`Los artículos están en el plan ${planQueIncluye("articulos")}`}
          texto="Escritos de especialistas sobre cada tipo de discapacidad y sobre el cuidado, elegidos de fuentes confiables y con el enlace para leerlos."
        />
      </>
    );
  }

  const lista = articulos || [];
  const delTipo = lista.filter((a) => esDelTipo(a.tipoDiscapacidad, tipo));
  const generales = lista.filter((a) => !a.tipoDiscapacidad);
  // primero los tipos de las personas a cargo
  const tipos = [...tiposDeMisPersonas, ...TIPOS_DISCAPACIDAD.filter((t) => !tiposDeMisPersonas.includes(t))];

  return (
    <>
      {encabezado}

      {!articulos ? (
        <Cargando />
      ) : lista.length === 0 ? (
        <Tarjeta>
          <EstadoVacio icono="libro" titulo="Todavía no hay artículos" />
        </Tarjeta>
      ) : (
        <>
          <div className="chips filtros-categorias" role="group" aria-label="Tipo de discapacidad">
            {tipos.map((t) => (
              <button key={t} type="button" className={`chip ${tipo === t ? "activo" : ""}`} aria-pressed={tipo === t} onClick={() => setTipo(t)}>
                {t}
                {tiposDeMisPersonas.includes(t) && <small>tu caso</small>}
              </button>
            ))}
          </div>

          {tipo && (
            <section className="seccion-articulos">
              <h2 className="rotulo">Para discapacidad {tipo.toLowerCase()}</h2>
              {delTipo.length === 0 ? (
                <p className="secundario">Todavía no hay artículos específicos de este tipo.</p>
              ) : (
                <div className="rejilla-articulos">
                  {delTipo.map((a) => (
                    <TarjetaArticulo key={a.id} articulo={a} />
                  ))}
                </div>
              )}
            </section>
          )}

          {generales.length > 0 && (
            <section className="seccion-articulos">
              <h2 className="rotulo">Sobre el cuidado, para cualquier persona que cuida</h2>
              <div className="rejilla-articulos">
                {generales.map((a) => (
                  <TarjetaArticulo key={a.id} articulo={a} />
                ))}
              </div>
            </section>
          )}

          <p className="nota-pie">
            <Icono nombre="info" tamano={14} />
            Los artículos son de sus autores y de quienes los publicaron: acá solo se cuenta de qué tratan y se enlazan.
          </p>
        </>
      )}
    </>
  );
}
