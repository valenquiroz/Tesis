import { useEffect, useState } from "react";
import Acceso from "./Acceso";
import { borrarSesion, EVENTO_SESION_VENCIDA, leerSesion } from "./api";
import Panel from "./Panel";

function App() {
  const [logueado, setLogueado] = useState(() => !!leerSesion());
  const [avisoAcceso, setAvisoAcceso] = useState("");

  const cerrarSesion = (aviso = "") => {
    borrarSesion();
    setAvisoAcceso(aviso);
    setLogueado(false);
  };

  // api.js dispara este evento con cada 401
  useEffect(() => {
    const alVencer = () => {
      borrarSesion();
      setAvisoAcceso("Tu sesión venció. Volvé a iniciar sesión para continuar.");
      setLogueado(false);
    };

    window.addEventListener(EVENTO_SESION_VENCIDA, alVencer);
    return () => window.removeEventListener(EVENTO_SESION_VENCIDA, alVencer);
  }, []);

  if (logueado) {
    return <Panel cerrarSesion={() => cerrarSesion()} />;
  }

  return (
    <Acceso
      key={avisoAcceso}
      avisoInicial={avisoAcceso}
      alIngresar={() => {
        setAvisoAcceso("");
        setLogueado(true);
      }}
    />
  );
}

export default App;
