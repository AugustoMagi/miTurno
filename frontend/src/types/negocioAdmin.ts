import type { EstadoReserva } from './negocio'

export interface NegocioAdmin {
  id: string
  nombre: string
  slug: string
  email: string
  telefono: string | null
  activo: boolean
  fechaCreacion: string
}

export interface HorarioAdmin {
  id: string
  diaSemana: number
  horaInicio: string
  horaFin: string
}

export interface ReservaAdmin {
  id: string
  fecha: string
  horaInicio: string
  horaFin: string
  clienteNombre: string
  clienteEmail: string
  estado: EstadoReserva
  precioTotal: number
}

export interface RecursoDetalleAdmin {
  id: string
  nombre: string
  tipo: string
  duracionTurnoMinutos: number
  precio: number
  activo: boolean
  horarios: HorarioAdmin[]
  reservas: ReservaAdmin[]
}

export interface NegocioDetalleAdmin {
  id: string
  nombre: string
  slug: string
  email: string
  activo: boolean
  recursos: RecursoDetalleAdmin[]
}
