export const MARCA = "Acompañar";
export const SUBTITULO_MARCA = "Plataforma de apoyo al cuidador";

export const MESES = [
  "enero", "febrero", "marzo", "abril", "mayo", "junio",
  "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre",
];
export const MESES_CORTOS = [
  "ene", "feb", "mar", "abr", "may", "jun",
  "jul", "ago", "sep", "oct", "nov", "dic",
];
export const DIAS_SEMANA = ["Lun", "Mar", "Mié", "Jue", "Vie", "Sáb", "Dom"];

// categorías del CUD
export const TIPOS_DISCAPACIDAD = [
  "Motora",
  "Visual",
  "Auditiva",
  "Intelectual",
  "Psicosocial",
  "Visceral",
  "Múltiple",
];

export const OBRAS_SOCIALES_SUGERIDAS = [
  "PAMI",
  "IOMA",
  "Incluir Salud",
  "OSDE",
  "Swiss Medical",
  "Galeno",
  "OSECAC",
  "Sin obra social",
];

export const LOCALIDADES_SUGERIDAS = [
  "Pilar",
  "Ciudad Autónoma de Buenos Aires",
  "La Plata",
  "Escobar",
  "San Isidro",
  "Tigre",
  "Córdoba",
  "Rosario",
  "Mendoza",
];

// iguales a las de DbInitializer.cs
export const NECESIDADES = [
  "Trámites y certificados",
  "Cobertura de salud",
  "Apoyo económico",
  "Transporte",
  "Rehabilitación",
  "Recursos cerca de casa",
];

export const TIPOS_CONTACTO = ["Familiar", "Profesional", "Institución", "Otro"];

export const PERSONA_VACIA = {
  nombreCompleto: "",
  fechaNacimiento: "",
  relacionConCuidador: "",
  tipoDiscapacidad: "",
  obraSocial: "",
  localidad: "",
  domicilio: "",
  necesidades: [],
  observaciones: "",
  grupoSanguineo: "",
  alergias: "",
  diagnostico: "",
  numeroAfiliado: "",
  medicoCabecera: "",
  telefonoMedico: "",
  informacionEmergencia: "",
  vencimientoCud: "",
};

export const GRUPOS_SANGUINEOS = ["0+", "0-", "A+", "A-", "B+", "B-", "AB+", "AB-"];

export const MEDICAMENTO_VACIO = {
  nombre: "",
  dosis: "",
  horarios: ["08:00"],
  indicaciones: "",
  medico: "",
  vencimientoReceta: "",
  activo: true,
  personaAsistidaId: "",
};

// días que faltan hasta una fecha (negativo si ya pasó)
export const diasHasta = (fecha, hoy = new Date()) => {
  if (!fecha) return null;
  const objetivo = new Date(String(fecha).slice(0, 10) + "T00:00:00");
  const base = new Date(hoy.getFullYear(), hoy.getMonth(), hoy.getDate());
  return Math.round((objetivo - base) / 86_400_000);
};

export const fechaISO = (fecha) => {
  const f = new Date(fecha);
  return `${f.getFullYear()}-${String(f.getMonth() + 1).padStart(2, "0")}-${String(f.getDate()).padStart(2, "0")}`;
};

export const TURNO_VACIO = {
  titulo: "",
  fechaHora: "",
  lugar: "",
  profesional: "",
  especialidad: "",
  observaciones: "",
  repetirCadaMeses: "",
  personaAsistidaId: "",
};

// controles que se repiten: estudios, audiometría, laboratorio, renovación de recetas
export const REPETICIONES = [
  { meses: 3, texto: "Cada 3 meses" },
  { meses: 6, texto: "Cada 6 meses" },
  { meses: 12, texto: "Cada año" },
];

export const CONTACTO_VACIO = {
  nombre: "",
  relacion: "",
  telefono: "",
  tipoContacto: "",
  personaAsistidaId: "",
};

export const iniciales = (nombre = "") =>
  nombre
    .trim()
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((parte) => parte.charAt(0).toUpperCase())
    .join("") || "?";

