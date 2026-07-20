import { apiClient } from './client'
import type { LoginAdminInput, SesionAdmin } from '../types/admin'

export async function loginAdmin(input: LoginAdminInput): Promise<SesionAdmin> {
  const { data } = await apiClient.post<SesionAdmin>('/api/admin/auth/login', input)
  return data
}
