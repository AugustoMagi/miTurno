export const Periodicidad = {
  Mensual: 0,
  Anual: 1,
} as const

export type Periodicidad = (typeof Periodicidad)[keyof typeof Periodicidad]

export interface Plan {
  id: string
  nombre: string
  precio: number
  periodicidad: Periodicidad
  limiteRecursos: number
  limiteReservasPorMes: number
  activo: boolean
  esPlanDePrueba: boolean
}

export interface PlanInput {
  nombre: string
  precio: number
  periodicidad: Periodicidad
  limiteRecursos: number
  limiteReservasPorMes: number
}
