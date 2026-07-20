import { apiClient } from './client'
import type { SuscripcionAdmin } from '../types/suscripcionAdmin'

export async function listarSuscripciones(): Promise<SuscripcionAdmin[]> {
  const { data } = await apiClient.get<SuscripcionAdmin[]>('/api/admin/suscripciones')
  return data
}

export async function cambiarPlanSuscripcion(id: string, nuevoPlanId: string): Promise<SuscripcionAdmin> {
  const { data } = await apiClient.patch<SuscripcionAdmin>(`/api/admin/suscripciones/${id}/plan`, {
    nuevoPlanId,
  })
  return data
}

export async function renovarSuscripcion(id: string, nuevoVencimiento: string): Promise<SuscripcionAdmin> {
  const { data } = await apiClient.patch<SuscripcionAdmin>(`/api/admin/suscripciones/${id}/renovar`, {
    nuevoVencimiento,
  })
  return data
}

export async function cancelarSuscripcionAdmin(id: string): Promise<void> {
  await apiClient.patch(`/api/admin/suscripciones/${id}/cancelar`)
}