export const primerNombre = (nombre = "") => nombre.trim().split(/\s+/)[0] || "";

export const separarLista = (texto) =>
  (texto || "")
    .split(",")
    .map((t) => t.trim())
    .filter(Boolean);

// "Motora, Múltiple" incluye "múltiple"
export const esDelTipo = (tipos, tipo) =>
  Boolean(tipo) && separarLista(tipos).some((t) => t.toLowerCase() === tipo.toLowerCase());

// "Motora, Múltiple" → "motora y múltiple"
export const nombrarTipos = (tipos) => {
  const lista = separarLista(tipos).map((t) => t.toLowerCase());
  return lista.length <= 1 ? lista.join("") : `${lista.slice(0, -1).join(", ")} y ${lista.at(-1)}`;
};

export const vacioANull = (valor) => {
  const limpio = typeof valor === "string" ? valor.trim() : valor;
  return limpio === "" || limpio === undefined ? null : limpio;
};

export const plural = (cantidad, singular, pluralTexto = `${singular}s`) =>
  `${cantidad} ${cantidad === 1 ? singular : pluralTexto}`;

// los turnos vienen sin zona, se toman como hora local
export const formatearHora = (fecha) =>
  new Date(fecha).toLocaleTimeString("es-AR", {
    hour: "2-digit",
    minute: "2-digit",
    hourCycle: "h23",
  });

export const formatearFechaLarga = (fecha) =>
  new Date(fecha).toLocaleDateString("es-AR", {
    weekday: "long",
    day: "numeric",
    month: "long",
  });

export const formatearFechaCorta = (fecha) =>
  new Date(fecha).toLocaleDateString("es-AR", {
    day: "numeric",
    month: "short",
    year: "numeric",
  });

export const partesFecha = (fecha) => {
  const f = new Date(fecha);
  return {
    dia: String(f.getDate()).padStart(2, "0"),
    mes: MESES_CORTOS[f.getMonth()],
  };
};

export const claveDia = (fecha) => {
  const f = new Date(fecha);
  return `${f.getFullYear()}-${f.getMonth()}-${f.getDate()}`;
};

export const mismoDia = (a, b) =>
  a.getFullYear() === b.getFullYear() &&
  a.getMonth() === b.getMonth() &&
  a.getDate() === b.getDate();

export const calcularEdad = (fechaNacimiento, hoy = new Date()) => {
  if (!fechaNacimiento) return null;
  const nacimiento = new Date(fechaNacimiento);
  let edad = hoy.getFullYear() - nacimiento.getFullYear();
  const mes = hoy.getMonth() - nacimiento.getMonth();
  if (mes < 0 || (mes === 0 && hoy.getDate() < nacimiento.getDate())) edad--;
  return edad >= 0 ? edad : null;
};

export const aInputFechaHora = (fecha) => (fecha ? String(fecha).slice(0, 16) : "");

export const aInputFecha = (fecha) => (fecha ? String(fecha).slice(0, 10) : "");

export const saludoDelDia = (hoy) => {
  const hora = hoy.getHours();
  if (hora < 12) return "Buen día";
  if (hora < 20) return "Buenas tardes";
  return "Buenas noches";
};

export const formatearTamano = (bytes = 0) =>
  bytes >= 1073741824
    ? `${+(bytes / 1073741824).toFixed(1)} GB`
    : bytes >= 1048576
    ? `${+(bytes / 1048576).toFixed(1)} MB`
    : `${Math.max(1, Math.round(bytes / 1024))} KB`;

export const extensionArchivo = (nombre = "") => {
  const partes = nombre.split(".");
  return partes.length > 1 ? partes.pop().toUpperCase().slice(0, 4) : "ARCH";
};

export const TAMANO_MAXIMO_ARCHIVO = 10 * 1024 * 1024;
export const EXTENSIONES_PERMITIDAS = ["pdf", "jpg", "jpeg", "png"];

// los campos que usa el recomendador
export const CAMPOS_PERFIL = ["tipoDiscapacidad", "obraSocial", "localidad", "necesidades"];

