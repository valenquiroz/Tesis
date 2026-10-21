import { createContext, useContext } from "react";

export const PanelContexto = createContext(null);

export const usePanel = () => useContext(PanelContexto);
