import type { Sesion } from '../types/auth'

const STORAGE_KEY = 'miturno.sesion'

export function getSesion(): Sesion | null {
  const raw = localStorage.getItem(STORAGE_KEY)
  if (!raw) return null
  try {
    return JSON.parse(raw) as Sesion
  } catch {
    return null
  }
}

export function setSesion(sesion: Sesion): void {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(sesion))
}

export function clearSesion(): void {
  localStorage.removeItem(STORAGE_KEY)
}
