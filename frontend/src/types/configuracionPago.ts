export const ProveedorPago = {
  MercadoPago: 0,
  Stripe: 1,
} as const

export type ProveedorPago = (typeof ProveedorPago)[keyof typeof ProveedorPago]

export interface ConfiguracionPago {
  id: string
  proveedor: ProveedorPago
  alias: string
  tieneAccessToken: boolean
  activo: boolean
  fechaCreacion: string
}

export interface ConectarConfiguracionPagoInput {
  proveedor: ProveedorPago
  alias: string
  accessToken?: string
}
