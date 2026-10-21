import { cloneElement, isValidElement, useEffect, useId, useRef } from "react";
import { createPortal } from "react-dom";
import { MARCA, SUBTITULO_MARCA } from "./utilidades";

const ICONOS = {
  inicio: "M3 10.5 12 3l9 7.5V20a1 1 0 0 1-1 1h-5v-6h-6v6H4a1 1 0 0 1-1-1z",
  estrella: "m12 3 2.7 5.6 6.1.9-4.4 4.3 1 6.1L12 17l-5.4 2.9 1-6.1L3.2 9.5l6.1-.9z",
  calendario: "M7 3v3M17 3v3M4 9h16M5 5h14a1 1 0 0 1 1 1v13a1 1 0 0 1-1 1H5a1 1 0 0 1-1-1V6a1 1 0 0 1 1-1z",
  documento: "M7 3h7l5 5v12a1 1 0 0 1-1 1H7a1 1 0 0 1-1-1V4a1 1 0 0 1 1-1zM14 3v5h5M9 13h6M9 17h4",
  comunidad: "M16 20v-1.5a3.5 3.5 0 0 0-3.5-3.5h-5A3.5 3.5 0 0 0 4 18.5V20M10 11a3.5 3.5 0 1 0 0-7 3.5 3.5 0 0 0 0 7zM20 20v-1.5a3.5 3.5 0 0 0-2.5-3.35M15.5 4.2a3.5 3.5 0 0 1 0 6.6",
  especialistas: "M12 12a4 4 0 1 0 0-8 4 4 0 0 0 0 8zM4 21v-1a6 6 0 0 1 6-6h4a6 6 0 0 1 6 6v1",
  emergencia: "M12 3 4 6v6c0 4.5 3.3 8 8 9 4.7-1 8-4.5 8-9V6zM12 9v6M9 12h6",
  telefono: "M5 4h4l2 5-2.5 1.5a11 11 0 0 0 5 5L15 13l5 2v4a2 2 0 0 1-2 2A16 16 0 0 1 3 6a2 2 0 0 1 2-2z",
  mas: "M12 5v14M5 12h14",
  descargar: "M12 4v11M7 11l5 5 5-5M5 20h14",
  subir: "M12 16V4M7 9l5-5 5 5M5 20h14",
  papelera: "M4 7h16M10 11v6M14 11v6M6 7l1 13h10l1-13M9 7V4h6v3",
  editar: "M4 20h4L19 9a2.8 2.8 0 0 0-4-4L4 16zM13.5 6.5l4 4",
  izquierda: "m15 6-6 6 6 6",
  derecha: "m9 6 6 6-6 6",
  ubicacion: "M12 21s7-6.2 7-11a7 7 0 1 0-14 0c0 4.8 7 11 7 11zM12 12a2.5 2.5 0 1 0 0-5 2.5 2.5 0 0 0 0 5z",
  salir: "M9 21H5a1 1 0 0 1-1-1V4a1 1 0 0 1 1-1h4M16 17l5-5-5-5M21 12H9",
  cerrar: "M6 6l12 12M18 6 6 18",
  check: "m5 12.5 4.5 4.5L19 7.5",
  alerta: "M12 4 2.8 19.5a1 1 0 0 0 .9 1.5h16.6a1 1 0 0 0 .9-1.5L12 4zM12 10v4.5M12 17.5v.01",
  info: "M12 21a9 9 0 1 0 0-18 9 9 0 0 0 0 18zM12 11v5M12 7.5v.01",
  reloj: "M12 21a9 9 0 1 0 0-18 9 9 0 0 0 0 18zM12 7v5l3 2",
  persona: "M12 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8zM5 21a7 7 0 0 1 14 0",
  corazon: "M12 20s-7.5-4.6-9.3-9.2C1.5 7.6 3.6 4.5 7 4.5c2 0 3.6 1.1 5 3 1.4-1.9 3-3 5-3 3.4 0 5.5 3.1 4.3 6.3C19.5 15.4 12 20 12 20z",
  externo: "M14 4h6v6M20 4l-9 9M18 14v5a1 1 0 0 1-1 1H5a1 1 0 0 1-1-1V7a1 1 0 0 1 1-1h5",
  ojo: "M2.5 12S6 5.5 12 5.5 21.5 12 21.5 12 18 18.5 12 18.5 2.5 12 2.5 12zM12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6z",
  ojoTachado: "M3 3l18 18M10.6 6.1A9.7 9.7 0 0 1 12 6c6 0 9.5 6 9.5 6a17 17 0 0 1-2.6 3.3M6.4 7.3C3.9 9 2.5 12 2.5 12S6 18 12 18a9.4 9.4 0 0 0 4.3-1M9.9 9.9a3 3 0 0 0 4.2 4.2",
  filtro: "M4 5h16l-6 7.5V19l-4 2v-8.5z",
  lugar: "M4 21h16M6 21V8l6-4 6 4v13M10 21v-5h4v5",
  medico: "M9 3v4a3 3 0 0 0 6 0V3M12 10v4a5 5 0 0 0 10 0v-1M22 12a1.5 1.5 0 1 1-3 0 1.5 1.5 0 0 1 3 0z",
  chispa: "M12 3v4M12 17v4M3 12h4M17 12h4M6 6l2.5 2.5M15.5 15.5 18 18M6 18l2.5-2.5M15.5 8.5 18 6",
  ajustes: "M4 7h9M17 7h3M4 17h3M11 17h9M15 5v4M9 15v4",
  enviar: "M21 3 10.5 13.5M21 3l-6.5 18-4-7.5L3 9.5z",
  lista: "M10 6h10M10 12h10M10 18h10M4 6l1.2 1.2L7.5 5M4 12l1.2 1.2 2.3-2.2M4 18l1.2 1.2 2.3-2.2",
  pastilla: "M10.5 20.5a5 5 0 0 1-7-7l6-6a5 5 0 0 1 7 7zM7 10l7 7",
  diario: "M6 3h11a1 1 0 0 1 1 1v16a1 1 0 0 1-1 1H6a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2zM8 3v18M11 8h4M11 12h4",
  bandera: "M5 21V4M5 4h11l-2 4 2 4H5",
  menu: "M4 7h16M4 12h16M4 17h10",
  libro: "M4 5.5A2.5 2.5 0 0 1 6.5 3H20v15H6.5A2.5 2.5 0 0 0 4 20.5zM4 20.5A2.5 2.5 0 0 0 6.5 23H20v-5",
  respuesta: "M21 12a8 8 0 0 1-11.6 7.1L4 20.5l1.4-5A8 8 0 1 1 21 12z",
};

