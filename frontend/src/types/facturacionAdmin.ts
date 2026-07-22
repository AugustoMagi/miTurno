export interface FacturacionPorPlan {
  planId: string
  planNombre: string
  total: number
  cantidadPagos: number
}

export interface FacturacionPorNegocio {
  negocioId: string
  negocioNombre: string
  total: number
  cantidadPagos: number
}

export interface FacturacionPlataforma {
  totalFacturado: number
  cantidadPagos: number
  porPlan: FacturacionPorPlan[]
  porNegocio: FacturacionPorNegocio[]
}
