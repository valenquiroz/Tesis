# Acompañar (carpeta `cuida`): contexto completo del proyecto

> Archivo de traspaso para cualquier IA (Claude Code, un chat nuevo u otra herramienta). Leelo entero antes de tocar nada.
> Última actualización: 2026-10-12 (Comunidad, Especialistas y administración de usuarios con backend).
>
> Si algo no está en el código, no lo inventes: dejalo marcado como `[NO CONFIRMADO]`.

---

## 1. Qué es el proyecto

Plataforma web para **cuidadores informales de personas con diversidad funcional en Argentina**. Es el Proyecto Final de Ingeniería en Informática de Val (USAL, docente Esteban Tissera). La desarrolla ella sola.

**Qué resuelve:** el cuidador organiza en un solo lugar la información de la persona que cuida (datos, turnos, documentos, contactos de emergencia) y recibe recursos recomendados según el perfil de esa persona (tipo de discapacidad, obra social, localidad y necesidades).

**Contexto argentino:** PAMI, CUD, ANDIS, SAME/107, Ley 25.326 (protección de datos personales).

**Diferencial técnico:** el motor de recomendaciones. Es lo que evita que parezca "solo un sistema de gestión" y hay que posicionarlo así en el documento.

**Alcance del MVP (para no pasarse):**
- El recomendador arranca **solo content-based** (según el perfil). Evoluciona a híbrido (content-based + colaborativo) cuando haya interacciones. No hay umbral definido para ese cambio.
- **No** integra SAME, PAMI ni ANDIS reales. No existe dataset ni API pública de profesionales o centros de salud: esos datos son **sintéticos (seed)**.
- No hay app móvil nativa: el celular se resuelve con diseño responsive (barra superior y navegación inferior).

---

## 2. Stack tecnológico

