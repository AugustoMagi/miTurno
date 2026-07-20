import type { Periodicidad } from './plan'

export interface PlanPublico {
  id: string
  nombre: string
  precio: number
  periodicidad: Periodicidad
  limiteRecursos: number
  limiteReservasPorMes: number
  esPlanDePrueba: boolean
}
