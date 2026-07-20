import { createContext, useContext, useMemo, useState, type ReactNode } from 'react'
import type { Sesion } from '../types/auth'
import { clearSesion, getSesion, setSesion } from '../auth/session'

interface AuthContextValue {
  sesion: Sesion | null
  login: (sesion: Sesion) => void
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [sesion, setSesionState] = useState<Sesion | null>(() => getSesion())

  const value = useMemo<AuthContextValue>(
    () => ({
      sesion,
      login: (nuevaSesion) => {
        setSesion(nuevaSesion)
        setSesionState(nuevaSesion)
      },
      logout: () => {
        clearSesion()
        setSesionState(null)
      },
    }),
    [sesion],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth debe usarse dentro de <AuthProvider>')
  return context
}
