import { apiClient } from './client'
import type { ActualizarRecursoInput, CrearRecursoInput, Recurso } from '../types/recurso'

export async function listarRecursos(): Promise<Recurso[]> {
  const { data } = await apiClient.get<Recurso[]>('/api/recursos')
  return data
}

export async function obtenerRecurso(id: string): Promise<Recurso> {
  const { data } = await apiClient.get<Recurso>(`/api/recursos/${id}`)
  return data
}

export async function crearRecurso(input: CrearRecursoInput): Promise<Recurso> {
  const { data } = await apiClient.post<Recurso>('/api/recursos', input)
  return data
}

export async function actualizarRecurso(id: string, input: ActualizarRecursoInput): Promise<Recurso> {
  const { data } = await apiClient.put<Recurso>(`/api/recursos/${id}`, input)
  return data
}

export async function activarRecurso(id: string): Promise<void> {
  await apiClient.patch(`/api/recursos/${id}/activar`)
}

export async function desactivarRecurso(id: string): Promise<void> {
  await apiClient.patch(`/api/recursos/${id}/desactivar`)
}
