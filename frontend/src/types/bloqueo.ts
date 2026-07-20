import type { EstadoReserva } from './negocio'

export interface ReservaAfectada {
  id: string
  clienteId: string
  clienteNombre: string
  clienteEmail: string
  horaInicio: string
  horaFin: string
  estado: EstadoReserva
}

export interface BloqueoFecha {
  id: string
  recursoId: string
  fecha: string // yyyy-MM-dd
  motivo: string | null
  reservasAfectadas: ReservaAfectada[]
}

export interface AgregarBloqueoInput {
  fecha: string
  motivo?: string
}
