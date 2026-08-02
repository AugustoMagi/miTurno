import { apiClient } from './client'
import type { MiSuscripcion } from '../types/miSuscripcion'

export async function obtenerMiSuscripcion(): Promise<MiSuscripcion> {
  const { data } = await apiClient.get<MiSuscripcion>('/api/suscripcion')
  return data
}

export async function elegirPlan(planId: string): Promise<MiSuscripcion> {
  const { data } = await apiClient.post<MiSuscripcion>('/api/suscripcion/elegir-plan', { planId })
  return data
}

export async function iniciarSuscripcionMercadoPago(cobrarInmediato = false): Promise<string> {
  const { data } = await apiClient.post<{ initPoint: string }>('/api/suscripcion/suscribirme', null, {
    params: { cobrarInmediato },
  })
  return data.initPoint
}

export async function cambiarPlanMiSuscripcion(nuevoPlanId: string): Promise<MiSuscripcion> {
  const { data } = await apiClient.patch<MiSuscripcion>('/api/suscripcion/plan', { nuevoPlanId })
  return data
}

export async function cancelarMiSuscripcion(): Promise<void> {
  await apiClient.patch('/api/suscripcion/cancelar')
}

export async function reanudarCobroAutomatico(): Promise<void> {
  await apiClient.patch('/api/suscripcion/reanudar-cobro-automatico')
}