export const perfilCompleto = (persona) =>
  CAMPOS_PERFIL.filter((campo) => !!persona?.[campo]).length;

export function etiquetaDia(fecha, hoy) {
  const f = new Date(fecha);
  const manana = new Date(hoy);
  manana.setDate(hoy.getDate() + 1);

  if (mismoDia(f, hoy)) return "Hoy";
  if (mismoDia(f, manana)) return "Mañana";
  return f.toLocaleDateString("es-AR", { weekday: "long" });
}

export function haceCuanto(fecha, ahora = new Date()) {
  const segundos = Math.max(0, Math.round((ahora - new Date(fecha)) / 1000));
  if (segundos < 60) return "Recién";

  const minutos = Math.round(segundos / 60);
  if (minutos < 60) return `Hace ${plural(minutos, "minuto")}`;

  const horas = Math.round(minutos / 60);
  if (horas < 24) return `Hace ${plural(horas, "hora")}`;

  const dias = Math.round(horas / 24);
  if (dias < 7) return dias === 1 ? "Ayer" : `Hace ${dias} días`;
  if (dias < 30) return `Hace ${plural(Math.round(dias / 7), "semana")}`;

  return formatearFechaCorta(fecha);
}

// "Lic. Mariana Ferreyra" → "Mariana Ferreyra"
export const sinTitulo = (nombre = "") => nombre.replace(/^(Lic\.|Dra?\.|Prof\.)\s*/i, "");

// Cada persona cuidada tiene un color propio que la acompaña en toda la app
const COLORES_PERSONA = ["sol", "pino", "cielo", "bermellon", "ciruela", "ocre"];

export const colorPersona = (id) => COLORES_PERSONA[Math.abs(Number(id) || 0) % COLORES_PERSONA.length];

// Mañana, mediodía, tarde o noche, para agrupar horarios como en un pastillero
export const momentoDelDia = (horario) => {
  const hora = Number(String(horario).slice(0, 2));
  if (hora < 12) return "Mañana";
  if (hora < 15) return "Mediodía";
  if (hora < 20) return "Tarde";
  return "Noche";
};

// ---------------------------------------------------------------
// Grupo de cuidado y planes (el control real lo hace la API)
// ---------------------------------------------------------------

// gestionar: turnos, medicación, documentos, contactos, trámites
export const puedeGestionar = (persona) => Boolean(persona) && persona.puedeGestionar !== false;
// registrar: tomas y diario (el responsable en modo observador no registra)
export const puedeRegistrar = (persona) => Boolean(persona) && persona.puedeRegistrar !== false;
// administrar: perfil, ficha, equipo
export const puedeAdministrar = (persona) => Boolean(persona) && persona.puedeAdministrar !== false;

// mismo contenido que Models/Planes.cs
export const PLANES = {
  Gratis: { nombre: "Gratis", precio: 0, tramites: false, diario: false, recursos: false, articulos: false, bienestar: false, grupo: false, supervision: false, informe: false },
  Equipo: { nombre: "Equipo", precio: 5, tramites: true, diario: true, recursos: true, articulos: false, bienestar: true, grupo: true, supervision: false, informe: false },
  Plus: { nombre: "Plus", precio: 15, tramites: true, diario: true, recursos: true, articulos: true, bienestar: true, grupo: true, supervision: true, informe: true },
};

// el plan más barato que incluye una función, para los avisos
export const planQueIncluye = (funcion) => Object.values(PLANES).find((p) => p[funcion])?.nombre;

// quién puede estar en el grupo de una persona además de su responsable
export const ROLES_EQUIPO = {
  Cuidador: { texto: "Cuidador/a", ayuda: "Registra tomas, diario, turnos y documentos." },
  Observador: { texto: "Observador/a", ayuda: "Ve todo lo que se registra, sin poder cambiarlo." },
};

// ¿el plan que cubre a esta persona incluye la función?
export const planIncluye = (persona, funcion) => Boolean((PLANES[persona?.plan] || PLANES.Gratis)[funcion]);
