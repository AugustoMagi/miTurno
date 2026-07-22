import { apiClient } from './client'
import type { MiSuscripcion } from '../types/miSuscripcion'

export async function obtenerMiSuscripcion(): Promise<MiSuscripcion> {
  const { data } = await apiClient.get<MiSuscripcion>('/api/suscripcion')
  return data
}

export async function iniciarSuscripcionMercadoPago(): Promise<string> {
  const { data } = await apiClient.post<{ initPoint: string }>('/api/suscripcion/suscribirme')
  return data.initPoint
}

export async function cambiarPlanMiSuscripcion(nuevoPlanId: string): Promise<MiSuscripcion> {
  const { data } = await apiClient.patch<MiSuscripcion>('/api/suscripcion/plan', { nuevoPlanId })
  return data
}

export async function cancelarMiSuscripcion(): Promise<void> {
  await apiClient.patch('/api/suscripcion/cancelar')
}
