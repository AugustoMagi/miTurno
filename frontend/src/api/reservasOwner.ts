import { apiClient } from './client'
import type { ReservaOwner } from '../types/reservaOwner'

export async function listarReservas(): Promise<ReservaOwner[]> {
  const { data } = await apiClient.get<ReservaOwner[]>('/api/reservas')
  return data
}

export async function cancelarReserva(id: string): Promise<void> {
  await apiClient.patch(`/api/reservas/${id}/cancelar`)
}

export async function confirmarPagoReserva(id: string): Promise<void> {
  await apiClient.patch(`/api/reservas/${id}/pago/confirmar`)
}

export async function rechazarPagoReserva(id: string): Promise<void> {
  await apiClient.patch(`/api/reservas/${id}/pago/rechazar`)
}
