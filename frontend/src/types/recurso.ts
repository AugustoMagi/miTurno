export interface Recurso {
  id: string
  negocioId: string
  nombre: string
  tipo: string
  duracionTurnoMinutos: number
  precio: number
  activo: boolean
}

export interface CrearRecursoInput {
  nombre: string
  tipo: string
  duracionTurnoMinutos: number
  precio: number
}

export type ActualizarRecursoInput = CrearRecursoInput
