import { apiClient } from './client'
import type { LoginInput, RegistroInput, Sesion } from '../types/auth'

export async function login(input: LoginInput): Promise<Sesion> {
  const { data } = await apiClient.post<Sesion>('/api/auth/login', input)
  return data
}

export async function registrar(input: RegistroInput): Promise<Sesion> {
  const { data } = await apiClient.post<Sesion>('/api/auth/registro', input)
  return data
}

export async function solicitarReseteoPassword(email: string): Promise<void> {
  await apiClient.post('/api/auth/solicitar-reseteo-password', { email })
}

export async function restablecerPassword(token: string, passwordNueva: string): Promise<void> {
  await apiClient.post('/api/auth/restablecer-password', { token, passwordNueva })
}
