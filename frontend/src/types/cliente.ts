import type { EstadoReserva } from './negocio'

export interface Cliente {
  id: string
  nombre: string
  email: string
  telefono: string | null
  totalReservas: number
  ultimaReserva: string // yyyy-MM-dd
}

export interface ReservaCliente {
  id: string
  recursoNombre: string
  fecha: string
  horaInicio: string
  horaFin: string
  precioTotal: number
  estado: EstadoReserva
}

export interface HistorialCliente {
  id: string
  nombre: string
  email: string
  telefono: string | null
  reservas: ReservaCliente[]
}
