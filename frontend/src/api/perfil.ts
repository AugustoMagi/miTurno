import { apiClient } from './client'
import type { ActualizarPerfilInput, CambiarPasswordInput, MiPerfil } from '../types/perfil'

export async function obtenerMiPerfil(): Promise<MiPerfil> {
  const { data } = await apiClient.get<MiPerfil>('/api/perfil')
  return data
}

export async function actualizarMiPerfil(input: ActualizarPerfilInput): Promise<MiPerfil> {
  const { data } = await apiClient.put<MiPerfil>('/api/perfil', input)
  return data
}

export async function cambiarMiPassword(input: CambiarPasswordInput): Promise<void> {
  await apiClient.patch('/api/perfil/password', input)
}
