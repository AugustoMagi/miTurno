import type { EstadoReserva } from './negocio'

export interface ReservaOwner {
  id: string
  recursoId: string
  recursoNombre: string
  clienteId: string
  clienteNombre: string
  clienteEmail: string
  fecha: string
  horaInicio: string
  horaFin: string
  precioTotal: number
  estado: EstadoReserva
}
