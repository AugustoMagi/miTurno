import { apiClient } from './client'
import type { EstadisticasOcupacion } from '../types/estadisticas'

export async function obtenerEstadisticasOcupacion(desde?: string, hasta?: string): Promise<EstadisticasOcupacion> {
  const { data } = await apiClient.get<EstadisticasOcupacion>('/api/estadisticas/ocupacion', {
    params: { desde, hasta },
  })
  return data
}
