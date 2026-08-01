export interface MiNegocio {
  id: string
  nombre: string
  slug: string
  descripcion: string | null
  direccion: string | null
  telefono: string | null
  email: string
  activo: boolean
  anticipacionMinimaHoras: number
}

export interface ActualizarMiNegocioInput {
  nombre: string
  descripcion?: string
  direccion?: string
  telefono?: string
  anticipacionMinimaHoras: number
}
