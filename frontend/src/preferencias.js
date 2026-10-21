// Tamaño de letra y contraste: preferencias de cada navegador, no de la cuenta.
const CLAVE = "preferencias";

export const TAMANOS = [
  { valor: "normal", texto: "Normal" },
  { valor: "grande", texto: "Grande" },
  { valor: "muy-grande", texto: "Muy grande" },
];

export function leerPreferencias() {
  try {
    return { tamano: "normal", contraste: false, ...JSON.parse(localStorage.getItem(CLAVE) || "{}") };
  } catch {
    return { tamano: "normal", contraste: false };
  }
}

export function aplicarPreferencias(preferencias = leerPreferencias()) {
  const raiz = document.documentElement;
  raiz.dataset.tamano = preferencias.tamano;
  raiz.classList.toggle("alto-contraste", !!preferencias.contraste);
}

export function guardarPreferencias(preferencias) {
  try {
    localStorage.setItem(CLAVE, JSON.stringify(preferencias));
  } catch {
    // en modo privado puede fallar: igual se aplican en esta sesión
  }
  aplicarPreferencias(preferencias);
}
