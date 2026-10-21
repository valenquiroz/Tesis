import { useCallback, useEffect, useState } from "react";
import { api } from "../api";
import { Boton, Cargando, EncabezadoVista, Icono } from "../componentes";
import { usePanel } from "../contexto";
import { formatearTamano } from "../utilidades";

const sinLimite = (valor, texto) => (valor == null ? "Sin límite" : texto(valor));

// lo que se compara entre planes, en el orden en que importa al elegir
const FILAS = [
  { texto: "Personas a cargo", valor: (p) => sinLimite(p.personas, (n) => (n === 1 ? "1" : `Hasta ${n}`)) },
  { texto: "Tu día, medicación y calendario", valor: () => true },
  { texto: "Emergencia: 107, contactos y ficha", valor: () => true },
  { texto: "Documentos", valor: (p) => sinLimite(p.espacioDocumentos, formatearTamano) },
  { texto: "Guía de trámites", valor: (p) => p.tramites },
  { texto: "Recursos recomendados", valor: (p) => p.recursos },
  { texto: "Tu bienestar", valor: (p) => p.bienestar },
  { texto: "Diario y resumen para la consulta", valor: (p) => (p.diario ? (p.informe ? "Historial de un año" : "Último mes") : false) },
  { texto: "Cuidadores y observadores en tu grupo", valor: (p) => (p.integrantes === 0 ? false : sinLimite(p.integrantes, (n) => `Hasta ${n}`)) },
  { texto: "Artículos de especialistas", valor: (p) => p.articulos },
  { texto: "Ver qué registró cada cuidador", valor: (p) => p.supervision },
];

const PARA_QUIEN = {
  Gratis: "Para organizar el día a día de una persona.",
  Equipo: "Para quien reparte el cuidado o contrató a alguien, con trámites y recomendaciones.",
  Plus: "Para quien coordina a muchas personas, como una residencia, y quiere supervisar a cada cuidador.",
};

function Celda({ valor }) {
  if (valor === true) return <Icono nombre="check" tamano={16} className="plan-si" />;
  if (!valor) return <span className="plan-no" aria-label="No incluido">—</span>;
  return <span>{valor}</span>;
}

export default function VistaPlan() {
  const { avisar, cambiarPlan, confirmar } = usePanel();
  const [datos, setDatos] = useState(null);
  const [cambiando, setCambiando] = useState(null);

  const cargar = useCallback(
    () =>
      api
        .obtener("/Cuenta/plan", "No se pudo cargar tu plan.")
        .then(setDatos)
        .catch((error) => avisar(error.message, "error")),
    [avisar]
  );

  useEffect(() => {
    let activo = true;
    api
      .obtener("/Cuenta/plan", "No se pudo cargar tu plan.")
      .then((d) => activo && setDatos(d))
      .catch((error) => activo && avisar(error.message, "error"));
    return () => {
      activo = false;
    };
  }, [avisar]);

  const elegir = (plan) =>
    confirmar({
      titulo: `¿Pasar al plan ${plan.nombre}?`,
      mensaje:
        plan.precioMensualUsd > 0
          ? `Cuesta USD ${plan.precioMensualUsd} por mes. En este prototipo el pago está simulado: el cambio es inmediato y no se cobra nada.`
          : "Vas a quedar con las funciones del plan Gratis.",
      textoBoton: "Cambiar de plan",
      variante: "principal",
      icono: "check",
      accion: async () => {
        setCambiando(plan.codigo);
        try {
          const r = await api.actualizar("/Cuenta/plan", { plan: plan.codigo });
          avisar(r.message);
          await cambiarPlan(r.plan);
          await cargar();
        } catch (error) {
          avisar(error.message, "error");
        } finally {
          setCambiando(null);
        }
      },
    });

  if (!datos) return <Cargando />;

  const { uso } = datos;

  return (
    <>
      <EncabezadoVista
        titulo="Tu plan"
        descripcion="Lo básico del día a día es gratis, y la emergencia nunca se cobra. Los planes pagos suman trámites, diario, recomendaciones y gente que te ayude."
      />

      {datos.dondeAyuda.map((g) => (
        <section key={g.nombre} className="plan-cubierto">
          <Icono nombre="check" tamano={20} />
          <div>
            <h2>
              Tenés el plan {g.plan} de {g.nombre}
            </h2>
            <p>
              Lo paga {g.dueno}. Para cuidar {g.personas === 1 ? "a la persona" : `a las ${g.personas} personas`} que te asignó
              tenés todas las funciones del plan {g.plan} y no pagás nada.
            </p>
          </div>
        </section>
      ))}

      {datos.dondeAyuda.length > 0 && (
        <h2 className="rotulo titulo-planes">
          {uso.personas === 0 ? "Si querés armar tu propio grupo" : "Tu propio grupo"}
        </h2>
      )}

      <section className="planes">
        {datos.planes.map((p) => {
          const actual = p.codigo === datos.plan;
          return (
            <article key={p.codigo} className={`plan ${actual ? "plan-actual" : ""}`}>
              {actual && <span className="plan-marca">{datos.dondeAyuda.length > 0 ? "Tu grupo propio" : "Tu plan"}</span>}
              <h2>{p.nombre}</h2>
              <p className="plan-precio">
                USD {p.precioMensualUsd} <span>{p.precioMensualUsd === 0 ? "para siempre" : "por mes"}</span>
              </p>
              <p className="plan-para">{PARA_QUIEN[p.codigo]}</p>
              <dl className="plan-filas">
                {FILAS.map((f) => (
                  <div key={f.texto}>
                    <dt>{f.texto}</dt>
                    <dd>
                      <Celda valor={f.valor(p)} />
                    </dd>
                  </div>
                ))}
              </dl>
              {actual ? (
                <p className="plan-uso">
                  Usás {uso.personas} {uso.personas === 1 ? "persona" : "personas"}, {uso.integrantes} en tu grupo
                  {uso.documentosBytes > 0 ? ` y ${formatearTamano(uso.documentosBytes)} en documentos` : ""}.
                </p>
              ) : (
                <Boton
                  variante={p.precioMensualUsd > 0 ? "principal" : "secundario"}
                  className="ancho-total"
                  cargando={cambiando === p.codigo}
                  onClick={() => elegir(p)}
                >
                  Pasar a {p.nombre}
                </Boton>
              )}
            </article>
          );
        })}
      </section>

      <p className="nota-pie">
        <Icono nombre="info" tamano={14} />
        Prototipo: el pago está simulado. Al cambiar de plan no se pide ninguna tarjeta ni se cobra nada.
      </p>
    </>
  );
}
