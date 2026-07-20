import type { Periodicidad } from './plan'
import type { EstadoSuscripcion } from './suscripcionAdmin'

export interface MiSuscripcion {
  id: string
  planId: string
  planNombre: string
  planPrecio: number
  periodicidad: Periodicidad
  estado: EstadoSuscripcion
  fechaProximoVencimiento: string
  estaActiva: boolean
}

export interface PagoSuscripcion {
  id: string
  monto: number
  estado: number
  linkPago: string | null
}
