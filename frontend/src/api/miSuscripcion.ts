import { apiClient } from './client'
import type { MiSuscripcion, PagoSuscripcion } from '../types/miSuscripcion'

export async function obtenerMiSuscripcion(): Promise<MiSuscripcion> {
  const { data } = await apiClient.get<MiSuscripcion>('/api/suscripcion')
  return data
}

export async function pagarMiSuscripcion(): Promise<PagoSuscripcion> {
  const { data } = await apiClient.post<PagoSuscripcion>('/api/suscripcion/pagar')
  return data
}

export async function cambiarPlanMiSuscripcion(nuevoPlanId: string): Promise<MiSuscripcion> {
  const { data } = await apiClient.patch<MiSuscripcion>('/api/suscripcion/plan', { nuevoPlanId })
  return data
}

export async function cancelarMiSuscripcion(): Promise<void> {
  await apiClient.patch('/api/suscripcion/cancelar')
}
