import { useMemo, useState } from "react";
import { Boton, BotonIcono, EncabezadoVista, EstadoVacio, Tarjeta } from "../componentes";
import { usePanel } from "../contexto";
import { claveDia, colorPersona, DIAS_SEMANA, formatearHora, MESES, plural, puedeGestionar } from "../utilidades";
import { FilaTurno } from "./comunes";

export default function VistaCalendario() {
  const { personas, turnosOrdenados, hoy, acciones } = usePanel();

  const [mesVisible, setMesVisible] = useState(
    () => new Date(hoy.getFullYear(), hoy.getMonth(), 1)
  );
  const [diaSeleccionado, setDiaSeleccionado] = useState(null);

  const turnosPorDia = useMemo(() => {
    const mapa = {};
    turnosOrdenados.forEach((t) => {
      const clave = claveDia(t.fechaHora);
      (mapa[clave] ||= []).push(t);
    });
    return mapa;
  }, [turnosOrdenados]);

  const anio = mesVisible.getFullYear();
  const mes = mesVisible.getMonth();
  const desfase = (mesVisible.getDay() + 6) % 7; // semana desde el lunes
  const cantidadDias = new Date(anio, mes + 1, 0).getDate();

  const celdas = [
    ...Array.from({ length: desfase }, () => null),
    ...Array.from({ length: cantidadDias }, (_, i) => i + 1),
  ];
  while (celdas.length % 7 !== 0) celdas.push(null);

  const turnosDelMes = turnosOrdenados.filter((t) => {
    const f = new Date(t.fechaHora);
    return f.getFullYear() === anio && f.getMonth() === mes;
  });

  const turnosDelPanel = diaSeleccionado
    ? turnosPorDia[`${anio}-${mes}-${diaSeleccionado}`] || []
    : turnosDelMes;

  const moverMes = (cantidad) => {
    setMesVisible(new Date(anio, mes + cantidad, 1));
    setDiaSeleccionado(null);
  };

  const irAHoy = () => {
    setMesVisible(new Date(hoy.getFullYear(), hoy.getMonth(), 1));
    setDiaSeleccionado(hoy.getDate());
  };

  // si hay un día elegido se propone ese día a las 9
  const nuevoTurno = () => {
    if (!diaSeleccionado) return acciones.nuevoTurno();
    const dosDigitos = (n) => String(n).padStart(2, "0");
    acciones.nuevoTurno(`${anio}-${dosDigitos(mes + 1)}-${dosDigitos(diaSeleccionado)}T09:00`);
  };

  const esMesActual = anio === hoy.getFullYear() && mes === hoy.getMonth();

  return (
    <>
      <EncabezadoVista
        titulo="Calendario"
      >
        {personas.some(puedeGestionar) && (
          <Boton icono="mas" onClick={nuevoTurno}>
            Nuevo turno
          </Boton>
        )}
      </EncabezadoVista>

      {personas.length === 0 && (
        <div className="alerta alerta-info">
          Para agendar turnos primero agregá una persona asistida desde Inicio.
        </div>
      )}

      <div className="rejilla-calendario">
        <Tarjeta className="tarjeta-calendario">
          <div className="calendario-cabecera">
            <h2>
              <span className="capitalizar">{MESES[mes]}</span> {anio}
            </h2>

            <div className="calendario-controles">
              {!esMesActual && (
                <Boton variante="secundario" className="boton-chico" onClick={irAHoy}>
                  Hoy
                </Boton>
              )}
              <BotonIcono icono="izquierda" etiqueta="Mes anterior" onClick={() => moverMes(-1)} />
              <BotonIcono icono="derecha" etiqueta="Mes siguiente" onClick={() => moverMes(1)} />
            </div>
          </div>

          <div className="calendario-semana" aria-hidden="true">
            {DIAS_SEMANA.map((dia) => (
              <span key={dia}>{dia}</span>
            ))}
          </div>

          <div className="calendario-dias">
            {celdas.map((dia, indice) => {
              if (dia === null) {
                return <div className="dia vacio" key={`vacio-${indice}`} />;
              }

              const lista = turnosPorDia[`${anio}-${mes}-${dia}`] || [];
              const esHoy = esMesActual && dia === hoy.getDate();
              const seleccionado = diaSeleccionado === dia;
              const finDeSemana = indice % 7 >= 5;

              return (
                <button
                  type="button"
                  key={dia}
                  className={[
                    "dia",
                    esHoy && "hoy",
                    seleccionado && "seleccionado",
                    lista.length > 0 && "con-turnos",
                    finDeSemana && "fin-de-semana",
                  ]
                    .filter(Boolean)
                    .join(" ")}
                  onClick={() => setDiaSeleccionado(seleccionado ? null : dia)}
                  aria-pressed={seleccionado}
                  aria-label={`${dia} de ${MESES[mes]}${
                    lista.length ? `, ${plural(lista.length, "turno")}` : ""
                  }`}
                >
                  <span className="dia-numero">{dia}</span>

                  <span className="dia-eventos">
                    {lista.slice(0, 2).map((turno) => (
                      <span
                        className="dia-evento"
                        data-color={colorPersona(turno.personaAsistidaId)}
                        key={turno.id}
                        title={`${formatearHora(turno.fechaHora)} hs · ${turno.titulo}`}
                      >
                        {turno.titulo}
                      </span>
                    ))}
                    {lista.length > 2 && (
                      <span className="dia-mas">+{lista.length - 2} más</span>
                    )}
                  </span>

                  {lista.length > 0 && (
                    <span className="dia-puntos" aria-hidden="true">
                      {lista.slice(0, 3).map((t) => (
                        <i key={t.id} />
                      ))}
                    </span>
                  )}
                </button>
              );
            })}
          </div>
        </Tarjeta>

        <Tarjeta
          className="panel-turnos"
          titulo={
            diaSeleccionado
              ? `${diaSeleccionado} de ${MESES[mes]}`
              : "Turnos del mes"
          }
          descripcion={plural(turnosDelPanel.length, "turno")}
          accion={
            diaSeleccionado && (
              <button type="button" className="enlace-accion" onClick={() => setDiaSeleccionado(null)}>
                Ver todo el mes
              </button>
            )
          }
        >
          {turnosDelPanel.length === 0 ? (
            <EstadoVacio
              icono="calendario"
              titulo={diaSeleccionado ? "Día libre" : "Sin turnos este mes"}
              accion={
                personas.length > 0 && (
                  <Boton variante="secundario" icono="mas" onClick={nuevoTurno} disabled={!personas.some(puedeGestionar)}>
                    Agendar turno
                  </Boton>
                )
              }
            >
              {diaSeleccionado ? "No hay nada agendado para este día." : null}
            </EstadoVacio>
          ) : (
            <ul className="lista">
              {turnosDelPanel.map((turno) => (
                <FilaTurno key={turno.id} turno={turno} hoy={hoy} acciones={acciones} />
              ))}
            </ul>
          )}
        </Tarjeta>
      </div>
    </>
  );
}
