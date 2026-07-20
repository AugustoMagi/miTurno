import type { SesionAdmin } from '../types/admin'

const STORAGE_KEY = 'miturno.sesionAdmin'

export function getSesionAdmin(): SesionAdmin | null {
  const raw = localStorage.getItem(STORAGE_KEY)
  if (!raw) return null
  try {
    return JSON.parse(raw) as SesionAdmin
  } catch {
    return null
  }
}

export function setSesionAdmin(sesion: SesionAdmin): void {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(sesion))
}

export function clearSesionAdmin(): void {
  localStorage.removeItem(STORAGE_KEY)
}
