import { apiClient } from './client'
import type { ConectarConfiguracionPagoInput, ConfiguracionPago } from '../types/configuracionPago'

export async function obtenerConfiguracionPago(): Promise<ConfiguracionPago> {
  const { data } = await apiClient.get<ConfiguracionPago>('/api/configuracion-pago')
  return data
}

export async function conectarConfiguracionPago(input: ConectarConfiguracionPagoInput): Promise<ConfiguracionPago> {
  const { data } = await apiClient.post<ConfiguracionPago>('/api/configuracion-pago', input)
  return data
}

export async function desconectarConfiguracionPago(): Promise<void> {
  await apiClient.delete('/api/configuracion-pago')
}
