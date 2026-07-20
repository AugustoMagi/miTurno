import { apiClient } from './client'
import type { PlanPublico } from '../types/planPublico'

export async function listarPlanesPublicos(): Promise<PlanPublico[]> {
  const { data } = await apiClient.get<PlanPublico[]>('/api/public/planes')
  return data
}
