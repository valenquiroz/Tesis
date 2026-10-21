import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import "./index.css";
import "./App.css";
import App from "./App.jsx";
import { aplicarPreferencias } from "./preferencias";

// antes de pintar, para que no se vea un salto de tamaño
aplicarPreferencias();

createRoot(document.getElementById("root")).render(
  <StrictMode>
    <App />
  </StrictMode>
);
