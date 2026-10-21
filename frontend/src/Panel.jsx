import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { api, guardarSesion, leerSesion, tieneRol } from "./api";
import { Avisos, Confirmacion, Icono, Marca } from "./componentes";
import { PanelContexto } from "./contexto";
import { ModalEquipo, ModalPreferencias } from "./cuenta";
import { FormularioContacto, FormularioMedicamento, FormularioPersona, FormularioTurno } from "./formularios";
import { iniciales, planIncluye, PLANES } from "./utilidades";
import VistaCalendario from "./vistas/VistaCalendario";
import VistaDocumentos from "./vistas/VistaDocumentos";
import VistaEmergencia from "./vistas/VistaEmergencia";
import VistaInicio from "./vistas/VistaInicio";
import VistaAdministracion from "./vistas/VistaAdministracion";
import VistaArticulos from "./vistas/VistaArticulos";
import VistaGrupo from "./vistas/VistaGrupo";
import VistaPlan from "./vistas/VistaPlan";
import VistaBienestar from "./vistas/VistaBienestar";
import VistaDiario from "./vistas/VistaDiario";
import VistaMedicacion from "./vistas/VistaMedicacion";
import VistaRecomendaciones from "./vistas/VistaRecomendaciones";
import VistaTramites from "./vistas/VistaTramites";

const SECCIONES = [
  {
    titulo: "Hoy",
    items: [{ id: "inicio", icono: "inicio", texto: "Tu día" }],
  },
  {
    titulo: "Cuidado diario",
    items: [
      { id: "medicacion", icono: "pastilla", texto: "Medicación" },
      { id: "diario", icono: "diario", texto: "Diario" },
      { id: "calendario", icono: "calendario", texto: "Calendario" },
      { id: "documentos", icono: "documento", texto: "Documentos" },
    ],
  },
  {
    titulo: "Orientación",
    items: [
      { id: "tramites", icono: "lista", texto: "Trámites" },
      { id: "recomendaciones", icono: "estrella", texto: "Recursos" },
      { id: "articulos", icono: "libro", texto: "Artículos" },
    ],
  },
  {
    titulo: "Quienes cuidan",
    items: [
      { id: "grupo", icono: "comunidad", texto: "Tu grupo" },
      { id: "bienestar", icono: "corazon", texto: "Tu bienestar" },
    ],
  },
  {
    titulo: "Cuenta",
    items: [{ id: "plan", icono: "bandera", texto: "Tu plan" }],
  },
];

// accesos fijos de la barra inferior en el celular; el resto va en "Más"
const ACCESOS_MOVIL = ["inicio", "medicacion", "calendario", "diario"];

const SECCION_ADMINISTRACION = {
  titulo: "Administración",
  items: [{ id: "administracion", icono: "ajustes", texto: "Administración" }],
};

const VISTAS = {
  administracion: VistaAdministracion,
  inicio: VistaInicio,
  recomendaciones: VistaRecomendaciones,
  tramites: VistaTramites,
  calendario: VistaCalendario,
  documentos: VistaDocumentos,
  articulos: VistaArticulos,
  grupo: VistaGrupo,
  plan: VistaPlan,
  emergencia: VistaEmergencia,
  medicacion: VistaMedicacion,
  diario: VistaDiario,
  bienestar: VistaBienestar,
};

const RUTAS = {
  personas: "/PersonasAsistidas",
  turnos: "/Turnos",
  documentos: "/Documentos",
  contactos: "/ContactosEmergencia",
  medicamentos: "/Medicamentos",
};

const ERRORES_CARGA = {
  personas: "No se pudieron cargar las personas asistidas.",
  turnos: "No se pudieron cargar los turnos.",
  documentos: "No se pudieron cargar los documentos.",
  contactos: "No se pudieron cargar los contactos de emergencia.",
  medicamentos: "No se pudo cargar la medicación.",
};

async function obtenerDatos(claves = Object.keys(RUTAS)) {
  const resultados = await Promise.allSettled(
    claves.map((clave) => api.obtener(RUTAS[clave], ERRORES_CARGA[clave]))
  );

  const nuevos = {};
  const errores = [];

  resultados.forEach((resultado, i) => {
    if (resultado.status === "fulfilled") {
      nuevos[claves[i]] = resultado.value || [];
    } else if (resultado.reason?.estado !== 401) {
      errores.push(resultado.reason.message);
    }
  });

  return { nuevos, errores };
}

