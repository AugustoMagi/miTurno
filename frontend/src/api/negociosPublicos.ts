import { apiClient } from './client'
import type { CrearReservaInput, NegocioPublico, Reserva, TurnoDisponible } from '../types/negocio'

export async function getNegocioPublico(slug: string): Promise<NegocioPublico> {
  const { data } = await apiClient.get<NegocioPublico>(`/api/public/negocios/${slug}`)
  return data
}

export async function getTurnosDisponibles(
  slug: string,
  recursoId: string,
  fecha: string,
): Promise<TurnoDisponible[]> {
  const { data } = await apiClient.get<TurnoDisponible[]>(
    `/api/public/negocios/${slug}/recursos/${recursoId}/turnos`,
    { params: { fecha } },
  )
  return data
}

export async function crearReserva(
  slug: string,
  recursoId: string,
  input: CrearReservaInput,
): Promise<Reserva> {
  const { data } = await apiClient.post<Reserva>(
    `/api/public/negocios/${slug}/recursos/${recursoId}/reservas`,
    input,
  )
  return data
}

export async function cancelarReservaCliente(slug: string, reservaId: string): Promise<void> {
  await apiClient.patch(`/api/public/negocios/${slug}/reservas/${reservaId}/cancelar`)
}
