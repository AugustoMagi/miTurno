// Espejo de los DTOs públicos de MiTurno.Application.Features.Public.Dtos.
// El orden de EstadoReserva debe coincidir con MiTurno.Domain.Enums.EstadoReserva:
// el backend no serializa el enum como string, así que el índice importa.
export const EstadoReserva = {
  Pendiente: 0,
  Confirmada: 1,
  Cancelada: 2,
  Completada: 3,
} as const

export type EstadoReserva = (typeof EstadoReserva)[keyof typeof EstadoReserva]

export interface RecursoPublico {
  id: string
  nombre: string
  tipo: string
  duracionTurnoMinutos: number
  precio: number
}

export interface NegocioPublico {
  id: string
  nombre: string
  slug: string
  descripcion: string | null
  direccion: string | null
  recursos: RecursoPublico[]
}

// TimeSpan del backend serializa como "HH:mm:ss".
export interface TurnoDisponible {
  horaInicio: string
  horaFin: string
}

export interface CrearReservaInput {
  fecha: string // yyyy-MM-dd
  horaInicio: string // HH:mm:ss
  clienteNombre: string
  clienteEmail: string
  clienteTelefono?: string
}

export interface Reserva {
  id: string
  recursoId: string
  clienteId: string
  fecha: string
  horaInicio: string
  horaFin: string
  precioTotal: number
  estado: EstadoReserva
  linkPago: string | null
  aliasPago: string | null
}