export default function Panel({ cerrarSesion }) {
  const sesion = leerSesion();
  const nombreUsuario = sesion?.nombreCompleto || "Cuidador/a";
  const esAdministrador = tieneRol(sesion, "Administrador");
  const secciones = esAdministrador ? [...SECCIONES, SECCION_ADMINISTRACION] : SECCIONES;

  // la sección queda en la dirección (#medicacion): funciona "Atrás" y se puede guardar el enlace
  const vistasValidas = [...secciones.flatMap((s) => s.items.map((i) => i.id)), "emergencia"];
  const vistaInicial = "inicio";
  const vistaDeLaDireccion = () => {
    const pedida = window.location.hash.slice(1);
    return vistasValidas.includes(pedida) ? pedida : vistaInicial;
  };
  const [vista, setVista] = useState(vistaDeLaDireccion);
  const [plan, setPlan] = useState(sesion?.plan || "Gratis");
  const [nombreGrupo, setNombreGrupo] = useState(sesion?.nombreGrupo || null);
  const [menuMovil, setMenuMovil] = useState(false);
  const [hoy, setHoy] = useState(() => new Date());

  const [datos, setDatos] = useState({
    personas: [],
    turnos: [],
    documentos: [],
    contactos: [],
    medicamentos: [],
  });
  const [cargando, setCargando] = useState(true);

  // cache por persona
  const [recomendaciones, setRecomendaciones] = useState({});
  const [personaElegidaId, setPersonaElegidaId] = useState(null);

  const [avisos, setAvisos] = useState([]);
  const [confirmacion, setConfirmacion] = useState(null);
  const [edicion, setEdicion] = useState(null); // { tipo, datos }
  const idAviso = useRef(0);
  const cerrarEdicion = useCallback(() => setEdicion(null), [setEdicion]);

  const cerrarAviso = useCallback(
    (id) => setAvisos((lista) => lista.filter((a) => a.id !== id)),
    []
  );

  const avisar = useCallback(
    (texto, tipo = "exito") => {
      const id = ++idAviso.current;
      setAvisos((lista) => [...lista.slice(-2), { id, texto, tipo }]);
      setTimeout(() => cerrarAviso(id), tipo === "error" ? 7000 : 4500);
    },
    [cerrarAviso]
  );

  const aplicarCarga = useCallback(
    ({ nuevos, errores }) => {
      // si la API está caída llegan 4 errores iguales
      [...new Set(errores)].forEach((mensaje) => avisar(mensaje, "error"));
      setDatos((anteriores) => ({ ...anteriores, ...nuevos }));
      setCargando(false);
    },
    [avisar]
  );

  const recargar = async (claves) => aplicarCarga(await obtenerDatos(claves));

  useEffect(() => {
    let activo = true;
    obtenerDatos().then((resultado) => activo && aplicarCarga(resultado));
    return () => {
      activo = false;
    };
  }, [aplicarCarga]);

  useEffect(() => {
    const intervalo = setInterval(() => setHoy(new Date()), 60_000);
    return () => clearInterval(intervalo);
  }, []);

  const { personas } = datos;

  const idRecomendaciones = personas.some((p) => p.id === personaElegidaId)
    ? personaElegidaId
    : personas[0]?.id ?? null;

  // los recursos dependen del plan que cubre a la persona
  const conRecursos = planIncluye(personas.find((p) => p.id === idRecomendaciones), "recursos");

  useEffect(() => {
    if (!idRecomendaciones || !conRecursos || recomendaciones[idRecomendaciones]) return;

    let cancelado = false;

    api
      .obtener(
        `/Recomendaciones/${idRecomendaciones}`,
        "No se pudieron obtener las recomendaciones."
      )
      .then((lista) => ({ estado: "listo", lista: lista || [] }))
      .catch((error) => ({ estado: "error", mensaje: error.message }))
      .then((resultado) => {
        if (!cancelado) {
          setRecomendaciones((r) => ({ ...r, [idRecomendaciones]: resultado }));
        }
      });

    return () => {
      cancelado = true;
    };
  }, [idRecomendaciones, conRecursos, recomendaciones]);

  const olvidarRecomendaciones = (personaId) =>
    setRecomendaciones((r) => {
      const copia = { ...r };
      delete copia[personaId];
      return copia;
    });

  // "me sirvió" / "no me sirvió": se marca en pantalla sin reordenar la lista
  // (que las tarjetas no salten); el nuevo orden se ve la próxima vez que se entra
  const MOTIVO_PROPIO = "Marcaste que te sirvió";
  const marcarValoracion = (personaId, recursoId, util) =>
    setRecomendaciones((r) => {
      const estado = r[personaId];
      if (estado?.estado !== "listo") return r;
      const lista = estado.lista.map((item) => {
        if (item.recurso.id !== recursoId) return item;
        const motivos = (item.motivos || []).filter((m) => m !== MOTIVO_PROPIO);
        return { ...item, valoracion: util, motivos: util ? [MOTIVO_PROPIO, ...motivos] : motivos };
      });
      return { ...r, [personaId]: { ...estado, lista, desactualizada: true } };
    });

  const valorarRecurso = async (personaId, recurso, util) => {
    const anterior = recomendaciones[personaId]?.lista?.find((i) => i.recurso.id === recurso.id)?.valoracion ?? null;
    const nuevo = anterior === util ? null : util;
    marcarValoracion(personaId, recurso.id, nuevo);
    try {
      await api.actualizar(`/Recomendaciones/${personaId}/valoraciones/${recurso.id}`, { util: nuevo });
    } catch (error) {
      marcarValoracion(personaId, recurso.id, anterior);
      avisar(error.message, "error");
    }
  };

  // al salir de Recursos, lo valorado se vuelve a pedir para que aparezca con el orden nuevo
  const olvidarValoradas = () =>
    setRecomendaciones((r) => {
      const viejas = Object.keys(r).filter((id) => r[id]?.desactualizada);
      if (viejas.length === 0) return r;
      const copia = { ...r };
      viejas.forEach((id) => delete copia[id]);
      return copia;
    });

  const ejecutar = async (operacion, textoExito) => {
    try {
      const resultado = await operacion();
      if (textoExito) avisar(textoExito);
      return resultado ?? true;
    } catch (error) {
      if (error.estado !== 401) avisar(error.message, "error");
      return false;
    }
  };

  const confirmarYEliminar = ({ titulo, mensaje, ruta, textoExito, recargarClaves, alTerminar }) =>
    setConfirmacion({
      titulo,
      mensaje,
      accion: async () => {
        const ok = await ejecutar(() => api.eliminar(ruta), textoExito);
        if (ok) {
          alTerminar?.();
          await recargar(recargarClaves);
        }
      },
    });

  const acciones = {
    nuevaPersona: () => setEdicion({ tipo: "persona", datos: null }),
    editarPersona: (persona) => setEdicion({ tipo: "persona", datos: persona }),

    guardarPersona: async (cuerpo, id) => {
      const ok = await ejecutar(
        () =>
          id
            ? api.actualizar(`/PersonasAsistidas/${id}`, cuerpo)
            : api.crear("/PersonasAsistidas", cuerpo, "No se pudo guardar la persona."),
        id ? "Los datos se actualizaron." : "Persona agregada correctamente."
      );

      if (ok) {
        if (id) olvidarRecomendaciones(id);
        setEdicion(null);
        // turnos y documentos muestran el nombre de la persona
        await recargar(id ? Object.keys(RUTAS) : ["personas"]);
      }
      return ok;
    },

    eliminarPersona: (persona) =>
      confirmarYEliminar({
        titulo: `¿Eliminar a ${persona.nombreCompleto}?`,
        mensaje:
          "También se eliminarán sus turnos, documentos y contactos de emergencia. Esta acción no se puede deshacer.",
        ruta: `/PersonasAsistidas/${persona.id}`,
        textoExito: "Persona eliminada.",
        alTerminar: () => olvidarRecomendaciones(persona.id),
      }),

    nuevoTurno: (fechaSugerida, valoresIniciales) =>
      setEdicion({ tipo: "turno", datos: null, fechaSugerida, valoresIniciales }),
    editarTurno: (turno) => setEdicion({ tipo: "turno", datos: turno }),

    guardarTurno: async (cuerpo, id) => {
      const ok = await ejecutar(
        () =>
          id
            ? api.actualizar(`/Turnos/${id}`, cuerpo)
            : api.crear("/Turnos", cuerpo, "No se pudo registrar el turno."),
        id ? "Turno actualizado." : "Turno agendado."
      );

      if (ok) {
        setEdicion(null);
        await recargar(["turnos"]);
      }
      return ok;
    },

    eliminarTurno: (turno) =>
      confirmarYEliminar({
        titulo: "¿Eliminar este turno?",
        mensaje: `Se eliminará "${turno.titulo}" del calendario.`,
        ruta: `/Turnos/${turno.id}`,
        textoExito: "Turno eliminado.",
        recargarClaves: ["turnos"],
      }),

    nuevoContacto: () => setEdicion({ tipo: "contacto", datos: null }),
    editarContacto: (contacto) => setEdicion({ tipo: "contacto", datos: contacto }),

    guardarContacto: async (cuerpo, id) => {
      const ok = await ejecutar(
        () =>
          id
            ? api.actualizar(`/ContactosEmergencia/${id}`, cuerpo)
            : api.crear("/ContactosEmergencia", cuerpo, "No se pudo registrar el contacto."),
        id ? "Contacto actualizado." : "Contacto agregado a tu red de apoyo."
      );

      if (ok) {
        setEdicion(null);
        await recargar(["contactos"]);
      }
      return ok;
    },

    eliminarContacto: (contacto) =>
      confirmarYEliminar({
        titulo: `¿Eliminar a ${contacto.nombre}?`,
        mensaje: "Se quitará de tu red de apoyo.",
        ruta: `/ContactosEmergencia/${contacto.id}`,
        textoExito: "Contacto eliminado.",
        recargarClaves: ["contactos"],
      }),

    subirDocumento: async (personaId, archivo, tramiteCodigo) => {
      const formulario = new FormData();
      formulario.append("archivo", archivo);

      const ok = await ejecutar(
        () =>
          api.subir(
            `/Documentos/subir?personaAsistidaId=${personaId}${tramiteCodigo ? `&tramiteCodigo=${tramiteCodigo}` : ""}`,
            formulario,
            "No se pudo subir el documento."
          ),
        "Documento cargado correctamente."
      );

      if (ok) await recargar(["documentos"]);
      return ok;
    },

    descargarDocumento: (documento) =>
      ejecutar(() =>
        api.descargar(`/Documentos/${documento.id}/descargar`, documento.nombreOriginal)
      ),

    eliminarDocumento: (documento) =>
      confirmarYEliminar({
        titulo: "¿Eliminar este documento?",
        mensaje: `Se eliminará "${documento.nombreOriginal}" de forma permanente.`,
        ruta: `/Documentos/${documento.id}`,
        textoExito: "Documento eliminado.",
        recargarClaves: ["documentos"],
      }),

    nuevoMedicamento: (personaId) => setEdicion({ tipo: "medicamento", datos: null, personaId }),
    editarMedicamento: (medicamento) => setEdicion({ tipo: "medicamento", datos: medicamento }),

    guardarMedicamento: async (cuerpo, id) => {
      const ok = await ejecutar(
        () => (id ? api.actualizar(`/Medicamentos/${id}`, cuerpo) : api.crear("/Medicamentos", cuerpo)),
        id ? "Medicamento actualizado." : "Medicamento agregado."
      );
      if (ok) {
        setEdicion(null);
        await recargar(["medicamentos"]);
      }
      return ok;
    },

    eliminarMedicamento: (medicamento) =>
      confirmarYEliminar({
        titulo: `¿Eliminar ${medicamento.nombre}?`,
        mensaje: "También se borra el registro de tomas. Si lo dejó de tomar, podés marcarlo como suspendido.",
        ruta: `/Medicamentos/${medicamento.id}`,
        textoExito: "Medicamento eliminado.",
        recargarClaves: ["medicamentos"],
      }),

    abrirEquipo: (persona) => setEdicion({ tipo: "equipo", datos: persona }),
    abrirPreferencias: () => setEdicion({ tipo: "preferencias" }),

    elegirPersonaRecomendaciones: (id) => setPersonaElegidaId(Number(id)),
    reintentarRecomendaciones: (id) => olvidarRecomendaciones(id),
    valorarRecurso,
  };

  const cambiarVista = (nuevaVista) => {
    if (vista === "recomendaciones" && nuevaVista !== vista) olvidarValoradas();
    if (nuevaVista !== vista) window.history.pushState(null, "", `#${nuevaVista}`);
    setVista(nuevaVista);
    setMenuMovil(false);
    window.scrollTo({ top: 0, behavior: "smooth" });
  };

  useEffect(() => {
    const alVolver = () => setVista(vistaDeLaDireccion());
    window.addEventListener("popstate", alVolver);
    return () => window.removeEventListener("popstate", alVolver);
  });

  const refHojaMenu = useRef(null);
  useEffect(() => {
    if (!menuMovil) return;
    refHojaMenu.current?.querySelector("button")?.focus();
    const alTeclear = (e) => e.key === "Escape" && setMenuMovil(false);
    document.addEventListener("keydown", alTeclear);
    return () => document.removeEventListener("keydown", alTeclear);
  }, [menuMovil]);

  const turnosOrdenados = useMemo(
    () => [...datos.turnos].sort((a, b) => new Date(a.fechaHora) - new Date(b.fechaHora)),
    [datos.turnos]
  );

  const contexto = {
    ...datos,
    turnosOrdenados,
    cargando,
    hoy,
    nombreUsuario,
    recomendaciones,
    idRecomendaciones,
    acciones,
    cambiarVista,
    sesion,
    esAdministrador,
    plan,
    // artículos y tu bienestar: el plan propio o el del grupo en el que cuida, el mejor (igual que la API)
    miPlanIncluye: (funcion) => Boolean((PLANES[plan] || PLANES.Gratis)[funcion]) || personas.some((p) => planIncluye(p, funcion)),
    nombreGrupo,
    cambiarNombreGrupo: (nuevo) => {
      setNombreGrupo(nuevo);
      guardarSesion({ ...leerSesion(), nombreGrupo: nuevo });
    },
    cambiarPlan: (nuevo) => {
      setPlan(nuevo);
      guardarSesion({ ...leerSesion(), plan: nuevo });
      return recargar(Object.keys(RUTAS));
    },
    recargar,
    avisar,
    ejecutar,
    confirmar: (datosConfirmacion) => setConfirmacion(datosConfirmacion),
  };

  const Vista = VISTAS[vista] || VistaInicio;

  const botonNavegacion = (item, clase) => (
    <button
      key={item.id}
      type="button"
      className={`${clase} ${vista === item.id ? "activo" : ""}`}
      onClick={() => cambiarVista(item.id)}
      aria-current={vista === item.id ? "page" : undefined}
    >
      <Icono nombre={item.icono} tamano={18} />
      <span>{item.texto}</span>
    </button>
  );

  const todosLosItems = secciones.flatMap((s) => s.items);
  const accesosMovil = ACCESOS_MOVIL.map((id) =>
    todosLosItems.find((i) => i.id === id)
  );
  const vistaEnMas = !accesosMovil.some((i) => i.id === vista);
  // quien cuida en el grupo de otro queda cubierto por el plan de ese dueño
  const orden = Object.keys(PLANES);
  const deOtro = personas
    .filter((p) => p.esTitular === false && orden.indexOf(p.plan) > orden.indexOf(plan))
    .sort((a, b) => orden.indexOf(b.plan) - orden.indexOf(a.plan))[0];
  const planVisible = deOtro?.plan ?? plan;
  const rolUsuario = esAdministrador
    ? "Administración"
    : deOtro
      ? `Plan ${PLANES[planVisible].nombre} · ${deOtro.nombreTitular}`
      : `Plan ${PLANES[plan]?.nombre ?? plan}`;

  const pieUsuario = (
    <div className="usuario-sidebar">
      <div className="avatar avatar-usuario">{iniciales(nombreUsuario)}</div>
      <div className="usuario-datos">
        <strong>{nombreUsuario}</strong>
        <span>{rolUsuario}</span>
      </div>
      <button type="button" className="boton-salir" onClick={acciones.abrirPreferencias} title="Preferencias" aria-label="Preferencias">
        <Icono nombre="ajustes" tamano={17} />
      </button>
      <button type="button" className="boton-salir" onClick={cerrarSesion} title="Cerrar sesión" aria-label="Cerrar sesión">
        <Icono nombre="salir" tamano={17} />
      </button>
    </div>
  );

  return (
    <PanelContexto.Provider value={contexto}>
      <div className="layout">
        <aside className="sidebar">
          <Marca subtitulo={nombreGrupo || deOtro?.nombreTitular || undefined} />

          <nav className="nav" aria-label="Secciones">
            {secciones.map((seccion) => (
              <div className="nav-grupo" key={seccion.titulo}>
                <div className="nav-titulo">{seccion.titulo}</div>
                {seccion.items.map((item) => botonNavegacion(item, "nav-item"))}
              </div>
            ))}
          </nav>

          {(
            <div className="rail-emergencia">
              <button
                type="button"
                className={`nav-emergencia ${vista === "emergencia" ? "activo" : ""}`}
                onClick={() => cambiarVista("emergencia")}
                aria-current={vista === "emergencia" ? "page" : undefined}
              >
                <Icono nombre="emergencia" tamano={18} />
                <span>Emergencia</span>
              </button>
              <a className="llamar-same" href="tel:107" aria-label="Llamar al SAME, 107">
                <Icono nombre="telefono" tamano={15} /> 107
              </a>
            </div>
          )}

          {pieUsuario}
        </aside>

        <header className="barra-movil">
          <Marca compacta />
          {(
            <a className="llamar-same" href="tel:107" aria-label="Llamar al SAME, 107">
              <Icono nombre="telefono" tamano={15} /> 107
            </a>
          )}
        </header>

        <main className="principal">
          <div className="vista" key={vista}>
            <Vista />
          </div>
        </main>

        <nav className="nav-movil" aria-label="Secciones">
          {accesosMovil.map((item) => botonNavegacion(item, "nav-movil-item"))}
          {(
            <button
              type="button"
              className={`nav-movil-item ${vistaEnMas ? "activo" : ""}`}
              onClick={() => setMenuMovil(true)}
              aria-haspopup="dialog"
              aria-expanded={menuMovil}
            >
              <Icono nombre="menu" tamano={18} />
              <span>Más</span>
            </button>
          )}
        </nav>

        {menuMovil && (
          <div className="hoja-menu-fondo" onClick={() => setMenuMovil(false)}>
            <div
              className="hoja-menu"
              ref={refHojaMenu}
              role="dialog"
              aria-modal="true"
              aria-label="Todas las secciones"
              onClick={(e) => e.stopPropagation()}
            >
              <div className="hoja-menu-cabecera">
                <strong>Todas las secciones</strong>
                <button type="button" className="boton-icono" onClick={() => setMenuMovil(false)} aria-label="Cerrar">
                  <Icono nombre="cerrar" />
                </button>
              </div>
              <nav className="hoja-menu-grupos">
                {secciones.map((seccion) => (
                  <div className="nav-grupo" key={seccion.titulo}>
                    <div className="nav-titulo">{seccion.titulo}</div>
                    {seccion.items.map((item) => botonNavegacion(item, "nav-item"))}
                  </div>
                ))}
                <div className="nav-grupo">
                  <button
                    type="button"
                    className={`nav-emergencia ${vista === "emergencia" ? "activo" : ""}`}
                    onClick={() => cambiarVista("emergencia")}
                  >
                    <Icono nombre="emergencia" tamano={18} />
                    <span>Emergencia</span>
                  </button>
                </div>
              </nav>
              {pieUsuario}
            </div>
          </div>
        )}
      </div>

      {edicion?.tipo === "persona" && (
        <FormularioPersona
          persona={edicion.datos}
          alGuardar={acciones.guardarPersona}
          alCerrar={() => setEdicion(null)}
        />
      )}

      {edicion?.tipo === "turno" && (
        <FormularioTurno
          turno={edicion.datos}
          fechaSugerida={edicion.fechaSugerida}
          valoresIniciales={edicion.valoresIniciales}
          personas={datos.personas}
          alGuardar={acciones.guardarTurno}
          alCerrar={() => setEdicion(null)}
        />
      )}

      {edicion?.tipo === "contacto" && (
        <FormularioContacto
          contacto={edicion.datos}
          personas={datos.personas}
          alGuardar={acciones.guardarContacto}
          alCerrar={() => setEdicion(null)}
        />
      )}

      {edicion?.tipo === "medicamento" && (
        <FormularioMedicamento
          medicamento={edicion.datos}
          personaSugerida={edicion.personaId}
          personas={datos.personas}
          alGuardar={acciones.guardarMedicamento}
          alCerrar={() => setEdicion(null)}
        />
      )}

      {edicion?.tipo === "equipo" && (
        <ModalEquipo
          persona={edicion.datos}
          alCerrar={cerrarEdicion}
          alCambiar={() => recargar(Object.keys(RUTAS))}
        />
      )}

      {edicion?.tipo === "preferencias" && <ModalPreferencias alCerrar={cerrarEdicion} />}

      <Confirmacion datos={confirmacion} alCerrar={() => setConfirmacion(null)} />
      <Avisos avisos={avisos} alCerrar={cerrarAviso} />
    </PanelContexto.Provider>
  );
}
