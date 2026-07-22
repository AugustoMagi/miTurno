import { apiClient } from './client'
import type { Plan, PlanInput } from '../types/plan'

export async function listarPlanes(): Promise<Plan[]> {
  const { data } = await apiClient.get<Plan[]>('/api/admin/planes')
  return data
}

export async function crearPlan(input: PlanInput): Promise<Plan> {
  const { data } = await apiClient.post<Plan>('/api/admin/planes', input)
  return data
}

export async function actualizarPlan(id: string, input: PlanInput): Promise<Plan> {
  const { data } = await apiClient.put<Plan>(`/api/admin/planes/${id}`, input)
  return data
}

export async function desactivarPlan(id: string): Promise<void> {
  await apiClient.patch(`/api/admin/planes/${id}/desactivar`)
}

export async function marcarPlanDePrueba(id: string): Promise<Plan> {
  const { data } = await apiClient.patch<Plan>(`/api/admin/planes/${id}/marcar-de-prueba`)
  return data
}

export async function desmarcarPlanDePrueba(id: string): Promise<Plan> {
  const { data } = await apiClient.patch<Plan>(`/api/admin/planes/${id}/desmarcar-de-prueba`)
  return data
}

export async function eliminarPlan(id: string): Promise<void> {
  await apiClient.delete(`/api/admin/planes/${id}`)
}
