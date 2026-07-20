import { apiClient } from './client'
import type { AgregarHorarioInput, HorarioDisponible } from '../types/horario'

export async function listarHorarios(recursoId: string): Promise<HorarioDisponible[]> {
  const { data } = await apiClient.get<HorarioDisponible[]>(`/api/recursos/${recursoId}/horarios`)
  return data
}

export async function agregarHorario(recursoId: string, input: AgregarHorarioInput): Promise<HorarioDisponible> {
  const { data } = await apiClient.post<HorarioDisponible>(`/api/recursos/${recursoId}/horarios`, input)
  return data
}

export async function eliminarHorario(recursoId: string, horarioId: string): Promise<void> {
  await apiClient.delete(`/api/recursos/${recursoId}/horarios/${horarioId}`)
}
