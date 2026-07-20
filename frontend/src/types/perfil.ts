export interface MiPerfil {
  id: string
  nombre: string
  email: string
  rol: string
}

export interface ActualizarPerfilInput {
  nombre: string
  email: string
}

export interface CambiarPasswordInput {
  passwordActual: string
  passwordNueva: string
}
