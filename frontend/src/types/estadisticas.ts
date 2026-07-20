import type { EstadoReserva } from './negocio'

export interface ReservasPorEstado {
  estado: EstadoReserva
  cantidad: number
}

export interface OcupacionRecurso {
  recursoId: string
  recursoNombre: string
  totalReservas: number
  reservasPorEstado: ReservasPorEstado[]
}

export interface EstadisticasOcupacion {
  ingresosTotales: number
  totalReservas: number
  reservasPorEstado: ReservasPorEstado[]
  ocupacionPorRecurso: OcupacionRecurso[]
}
