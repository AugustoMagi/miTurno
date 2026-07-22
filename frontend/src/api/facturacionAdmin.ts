import { apiClient } from './client'
import type { FacturacionPlataforma } from '../types/facturacionAdmin'

export async function obtenerFacturacion(desde?: string, hasta?: string): Promise<FacturacionPlataforma> {
  const { data } = await apiClient.get<FacturacionPlataforma>('/api/admin/facturacion', {
    params: { desde: desde || undefined, hasta: hasta || undefined },
  })
  return data
}
