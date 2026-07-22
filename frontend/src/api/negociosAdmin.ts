import { apiClient } from './client'
import type { NegocioAdmin, NegocioDetalleAdmin } from '../types/negocioAdmin'

export async function listarNegocios(): Promise<NegocioAdmin[]> {
  const { data } = await apiClient.get<NegocioAdmin[]>('/api/admin/negocios')
  return data
}

export async function obtenerNegocioDetalle(id: string): Promise<NegocioDetalleAdmin> {
  const { data } = await apiClient.get<NegocioDetalleAdmin>(`/api/admin/negocios/${id}`)
  return data
}

export async function activarNegocio(id: string): Promise<void> {
  await apiClient.patch(`/api/admin/negocios/${id}/activar`)
}

export async function desactivarNegocio(id: string): Promise<void> {
  await apiClient.patch(`/api/admin/negocios/${id}/desactivar`)
}