export function Icono({ nombre, tamano = 18, className = "" }) {
  return (
    <svg
      className={`icono ${className}`}
      width={tamano}
      height={tamano}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      focusable="false"
    >
      <path d={ICONOS[nombre] || ICONOS.info} />
    </svg>
  );
}

// Dos arcos: uno grande y uno chico al lado, que se apoya en el primero
export function Logo({ tamano = 36 }) {
  return (
    <svg className="logo" width={tamano} height={tamano} viewBox="0 0 40 40" aria-hidden="true">
      <rect width="40" height="40" rx="12" fill="var(--color-logo)" />
      <path
        d="M20 29.5s-8.5-5-10.4-10.2C8.3 15.7 10.6 12 14.4 12c2.3 0 4.1 1.3 5.6 3.4 1.5-2.1 3.3-3.4 5.6-3.4 3.8 0 6.1 3.7 4.8 7.3C28.5 24.5 20 29.5 20 29.5z"
        fill="#fff"
      />
      <path
        d="M14.5 20.5h3l1.6-2.6 2.2 5 1.6-2.4h2.6"
        fill="none"
        stroke="var(--color-logo)"
        strokeWidth="1.6"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

export function Marca({ className = "", compacta = false, subtitulo = SUBTITULO_MARCA }) {
  return (
    <div className={`marca ${className}`}>
      <Logo tamano={compacta ? 30 : 36} />
      <div className="marca-texto">
        <strong>{MARCA}</strong>
        {!compacta && <span>{subtitulo}</span>}
      </div>
    </div>
  );
}

export function EncabezadoVista({ titulo, descripcion, antetitulo, children }) {
  return (
    <header className="encabezado-vista">
      <div>
        {antetitulo && <span className="antetitulo">{antetitulo}</span>}
        <h1>{titulo}</h1>
        {descripcion && <p>{descripcion}</p>}
      </div>
      {children && <div className="encabezado-acciones">{children}</div>}
    </header>
  );
}

export function Tarjeta({ titulo, descripcion, accion, children, className = "" }) {
  return (
    <section className={`tarjeta ${className}`}>
      {(titulo || accion) && (
        <div className="tarjeta-cabecera">
          <div>
            {titulo && <h2>{titulo}</h2>}
            {descripcion && <p>{descripcion}</p>}
          </div>
          {accion}
        </div>
      )}
      {children}
    </section>
  );
}

export function EstadoVacio({ icono = "info", titulo, children, accion }) {
  return (
    <div className="estado-vacio">
      <div className="estado-vacio-icono">
        <Icono nombre={icono} tamano={22} />
      </div>
      <strong>{titulo}</strong>
      {children && <p>{children}</p>}
      {accion}
    </div>
  );
}

export function Cargando({ texto = "Cargando..." }) {
  return (
    <div className="cargando" role="status">
      <span className="spinner" aria-hidden="true" />
      {texto}
    </div>
  );
}

export function Campo({ etiqueta, ayuda, obligatorio, ancho, htmlFor, children }) {
  const id = useId();
  const esControl =
    isValidElement(children) &&
    ["input", "select", "textarea"].includes(children.type);

  const control =
    esControl && !children.props.id ? cloneElement(children, { id }) : children;

  const idEtiqueta = htmlFor || (esControl ? control.props.id : undefined);

  return (
    <div className={`campo ${ancho === "completo" ? "campo-completo" : ""}`}>
      <label htmlFor={idEtiqueta}>
        {etiqueta}
        {obligatorio && <span className="obligatorio" aria-hidden="true"> *</span>}
      </label>
      {control}
      {ayuda && <small className="ayuda">{ayuda}</small>}
    </div>
  );
}

export function Boton({
  variante = "principal",
  icono,
  cargando = false,
  children,
  className = "",
  ...props
}) {
  return (
    <button
      type="button"
      className={`boton boton-${variante} ${className}`}
      disabled={cargando || props.disabled}
      {...props}
    >
      {cargando ? (
        <span className="spinner spinner-chico" aria-hidden="true" />
      ) : (
        icono && <Icono nombre={icono} tamano={16} />
      )}
      {children && <span>{children}</span>}
    </button>
  );
}

export function BotonIcono({ icono, etiqueta, variante = "", ...props }) {
  return (
    <button
      type="button"
      className={`boton-icono ${variante}`}
      title={etiqueta}
      aria-label={etiqueta}
      {...props}
    >
      <Icono nombre={icono} tamano={16} />
    </button>
  );
}

export function Modal({ titulo, descripcion, abierto, alCerrar, children, pie, ancho = "medio" }) {
  const refDialogo = useRef(null);
  const refCerrar = useRef(alCerrar);

  // en un ref: si alCerrar va en las dependencias el foco salta en cada tecla
  useEffect(() => {
    refCerrar.current = alCerrar;
  });

  useEffect(() => {
    if (!abierto) return;

    const anterior = document.activeElement;
    const enfocable = refDialogo.current?.querySelector(
      "input, select, textarea, button:not(.modal-cerrar)"
    );
    enfocable?.focus();

    const alPresionar = (e) => {
      if (e.key === "Escape") refCerrar.current();
    };

    document.addEventListener("keydown", alPresionar);
    document.body.classList.add("sin-scroll");

    return () => {
      document.removeEventListener("keydown", alPresionar);
      document.body.classList.remove("sin-scroll");
      anterior?.focus?.();
    };
  }, [abierto]);

  if (!abierto) return null;

  // portal: si no, queda atrapado dentro de .vista (tiene transform)
  return createPortal(
    <div
      className="modal-fondo"
      onMouseDown={(e) => {
        if (e.target === e.currentTarget) alCerrar();
      }}
    >
      <div
        className={`modal modal-${ancho}`}
        role="dialog"
        aria-modal="true"
        aria-labelledby="modal-titulo"
        ref={refDialogo}
      >
        <header className="modal-cabecera">
          <div>
            <h2 id="modal-titulo">{titulo}</h2>
            {descripcion && <p>{descripcion}</p>}
          </div>
          <BotonIcono icono="cerrar" etiqueta="Cerrar" variante="modal-cerrar" onClick={alCerrar} />
        </header>

        <div className="modal-cuerpo">{children}</div>

        {pie && <footer className="modal-pie">{pie}</footer>}
      </div>
    </div>,
    document.body
  );
}

export function Confirmacion({ datos, alCerrar }) {
  const abierto = !!datos;

  return (
    <Modal
      abierto={abierto}
      alCerrar={alCerrar}
      titulo={datos?.titulo}
      ancho="chico"
      pie={
        <>
          <Boton variante="secundario" onClick={alCerrar}>
            Cancelar
          </Boton>
          <Boton
            variante={datos?.variante || "peligro"}
            icono={datos?.icono || "papelera"}
            onClick={async () => {
              const accion = datos.accion;
              alCerrar();
              await accion();
            }}
          >
            {datos?.textoBoton || "Eliminar"}
          </Boton>
        </>
      }
    >
      <p className="confirmacion-texto">{datos?.mensaje}</p>
    </Modal>
  );
}

export function Avisos({ avisos, alCerrar }) {
  return (
    <div className="avisos" aria-live="polite">
      {avisos.map((aviso) => (
        <div key={aviso.id} className={`aviso aviso-${aviso.tipo}`} role="status">
          <Icono nombre={aviso.tipo === "error" ? "alerta" : "check"} tamano={18} />
          <span>{aviso.texto}</span>
          <button
            type="button"
            className="aviso-cerrar"
            onClick={() => alCerrar(aviso.id)}
            aria-label="Cerrar aviso"
          >
            <Icono nombre="cerrar" tamano={14} />
          </button>
        </div>
      ))}
    </div>
  );
}
