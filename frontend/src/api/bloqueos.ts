import { apiClient } from './client'
import type { AgregarBloqueoInput, BloqueoFecha } from '../types/bloqueo'

export async function listarBloqueos(recursoId: string): Promise<BloqueoFecha[]> {
  const { data } = await apiClient.get<BloqueoFecha[]>(`/api/recursos/${recursoId}/bloqueos`)
  return data
}

export async function agregarBloqueo(recursoId: string, input: AgregarBloqueoInput): Promise<BloqueoFecha> {
  const { data } = await apiClient.post<BloqueoFecha>(`/api/recursos/${recursoId}/bloqueos`, input)
  return data
}

export async function eliminarBloqueo(recursoId: string, bloqueoId: string): Promise<void> {
  await apiClient.delete(`/api/recursos/${recursoId}/bloqueos/${bloqueoId}`)
}
