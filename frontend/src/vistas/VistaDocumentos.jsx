import { useRef, useState } from "react";
import {
  Boton,
  BotonIcono,
  Campo,
  Cargando,
  EncabezadoVista,
  EstadoVacio,
  Icono,
  Tarjeta,
} from "../componentes";
import { usePanel } from "../contexto";
import {
  EXTENSIONES_PERMITIDAS,
  extensionArchivo,
  formatearFechaCorta,
  formatearTamano,
  plural,
  TAMANO_MAXIMO_ARCHIVO,
  puedeGestionar,
} from "../utilidades";

const TIPOS_LEGIBLES = {
  "application/pdf": "PDF",
  "image/jpeg": "Imagen JPG",
  "image/png": "Imagen PNG",
};

function validarArchivo(archivo) {
  const extension = archivo.name.split(".").pop().toLowerCase();
  if (!EXTENSIONES_PERMITIDAS.includes(extension)) {
    return "Solo se aceptan archivos PDF, JPG o PNG.";
  }
  if (archivo.size > TAMANO_MAXIMO_ARCHIVO) {
    return "El archivo supera el máximo de 10 MB.";
  }
  if (archivo.size === 0) return "El archivo está vacío.";
  return null;
}

export default function VistaDocumentos() {
  const { personas, documentos, cargando, acciones } = usePanel();

  const [personaId, setPersonaId] = useState("");
  const [archivo, setArchivo] = useState(null);
  const [errorArchivo, setErrorArchivo] = useState("");
  const [arrastrando, setArrastrando] = useState(false);
  const [subiendo, setSubiendo] = useState(false);
  const [filtroPersona, setFiltroPersona] = useState("todas");
  const [busqueda, setBusqueda] = useState("");
  const refInput = useRef(null);

  // suben quienes gestionan a la persona; el espacio depende del plan
  const subibles = personas.filter(puedeGestionar);
  const puedeBorrar = (documento) => puedeGestionar(personas.find((p) => p.id === documento.personaAsistidaId));

  // con una sola persona no hace falta elegir
  const personaElegida = subibles.length === 1 ? String(subibles[0].id) : personaId;

  const elegirArchivo = (nuevo) => {
    if (!nuevo) return;
    const error = validarArchivo(nuevo);
    setErrorArchivo(error || "");
    setArchivo(error ? null : nuevo);
  };

  const limpiarArchivo = () => {
    setArchivo(null);
    setErrorArchivo("");
    if (refInput.current) refInput.current.value = "";
  };

  const subir = async (e) => {
    e.preventDefault();
    if (!personaElegida || !archivo) return;

    setSubiendo(true);
    const ok = await acciones.subirDocumento(personaElegida, archivo);
    setSubiendo(false);

    if (ok) limpiarArchivo();
  };

  const texto = busqueda.trim().toLowerCase();
  const visibles = documentos.filter(
    (d) =>
      (filtroPersona === "todas" || String(d.personaAsistidaId) === filtroPersona) &&
      (!texto || d.nombreOriginal.toLowerCase().includes(texto))
  );

  return (
    <>
      <EncabezadoVista
        titulo="Documentos"
        descripcion="Estudios, recetas y certificados de cada persona."
      />

      {(personas.length === 0 || subibles.length > 0) && (
      <Tarjeta titulo="Subir un documento" className="tarjeta-subida">
        {personas.length === 0 ? (
          <div className="alerta alerta-info">
            Para subir documentos primero agregá una persona asistida desde Inicio.
          </div>
        ) : (
          <form className="subida" onSubmit={subir}>
            {subibles.length > 1 && (
              <Campo etiqueta="¿De quién es el documento?" obligatorio>
                <select value={personaId} onChange={(e) => setPersonaId(e.target.value)} required>
                  <option value="">Seleccioná una persona</option>
                  {subibles.map((p) => (
                    <option key={p.id} value={p.id}>
                      {p.nombreCompleto}
                    </option>
                  ))}
                </select>
              </Campo>
            )}

            <label
              className={`zona-archivo ${arrastrando ? "arrastrando" : ""} ${archivo ? "con-archivo" : ""}`}
              onDragOver={(e) => {
                e.preventDefault();
                setArrastrando(true);
              }}
              onDragLeave={() => setArrastrando(false)}
              onDrop={(e) => {
                e.preventDefault();
                setArrastrando(false);
                elegirArchivo(e.dataTransfer.files[0]);
              }}
            >
              <input
                ref={refInput}
                type="file"
                className="oculto-visual"
                accept=".pdf,.jpg,.jpeg,.png,application/pdf,image/jpeg,image/png"
                onChange={(e) => elegirArchivo(e.target.files[0])}
              />

              {archivo ? (
                <span className="zona-archivo-elegido">
                  <span className={`tipo-archivo tipo-${extensionArchivo(archivo.name).toLowerCase()}`}>
                    {extensionArchivo(archivo.name)}
                  </span>
                  <span className="fila-datos">
                    <strong className="truncar">{archivo.name}</strong>
                    <span className="secundario">{formatearTamano(archivo.size)} · Listo para subir</span>
                  </span>
                  <BotonIcono
                    icono="cerrar"
                    etiqueta="Quitar archivo"
                    onClick={(e) => {
                      e.preventDefault();
                      limpiarArchivo();
                    }}
                  />
                </span>
              ) : (
                <>
                  <span className="zona-archivo-icono">
                    <Icono nombre="subir" tamano={22} />
                  </span>
                  <strong>Arrastrá un archivo o hacé clic para elegirlo</strong>
                  <span className="secundario">PDF, JPG o PNG · hasta 10 MB</span>
                </>
              )}
            </label>

            {errorArchivo && (
              <div className="alerta alerta-error" role="alert">
                <Icono nombre="alerta" tamano={16} />
                {errorArchivo}
              </div>
            )}

            <div className="subida-acciones">
              <Boton
                type="submit"
                icono="subir"
                cargando={subiendo}
                disabled={!archivo || !personaElegida}
              >
                {subiendo ? "Subiendo..." : "Subir documento"}
              </Boton>
            </div>
          </form>
        )}
      </Tarjeta>
      )}

      {personas.length > 0 && (
      <Tarjeta
        titulo="Documentos cargados"
        descripcion={plural(documentos.length, "documento cargado", "documentos cargados")}
        accion={
          documentos.length > 0 && (
            <div className="filtros-documentos">
              <input
                type="search"
                placeholder="Buscar por nombre..."
                value={busqueda}
                onChange={(e) => setBusqueda(e.target.value)}
                aria-label="Buscar documentos"
              />
              {personas.length > 1 && (
                <select
                  value={filtroPersona}
                  onChange={(e) => setFiltroPersona(e.target.value)}
                  aria-label="Filtrar por persona"
                >
                  <option value="todas">Todas las personas</option>
                  {personas.map((p) => (
                    <option key={p.id} value={p.id}>
                      {p.nombreCompleto}
                    </option>
                  ))}
                </select>
              )}
            </div>
          )
        }
      >
        {cargando ? (
          <Cargando texto="Cargando documentos..." />
        ) : documentos.length === 0 ? (
          <EstadoVacio icono="documento" titulo="No hay documentos cargados">
            Usá el formulario de arriba para subir el primero.
          </EstadoVacio>
        ) : visibles.length === 0 ? (
          <EstadoVacio icono="filtro" titulo="Sin resultados">
            Ningún documento coincide con la búsqueda.
          </EstadoVacio>
        ) : (
          <div className="tabla-contenedor">
            <table className="tabla">
              <thead>
                <tr>
                  <th>Archivo</th>
                  <th>Persona</th>
                  <th>Fecha</th>
                  <th>Tamaño</th>
                  <th>
                    <span className="oculto-visual">Acciones</span>
                  </th>
                </tr>
              </thead>

              <tbody>
                {visibles.map((documento) => {
                  const extension = extensionArchivo(documento.nombreOriginal);
                  return (
                    <tr key={documento.id}>
                      <td>
                        <div className="celda-archivo">
                          <span className={`tipo-archivo tipo-${extension.toLowerCase()}`}>
                            {extension}
                          </span>
                          <div className="fila-datos">
                            <strong className="truncar" title={documento.nombreOriginal}>
                              {documento.nombreOriginal}
                            </strong>
                            <span className="secundario">
                              {TIPOS_LEGIBLES[documento.tipoContenido] || "Archivo"}
                            </span>
                          </div>
                        </div>
                      </td>
                      <td data-etiqueta="Persona">
                        {documento.personaAsistida?.nombreCompleto || "—"}
                      </td>
                      <td data-etiqueta="Fecha">{formatearFechaCorta(documento.fechaCarga)}</td>
                      <td data-etiqueta="Tamaño">{formatearTamano(documento.tamanoBytes)}</td>
                      <td>
                        <div className="acciones-fila">
                          <BotonIcono
                            icono="descargar"
                            etiqueta="Descargar"
                            onClick={() => acciones.descargarDocumento(documento)}
                          />
                          {puedeBorrar(documento) && (
                            <BotonIcono
                              icono="papelera"
                              etiqueta="Eliminar"
                              variante="peligro"
                              onClick={() => acciones.eliminarDocumento(documento)}
                            />
                          )}
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </Tarjeta>
      )}
    </>
  );
}