| Capa | Tecnología | Estado |
|---|---|---|
| IDE | Visual Studio | confirmado |
| Frontend | React 19 + Vite 8, JavaScript (JSX), sin librerías de UI, lint con oxlint | confirmado |
| Backend | ASP.NET Core Web API (C#), **net10.0** (SDK 10.0.4xx) | confirmado |
| Acceso a datos | Entity Framework Core 10, code-first con migraciones | confirmado |
| Base de datos | SQL Server Express local (`DESKTOP-1OQVG0V\SQLEXPRESS`, base `CuidaDb`) | confirmado |
| Autenticación | ASP.NET Core Identity + JWT (Bearer), bloqueo por intentos fallidos | confirmado |
| Docs de API | Swagger / Swashbuckle (solo en Development) | confirmado |
| Hosting previsto | Azure: App Service, Azure SQL, Blob Storage, Static Web Apps | **solo Azure SQL (TDE) está escrito en el Cap. 4**; el resto hay que reconfirmarlo |

> Ojo: en una etapa se asumió un stack equivocado (FastAPI/Python, PostgreSQL/Supabase, Next.js, React Native, Vercel, Render). **No asumir el stack: confirmarlo con Val.**

---

## 3. Estructura del repositorio

```
cuida/
├── .gitignore                    ← raíz: ignora bin/, obj/, node_modules/, dist/, .env y Archivos/
├── docs/
│   └── CLAUDE.md                 ← este archivo
├── backend/Cuida.Api/
│   ├── Program.cs                ← Identity, JWT, CORS, validación, Swagger, seed
│   ├── appsettings.json          ← SIN secretos (la clave JWT va en user-secrets)
│   ├── Cuida.Api.http            ← pruebas rápidas desde Visual Studio
│   ├── Controllers/              ← Auth, PersonasAsistidas, Turnos, Documentos,
│   │                                ContactosEmergencia, Recomendaciones, RecursosRecomendables
│   ├── Data/                     ← ApplicationDbContext, DbInitializer (seed)
│   ├── Models/                   ← entidades + DTOs de entrada
│   ├── Services/                 ← RecomendacionService, AlmacenamientoDocumentos,
│   │                                IdentityErroresEnEspanol
│   ├── Migrations/               ← 11 migraciones
│   └── Archivos/Documentos/      ← archivos subidos (NO van al repositorio)
└── frontend/
    ├── index.html                ← carga las fuentes, título "Acompañar"
    ├── .env.example              ← VITE_API_URL
    └── src/
        ├── main.jsx              ← importa index.css y App.css
        ├── App.jsx               ← decide entre Acceso y Panel
        ├── Acceso.jsx            ← login y registro
        ├── Panel.jsx             ← layout, estado, carga de datos y acciones
        ├── api.js                ← fetch, token, errores, sesión vencida
        ├── contexto.js           ← contexto del panel (usePanel)
        ├── componentes.jsx       ← Icono, Logo, Marca, Tarjeta, Campo, Boton, Modal, Avisos...
        ├── formularios.jsx       ← modales de persona, turno y contacto (alta y edición)
        ├── utilidades.js         ← constantes, catálogos y formato de fechas/tamaños
        ├── index.css             ← tokens de diseño y estilos base
        ├── App.css               ← estilos de componentes, por secciones numeradas
        └── vistas/               ← Inicio, Recomendaciones, Calendario, Documentos,
                                     Comunidad, Especialistas, Emergencia, comunes.jsx
```

No hay `docs/gestion/{BACKLOG,BITACORA,DECISIONES}.md` en la carpeta actual `[NO CONFIRMADO si existen en otro lado]`. Se hizo `git init` en esta sesión; el historial anterior (el zip) no está en esta carpeta.

### Cómo correr

**Todo junto:** doble clic en `iniciar.bat` (raíz del proyecto). Hace esto:
- Cierra un backend que haya quedado abierto.
- Revisa que exista `appsettings.Local.json` e instala `node_modules` si falta.
- Levanta el backend y espera a que responda.
- Levanta el frontend y abre el navegador.

Todo corre en la misma ventana; Ctrl+C apaga los dos. `iniciar.bat sinabrir` hace lo mismo sin abrir el navegador.

**Por separado:**

**Backend** (Visual Studio o consola, en `backend/Cuida.Api`):

```bash
dotnet ef database update            # aplica migraciones pendientes (no necesita la clave JWT)
dotnet run --launch-profile http     # escucha en http://localhost:5282
```

La clave JWT está en `backend/Cuida.Api/appsettings.Local.json`, que está en `.gitignore`. En la máquina de Val ya existe. En otra máquina hay que copiar `appsettings.Local.example.json` como `appsettings.Local.json` y poner una clave aleatoria de al menos 32 caracteres. Si falta, la API no arranca y explica qué hacer. (Se descartó user-secrets porque la clave guardada ahí no era visible desde la terminal de Val.)

Opcional, para tener un usuario **Administrador** (gestiona recursos recomendables): completar `Admin:Email` y `Admin:Password` en el mismo `appsettings.Local.json` y reiniciar el backend.

**Frontend** (en `frontend`): `npm install` y `npm run dev` (Vite, http://localhost:5173). La URL de la API sale de `VITE_API_URL` (ver `.env.example`); si no está definida, usa `http://localhost:5282/api`.

---

## 4. Backend

### Configuración (`Program.cs`)
- **Identity:** email único; bloqueo de 5 minutos tras 5 intentos fallidos; mensajes de error traducidos (`IdentityErroresEnEspanol`). La contraseña usa la política por defecto de Identity: mínimo 6 caracteres, con mayúscula, minúscula, número y símbolo.
- **JWT:** HMAC-SHA256, valida issuer (`Cuida.Api`), audience (`Cuida.Frontend`), vida útil (`Jwt:ExpirationMinutes`, 60) y firma. `Jwt:Key` viene de **user-secrets** y debe tener ≥ 32 bytes.
- **CORS:** política `Frontend`, orígenes en `Cors:OrigenesPermitidos` (por defecto `http://localhost:5173`).
- **Validación:** todo error de modelo responde `400 { message, errors: [..] }` con mensajes en español.
- **Seed (`DbInitializer`)** en cada arranque, idempotente: crea roles (`Cuidador`, `Especialista`, `Administrador`), crea el admin si está configurado y agrega los recursos que falten (por título). También completa `Necesidades` en recursos que no lo tenían. No pisa cambios de un administrador.

### Entidades
| Entidad | Campos principales | Relaciones |
|---|---|---|
| `ApplicationUser` (Identity) | `NombreCompleto` | 1–N `PersonasAsistidas` (cascada) |
| `PersonaAsistida` | `NombreCompleto`, `FechaNacimiento`, `RelacionConCuidador`, `TipoDiscapacidad`, `ObraSocial`, `Localidad`, `Necesidades` (lista separada por comas), `Observaciones`, `CuidadorId` | 1–N Turnos, Documentos, Contactos (cascada) |
| `Turno` | `Titulo`, `FechaHora`, `Lugar`, `Profesional`, `Especialidad`, `Observaciones` | Persona (cascada), Cuidador (restrict) |
| `Documento` | `NombreArchivo` (GUID), `NombreOriginal`, `TipoContenido`, `TamanoBytes`, `RutaArchivo` (no se expone), `FechaCarga` (UTC) | Persona (cascada), Cuidador (restrict) |
| `ContactoEmergencia` | `Nombre`, `Relacion`, `Telefono`, `TipoContacto` | Persona (cascada), Cuidador (restrict) |
| `RecursoRecomendable` | `Titulo`, `Descripcion`, `Categoria`, `TipoDiscapacidad`, `ObraSocial`, `Localidad`, `Necesidades`, `Enlace`, `Activo` | — |
| `Publicacion` | `Texto` (≤2000), `Etiqueta` (tema, opcional), `FechaCreacion` (UTC), `AutorId` | Autor (restrict); 1–N Respuestas y Utiles (cascada) |
| `RespuestaPublicacion` | `Texto` (≤1000), `FechaCreacion`, `AutorId` | Publicación (cascada), Autor (restrict) |
| `PublicacionUtil` | clave `(PublicacionId, UsuarioId)` | una marca "útil" por usuario |
| `Articulo` | `Titulo`, `Resumen`, `Contenido` (texto plano, párrafos separados por línea en blanco), `Etiqueta`, `FechaPublicacion`, `FechaActualizacion`, `AutorId` | Autor (restrict) |

`ApplicationUser` tiene además `Profesion` (solo para especialistas).

Las navegaciones a `Cuidador` llevan `[JsonIgnore]` para no exponer datos del usuario. La entidad de prueba `Prueba` se eliminó.

### Endpoints (todos bajo `/api`)
Todo, salvo login y registro, requiere `Authorization: Bearer <token>`, y los datos se filtran por el cuidador autenticado. Un `401` hace que el frontend cierre sesión.

| Método | Ruta | Rol | Notas |
|---|---|---|---|
| POST | `/Auth/register` | — | `{nombreCompleto, email, password}` → asigna rol Cuidador |
| POST | `/Auth/login` | — | `{email, password}` → `{token, nombreCompleto, email, rol}` |
| GET/POST | `/PersonasAsistidas` | Cuidador | |
| GET/PUT/DELETE | `/PersonasAsistidas/{id}` | Cuidador | El DELETE borra en cascada y elimina los archivos del disco |
| GET/POST | `/Turnos` | Cuidador | GET incluye `personaAsistida` |
| GET/PUT/DELETE | `/Turnos/{id}` | Cuidador | |
| GET | `/Documentos` | Cuidador | Incluye `personaAsistida.nombreCompleto` y `fechaCarga` en UTC con "Z" |
| POST | `/Documentos/subir?personaAsistidaId={id}` | Cuidador | `multipart/form-data`, campo `archivo` |
| GET | `/Documentos/{id}/descargar` | Cuidador | |
| DELETE | `/Documentos/{id}` | Cuidador | |
| GET/POST | `/ContactosEmergencia` | Cuidador | GET incluye `personaAsistida` |
| GET/PUT/DELETE | `/ContactosEmergencia/{id}` | Cuidador | |
| GET | `/Recomendaciones/{personaId}` | Cuidador | Ver sección 4.1 |
| GET | `/RecursosRecomendables[/{id}]` | Cuidador, Administrador | |
| POST/PUT/DELETE | `/RecursosRecomendables[/{id}]` | Administrador | DELETE = baja lógica (`Activo = false`) |
| GET | `/Comunidad/publicaciones?etiqueta=` | Cuidador, Admin | Últimas 100. Autor como "Nombre I.", `esMia`, `puedeEliminar`, contadores, `marcadaUtil` |
| GET | `/Comunidad/etiquetas` | Cuidador, Admin | `{ total, etiquetas: [{etiqueta, cantidad}] }` |
| POST | `/Comunidad/publicaciones` | Cuidador, Admin | `{texto, etiqueta}` |
| DELETE | `/Comunidad/publicaciones/{id}` | autor o Admin | Borra respuestas y marcas en cascada |
| GET/POST | `/Comunidad/publicaciones/{id}/respuestas` | Cuidador, Admin | |
| DELETE | `/Comunidad/respuestas/{id}` | autor o Admin | |
| POST | `/Comunidad/publicaciones/{id}/util` | Cuidador, Admin | Alterna la marca; devuelve `{marcadaUtil, cantidadUtiles}` |
| GET | `/Articulos[/{id}]` | cualquier usuario autenticado | La lista no trae el contenido; trae `minutosLectura` y `puedeEditar` |
| POST | `/Articulos` | Especialista, Admin | |
| PUT/DELETE | `/Articulos/{id}` | autor o Admin | |
| GET | `/Usuarios` | Administrador | Usuarios con sus roles y profesión |
| POST/DELETE | `/Usuarios/{id}/especialista` | Administrador | Asigna (con `{profesion}`) o quita el rol Especialista |

El login devuelve además `roles` (todos) y `profesion`. Un especialista conserva el rol Cuidador. Los cambios de rol se aplican en el próximo inicio de sesión (van dentro del token).

Los `PUT` reciben los mismos DTOs que los `POST` (`PersonaAsistidaRequest`, `CrearTurnoDto`, `CrearContactoEmergenciaDto`).

### Documentos (`AlmacenamientoDocumentos`)
- Se aceptan PDF, JPG y PNG de hasta 10 MB.
- Se valida la extensión **y la firma real de los primeros bytes**: un `.pdf` que no es un PDF se rechaza. El tipo MIME guardado sale de la extensión, no del navegador.
- Se guardan con nombre GUID en `Almacenamiento:CarpetaDocumentos` (por defecto `Archivos/Documentos`). Para Azure se reemplazaría esta clase por Blob Storage.

### 4.1 Motor de recomendaciones (`RecomendacionService`)
Content-based. Para cada recurso activo suma puntos por coincidencia con el perfil de la persona:

| Criterio | Puntos |
|---|---|
| Tipo de discapacidad | 40 |
| Obra social | 30 |
| Localidad | 30 |
| Al menos una necesidad en común | 35 |

- **Puntaje máximo:** 135. Se devuelve `puntuacion`, `porcentaje` (sobre 135), `nivel` (`Alta` ≥ 60, `Media` ≥ 30, `Baja`, `General`) y `motivos`, que explican cada coincidencia.
- **Comparación tolerante:** ignora mayúsculas, tildes, espacios extra y el prefijo "Discapacidad". Tiene sinónimos (motriz/física → motora, mental/cognitiva → intelectual, hipoacusia → auditiva, etc.).
- **Recursos generales:** un recurso sin criterios de perfil (discapacidad, obra social, localidad) que no sumó puntos aparece como "General" con 10 puntos. Si coincide una necesidad, sube a 35.
- **Catálogo de necesidades** (igual en `DbInitializer.cs` y `utilidades.js`): Trámites y certificados, Cobertura de salud, Apoyo económico, Transporte, Rehabilitación, Recursos cerca de casa.

---

## 5. Frontend

### Arquitectura
- `App` decide entre `Acceso` y `Panel` según haya sesión. La sesión se guarda en `localStorage` bajo la clave `sesion`, como `{token, nombreCompleto, email, rol}`.
- `api.js` centraliza los `fetch`. Agrega el token, traduce los errores (formato propio y ProblemDetails) a `ErrorApi` y, ante un `401`, dispara el evento `acompanar:sesion-vencida`: `App` cierra la sesión y muestra el aviso en el login.
- `Panel` tiene el estado (personas, turnos, documentos, contactos, recomendaciones cacheadas por persona) y las acciones. Lo comparte con las vistas por `PanelContexto` (`usePanel()`).
- Las altas y ediciones se hacen en **modales** (`formularios.jsx`). Las bajas usan un **diálogo de confirmación** propio (ya no `window.confirm`). Los resultados se muestran como **avisos** (toasts) abajo a la derecha.

### Vistas
| Vista | Qué muestra | Datos |
|---|---|---|
| Inicio | Fecha grande con resumen en una frase, línea del día (turnos y tomas con tildes rápidos), "Para atender", próximos días, personas a cargo con edición y medidor de "perfil completo", documentos recientes | reales |
| Recomendaciones | Banner con la persona (selector si hay varias) y su perfil, botón "Completar perfil", filtro por categoría, tarjetas con nivel, motivos, % de coincidencia y enlace | reales |
| Calendario | Grilla mensual (lunes a domingo), día de hoy resaltado; al elegir un día, "Nuevo turno" lo propone a las 9:00; panel lateral con edición y baja | reales |
| Documentos | Zona para arrastrar archivos con validación previa, búsqueda, filtro por persona, tabla (tarjetas en celular), descarga y baja | reales |
| Artículos | Escritos de especialistas ya publicados (SciELO, OMS, Ministerio, CAO): autores, fuente, año, resumen propio y "Leer en la fuente"; filtro por tipo de discapacidad | reales |
| Tu plan | Gratis / Equipo / Plus lado a lado, uso actual y cambio de plan (pago simulado) | reales |
| Tu grupo | Para el dueño (familia o clínica): se mira **por persona a cargo** o **por quien ayuda**, lista con buscador (desde 6) a la izquierda y el detalle a la derecha con selector para cambiar rápido (en celular, lista y después detalle con "volver"). El detalle muestra quién la cuida, tomas registradas y sin registrar, y todo lo registrado por día (tomas, diario, documentos) con su autor, filtrable por tipo y por persona o autor. Equipo: últimos 7 días. Plus: 7/14/30 días, cumplimiento de cada cuidador y tomas tarde. Sumar a alguien y cambiar a quiénes acompaña (con buscador y "todas") en modales | reales |
| Administración (solo Administrador) | Alta, edición y baja de artículos y recursos recomendables | reales |
| Emergencia | Bloque SAME 107 (`tel:107`), 911 y 100, red de apoyo con "Llamar", edición y baja | reales |

### Diseño
Sistema visual (rediseño del 2026-10-13). Subtítulo de la marca: "Plataforma de apoyo al cuidador" (a Val no le gustó "Cuaderno de cuidado").
- Tokens en `index.css`:
  - Tinta y papel: `--tinta #1e1c19`, `--fondo #f5f3ee`, `--papel #fff`, `--linea`.
  - Un solo acento, `--sol #f4c430`, para lo importante: "ahora", la sección activa y el foco.
  - Señales: `--bermellon` (emergencia), `--pino`, `--cielo` (modo especialista), `--ciruela`, `--ocre`.
  - Los nombres viejos (`--verde`, `--naranja`, `--crema`…) son alias de la paleta nueva. No agregar colores sueltos: usar tokens.
- Tipografía:
  - Bricolage Grotesque en títulos.
  - Atkinson Hyperlegible Next en texto (diseñada por el Braille Institute para baja visión).
  - Atkinson Hyperlegible Mono para horas, fechas, teléfonos y rótulos (`.mono`, `.rotulo`).
- Forma: radios de 4, 6 y 10 px, líneas finas en lugar de sombras, sin píldoras. El foco es un anillo amarillo con borde de tinta, al estilo GOV.UK.
- Color por persona:
  - `colorPersona(id)` en `utilidades.js` devuelve el atributo `data-color`, que define `--c`, `--c-suave` y `--c-tinta`.
  - Se usa en avatares, tomas y eventos del calendario.
  - Así cada persona se reconoce en toda la app.
- Inicio: línea del día (turnos y tomas por hora, con marcador de "ahora" y tildes rápidos), bloque "Para atender" y próximos días. No hay tarjetas de KPI.
- Medicación: agenda agrupada por momento del día (`momentoDelDia`), como un pastillero.
- Logo: el original (corazón blanco con línea de pulso sobre cuadrado naranja `--color-logo #c8643a`). Val prefirió volver a este. Favicon a juego.
- Login: solo el formulario centrado en una tarjeta, sin panel lateral (Val lo pidió así).
- Responsive:
  - Hasta 1180px el calendario pasa a una columna.
  - Hasta 900px la sidebar se reemplaza por una barra superior con el botón 107 y una navegación inferior: Tu día, Medicación, Calendario, Diario y "Más", que abre una hoja con todas las secciones y se cierra con Escape.
  - Hasta 720px los modales salen desde abajo.
- Accesibilidad:
  - Etiquetas asociadas a los campos y `aria-label` en los botones de ícono.
  - Foco visible; Escape cierra modales y la hoja "Más".
  - Se respeta `prefers-reduced-motion`.
  - Alto contraste redefine los tokens nuevos.
- Convención: clases y nombres en español.

---

## 6. Qué se hizo y cuándo

**Etapas anteriores (resumen):**
- Prototipo HTML interactivo (móvil y desktop). De ahí salieron las capturas para el Cap. 4.
- Documento: Cap. 1 a 5 completos. El Cap. 4 (Marco Tecnológico) se revisó después de que Val corrigió el stack asumido.
- MVP mínimo de punta a punta (hacia el 2026-09-22).
- 2026-10-11 (primera parte): rediseño del `App.jsx` con navegación por vistas.

**Sesión del 2026-10-11 (corrección integral y rediseño), con Claude Code:**

Problemas encontrados y corregidos:
1. `App.css` seguía siendo la hoja vieja: ninguna clase del JSX nuevo tenía estilos, y `index.css` no se importaba. → Se reescribieron `index.css` y `App.css` desde cero y `main.jsx` importa los dos.
2. `GET /Documentos` no devolvía el nombre de la persona (la columna salía vacía). → Ahora incluye `personaAsistida`.
3. El criterio "Necesidades" nunca sumaba: el frontend no lo enviaba y los recursos no lo tenían. → Hay selector de necesidades en el formulario, el seed lo completa y el `PUT` lo guarda.
4. Los `PUT` recibían la entidad entera con `CuidadorId` obligatorio y fallaban con 400. → Usan DTOs.
5. EF había creado una FK duplicada (`ApplicationUserId`) en `PersonasAsistidas`. → Se corrigió con `.WithMany(u => u.PersonasAsistidas)`. La migración `CorregirRelacionesYQuitarPruebas` la elimina, junto con la tabla `Pruebas`.
6. El recomendador comparaba texto exacto. → Normalización y sinónimos; se agregaron `porcentaje` y `nivel`.
7. Seguridad:
   - Clave JWT fuera de `appsettings.json` (user-secrets, clave nueva).
   - Se quitaron los `Console.WriteLine` que mostraban datos personales.
   - Validación real de archivos.
   - Bloqueo por intentos fallidos.
   - `[JsonIgnore]` en `Cuidador` y `RutaArchivo`.
   - Al borrar una persona se borran sus archivos del disco.
8. No existía ningún administrador. → Se crea desde user-secrets si está configurado. Se quitaron los `[Authorize]` duplicados.
9. Errores de Identity en inglés. → Traducidos. Los errores de validación ahora tienen el formato `{message, errors}`.
10. Restos de plantilla eliminados: `Prueba`, `weatherforecast` en `.http`, carpeta `services` vacía, imágenes de Vite, `<title>frontend</title>`. Se agregó un `.gitignore` en la raíz y se hizo `git init`.
11. Advertencias de oxlint: quedaron en 0. `App.jsx` (2100 líneas) se separó en módulos.

Funcionalidades nuevas: edición de personas, turnos y contactos; diálogos de confirmación; avisos; filtro por categoría en recomendaciones; búsqueda y filtro de documentos; perfil completo; navegación móvil.

Verificado en el navegador, contra una copia del backend nuevo en el puerto 5290 y la base real, con una cuenta de prueba que después se borró: login, alta y edición de persona, recomendaciones, turnos en el calendario, subida y rechazo de un archivo falso, descarga, contactos, baja en cascada y vista de celular.

---

**Sesión del 2026-10-12:**
- Login: el pie ahora dice "Hecho en Argentina para quienes cuidan" (se quitó "Proyecto Final de Ingeniería en Informática").
- Comunidad con backend: publicaciones, respuestas, "me resultó útil" y moderación del admin. Los demás usuarios ven solo el nombre y la inicial del apellido.
- Voces especializadas con backend: artículos escritos por usuarios con rol Especialista.
- Administración de usuarios para asignar o quitar el rol Especialista. El admin que se crea por configuración también recibe el rol Cuidador.
- Migración `AgregarComunidadYArticulos` (solo agrega tablas y la columna `Profesion`), ya aplicada.
- La clave JWT pasó de user-secrets a `appsettings.Local.json` (en `.gitignore`), porque la terminal de Val no veía los user-secrets.
- Se agregaron `iniciar.bat` y `DesignTimeDbContextFactory`, para que `dotnet ef` no necesite arrancar la app.
- Verificado con un script contra la API (permisos 403 incluidos) y en el navegador. Los datos de prueba se borraron.

**Sesión del 2026-10-12 (datos de demo):**
- Se agregó el campo `Domicilio` a la persona asistida (migración `AgregarDomicilioPersona`).
- `Data/DatosDemo.cs` carga datos de demostración cuando se arranca con `Demo:Cargar = true` (`appsettings.Local.json` o `--Demo:Cargar=true`). No duplica nada. Carga:
  - 6 personas asistidas con distintas discapacidades, obras sociales, localidades y domicilios, cada una con turnos (fechas relativas a hoy) y contactos.
  - 4 especialistas y 3 cuidadores (`@acompanar.demo`, contraseña aleatoria).
  - 5 artículos y 5 publicaciones con respuestas.
- Se agregaron 9 recursos al seed (visual, auditiva, intelectual, psicosocial y locales sintéticos), para un total de 21.
- Recomendador: un recurso con localidad, obra social o discapacidad específica se **descarta** si la persona no coincide. Antes un recurso de Rosario podía aparecer para alguien de Escobar porque sumaba por necesidad.
- Los argumentos de línea de comandos y las variables de entorno ahora tienen prioridad sobre `appsettings.Local.json`.
- Se borraron las dos personas llamadas "prueba" (una en cada cuenta) y sus 2 archivos.

**Sesión del 2026-10-12 (cuentas de demo y modo especialista):**
- La demo ahora son 8 cuentas de cuidadores con 1 persona a cargo (una con 2) y 4 especialistas que solo tienen el rol Especialista. Correo `nombreapellido@gmail.com` y contraseña `Nombre123!` (`DatosDemo.EmailDe` / `ClaveDe`).
- El detalle de cuentas y contraseña está en `docs/cuentas-demo.md` (en `.gitignore`).
- Se borraron las personas de demo de `valentina.frontend` y las cuentas de demo anteriores.
- Modo especialista (rol Especialista sin rol Cuidador):
  - Sidebar azul pizarra y subtítulo "Panel profesional".
  - Inicio propio (`VistaInicioEspecialista`): sus artículos, preguntas sin respuesta y botón para escribir.
  - Menú solo con Inicio, Artículos y Comunidad. No se piden los datos de cuidador.
- Comunidad: también pueden usarla los especialistas. Sus mensajes muestran nombre completo e insignia con la profesión (`autorProfesion`).
- Artículos: filtro "Mis artículos" (`esMio` en la API).
- Al quitar el rol Especialista se borra también la profesión.

**Sesión del 2026-10-12 (sección Trámites):**
- Nueva sección "Trámites" (cuidadores): hoja de ruta por persona en 4 etapas (consulta del CUD, estudios según el tipo de discapacidad, junta evaluadora y derechos con el CUD).
- El contenido está en código: `Services/GuiaTramites.cs`, armado a partir de las normativas oficiales de certificación (Disp. SNR 1019, 639, 82, 648 y 500 de 2015) y de páginas de argentina.gob.ar, SSSalud, CNRT y ANSES. Cada paso tiene su fuente y su enlace. Revisado en octubre de 2026.
- Filtros: por tipo de discapacidad (Intelectual y Psicosocial usan la misma normativa), líneas de documentación según la edad (`HastaEdad`) y la asignación por hijo solo si la relación empieza con "Hij".
- El avance se guarda en la tabla `TramitesPersona` (migración AgregarTramitesPersona; índice único por persona y código; se borra en cascada con la persona). Estados: Pendiente, EnCurso, Hecho, NoCorresponde.
- API: `GET /api/Tramites/{personaId}` y `PUT /api/Tramites/{personaId}/{codigo}`, solo para el rol Cuidador.
- Desde enero de 2026 la ANDIS es la Secretaría Nacional de Discapacidad (Ministerio de Salud): la guía usa ese nombre.
- La demo carga avances de ejemplo para Rosa, Tomás, Héctor, Lucía y Osvaldo.

**Sesión del 2026-10-12 (todo lo que faltaba):**
- Cuidado compartido: tabla `PersonasCompartidas`. El titular sigue siendo `PersonaAsistida.CuidadorId` (único que borra o comparte). Todos los controladores filtran con `Services/AccesoPersonas.cs` (`PersonasDe`, `IdsPersonasDe`, `PersonaDeAsync`, `PuedeVerAsync`): no volver a filtrar por `CuidadorId` a mano. La API de personas devuelve `esTitular` y `nombreTitular` (NotMapped).
- Medicación: `Medicamentos` + `TomasMedicamentos` (única por medicamento, fecha y horario). `GET /api/Medicamentos/agenda?fecha=` arma las tomas del día; `PUT /api/Medicamentos/{id}/tomas` (estado null deshace).
- Diario: `EntradasDiario`; solo el autor edita o borra. `GET /api/Diario/resumen` alimenta la hoja imprimible para la consulta.
- Bienestar: Índice de Esfuerzo del Cuidador (13 sí/no, corte 7), en `Services/EscalaEsfuerzo.cs`. Se eligió en lugar de Zarit porque Zarit tiene licencia comercial. Líneas de ayuda verificadas: 0800-999-0091 (nacional, 24 h) y 135 / (011) 5275-1135.
- Ficha de emergencia: campos nuevos en `PersonaAsistida` (grupo sanguíneo, alergias, diagnóstico, afiliado, médico, info de emergencia, `VencimientoCud`). Con vencimiento se crea un turno con `Origen = "cud-vencimiento"` 60 días antes.
- Trámites: los documentos pueden llevar `TramiteCodigo`; botón Agendar abre un turno precargado.
- Comunidad: `ReportesComunidad`; el admin los ve en Administración. Desde la respuesta la FK es Restrict: al borrar publicaciones o respuestas se borran antes sus reportes.
- Contraseñas: `/Auth/olvide`, `/Auth/restablecer`, `/Auth/cambiar-password`. Mail por SMTP (`Services/Correo.cs`, sección `Correo` de appsettings.Local.json); sin SMTP, en Development el enlace sale en la consola.
- Accesibilidad: tamaño de letra y alto contraste en `frontend/src/preferencias.js` (localStorage, se aplica en main.jsx antes de pintar).
- Pruebas: `backend/Cuida.Tests` (xUnit + SQLite en memoria, 50 pruebas). Desde `backend`: `dotnet test Cuida.slnx`.
- Demo: nueva cuenta lorenagomez@gmail.com (colaboradora de Elena y Raúl); medicación, diario, fichas y evaluaciones en `Data/DatosDemo.Extras.cs`.

**Sesión del 2026-10-13 (rediseño visual):**
- Se rediseñaron todas las pantallas (ver 5. Diseño), sin cambios de backend ni de funcionalidad.
- Inicio nuevo (línea del día), medicación agrupada por momento, colores por persona, navegación móvil con "Más", login solo con el formulario.
- Verificado en el navegador a 1440px y 375px (sin desborde horizontal en las 11 secciones), con letra muy grande y alto contraste, y en modo especialista. Administración no se pudo ver (no hay contraseña de admin a mano).
- Después, a pedido de Val: login sin panel lateral, logo y subtítulo originales, y se quitaron las notas al pie de Trámites (fuentes y fecha de revisión) y de Tu bienestar (cita del índice). Las fuentes siguen en `GuiaTramites.cs` y `EscalaEsfuerzo.cs`.
- Recursos: cada uno tiene ahora el enlace a la página oficial que describe, con el texto ajustado a lo que dice esa página (revisados uno por uno en octubre de 2026). Los locales dejaron de ser sintéticos: Pilar (Dirección de Inclusión y Centro Pilares de Esperanza), Hospital Santa Lucía (CABA), curso de LSA (Córdoba), Dirección de Educación Especial (La Plata), centros de salud de Rosario y centros de día de Escobar. `DbInitializer` corrige al arrancar los recursos que todavía tienen un enlace viejo (`EnlacesViejos`), renombra por `TitulosAnteriores` y desactiva 3 duplicados de la primera versión; no toca un recurso que un admin haya editado.
- Recomendador híbrido: botones "Me sirvió / No me sirvió" en cada recurso (tabla `ValoracionesRecursos`, una por persona y recurso, migración `AgregarValoracionesRecursos` ya aplicada; `PUT /api/Recomendaciones/{personaId}/valoraciones/{recursoId}` con `{util: true|false|null}`). En `RecomendacionService`: lo propio "me sirvió" suma 30 y "no me sirvió" manda el recurso al final (no desaparece); los votos de otros cuidadores con el mismo tipo de discapacidad suman o restan hasta 20 (se achica con menos de 3 votos) y la tarjeta lo explica ("A N personas que cuidan a alguien con discapacidad X les sirvió"). El % de coincidencia y el nivel siguen siendo solo por perfil. En pantalla no reordena al tocar (para que no salten las tarjetas): el orden nuevo se ve al volver a entrar. 4 pruebas nuevas (54 en total).

**Sesión del 2026-10-19 (correcciones del profesor):**
- Se sacaron **Especialistas** (rol, cuentas de demo, panel y sección) y la **Comunidad** (foros: se maneja información sensible y un consejo de medicación puede perjudicar a otra persona). La migración `ObservadoresInstitucionesYPlanes` borra sus tablas y la columna `Profesion`.
- En su lugar, **Artículos**: escritos ya publicados por especialistas, con enlace a la fuente (tabla `ArticulosRecomendados`, 15 cargados en `Data/ArticulosIniciales.cs`, todos verificados). Los carga el administrador desde Administración.
- **Equipo de cuidado y modo observador** (`Services/AccesoPersonas.cs`, no filtrar a mano):
  - `PersonaAsistida.ResponsableId` (columna `CuidadorId`): la familia o el gerente. `ResponsableCuida = false` → el responsable solo mira.
  - `PersonasCompartidas.Rol`: `Cuidador` (registra) u `Observador` (solo mira).
  - Niveles: ver (todos) · gestionar turnos, medicación, documentos, contactos, trámites (responsable y cuidadores) · registrar tomas y diario (cuidadores y el responsable si cuida) · administrar perfil, ficha y equipo (responsable o gerente).
  - Cada registro guarda quién lo hizo; el diario solo lo edita su autor.
- **Instituciones**: `Organizacion` (límite de pacientes), `ApplicationUser.OrganizacionId` + `RolOrganizacion` (Gerente / Empleado), rol Identity `Gerente`. El gerente es responsable de todos los pacientes de su institución; los empleados se asignan como cuidadores desde el equipo de cada paciente. `InstitucionController` calcula tomas programadas contra registradas por empleado (tarde = más de 60 min). Registro de instituciones desde la pantalla de acceso.
- **Planes** (`Models/Planes.cs` y `PLANES` en `utilidades.js`, mantener iguales): Gratis USD 0 (1 persona, sin equipo, sin documentos ni diario), Familia USD 4 (3 personas, 3 en el equipo, modo observador, 500 MB, diario y resumen del último mes), Plus USD 9 (sin límites, 5 GB, historial de un año), Institución USD 50/mes hasta 30 pacientes (paga la institución; = USD 600/año del Cap. 5). La emergencia, los trámites, los recursos y los artículos son gratis siempre. El pago está simulado (`PUT /api/Cuenta/plan`); no se puede bajar a un plan que no alcanza para lo cargado.
- Demo nueva: Marta Suárez (cuidadora contratada de dos familias), Lorena (observadora), Jorge en modo observador, residencia "Las Glicinas (demo)" con Andrea (gerente), Carla y Diego (empleados). Ver `docs/cuentas-demo.md`.
- Pruebas: `EquipoYPlanesTests` (10 casos nuevos); 60 en total.
- (Lo de instituciones y los planes de esta sesión se reemplazó el 2026-10-20, ver abajo.)

**Sesión del 2026-10-20 (grupo del dueño y tres planes):**
- Val no quería cuentas "familia" e "institución" separadas. Ahora **cualquier cuenta es dueña de su grupo**: carga a sus personas y suma cuidadores u observadores por correo (cada uno con su cuenta), eligiendo a qué personas acompaña cada uno. Una residencia es un dueño más (con plan Plus y `ApplicationUser.NombreGrupo`, que es lo que ven sus cuidadores).
- Se borró `Organizacion`, `OrganizacionId`, `RolOrganizacion` y el rol Identity `Gerente` (el `DbInitializer` le pasa `Cuidador` a quien lo tenía y borra el rol). Migración `GruposYTresPlanes` (aplicada): copia el nombre de la institución al `NombreGrupo` del gerente y pasa Familia → Equipo e Institución → Plus.
- `GrupoController` (`/api/Grupo`, `/Grupo/integrantes` GET/POST/DELETE, `/Grupo/integrantes/{id}` con la actividad, solo Plus) reemplaza a `InstitucionController`. `Services/Grupos.cs` tiene el control de cuántos integrantes permite el plan (lo usan el grupo y el equipo de cada persona). `PUT /api/Cuenta/grupo` cambia el nombre del grupo.
- **Planes** (`Models/Planes.cs` = `PLANES` en `utilidades.js`): Gratis USD 0 (1 persona, nadie más en el grupo; tu día, medicación, calendario, documentos con 100 MB y emergencia), Equipo USD 5 (+ trámites, diario con resumen del último mes, recursos recomendados, tu bienestar, hasta 5 personas y 3 en el grupo, 1 GB), Plus USD 15 (+ artículos, sin límites, 10 GB, historial de un año y ver qué registró cada cuidador). Lo decidió Val: recursos, artículos y bienestar fuera del Gratis y artículos solo en Plus. La emergencia va en todos. Recursos y trámites dependen del plan de la persona; artículos y bienestar, del mejor plan entre el propio y el de los grupos donde cuida (`PlanDelUsuarioAsync`, y `miPlanIncluye` en el panel).
- Trámites: el GET ahora lo ve todo el equipo y el PUT solo quien gestiona (antes estaba al revés).
- Registro: un solo tipo de cuenta. Navegación: "Tu grupo" en "Para vos" para todos.
- Pruebas: 64 (se reemplazaron las de institución por grupo y se sumaron trámites en Gratis y supervisión solo en Plus).
- Después, "Tu grupo" se rehízo para familias y clínicas: endpoints `GET /Grupo/personas`, `GET /Grupo/personas/{id}`, `GET /Grupo/integrantes/{id}` (ahora también en Equipo, sin las métricas de cumplimiento) y `PUT /Grupo/integrantes/{id}` (`{rol, personasIds}` reemplaza a quiénes acompaña). Lo registrado sale de tomas, diario y documentos (`RegistrosAsync`); las tomas programadas, de los horarios de la medicación activa (`TomasProgramadasAsync`). 66 pruebas.
- **Recursos y artículos por tipo de discapacidad** (pedido de Val): cada recurso tiene uno o más tipos ("Motora, Múltiple") y el motor solo recomienda los del tipo de la persona; sin tipo cargado no recomienda nada y pide completar el perfil. Lo general (CUD, pensión, Incluir Salud, transporte) se desactivó en Recursos porque está en la guía de Trámites (`TitulosGenerales` en `DbInitializer`). Ya no existe el nivel "General". Artículos: los del tipo elegido (chips, empezando por los de mis personas) y aparte "Sobre el cuidado" (sin tipo). Administración elige tipos con casillas; un recurso necesita al menos uno.
- Catálogo: 20 recursos activos (nuevos: Sangalli Dupuy de Escobar, Juana Azurduy y Frida Kahlo de Tigre, Hospital Central de San Isidro, Ley 27.447 de trasplante, Biblioteca Argentina para Ciegos) y 20 artículos (nuevos: informe mundial sobre la audición de OPS, esquizofrenia y enfermedades cardiovasculares de OMS, sobrecarga en hemodiálisis de la Rev. Mex. Med. Fam., grupos de apoyo en discapacidad múltiple de la Rev. Peru. Med. Exp.). Todos los enlaces revisados el 2026-10-20 (SciELO Perú bloquea a curl pero abre en el navegador).
- **D10 corregido**: la obra social "Sin obra social" en un recurso lo deja solo para quien no tiene obra social o tiene Incluir Salud. **D11 corregido**: los 9 perfiles de las familias reciben al menos un recurso local y uno de su tipo. Evaluación nueva: 13 personas, 41 recomendaciones, todas explicadas, 20/20 recursos usados; tiempos n=65, mediana 13,6 ms, p95 35,7 ms. 68 pruebas.
- **Mejoras del 2026-10-20 (noche):** (1) Tu día de quien administra sin registrar: cada toma muestra su estado, quién la registró o a cargo de quién está (`PersonaAsistida.Cuidadores`, NotMapped, lo llena `CompletarAccesoAsync` para el dueño); "Para atender" lleva a Tu grupo. (2) Controles que se repiten: `Turno.RepetirCadaMeses` (migración `ControlesQueSeRepiten`, aplicada), opción en el formulario de turno; "Para atender" avisa desde 30 días antes y el aviso abre un turno nuevo con los datos del anterior. (3) Resumen para la consulta: cuántas tomas se dieron de cada medicamento y la lista de las que no se dieron, con quién lo registró. (4) Formulario de persona: el tipo de discapacidad es obligatorio en la interfaz (la API lo sigue aceptando vacío) y se explica para qué sirve el perfil.
- **Revisión del 2026-10-21:** menú reordenado (Hoy · Cuidado diario con Documentos · Orientación: Trámites, Recursos, Artículos · Quienes cuidan: Tu grupo, Tu bienestar · Cuenta: Tu plan); "Recursos para su situación" como título; Tu bienestar ya no manda a la comunidad (no existe) y "compartir el cuidado" lleva a Tu grupo; horario del 135 corregido (8 a 2 h); la pensión no contributiva enlaza a argentina.gob.ar (la página de ANSES no responde). Todos los enlaces del sistema revisados: responden. **Demo al día**: `DatosDemo.RenovarAsync` corre al arrancar si `Demo:Renovar` es true (está en appsettings.Development.json): completa las tomas de la última semana de las personas de las cuentas demo (Diego sigue dejando tomas sin registrar y anotando tarde) y corre el diario de prueba para que lo último quede de ayer.
- **Pendiente para el documento**: rehacer Cap. 5 (modelo de ingresos con Equipo y Plus, sin plan Institución) y actualizar Caps. 4, 6, 7 y 8 (instituciones → grupos; capturas de "Tu grupo" y "Tu plan").

## 7. Pendiente

**Prioridad alta**
- [ ] Reiniciar el backend que esté corriendo (el anterior no tiene los cambios).
- [ ] Hacer el primer commit del repositorio (`USUARIOS.txt` ya está en `.gitignore`: tiene contraseñas).

**Código**
- [ ] El token se guarda en `localStorage`: evaluarlo contra la sección de seguridad del Cap. 4 (riesgo XSS aceptado en el prototipo; la alternativa es una cookie httpOnly).
- [ ] Zona horaria: `Turno.FechaHora` viaja y se guarda como hora local de Argentina, sin zona. Funciona mientras todos usen la misma zona. `Documento.FechaCarga` está en UTC.
- [x] Comunidad y Especialistas con backend (2026-10-12).
- [x] Recomendador híbrido con valoraciones "me sirvió" (2026-10-13). Falta, si se quiere, sumar también las marcas "útil" de la Comunidad.
- [x] Pantalla de administración de recursos y artículos.
- [x] Tests automatizados: `backend/Cuida.Tests` (68 pruebas).

**Producto / documento**
- [ ] Nombre final: la interfaz dice "Acompañar" (`MARCA` en `utilidades.js`); el código y la base todavía se llaman "Cuida".
- [x] El recomendador ya es híbrido (contenido + valoraciones), ver sesión del 2026-10-13.
- [ ] Reconfirmar los servicios de Azure.
- [ ] Cap. 6 (Análisis de Riesgos) `[CONFIRMAR si ya se entregó]`. La cátedra pide: riesgos en formato **causa–evento–efecto**, matriz **probabilidad × impacto de 5×5**, las **cuatro respuestas** (evitar, mitigar, transferir, aceptar), y supuestos y restricciones separados de los riesgos.
- [ ] Cap. 7 el 13/10, Cap. 8 el 20/10, entrega final (documento + prototipo + presentación) el 17/11.

---

## 8. Decisiones tomadas (no reabrir sin hablar con Val)

- Evaluación según los indicadores UX del Cap. 2: comprensión, facilidad de uso, utilidad percibida, satisfacción, intención de reuso, adaptabilidad. **No** precision/recall.
- Recomendador content-based al inicio, híbrido después.
- Web responsive en vez de app nativa.
- Fuentes reales y verificables; nunca inventar referencias. Los enlaces del seed apuntan solo a sitios oficiales (argentina.gob.ar, pami.org.ar).
- Formato de entregables: Word (.docx) para los capítulos, HTML para los prototipos.
- (2026-10-11) Altas y ediciones en modales; bajas siempre con confirmación.
- (2026-10-11) Los turnos se guardan en hora local sin zona; las fechas de carga, en UTC.
- (2026-10-11) Secretos solo en user-secrets o variables de entorno, nunca en el repositorio.

**Marco teórico (Cap. 3):** modelo biopsicosocial; CIF (OMS, 2001); ISO 9241-210 (2019); modelo de proceso de estrés del cuidador de Pearlin et al. (1990). Referencias ya incorporadas: encuestas INDEC, de-Moraes-Ribeiro et al. (2024), Shin et al. (2022), Laranjo et al. (2018), Milne-Ives et al. (2020), Parmar et al. (2022), An et al. (2025), Su & Khoshgoftaar (2009), Pazzani & Billsus (2007). Producto comparable: Caring Village.

---

## 9. Cómo trabajar con Val

- Prefiere prosa natural y fluida, no frases simétricas ni texto que suene a IA.
- Para material técnico o de examen, resúmenes concisos en viñetas.
- Lenguaje simple, sin jerga de consultoría, y evaluaciones honestas de complejidad.
- Le gusta iterar: primeros borradores simples en vez de versiones exhaustivas de entrada.
- Intenta resolver los ejercicios sola antes de ver la solución.
- Cuando se entrega código, pasarlo **completo** (pidió los archivos enteros, no fragmentos).
- Responder en español rioplatense.

---

## 10. Cómo mantener este archivo

Al terminar cada sesión: actualizar la fecha de arriba, agregar lo hecho en la sección 6, tachar o agregar ítems en la 7 y anotar en la 8 cualquier decisión nueva.
