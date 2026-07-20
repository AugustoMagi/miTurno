import { apiClient } from './client'
import type { Cliente, HistorialCliente } from '../types/cliente'

export async function listarClientes(): Promise<Cliente[]> {
  const { data } = await apiClient.get<Cliente[]>('/api/clientes')
  return data
}

export async function obtenerHistorialCliente(id: string): Promise<HistorialCliente> {
  const { data } = await apiClient.get<HistorialCliente>(`/api/clientes/${id}`)
  return data
}
