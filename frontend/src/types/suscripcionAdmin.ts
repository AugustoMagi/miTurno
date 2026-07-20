export const EstadoSuscripcion = {
  EnPrueba: 0,
  Activa: 1,
  Vencida: 2,
  Cancelada: 3,
} as const

export type EstadoSuscripcion = (typeof EstadoSuscripcion)[keyof typeof EstadoSuscripcion]

export interface SuscripcionAdmin {
  id: string
  negocioId: string
  negocioNombre: string
  negocioEmail: string
  planId: string
  planNombre: string
  planPrecio: number
  estado: EstadoSuscripcion
  fechaInicio: string
  fechaProximoVencimiento: string
}
