import { createContext, useContext, useMemo, useState, type ReactNode } from 'react'
import type { SesionAdmin } from '../types/admin'
import { clearSesionAdmin, getSesionAdmin, setSesionAdmin } from '../auth/sessionAdmin'

interface AdminAuthContextValue {
  sesion: SesionAdmin | null
  login: (sesion: SesionAdmin) => void
  logout: () => void
}

const AdminAuthContext = createContext<AdminAuthContextValue | undefined>(undefined)

export function AdminAuthProvider({ children }: { children: ReactNode }) {
  const [sesion, setSesionState] = useState<SesionAdmin | null>(() => getSesionAdmin())

  const value = useMemo<AdminAuthContextValue>(
    () => ({
      sesion,
      login: (nuevaSesion) => {
        setSesionAdmin(nuevaSesion)
        setSesionState(nuevaSesion)
      },
      logout: () => {
        clearSesionAdmin()
        setSesionState(null)
      },
    }),
    [sesion],
  )

  return <AdminAuthContext.Provider value={value}>{children}</AdminAuthContext.Provider>
}

export function useAdminAuth(): AdminAuthContextValue {
  const context = useContext(AdminAuthContext)
  if (!context) throw new Error('useAdminAuth debe usarse dentro de <AdminAuthProvider>')
  return context
}
