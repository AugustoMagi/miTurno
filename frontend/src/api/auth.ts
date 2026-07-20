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
