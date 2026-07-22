import { apiClient } from './client'
import type { ActualizarMiNegocioInput, MiNegocio } from '../types/miNegocio'

export async function obtenerMiNegocio(): Promise<MiNegocio> {
  const { data } = await apiClient.get<MiNegocio>('/api/mi-negocio')
  return data
}

export async function actualizarMiNegocio(input: ActualizarMiNegocioInput): Promise<MiNegocio> {
  const { data } = await apiClient.put<MiNegocio>('/api/mi-negocio', input)
  return data
}
